using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using AnmolDristi.DAL; // Assuming ShiftManager is here
using static AnmolDristi.DAL.ShiftManager;

namespace AnmolDristi
{
    public partial class qaqc_packet_weight : System.Web.UI.Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // --- Helper Property: Get Logged-in User ID Safely ---
        private int CurrentUserId
        {
            get
            {
                if (Session["CurrentUserId"] == null)
                {
                    Response.Redirect("login.aspx", true);
                }
                int userId;
                if (!int.TryParse(Session["CurrentUserId"].ToString(), out userId))
                {
                    throw new InvalidOperationException("Invalid user session value.");
                }
                return userId;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Validate Session on every load
            if (Session["CurrentUserId"] == null)
            {
                Response.Redirect("login.aspx", true);
                return;
            }

            if (!IsPostBack)
            {
                // 2. Resolve Shift Context (Display purposes)
                ResolveCurrentShift();

                // 3. Load the specific QC Record (Logic updated for URL/Stateless)
                LoadTargetQCRecord();
            }
        }

        /* --------------------------------------------------------------------------
           LOGIC: Determine WHICH record to load
           Priority 1: QueryString (Stateless/Tab-Safe)
           Priority 2: Fallback to DB (Safety Net)
           -------------------------------------------------------------------------- */
        private void LoadTargetQCRecord()
        {
            int targetQcId = 0;

            // 1. Check QueryString (Preferred)
            if (!string.IsNullOrEmpty(Request.QueryString["qcid"]))
            {
                int requestedId;
                if (int.TryParse(Request.QueryString["qcid"], out requestedId))
                {
                    // SECURITY CHECK: Does this ID belong to the logged-in User?
                    if (ValidateRecordOwnership(requestedId))
                    {
                        targetQcId = requestedId;
                    }
                    else
                    {
                        // ID exists, but user doesn't own it (Security Risk or Session Timeout)
                        // Send them back to the dashboard
                        Response.Redirect("qaqc_inspector_rpt.aspx", true);
                        return;
                    }
                }
            }

            // 2. Fallback: If URL is empty, find the OLDEST pending record for this user
            if (targetQcId == 0)
            {
                targetQcId = GetOldestPendingQCInspectorId();
            }

            // 3. Final Decision
            if (targetQcId > 0)
            {
                // Store in HiddenField for the Submit button to use later
                hdnQCInspectorID.Value = targetQcId.ToString();
                Session["CurrentQCId"] = targetQcId; // Optional: Keep for legacy consistency

                LoadQCContext(targetQcId);
                BindIntervals(targetQcId);
            }
            else
            {
                // No work found -> Go back to Dashboard
                Response.Redirect("qaqc_inspector_rpt.aspx", true);
            }
        }

        /* --------------------------------------------------------------------------
           SECURITY: Ensure User Owns the Record
           -------------------------------------------------------------------------- */
        private bool ValidateRecordOwnership(int qcId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT 1 
                FROM TRN_qcinspector 
                WHERE ID = @ID 
                  AND SubmittedById = @UserId 
                  AND QC_Status = 'STEP2_PENDING'", con))
            {
                cmd.Parameters.AddWithValue("@ID", qcId);
                cmd.Parameters.AddWithValue("@UserId", CurrentUserId);

                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        /* --------------------------------------------------------------------------
           FALLBACK: Find oldest pending record if URL is missing
           -------------------------------------------------------------------------- */
        private int GetOldestPendingQCInspectorId()
        {
            // Context from Login Session
            string currentPlant = Session["PLANTID"] as string;
            string currentLine = Session["CurrentLine"] as string;

            if (string.IsNullOrEmpty(currentPlant) || string.IsNullOrEmpty(currentLine))
                return 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.GetOldestPendingQCInspectorId", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = CurrentUserId;
                cmd.Parameters.Add("@PlantName", SqlDbType.NVarChar, 100).Value = currentPlant;
                cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = currentLine;

                con.Open();
                object result = cmd.ExecuteScalar();

                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            }
        }

        /* --------------------------------------------------------------------------
           UI BINDING: Load Header Info
           -------------------------------------------------------------------------- */
        private void LoadQCContext(int qcInspectorId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
            SELECT  
                q.PlantName, q.Line, q.SKUId, q.ProductCategory, q.ProductBrand, 
                q.SubmittedDate, q.SubmittedTime, q.Shift, 
                s.SKU_name AS StandardWeightText
            FROM TRN_qcinspector q
            INNER JOIN MST_Brand_SKU s ON q.SKUId = s.SKUId
            WHERE q.ID = @ID", con))
            {
                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = qcInspectorId;
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        // Using your existing helper methods to resolve names
                        lblPlant.Text = GetPlantName(Convert.ToInt32(dr["PlantName"])) ?? dr["PlantName"].ToString();
                        lblLine.Text = GetLineName(Convert.ToInt32(dr["PlantName"]), Convert.ToInt32(dr["Line"])) ?? dr["Line"].ToString();

                        lbl_qcid.Text = qcInspectorId.ToString();
                        lblSKU.Text = GetSKUName(dr["SKUId"].ToString()) ?? dr["SKUId"].ToString();

                        lblVariety.Text = GetBrandName(
                            Convert.ToInt32(dr["PlantName"]),
                            Convert.ToInt32(dr["Line"]),
                            Convert.ToInt32(dr["ProductCategory"]),
                            Convert.ToInt32(dr["ProductBrand"])
                        ) ?? dr["ProductBrand"].ToString();

                        lblShift.Text = dr["Shift"].ToString();

                        DateTime qcSubmitted = Convert.ToDateTime(dr["SubmittedDate"]).Add((TimeSpan)dr["SubmittedTime"]);
                        lblQCSubmittedAt.Text = qcSubmitted.ToString("dd-MMM-yyyy HH:mm");

                        decimal standardWeight;
                        if (decimal.TryParse(dr["StandardWeightText"].ToString(), out standardWeight))
                        {
                            hdnStdWeight.Value = standardWeight.ToString("0.00");
                        }
                    }
                }
            }
        }

        /* --------------------------------------------------------------------------
           UI BINDING: Generate Intervals (The 30-min slots)
           -------------------------------------------------------------------------- */
        private void BindIntervals(int qcInspectorId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.GetPacketWeightIntervalsByQCInspectorId", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@QCInspectorID", SqlDbType.Int).Value = qcInspectorId;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            rptIntervals.DataSource = dt;
            rptIntervals.DataBind();

            if (dt.Rows.Count > 0)
            {
                lblWindow.Text = $"{dt.Rows[0]["IntervalStartTime"]} – {dt.Rows[dt.Rows.Count - 1]["IntervalEndTime"]}";
            }
            else
            {
                lblWindow.Text = "No pending intervals required.";
            }
        }

        /* --------------------------------------------------------------------------
           SUBMISSION LOGIC
           -------------------------------------------------------------------------- */
        protected void btnSubmitPacket_Click(object sender, EventArgs e)
        {
            try
            {
                // Ensure we have the QC ID from the hidden field
                int qcInspectorId;
                if (!int.TryParse(hdnQCInspectorID.Value, out qcInspectorId))
                {
                    lblError.Text = "Session lost. Please reload the page.";
                    return;
                }

                // 1. Build Data Table
                DataTable packetDetails = BuildPacketDetailTable();
                if (packetDetails.Rows.Count == 0)
                {
                    lblError.Text = "No packet data entered.";
                    return;
                }

                // 2. Calculate Summary (Updates UI labels)
                CalculateSummary(packetDetails);

                // 3. Insert Data into DB
                InsertPacketWeight(qcInspectorId, packetDetails);

                // 4. Update Header Status
                UpdateQcStatus(qcInspectorId, "STEP2_COMPLETED");

                // 5. Cleanup & Redirect
                Session.Remove("CurrentQCId"); // Clean up legacy session

                // Return to Dashboard to start next task
                Response.Redirect("qaqc_inspector_rpt.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                lblError.Text = "Error: " + ex.Message;
            }
        }

        private DataTable BuildPacketDetailTable()
        {
            // MUST MATCH SQL TYPE: [dbo].[PacketWeightDetailType]
            DataTable dt = new DataTable();
            dt.Columns.Add("IntervalStart", typeof(TimeSpan));
            dt.Columns.Add("IntervalEnd", typeof(TimeSpan));
            dt.Columns.Add("PacketNo", typeof(int));
            dt.Columns.Add("PacketWeight", typeof(decimal));

            foreach (RepeaterItem item in rptIntervals.Items)
            {
                HiddenField hfStart = item.FindControl("hfStart") as HiddenField;
                HiddenField hfEnd = item.FindControl("hfEnd") as HiddenField;

                if (hfStart == null || hfEnd == null) continue;

                TimeSpan start = TimeSpan.Parse(hfStart.Value);
                TimeSpan end = TimeSpan.Parse(hfEnd.Value);

                for (int i = 1; i <= 5; i++)
                {
                    TextBox txt = item.FindControl($"txtPacket{i}") as TextBox;

                    // Simple validation: Ensure text exists
                    if (txt != null && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        decimal weight;
                        if (decimal.TryParse(txt.Text.Trim(), out weight))
                        {
                            dt.Rows.Add(start, end, i, weight);
                        }
                        else
                        {
                            throw new Exception($"Invalid weight format at {start}-{end}, Packet {i}");
                        }
                    }
                    else
                    {
                        // Throw error if fields are empty (Assuming mandatory)
                        throw new Exception($"Missing weight at {start}-{end}, Packet {i}");
                    }
                }
            }
            return dt;
        }

        private void InsertPacketWeight(int qcId, DataTable packetDetails)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.InsertPacketWeightData_Final", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                decimal stdWeight = 0;
                decimal.TryParse(hdnStdWeight.Value, out stdWeight);

                cmd.Parameters.AddWithValue("@QCInspectorID", qcId);
                cmd.Parameters.AddWithValue("@StandardWeight", stdWeight);
                cmd.Parameters.AddWithValue("@Shift", lblShift.Text);
                cmd.Parameters.AddWithValue("@SubmittedById", CurrentUserId);

                // Pass Table Valued Parameter
                SqlParameter tvp = cmd.Parameters.AddWithValue("@PacketDetails", packetDetails);
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "dbo.PacketWeightDetailType"; // Must match SQL Type Name

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateQcStatus(int qcId, string status)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("UPDATE TRN_qcinspector SET QC_Status = @Status WHERE ID = @ID", con))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@ID", qcId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void ResolveCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            ShiftInfo shift = shiftManager.GetCurrentShiftInfo();
            lblShift.Text = shift.ShiftName;

            Session["SHIFT_NAME"] = shift.ShiftName;
            Session["SHIFT_START"] = shift.StartTime;
            Session["SHIFT_END"] = shift.EndTime;
        }

        private void CalculateSummary(DataTable packetDetails)
        {
            var weights = packetDetails.AsEnumerable()
                           .Select(r => r.Field<decimal>("PacketWeight"))
                           .ToList();

            if (weights.Count == 0) return;

            decimal stdWeight;
            if (!decimal.TryParse(hdnStdWeight.Value, out stdWeight) || stdWeight <= 0)
            {
                // Safety check
                return;
            }

            decimal min = weights.Min();
            decimal max = weights.Max();
            decimal averageWeight = weights.Average();
            decimal giveAwayPercent = ((averageWeight - stdWeight) / stdWeight) * 100;

            lblMinWeight.Text = min.ToString("0.00");
            lblMaxWeight.Text = max.ToString("0.00");
            lblAverage.Text = averageWeight.ToString("0.00");
            lblGiveAway.Text = giveAwayPercent.ToString("0.00") + " %";
        }

        // --- MASTER LOOKUP HELPERS (Unchanged as requested) ---

        private string GetMasterDescription(string cachePrefix, string sql, Action<SqlParameterCollection> bindParams, int cacheSeconds = 300)
        {
            string cacheKey = cachePrefix;
            var cached = HttpRuntime.Cache[cacheKey] as string;
            if (!string.IsNullOrEmpty(cached)) return cached;

            try
            {
                using (var con = new SqlConnection(connectionString))
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
                System.Diagnostics.Trace.TraceWarning("MasterLookup", ex.ToString());
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
    }
}