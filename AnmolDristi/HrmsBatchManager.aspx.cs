using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Web;
using System.Linq;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace AnmolDristi
{
    public partial class HrmsBatchManager : AnmolDristi.BasePage
    {
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Enforce Security Gateway
            if (Session["UserId"] == null || string.IsNullOrEmpty(Session["UserId"].ToString()))
            {
                // Redirect unauthorized users safely
                Response.Redirect("~/Login.aspx?msg=session_expired", true);
                return;
            }

            if (!IsPostBack)
            {
                LoadBatches();
                LoadOverwriteFields();
            }
        }

        private void LoadBatches()
        {
            lblPageError.Text = string.Empty;
            lblRunMessage.Text = string.Empty;

            // Updated query to aggregate row counts dynamically based on Staging Table
            string query = @"
                SELECT 
                    b.BatchId AS ImportBatchId, 
                    b.FileName AS AnyFile,
                    COUNT(s.StagingId) AS TotalRows,
                    SUM(CASE WHEN s.ImportStatus = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
                    SUM(CASE WHEN s.ImportStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
                    SUM(CASE WHEN s.ImportStatus = 'Pending' THEN 1 ELSE 0 END) AS WaitingCount,
                    b.UploadDate AS LastModifiedIST,
                    b.Status
                FROM dbo.ImportBatchMaster b
                LEFT JOIN dbo.Staging_EmployeeData s ON b.BatchId = s.BatchId
                WHERE 1=1 ";

            DateTime fromDate;
            DateTime toDate;

            // Strict C# 5.0 compatible TryParse logic
            if (DateTime.TryParse(txtFrom.Text, out fromDate))
            {
                query += " AND b.UploadDate >= @FromDate ";
            }

            if (DateTime.TryParse(txtTo.Text, out toDate))
            {
                query += " AND b.UploadDate <= @ToDate ";
            }

            if (!string.IsNullOrEmpty(ddlStatus.SelectedValue))
            {
                query += " AND b.Status = @Status ";
            }

            query += " GROUP BY b.BatchId, b.FileName, b.UploadDate, b.Status ORDER BY b.UploadDate DESC";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (fromDate != DateTime.MinValue)
                        cmd.Parameters.AddWithValue("@FromDate", fromDate);

                    if (toDate != DateTime.MinValue)
                        cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1).AddSeconds(-1));

                    if (!string.IsNullOrEmpty(ddlStatus.SelectedValue))
                        cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);

                    // 2. Wrap Adapter and DataTable in 'using' statements for immediate cleanup
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    using (DataTable dt = new DataTable())
                    {
                        da.Fill(dt);
                        gvBatches.DataSource = dt;
                        gvBatches.DataBind();
                    }
                }
            }
        }

        private void LoadOverwriteFields()
        {
            using (SqlConnection conn =
                new SqlConnection(GetConnectionString()))
            {
                string sql =
                @"SELECT *
          FROM MST_HRMSImportField
          WHERE IsVisible=1
          ORDER BY Category,DisplayOrder";

                SqlDataAdapter da =
                    new SqlDataAdapter(sql, conn);

                DataTable dt =
                    new DataTable();

                da.Fill(dt);

                rptOverwriteFields.DataSource = dt;

                rptOverwriteFields.DataBind();
            }
        }

        protected void rblProcessingMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            divOverwriteFields.Visible =
                rblProcessingMode.SelectedValue == "2";
        }

        protected void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk =
                    item.FindControl("chkField") as CheckBox;

                if (chk != null)
                    chk.Checked = true;
            }
        }

        protected void btnClearAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk =
                    item.FindControl("chkField") as CheckBox;

                if (chk != null)
                    chk.Checked = false;
            }
        }

        private string GetSelectedOverwriteFields()
        {
            List<string> fields = new List<string>();

            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk =
                    item.FindControl("chkField") as CheckBox;

                HiddenField hf =
                    item.FindControl("hfFieldCode") as HiddenField;

                if (chk != null &&
                    hf != null &&
                    chk.Checked)
                {
                    fields.Add(hf.Value.Trim());
                }
            }

            return string.Join("|", fields);
        }

        public class SyncExecutionContext
        {
            public int BatchId { get; set; }

            public bool AllowOverwrite { get; set; }

            public int ProcessingMode { get; set; }

            public string Modules { get; set; }

            public string OverwriteFields { get; set; }

            public string ExecutedBy { get; set; }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            ClearSelection();
            LoadBatches();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtFrom.Text = string.Empty;
            txtTo.Text = string.Empty;
            ddlStatus.SelectedIndex = 0;
            ClearSelection();
            LoadBatches();
        }

        private void ClearSelection()
        {
            hfSelectedBatch.Value = string.Empty;
            lblSelectedBatch.Text = "(none selected)";
            lblRunMessage.Text = string.Empty;
            gvResult.DataSource = null;
            gvResult.DataBind();
        }

        protected void btnRun_Click(object sender, EventArgs e)
        {
            lblPageError.Text = "";
            lblRunMessage.Text = "";

            if (string.IsNullOrWhiteSpace(hfSelectedBatch.Value))
            {
                lblPageError.Text = "Please select a batch first.";
                return;
            }

            int batchId = Convert.ToInt32(hfSelectedBatch.Value);

            int processingMode = 0;
            int.TryParse(rblProcessingMode.SelectedValue, out processingMode);

            bool allowOverwrite = processingMode != 0;

            string modules = GetSelectedModules();

            string overwriteFields = GetSelectedOverwriteFields();

            string executedBy =
                Session["UserId"] == null
                ? ""
                : Session["UserId"].ToString();

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(GetConnectionString()))
                {
                    using (SqlCommand cmd =
                        new SqlCommand("usp_RunHRMSSynchronization", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 300;

                        cmd.Parameters.AddWithValue("@BatchId", batchId);
                        cmd.Parameters.AddWithValue("@AllowOverwrite", allowOverwrite);
                        cmd.Parameters.AddWithValue("@ProcessingMode", processingMode);
                        cmd.Parameters.AddWithValue("@Modules", modules);
                        cmd.Parameters.AddWithValue("@OverwriteFields", overwriteFields);
                        cmd.Parameters.AddWithValue("@ExecutedBy", executedBy);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);

                        DataTable dt = new DataTable();

                        da.Fill(dt);

                        gvResult.DataSource = dt;
                        gvResult.DataBind();

                        lblRunMessage.Text =
                            "Synchronization completed successfully.";
                    }
                }

                LoadBatches();
            }
            catch (Exception ex)
            {
                lblPageError.Text = ex.Message;
            }
        }

        private string GetSelectedModules()
        {
            List<string> modules = new List<string>();

            foreach (ListItem item in chkModules.Items)
            {
                if (item.Selected)
                    modules.Add(item.Value.Trim().ToUpper());
            }

            return string.Join("|", modules);
        }

        protected void btnExportReport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfSelectedBatch.Value))
            {
                lblPageError.Text = "Please select a batch before downloading a report.";
                return;
            }

            int batchId = Convert.ToInt32(hfSelectedBatch.Value);

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string query = "SELECT EmpCode, EmpName, Email, MobileNo, ImportStatus, ErrorMessage FROM dbo.Staging_EmployeeData WHERE BatchId = @BatchId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BatchId", batchId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("EmpCode,EmpName,Email,Mobile,Status,ErrorMessage");

                        while (reader.Read())
                        {
                            string code = reader["EmpCode"] != DBNull.Value ? reader["EmpCode"].ToString() : "";
                            string name = reader["EmpName"] != DBNull.Value ? reader["EmpName"].ToString() : "";
                            string email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "";
                            string mobile = reader["MobileNo"] != DBNull.Value ? reader["MobileNo"].ToString() : "";
                            string status = reader["ImportStatus"] != DBNull.Value ? reader["ImportStatus"].ToString() : "";
                            string error = reader["ErrorMessage"] != DBNull.Value ? reader["ErrorMessage"].ToString() : "";

                            // Wrap error message in quotes to prevent CSV breakage from commas
                            error = string.Format("\"{0}\"", error.Replace("\"", "\"\""));

                            sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", code, name, email, mobile, status, error));
                        }

                        // Stream CSV file directly to the client browser
                        Response.Clear();
                        Response.Buffer = true;
                        Response.AddHeader("content-disposition", string.Format("attachment;filename=HRMS_BatchReport_{0}.csv", batchId));
                        Response.Charset = "";
                        Response.ContentType = "text/csv";
                        Response.Output.Write(sb.ToString());
                        Response.Flush();
                        System.Web.HttpContext.Current.ApplicationInstance.CompleteRequest();
                        //Response.End();
                    }
                }
            }
        }
    }
}