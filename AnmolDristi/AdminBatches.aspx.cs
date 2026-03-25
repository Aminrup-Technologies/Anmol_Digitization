using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
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
            // 1. SECURITY: Check session on EVERY request (including PostBacks)
            if (Session["USERID"] == null || Session["USERNAME"] == null)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            lblPageError.Text = ""; // Clear errors on reload

            if (!IsPostBack)
            {
                // Sensible defaults (IST)
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
                var sql = new StringBuilder(@"
                    SELECT TOP (50) s.ImportBatchId,
                           MIN(s.ImportFileName) AS AnyFile,
                           COUNT(*) AS TotalRows,
                           SUM(CASE WHEN s.ImportStatus = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
                           SUM(CASE WHEN s.ImportStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
                           SUM(CASE WHEN s.ImportStatus IN ('Inserted','Imported','Pending','New') THEN 1 ELSE 0 END) AS WaitingCount,
                           MAX(s.ModifiedDateTime) AS LastModifiedIST
                    FROM dbo.SalesDailyActivity_Staging s
                    WHERE 1=1 ");

                using (var cn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = cn;

                    // Safely append date parameters (C# 5.0 compatible)
                    DateTime fromDate;
                    if (DateTime.TryParse(txtFrom.Text, out fromDate))
                    {
                        sql.Append(" AND s.ModifiedDateTime >= @FromIST ");
                        cmd.Parameters.AddWithValue("@FromIST", fromDate.Date); // Start of day
                    }

                    DateTime toDate;
                    if (DateTime.TryParse(txtTo.Text, out toDate))
                    {
                        sql.Append(" AND s.ModifiedDateTime <= @ToIST ");
                        // Add 1 day minus 1 tick to cover the whole 'To' day up to 23:59:59
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
            // Re-apply highlight if the grid is rebound after a postback
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

            // C# 5.0 compatible out variable extraction
            if (string.IsNullOrWhiteSpace(batchIdRaw) || !Guid.TryParse(batchIdRaw, out importBatchId))
            {
                ShowRunMessage("Please select a valid Batch from the grid.", true);
                return;
            }

            // Server-side guard against concurrent/duplicate runs
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

            // Parse Inputs Safely
            decimal ratePerKm;
            if (!decimal.TryParse(txtRatePerKm.Text, out ratePerKm))
            {
                ratePerKm = 2.75M; // Fallback default
            }

            // Null-safe check without the C# 6.0 ?. operator
            string sessionUserName = Session["USERNAME"] != null ? Session["USERNAME"].ToString() : null;
            string processedBy = string.IsNullOrWhiteSpace(txtProcessedBy.Text)
                                 ? sessionUserName
                                 : txtProcessedBy.Text.Trim();

            bool allowOverwrite = chkAllowOverwrite.Checked;

            // Execute the Stored Procedure
            string message;
            var dt = ExecuteProcessWithAudit(importBatchId, ratePerKm, processedBy, allowOverwrite, out message);

            if (dt != null)
            {
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

            // Refresh the grid to show updated processed/error row counts
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
                cmd.CommandTimeout = 300; // 5 mins max for a heavy batch

                cmd.Parameters.Add(new SqlParameter("@ImportBatchId", SqlDbType.UniqueIdentifier) { Value = importBatchId });
                cmd.Parameters.Add(new SqlParameter("@RatePerKm", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = ratePerKm });
                cmd.Parameters.Add(new SqlParameter("@ProcessedByUser", SqlDbType.NVarChar, 128) { Value = string.IsNullOrEmpty(processedBy) ? DBNull.Value : (object)processedBy });
                cmd.Parameters.Add(new SqlParameter("@AllowOverwrite", SqlDbType.Bit) { Value = allowOverwrite ? 1 : 0 });

                try
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }

                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        string importAuditId = dt.Columns.Contains("ImportAuditId") ? row["ImportAuditId"].ToString() : "-";
                        string status = dt.Columns.Contains("Status") ? row["Status"].ToString() : "Completed";
                        string inserted = dt.Columns.Contains("InsertedRows") ? row["InsertedRows"].ToString() : "0";
                        string errors = dt.Columns.Contains("ErrorRows") ? row["ErrorRows"].ToString() : "0";

                        // C# 5.0 compatible string formatting
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
                FROM dbo.ImportAudit
                WHERE ImportBatchId = @ImportBatchId
                ORDER BY ImportAuditId DESC; -- latest audit row
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
                catch
                {
                    return BatchState.Unknown; // Fail safe
                }
            }
        }


        protected void btnExportReport_Click(object sender, EventArgs e)
        {
            string batchIdRaw = hfSelectedBatch.Value;
            Guid importBatchId;

            // Validate selection
            if (string.IsNullOrWhiteSpace(batchIdRaw) || !Guid.TryParse(batchIdRaw, out importBatchId))
            {
                ShowRunMessage("Please select a batch from the grid to export.", true);
                return;
            }

            // Query to pull the readable report for the user
            string sql = @"
                SELECT 
                    User_Id AS [User ID], 
                    User_Name AS [Employee Name], 
                    Zone, 
                    CONVERT(varchar, [Date], 103) AS [Activity Date], 
                    ImportStatus AS [Final Status], 
                    ProcessingMessage AS [System Remarks / Reason]
                FROM dbo.SalesDailyActivity_Staging
                WHERE ImportBatchId = @BatchId
                ORDER BY ImportStatus, [Date]";

            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@BatchId", importBatchId);

                using (var da = new SqlDataAdapter(cmd))
                {
                    DataTable dtReport = new DataTable();
                    da.Fill(dtReport);

                    if (dtReport.Rows.Count > 0)
                    {
                        ExportDataTableToCsv(dtReport, "BatchReport_" + batchIdRaw.Substring(0, 8) + ".csv");
                    }
                    else
                    {
                        ShowRunMessage("No data found for this batch to export.", true);
                    }
                }
            }
        }

        private void ExportDataTableToCsv(DataTable dt, string filename)
        {
            // Clear the response to prepare for file download
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=" + filename);
            Response.Charset = "";
            Response.ContentType = "text/csv";

            StringBuilder sb = new StringBuilder();

            // 1. Write Headers
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                sb.Append("\"" + dt.Columns[i].ColumnName + "\"");
                if (i < dt.Columns.Count - 1) sb.Append(",");
            }
            sb.AppendLine();

            // 2. Write Data Rows
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    // Escape existing quotes to prevent CSV breakage
                    string data = row[i].ToString().Replace("\"", "\"\"");
                    sb.Append("\"" + data + "\"");

                    if (i < dt.Columns.Count - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            // Push to browser
            Response.Output.Write(sb.ToString());
            Response.Flush();
            Response.End();
        }


    }
}