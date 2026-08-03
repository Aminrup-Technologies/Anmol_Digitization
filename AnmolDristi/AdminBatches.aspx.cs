using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class AdminBatches : BasePage
    {
        private string ConnString
        {
            get { return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["USERID"] == null || Session["USERNAME"] == null)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            lblPageError.Text = "";

            if (!IsPostBack)
            {
                txtFrom.Text = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                txtTo.Text = DateTime.Now.ToString("yyyy-MM-dd");
                BindBatches();
            }

            lblSelectedBatch.Text = string.IsNullOrEmpty(hfSelectedBatch.Value) ? "(none selected)" : hfSelectedBatch.Value;
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            BindBatches();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtFrom.Text = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            txtTo.Text = DateTime.Now.ToString("yyyy-MM-dd");
            ddlStatus.SelectedIndex = 0;
            hfSelectedBatch.Value = "";
            lblSelectedBatch.Text = "(none selected)";
            BindBatches();
        }

        private void BindBatches()
        {
            try
            {
                // Added WITH (NOLOCK) to prevent timeouts while bulk inserts are running concurrently
                var sql = new StringBuilder(@"
                    SELECT TOP (50) s.ImportBatchId,
                           MIN(s.ImportFileName) AS AnyFile,
                           COUNT(*) AS TotalRows,
                           SUM(CASE WHEN s.ImportStatus = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
                           SUM(CASE WHEN s.ImportStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
                           SUM(CASE WHEN s.ImportStatus IN ('Inserted','Imported','Pending','New') THEN 1 ELSE 0 END) AS WaitingCount,
                           MAX(s.ModifiedDateTime) AS LastModifiedIST
                    FROM dbo.SalesDailyActivity_Staging s WITH (NOLOCK)
                    WHERE 1=1 ");

                using (var cn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;

                    DateTime fromDate;
                    if (DateTime.TryParse(txtFrom.Text, out fromDate))
                    {
                        sql.Append(" AND s.ModifiedDateTime >= @FromIST ");
                        cmd.Parameters.AddWithValue("@FromIST", fromDate.Date);
                    }

                    DateTime toDate;
                    if (DateTime.TryParse(txtTo.Text, out toDate))
                    {
                        sql.Append(" AND s.ModifiedDateTime <= @ToIST ");
                        cmd.Parameters.AddWithValue("@ToIST", toDate.Date.AddDays(1).AddTicks(-1));
                    }

                    if (!string.IsNullOrWhiteSpace(ddlStatus.SelectedValue))
                    {
                        sql.Append(" AND s.ImportStatus = @Status ");
                        cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                    }

                    sql.Append(" GROUP BY s.ImportBatchId ORDER BY MAX(s.ModifiedDateTime) DESC;");

                    cmd.CommandText = sql.ToString();
                    cmd.CommandTimeout = 120;

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        gvBatches.DataSource = dt;
                        gvBatches.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                lblPageError.Text = "Failed to load batches: " + ex.Message;
            }
        }

        protected void gvBatches_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string rowBatchId = DataBinder.Eval(e.Row.DataItem, "ImportBatchId").ToString();
                if (rowBatchId == hfSelectedBatch.Value)
                {
                    e.Row.CssClass = "selected-row";
                }
            }
        }

        protected void btnRun_Click(object sender, EventArgs e)
        {
            string batchIdRaw = hfSelectedBatch.Value;
            Guid importBatchId;

            if (string.IsNullOrWhiteSpace(batchIdRaw) || !Guid.TryParse(batchIdRaw, out importBatchId))
            {
                ShowRunMessage("Please select a valid Batch from the grid.", true);
                return;
            }

            var state = GetBatchState(importBatchId);

            if (state == BatchState.InProgress)
            {
                ShowRunMessage("Cannot run: This batch is currently processing in the background.", true);
                return;
            }
            if (state == BatchState.Completed)
            {
                ShowRunMessage("Cannot run: This batch has already been successfully completed.", true);
                return;
            }
            if (state == BatchState.Unknown)
            {
                ShowRunMessage("Batch has an unknown previous state. Please check the Audit Logs. Aborting.", true);
                return;
            }

            decimal ratePerKm;
            if (!decimal.TryParse(txtRatePerKm.Text, out ratePerKm)) ratePerKm = 3.75M;

            string sessionUserName = Session["USERNAME"] != null ? Session["USERNAME"].ToString() : null;
            string processedBy = string.IsNullOrWhiteSpace(txtProcessedBy.Text) ? sessionUserName : txtProcessedBy.Text.Trim();
            bool allowOverwrite = chkAllowOverwrite.Checked;

            string message;
            var dt = ExecuteProcessWithAudit(importBatchId, ratePerKm, processedBy, allowOverwrite, out message);

            if (dt != null)
            {
                // NOTE: It is heavily advised to move SyncHierarchyForBatch inside the Stored Procedure above for transactional integrity.
                try
                {
                    int updatedRows = SyncHierarchyForBatch(importBatchId);
                    message += string.Format(" | Hierarchy Synced ({0} levels updated).", updatedRows);
                }
                catch (Exception ex)
                {
                    message += " | Hierarchy Sync Failed: " + ex.Message;
                }

                gvResult.DataSource = dt;
                gvResult.DataBind();
                ShowRunMessage(message ?? "Process Completed Successfully.", false);
            }
            else
            {
                gvResult.DataSource = null;
                gvResult.DataBind();
                ShowRunMessage(message ?? "Procedure failed or returned no result.", true);
            }

            BindBatches();
        }

        private void ShowRunMessage(string msg, bool isError)
        {
            lblRunMessage.Text = msg;
            lblRunMessage.CssClass = isError ? "text-danger font-weight-bold" : "text-success font-weight-bold";
        }

        private DataTable ExecuteProcessWithAudit(Guid importBatchId, decimal ratePerKm, string processedBy, bool allowOverwrite, out string message)
        {
            message = null;
            var dt = new DataTable();

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("dbo.usp_Run_Process_StagingToFinal_WithAudit", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;

                cmd.Parameters.Add(new SqlParameter("@ImportBatchId", SqlDbType.UniqueIdentifier) { Value = importBatchId });
                cmd.Parameters.Add(new SqlParameter("@RatePerKm", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = ratePerKm });
                cmd.Parameters.Add(new SqlParameter("@ProcessedByUser", SqlDbType.NVarChar, 128) { Value = string.IsNullOrEmpty(processedBy) ? DBNull.Value : (object)processedBy });
                cmd.Parameters.Add(new SqlParameter("@AllowOverwrite", SqlDbType.Bit) { Value = allowOverwrite ? 1 : 0 });

                try
                {
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        string importAuditId = dt.Columns.Contains("ImportAuditId") ? row["ImportAuditId"].ToString() : "-";
                        string status = dt.Columns.Contains("Status") ? row["Status"].ToString() : "Completed";
                        string inserted = dt.Columns.Contains("InsertedRows") ? row["InsertedRows"].ToString() : "0";
                        string errors = dt.Columns.Contains("ErrorRows") ? row["ErrorRows"].ToString() : "0";

                        message = string.Format("Result: {0} | Inserted: {1} | Errors: {2} | Audit ID: {3}", status, inserted, errors, importAuditId);
                    }
                    else
                    {
                        message = "Procedure executed successfully but returned no summary rows.";
                    }
                }
                catch (SqlException ex)
                {
                    message = "SQL Database Error: " + ex.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    message = "Application Error: " + ex.Message;
                    return null;
                }
            }
            return dt;
        }

        private enum BatchState { NotProcessed, InProgress, Completed, Failed, Unknown }

        private BatchState GetBatchState(Guid importBatchId)
        {
            const string sql = @"
                SELECT TOP(1) Status
                FROM dbo.ImportAudit WITH (NOLOCK)
                WHERE ImportBatchId = @ImportBatchId
                ORDER BY ImportAuditId DESC;
            ";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@ImportBatchId", importBatchId);
                try
                {
                    cn.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj == null || obj == DBNull.Value) return BatchState.NotProcessed;

                    var s = obj.ToString();
                    if (s.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return BatchState.InProgress;
                    if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return BatchState.Completed;
                    if (s.Equals("Failed", StringComparison.OrdinalIgnoreCase)) return BatchState.Failed;
                    return BatchState.Unknown;
                }
                catch { return BatchState.Unknown; }
            }
        }

        private int SyncHierarchyForBatch(Guid importBatchId)
        {
            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("dbo.usp_SyncHierarchyForBatch", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                cmd.Parameters.Add(new SqlParameter("@TargetBatchId", SqlDbType.UniqueIdentifier) { Value = importBatchId });
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        protected void btnExportReport_Click(object sender, EventArgs e)
        {
            string batchIdRaw = hfSelectedBatch.Value;
            Guid importBatchId;

            if (string.IsNullOrWhiteSpace(batchIdRaw) || !Guid.TryParse(batchIdRaw, out importBatchId))
            {
                ShowRunMessage("Please select a batch from the grid to export.", true);
                return;
            }

            string sql = @"
                SELECT 
                    User_Id AS [User ID], 
                    User_Name AS [Employee Name], 
                    Zone, 
                    CONVERT(varchar, [Date], 103) AS [Activity Date], 
                    ImportStatus AS [Final Status], 
                    ProcessingMessage AS [System Remarks / Reason]
                FROM dbo.SalesDailyActivity_Staging WITH (NOLOCK)
                WHERE ImportBatchId = @BatchId
                ORDER BY ImportStatus, [Date]";

            ExportQueryToCsv(sql, importBatchId, "BatchReport_" + batchIdRaw.Substring(0, 8) + ".csv");
        }

        // 🚀 Highly optimized memory-safe CSV Export streaming directly from SQL
        private void ExportQueryToCsv(string sqlQuery, Guid batchId, string filename)
        {
            Response.Clear();
            Response.Buffer = false; // Disable buffer to stream directly to client
            Response.AddHeader("content-disposition", "attachment;filename=" + filename);
            Response.ContentType = "text/csv";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sqlQuery, cn))
            {
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.CommandTimeout = 120;
                cn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        // Write Headers
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Response.Write("\"" + reader.GetName(i) + "\"");
                            if (i < reader.FieldCount - 1) Response.Write(",");
                        }
                        Response.Write(Environment.NewLine);

                        // Write Rows directly to response (Saves MBs of RAM)
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string data = reader[i] == DBNull.Value ? "" : reader[i].ToString().Replace("\"", "\"\"");
                                Response.Write("\"" + data + "\"");
                                if (i < reader.FieldCount - 1) Response.Write(",");
                            }
                            Response.Write(Environment.NewLine);
                        }
                    }
                    else
                    {
                        Response.Write("No data found for this batch.");
                    }
                }
            }

            // Safe exit that avoids the ThreadAbortException caused by Response.End()
            Response.Flush();
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}