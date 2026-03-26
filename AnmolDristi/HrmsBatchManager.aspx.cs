using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;

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
            if (Session["UserId"] == null)
            {
                // Response.Redirect("Login.aspx");
            }

            if (!IsPostBack)
            {
                LoadBatches();
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

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvBatches.DataSource = dt;
                    gvBatches.DataBind();
                }
            }
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
            lblPageError.Text = string.Empty;
            lblRunMessage.Text = string.Empty;

            if (string.IsNullOrEmpty(hfSelectedBatch.Value))
            {
                lblPageError.Text = "Please select a batch from the grid first.";
                return;
            }

            int batchId = Convert.ToInt32(hfSelectedBatch.Value);
            bool allowOverwrite = chkAllowOverwrite.Checked;

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_ProcessEmployeeBatch", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 300; // Increase to 5 minutes to allow complex hierarchy syncing

                        cmd.Parameters.AddWithValue("@BatchId", batchId);
                        cmd.Parameters.AddWithValue("@AllowOverwrite", allowOverwrite);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dtResult = new DataTable();
                        da.Fill(dtResult); // Fills the DataGrid with the Output Table from your Stored Procedure

                        // Bind the output summary to the Result GridView
                        gvResult.DataSource = dtResult;
                        gvResult.DataBind();

                        lblRunMessage.Text = string.Format("Batch {0} executed successfully.", batchId);
                    }
                }

                // Refresh the master grid to update Processed/Waiting counts
                LoadBatches();
            }
            catch (Exception ex)
            {
                lblPageError.Text = string.Format("Error processing batch: {0}", ex.Message);
            }
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
                        Response.End();
                    }
                }
            }
        }
    }
}