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
        private int currentUserId
        {
            get { return Convert.ToInt32(Session["CurrentUserId"]); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx");
                }
                else
                {
                    ResolveCurrentShift();   // 👈 ADD THIS
                    LoadOldestPendingQC();
                }

            }
        }

        private void LoadOldestPendingQC()
        {
            int qcInspectorId = GetOldestPendingQCInspectorId();

            if (qcInspectorId == 0)
            {
                ShowNoPendingMessage();
                return;
            }

            Session["QCInspectorID"] = qcInspectorId;

            LoadQCContext(qcInspectorId);
            BindIntervals(qcInspectorId);
        }

        private int GetOldestPendingQCInspectorId()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 q.ID
                FROM TRN_qcinspector q
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM TRN_qc_packet_weight_hdr p
                    WHERE p.QCInspectorID = q.ID
                )
                ORDER BY q.SubmittedDate, q.SubmittedTime", con))
            {
                con.Open();
                object result = cmd.ExecuteScalar();
                return result == null ? 0 : Convert.ToInt32(result);
            }
        }

        private void LoadQCContext(int qcInspectorId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
                    SELECT  
                q.PlantName,
                q.Line,
                q.SKUId,
                q.ProductBrand,
                q.SubmittedDate,
                q.SubmittedTime,
                q.Shift,
                s.SKU_name AS StandardWeightText
            FROM TRN_qcinspector q
            INNER JOIN MST_Brand_SKU s
                ON q.SKUId = s.SKUId
            WHERE q.ID = @ID", con))
            {
                cmd.Parameters.AddWithValue("@ID", qcInspectorId);

                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        lblPlant.Text = dr["PlantName"].ToString();
                        lblLine.Text = dr["Line"].ToString();
                        lblSKU.Text = dr["SKUId"].ToString();
                        lblVariety.Text = dr["ProductBrand"].ToString();
                        lblShift.Text = dr["Shift"].ToString();

                        DateTime qcSubmitted = Convert.ToDateTime(dr["SubmittedDate"]).Add((TimeSpan)dr["SubmittedTime"]);
                        lblQCSubmittedAt.Text = qcSubmitted.ToString("dd-MMM-yyyy HH:mm");

                        string stdWeightText = dr["StandardWeightText"].ToString();

                        if (!decimal.TryParse(stdWeightText, out standardWeight))
                            throw new Exception(
                                $"Invalid standard weight configured for SKU {lblSKU.Text}.");
                        hdnStdWeight.Value = standardWeight.ToString("0.00");
                        hdnQCInspectorID.Value = qcInspectorId.ToString();
                        // Optional UX visibility
                        //lblStdWeight.Text = standardWeight.ToString("0.00") + " g";
                    }

                }
            }
        }

        private void BindIntervals_OLD(int qcInspectorId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("GetPacketWeightIntervalsByQCInspectorId", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@QCInspectorID", qcInspectorId);
                cmd.Parameters.AddWithValue("@ShiftStartTime", shiftStartTime);
                cmd.Parameters.AddWithValue("@ShiftEndTime", shiftEndTime);

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

                LockUI();
                ShowSuccess();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
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
                cmd.Parameters.AddWithValue("@SubmittedById", currentUserId);

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

            TimeSpan shiftStartTime = (TimeSpan)Session["SHIFT_START"];
            TimeSpan shiftEndTime = (TimeSpan)Session["SHIFT_END"];

            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "GetPacketWeightIntervalsByQCInspectorId", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@QCInspectorID", qcInspectorId);
                cmd.Parameters.AddWithValue("@ShiftStartTime", shiftStartTime);
                cmd.Parameters.AddWithValue("@ShiftEndTime", shiftEndTime);

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