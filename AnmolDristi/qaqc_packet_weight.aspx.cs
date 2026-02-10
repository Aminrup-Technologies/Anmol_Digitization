using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.DAL;
using static AnmolDristi.DAL.ShiftManager;
using System.Configuration;

namespace AnmolDristi
{
    public partial class qaqc_packet_weight : System.Web.UI.Page
    {
        private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // These should ideally come from a Shift Master table
        private readonly TimeSpan shiftStartTime;
        private readonly TimeSpan shiftEndTime;

        // Standard weight should be SKU-driven; hardcoded ONLY if unavoidable
        private decimal standardWeight = 000.00M;

        // Logged-in user
        private int CurrentUserId
        {
            get
            {
                if (Session["CurrentUserId"] == null)
                    throw new InvalidOperationException("User session expired.");

                int userId;
                if (!int.TryParse(Session["CurrentUserId"].ToString(), out userId))
                    throw new InvalidOperationException("Invalid user session value.");

                return userId;
            }
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            // 1️⃣ Validate login session (single contract)
            ValidateSession(); // checks Session["CurrentUserId"]

            // 2️⃣ Enforce Step-2 access (cannot be skipped)
            if (!IsStep2Allowed())
            {
                Response.Redirect("qaqc_inspector_rpt.aspx", true);
                return;
            }

            // 3️⃣ First-time page load only
            if (!IsPostBack)
            {
                ResolveCurrentShift();   // sets shift & window context
                LoadOldestPendingQC();   // loads QCInspectorID & context
            }
        }

        private void ValidateSession()
        {
            if (Session["CurrentUserId"] == null)
            {
                Response.Redirect("login.aspx", true);
            }
        }

        private bool IsStep2Allowed()
        {
            int userId = Convert.ToInt32(Session["CurrentUserId"]);

            // If we have a target ID, verify ownership of THAT specific record
            if (Session["PendingQC_ID"] != null)
            {
                int targetId = Convert.ToInt32(Session["PendingQC_ID"]);
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(@"
            SELECT 1 FROM TRN_qcinspector 
            WHERE ID = @ID AND SubmittedById = @UserId AND QC_Status = 'STEP2_PENDING'", con))
                {
                    cmd.Parameters.AddWithValue("@ID", targetId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    con.Open();
                    return cmd.ExecuteScalar() != null;
                }
            }

            // Fallback: Just check if they have ANY pending work allowed
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT 1 FROM TRN_qcinspector 
        WHERE SubmittedById = @UserId AND QC_Status = 'STEP2_PENDING'", con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        //private bool IsStep2Allowed()
        //{
        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    using (SqlCommand cmd = new SqlCommand(@"
        //    SELECT 1
        //    FROM TRN_qcinspector
        //    WHERE SubmittedById = @UserId
        //      AND QC_Status = 'STEP2_PENDING'", con))
        //    {
        //        cmd.Parameters.Add("@UserId", SqlDbType.Int)
        //                      .Value = Convert.ToInt32(Session["CurrentUserId"]);

        //        con.Open();
        //        return cmd.ExecuteScalar() != null;
        //    }
        //}


        //private void LoadOldestPendingQC()
        //{
        //    int qcInspectorId = GetOldestPendingQCInspectorId();

        //    if (qcInspectorId == 0)
        //    {
        //        ShowNoPendingMessage();
        //        return;
        //    }

        //    Session["CurrentQCId"] = qcInspectorId;

        //    LoadQCContext(qcInspectorId);
        //    BindIntervals(qcInspectorId);
        //}
        //Commented on 10-Feb-2026

        private void LoadOldestPendingQC()
        {
            int qcInspectorId = 0;

            // 1️⃣ PRIORITY: Check if the previous page passed a specific ID
            if (Session["PendingQC_ID"] != null)
            {
                qcInspectorId = Convert.ToInt32(Session["PendingQC_ID"]);
            }

            // 2️⃣ FALLBACK: If no specific ID found (e.g. direct URL access), search DB
            if (qcInspectorId == 0)
            {
                qcInspectorId = GetOldestPendingQCInspectorId();
            }

            // 3️⃣ FINAL CHECK: If still 0, user has no pending work.
            if (qcInspectorId == 0)
            {
                // Redirect back to report/dashboard because they shouldn't be here
                Response.Redirect("qaqc_inspector_rpt.aspx", true);
                return;
            }

            // 4️⃣ Load Data
            Session["CurrentQCId"] = qcInspectorId;
            LoadQCContext(qcInspectorId);
            BindIntervals(qcInspectorId);
        }

        private int GetOldestPendingQCInspectorId()
        {
            // Ensure these Session keys match your Login logic exactly!
            int currentUserId = Convert.ToInt32(Session["CurrentUserId"]);
            string currentPlant = Session["PLANTID"] as string;
            string currentLine = Session["CurrentLine"] as string;

            if (string.IsNullOrEmpty(currentPlant) || string.IsNullOrEmpty(currentLine))
            {
                return 0;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.GetPendingQCStep2Id_ByLine", con)) // Use the SP we created earlier
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = currentUserId;
                cmd.Parameters.Add("@PlantName", SqlDbType.NVarChar, 100).Value = currentPlant;
                cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = currentLine;

                con.Open();
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                return 0;
            }
        }

        //private int GetOldestPendingQCInspectorId()
        //{
        //    // 1. Get Context from Session (assuming user selected Plant/Line on login)
        //    int currentUserId = Convert.ToInt32(Session["CurrentUserId"]);
        //    string currentPlant = Session["PLANTID"] as string;
        //    string currentLine = Session["CurrentLine"] as string;

        //    // Safety: If context is missing, we can't safely fetch a pending record
        //    if (string.IsNullOrEmpty(currentPlant) || string.IsNullOrEmpty(currentLine))
        //    {
        //        return 0;
        //    }

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    using (SqlCommand cmd = new SqlCommand("dbo.GetOldestPendingQCInspectorId", con))
        //    {
        //        cmd.CommandType = CommandType.StoredProcedure;

        //        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = currentUserId;
        //        cmd.Parameters.Add("@PlantName", SqlDbType.NVarChar, 100).Value = currentPlant;
        //        cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = currentLine;

        //        con.Open();
        //        object result = cmd.ExecuteScalar();

        //        if (result != null && result != DBNull.Value)
        //        {
        //            return Convert.ToInt32(result);
        //        }
        //        return 0;
        //    }
        //}

        //private int GetOldestPendingQCInspectorId()
        //{
        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    using (SqlCommand cmd = new SqlCommand(@"
        //SELECT TOP 1 ID
        //FROM TRN_qcinspector
        //WHERE SubmittedById = @UserId
        //  AND QC_Status = 'STEP2_PENDING'
        //ORDER BY SubmittedDate, SubmittedTime", con))
        //    {
        //        cmd.Parameters.Add("@UserId", SqlDbType.Int)
        //                      .Value = Convert.ToInt32(Session["CurrentUserId"]);

        //        con.Open();
        //        object result = cmd.ExecuteScalar();
        //        return result == null ? 0 : Convert.ToInt32(result);
        //    }
        //}
        private void LoadQCContext(int qcInspectorId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT  
                q.PlantName,
                q.Line,
                q.SKUId,
                q.ProductCategory,
                q.ProductBrand,
                q.SubmittedDate,
                q.SubmittedTime,
                q.Shift,
                s.SKU_name AS StandardWeightText
            FROM TRN_qcinspector q
            INNER JOIN MST_Brand_SKU s ON q.SKUId = s.SKUId
            WHERE q.ID = @ID
            AND q.QC_Status = 'STEP2_PENDING'", con))
            {
                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = qcInspectorId;

                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new Exception("Invalid or completed QC record.");

                    //lblPlant.Text = dr["PlantName"].ToString();
                    lblPlant.Text = GetPlantName(Convert.ToInt32(dr["PlantName"])) ?? dr["PlantName"].ToString();

                    //lblLine.Text = dr["Line"].ToString();
                    lblLine.Text = GetLineName(
                        Convert.ToInt32(dr["PlantName"]),
                        Convert.ToInt32(dr["Line"])
                    ) ?? dr["Line"].ToString();

                    lbl_qcid.Text = qcInspectorId.ToString();

                    //lblSKU.Text = dr["SKUId"].ToString();
                    lblSKU.Text = GetSKUName(dr["SKUId"].ToString()) ?? dr["SKUId"].ToString();

                    //lblVariety.Text = dr["ProductBrand"].ToString();
                    lblVariety.Text = GetBrandName(
                        Convert.ToInt32(dr["PlantName"]),
                        Convert.ToInt32(dr["Line"]),
                        Convert.ToInt32(dr["ProductCategory"]),
                        Convert.ToInt32(dr["ProductBrand"])
                    ) ?? dr["ProductBrand"].ToString();

                    lblShift.Text = dr["Shift"].ToString();

                    DateTime qcSubmitted =
                        Convert.ToDateTime(dr["SubmittedDate"])
                        .Add((TimeSpan)dr["SubmittedTime"]);

                    lblQCSubmittedAt.Text = qcSubmitted.ToString("dd-MMM-yyyy HH:mm");

                    if (!decimal.TryParse(dr["StandardWeightText"].ToString(), out standardWeight))
                        throw new Exception($"Invalid standard weight for SKU {lblSKU.Text}");

                    hdnStdWeight.Value = standardWeight.ToString("0.00");
                    hdnQCInspectorID.Value = qcInspectorId.ToString();
                }
            }
        }

        private string GetMasterDescription(string cachePrefix, string sql, Action<SqlParameterCollection> bindParams, int cacheSeconds = 300)
        {
            string cacheKey = cachePrefix;
            var cached = HttpRuntime.Cache[cacheKey] as string;
            if (!string.IsNullOrEmpty(cached)) return cached;

            try
            {
                using (var con = new SqlConnection(
                       ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    bindParams(cmd.Parameters);
                    con.Open();

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string value = Convert.ToString(result);
                        HttpRuntime.Cache.Insert(
                            cacheKey,
                            value,
                            null,
                            DateTime.Now.AddSeconds(cacheSeconds),
                            System.Web.Caching.Cache.NoSlidingExpiration);

                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Warn("MasterLookup", ex.ToString());
            }

            return null;
        }

        private string GetPlantName(int plantId)
        {
            return GetMasterDescription(
                $"PLANT::{plantId}",
                @"SELECT TOP 1 plant_name + ' (' + sap_code + ')'
          FROM MST_PlantDetails
          WHERE plant_id = @plant_id",
                p => p.AddWithValue("@plant_id", plantId)
            );
        }
        private string GetLineName(int plantId, int lineId)
        {
            return GetMasterDescription(
                $"LINE::{plantId}::{lineId}",
                @"SELECT TOP 1 line_name
          FROM MST_Plant_Lines
          WHERE plant_id = @plant_id AND line_id = @line_id",
                p =>
                {
                    p.AddWithValue("@plant_id", plantId);
                    p.AddWithValue("@line_id", lineId);
                }
            );
        }
        private string GetCategoryName(int plantId, int lineId, int categoryId)
        {
            return GetMasterDescription(
                $"CAT::{plantId}::{lineId}::{categoryId}",
                @"SELECT TOP 1 category_name
          FROM MST_LineCategory
          WHERE plant_id=@plant AND line_id=@line AND category_id=@cat",
                p =>
                {
                    p.AddWithValue("@plant", plantId);
                    p.AddWithValue("@line", lineId);
                    p.AddWithValue("@cat", categoryId);
                }
            );
        }
        private string GetBrandName(int plantId, int lineId, int categoryId, int brandId)
        {
            return GetMasterDescription(
                $"BRAND::{plantId}::{lineId}::{categoryId}::{brandId}",
                @"SELECT TOP 1 brand_name
          FROM MST_LineCatBrands
          WHERE plant_id=@p AND line_id=@l AND category_id=@c AND brand_id=@b",
                p =>
                {
                    p.AddWithValue("@p", plantId);
                    p.AddWithValue("@l", lineId);
                    p.AddWithValue("@c", categoryId);
                    p.AddWithValue("@b", brandId);
                }
            );
        }
        private string GetSKUName(string skuId)
        {
            return GetMasterDescription(
                $"SKU::{skuId}",
                @"SELECT TOP 1 SKU_name
          FROM MST_Brand_SKU
          WHERE SKUId=@sku",
                p => p.AddWithValue("@sku", skuId)
            );
        }


        protected void btnSubmitPacket_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable packetDetails = BuildPacketDetailTable();

                if (packetDetails.Rows.Count == 0)
                {
                    lblError.Text = "No packet data found.";
                    return;
                }

                CalculateSummary(packetDetails);
                InsertPacketWeight(packetDetails);

                UpdateQcStatus(Convert.ToInt32(Session["CurrentQCId"]), "STEP2_COMPLETED");
                Session.Remove("CurrentQCId");
                Response.Redirect("qaqc_inspector_rpt.aspx", true);
                //LockUI();
                //ShowSuccess();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        private void UpdateQcStatus(int qcId, string status)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
            UPDATE TRN_qcinspector
            SET QC_Status = @Status
            WHERE ID = @QcId", con))
            {
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 30).Value = status;
                cmd.Parameters.Add("@QcId", SqlDbType.Int).Value = qcId;

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }


        private DataTable BuildPacketDetailTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IntervalStart", typeof(TimeSpan));
            dt.Columns.Add("IntervalEnd", typeof(TimeSpan));
            dt.Columns.Add("PacketNo", typeof(int));
            dt.Columns.Add("PacketWeight", typeof(decimal));

            foreach (RepeaterItem item in rptIntervals.Items)
            {
                HiddenField hfStart = item.FindControl("hfStart") as HiddenField;
                HiddenField hfEnd = item.FindControl("hfEnd") as HiddenField;

                if (hfStart == null || hfEnd == null)
                    throw new Exception("Interval metadata missing. Please reload the page.");

                TimeSpan start = TimeSpan.Parse(hfStart.Value);
                TimeSpan end = TimeSpan.Parse(hfEnd.Value);

                for (int i = 1; i <= 5; i++)
                {
                    TextBox txt = item.FindControl($"txtPacket{i}") as TextBox;

                    if (txt == null)
                        throw new Exception($"Packet {i} input missing for interval {start} – {end}.");

                    string raw = txt.Text.Trim();

                    if (string.IsNullOrWhiteSpace(raw))
                        throw new Exception(
                            $"Packet {i} weight is mandatory for interval {start} – {end}.");

                    if (raw.Contains(","))
                        throw new Exception(
                            $"Enter only ONE value per packet. Interval {start} – {end}, Packet {i}.");

                    decimal weight;
                    if (!decimal.TryParse(raw, out weight))
                        throw new Exception(
                            $"Invalid numeric value at interval {start} – {end}, Packet {i}.");

                    dt.Rows.Add(start, end, i, weight);
                }
            }

            return dt;
        }

        private DataTable BuildPacketDetailTable_OLD()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IntervalStart", typeof(TimeSpan));
            dt.Columns.Add("IntervalEnd", typeof(TimeSpan));
            dt.Columns.Add("PacketNo", typeof(int));
            dt.Columns.Add("PacketWeight", typeof(decimal));

            foreach (RepeaterItem item in rptIntervals.Items)
            {
                TimeSpan start = TimeSpan.Parse(
                    ((HiddenField)item.FindControl("hfStart")).Value);

                TimeSpan end = TimeSpan.Parse(
                    ((HiddenField)item.FindControl("hfEnd")).Value);

                for (int i = 1; i <= 5; i++)
                {
                    TextBox txt = (TextBox)item.FindControl($"txtPacket{i}");

                    if (string.IsNullOrWhiteSpace(txt.Text))
                        throw new Exception("All packet weights are mandatory.");

                    dt.Rows.Add(start, end, i, Convert.ToDecimal(txt.Text));
                }
            }

            return dt;
        }

        private void InsertPacketWeight(DataTable packetDetails)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("InsertPacketWeightData_Final", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                //cmd.Parameters.AddWithValue("@QCInspectorID",(int)Session["QCInspectorID"]);
                //cmd.Parameters.AddWithValue("@StandardWeight", standardWeight);
                // QC Inspector ID
                int qcInspectorId;
                if (!int.TryParse(hdnQCInspectorID.Value, out qcInspectorId) || qcInspectorId <= 0)
                {
                    throw new Exception("QC reference lost. Please reload the page and try again.");
                }

                // Standard Weight
                decimal stdWeight;
                if (!decimal.TryParse(hdnStdWeight.Value, out stdWeight) || stdWeight <= 0)
                {
                    throw new Exception("Standard packet weight not available. Please reload the page.");
                }

                // Bind parameters
                cmd.Parameters.AddWithValue("@QCInspectorID", qcInspectorId);
                cmd.Parameters.AddWithValue("@StandardWeight", stdWeight);

                cmd.Parameters.AddWithValue("@Shift", lblShift.Text);
                cmd.Parameters.AddWithValue("@SubmittedById", CurrentUserId);

                SqlParameter tvp = cmd.Parameters.AddWithValue("@PacketDetails", packetDetails);

                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "dbo.PacketWeightDetailType";

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //private void LockUI_OLD()
        //{
        //    rptIntervals.Enabled = false;
        //    btnSubmitPacket.Enabled = false;
        //}

        private void LockUI()
        {
            foreach (RepeaterItem item in rptIntervals.Items)
            {
                foreach (Control ctrl in item.Controls)
                {
                    DisableControlsRecursively(ctrl);
                }
            }

            btnSubmitPacket.Enabled = false;
        }
        private void DisableControlsRecursively(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox)
                    ((TextBox)c).Enabled = false;
                else if (c is DropDownList)
                    ((DropDownList)c).Enabled = false;
                else if (c is RadioButton)
                    ((RadioButton)c).Enabled = false;
                else if (c is CheckBox)
                    ((CheckBox)c).Enabled = false;

                if (c.HasControls())
                    DisableControlsRecursively(c);
            }
        }


        private void ShowSuccess()
        {
            pnlSuccess.Visible = true;
        }

        private void ShowNoPendingMessage()
        {
            lblError.Text = "No pending Packet Weight entries available.";
            btnSubmitPacket.Enabled = false;
        }

        private void ResolveCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            ShiftInfo shift = shiftManager.GetCurrentShiftInfo();

            // Display
            lblShift.Text = shift.ShiftName;

            // Persist for interval logic
            Session["SHIFT_NAME"] = shift.ShiftName;
            Session["SHIFT_START"] = shift.StartTime;
            Session["SHIFT_END"] = shift.EndTime;
        }

        private void BindIntervals(int qcInspectorId)
        {
            if (Session["SHIFT_START"] == null || Session["SHIFT_END"] == null)
                throw new Exception("Shift information not available.");

            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "GetPacketWeightIntervalsByQCInspectorId", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@QCInspectorID", SqlDbType.Int)
                              .Value = qcInspectorId;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            rptIntervals.DataSource = dt;
            rptIntervals.DataBind();

            if (dt.Rows.Count > 0)
            {
                lblWindow.Text =
                    $"{dt.Rows[0]["IntervalStartTime"]} – {dt.Rows[dt.Rows.Count - 1]["IntervalEndTime"]}";
            }
        }


        private void CalculateSummary(DataTable packetDetails)
        {
            var weights = packetDetails.AsEnumerable()
                                       .Select(r => r.Field<decimal>("PacketWeight"))
                                       .ToList();

            decimal stdWeight;

            if (!decimal.TryParse(hdnStdWeight.Value, out stdWeight) || stdWeight <= 0)
            {
                lblError.Text = "Standard packet weight missing or invalid. Please reload the page.";
                return;
            }


            decimal min = weights.Min();
            decimal max = weights.Max();
            //decimal avg = weights.Average();

            //decimal giveAway =
            //    ((avg - standardWeight) / standardWeight) * 100;

            decimal averageWeight = weights.Average();
            decimal giveAwayPercent = ((averageWeight - stdWeight) / stdWeight) * 100;

            lblMinWeight.Text = min.ToString("0.00");
            lblMaxWeight.Text = max.ToString("0.00");
            lblAverage.Text = averageWeight.ToString("0.00");
            lblGiveAway.Text = giveAwayPercent.ToString("0.00") + " %";
        }

    }
}