using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class AdminBatches : BasePage
    {
        private string ConnString => ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                System.Diagnostics.Debug.WriteLine("IsPostBack true; request.Form keys: " + Request.Form.Count);
                lblRunMessage.Text = "IsPostBack at " + DateTime.Now.ToLongTimeString();

                // sensible defaults (IST)
                txtTo.Text = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
                txtFrom.Text = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd 00:00:00");
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
            txtFrom.Text = "";
            txtTo.Text = "";
            ddlStatus.SelectedIndex = 0;
            BindBatches();
        }

        private void BindBatches()
        {
            string sql = @"
            SELECT TOP (50) s.ImportBatchId,
                   MIN(s.ImportFileName) AS AnyFile,
                   COUNT(*) AS TotalRows,
                   SUM(CASE WHEN s.ImportStatus = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
                   SUM(CASE WHEN s.ImportStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
                   SUM(CASE WHEN s.ImportStatus IN ('Inserted','Imported','Pending','New') THEN 1 ELSE 0 END) AS WaitingCount,
                   MAX(s.ModifiedDateTime) AS LastModifiedIST
            FROM dbo.SalesDailyActivity_Staging s
            /**where_clause**/
            GROUP BY s.ImportBatchId
            ORDER BY MAX(s.ModifiedDateTime) DESC;
            ";

            string where = " WHERE 1=1 ";
            var cmd = new SqlCommand();
            DateTime d;
            if (!string.IsNullOrWhiteSpace(txtFrom.Text) && DateTime.TryParse(txtFrom.Text, out d))
            {
                where += " AND s.ModifiedDateTime >= @FromIST ";
                cmd.Parameters.AddWithValue("@FromIST", d);
            }
            if (!string.IsNullOrWhiteSpace(txtTo.Text) && DateTime.TryParse(txtTo.Text, out d))
            {
                where += " AND s.ModifiedDateTime <= @ToIST ";
                cmd.Parameters.AddWithValue("@ToIST", d);
            }
            if (!string.IsNullOrWhiteSpace(ddlStatus.SelectedValue))
            {
                where += " AND s.ImportStatus = @Status ";
                cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
            }

            sql = sql.Replace("/**where_clause**/", where);

            DataTable dt = new DataTable();
            //using (var cn = new SqlConnection(ConnString))
            //using (var da = new SqlDataAdapter(sql, cn))
            //{
            //    if (cmd.Parameters.Count > 0)
            //        da.SelectCommand.Parameters.AddRange(cmd.Parameters);

            //    da.SelectCommand.CommandTimeout = 120;
            //    da.Fill(dt);
            //}

            using (var cn = new SqlConnection(ConnString))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                if (!string.IsNullOrWhiteSpace(txtFrom.Text))
                    da.SelectCommand.Parameters.AddWithValue("@FromIST", DateTime.Parse(txtFrom.Text));

                if (!string.IsNullOrWhiteSpace(txtTo.Text))
                    da.SelectCommand.Parameters.AddWithValue("@ToIST", DateTime.Parse(txtTo.Text));

                if (!string.IsNullOrWhiteSpace(ddlStatus.SelectedValue))
                    da.SelectCommand.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);

                da.SelectCommand.CommandTimeout = 120;
                da.Fill(dt);
            }


            gvBatches.DataSource = dt;
            gvBatches.DataBind();
        }

        protected void btnRun_Click(object sender, EventArgs e)
        {
            string batchIdRaw = hfSelectedBatch.Value;
            if (string.IsNullOrWhiteSpace(batchIdRaw))
            {
                lblRunMessage.Text = "No batch selected.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            Guid importBatchId;
            if (!Guid.TryParse(batchIdRaw, out importBatchId))
            {
                lblRunMessage.Text = "Selected batch id is not a valid GUID.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            // Server-side guard
            var state = GetBatchState(importBatchId);
            if (state == BatchState.InProgress)
            {
                lblRunMessage.Text = "Cannot run: batch is already InProgress.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }
            if (state == BatchState.Completed)
            {
                lblRunMessage.Text = "Cannot run: batch was already Completed earlier.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            // optionally notify about Failed/Unknown and allow re-run only on Failed:
            if (state == BatchState.Unknown)
            {
                lblRunMessage.Text = "Batch has unknown previous state — please check ImportAudit. Aborting.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            if (state == BatchState.Failed)
            {
                // allow but require explicit confirm
                if (!ConfirmRetryFromUI())
                {
                    lblRunMessage.Text = "Retry cancelled.";
                    return;
                }
            }

            decimal ratePerKm;
            if (!decimal.TryParse(txtRatePerKm.Text, out ratePerKm))
            {
                ratePerKm = 2.75M;
            }

            string processedBy = string.IsNullOrWhiteSpace(txtProcessedBy.Text) ? null : txtProcessedBy.Text;
            bool allowOverwrite = chkAllowOverwrite.Checked;

            string message;
            var dt = ExecuteProcessWithAudit(importBatchId, ratePerKm, processedBy, allowOverwrite, out message);

            if (dt != null)
            {
                gvResult.DataSource = dt;
                gvResult.DataBind();
                lblRunMessage.Text = message ?? "Completed";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                lblRunMessage.Text = message ?? "Procedure failed or returned no result.";
                lblRunMessage.ForeColor = System.Drawing.Color.Red;
            }

            // refresh list
            BindBatches();
        }

        private bool ConfirmRetryFromUI()
        {
            return string.Equals(hfRetryConfirmed.Value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private DataTable ExecuteProcessWithAudit(Guid importBatchId, decimal ratePerKm, string processedBy, bool allowOverwrite, out string message)
        {
            message = null;
            var dt = new DataTable();
            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("dbo.usp_Run_Process_StagingToFinal_WithAudit", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0; // allow long running; change if needed

                cmd.Parameters.Add(new SqlParameter("@ImportBatchId", SqlDbType.UniqueIdentifier) { Value = importBatchId });
                cmd.Parameters.Add(new SqlParameter("@RatePerKm", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = ratePerKm });
                if (string.IsNullOrEmpty(processedBy))
                    cmd.Parameters.Add(new SqlParameter("@ProcessedByUser", SqlDbType.NVarChar, 128) { Value = DBNull.Value });
                else
                    cmd.Parameters.Add(new SqlParameter("@ProcessedByUser", SqlDbType.NVarChar, 128) { Value = processedBy });
                cmd.Parameters.Add(new SqlParameter("@AllowOverwrite", SqlDbType.Bit) { Value = allowOverwrite ? 1 : 0 });

                try
                {
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        string importAuditId = dt.Columns.Contains("ImportAuditId") ? row["ImportAuditId"].ToString() : null;
                        string status = dt.Columns.Contains("Status") ? row["Status"].ToString() : null;
                        string inserted = dt.Columns.Contains("InsertedRows") ? row["InsertedRows"].ToString() : null;
                        string errors = dt.Columns.Contains("ErrorRows") ? row["ErrorRows"].ToString() : null;

                        message = string.Format("Status: {0}. Inserted: {1}. Errors: {2}. AuditId: {3}",
                                                status ?? "Unknown", inserted ?? "-", errors ?? "-", importAuditId ?? "-");
                    }
                    else
                    {
                        message = "Procedure executed successfully but returned no rows.";
                    }
                }
                catch (SqlException ex)
                {
                    // TODO: integrate with your app logging (ELMAH/NLog/Serilog)
                    message = "SQL Error: " + ex.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    message = "Error: " + ex.Message;
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
                cn.Open();
                var obj = cmd.ExecuteScalar();
                if (obj == null || obj == DBNull.Value) return BatchState.NotProcessed;
                var s = obj.ToString();
                if (s.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return BatchState.InProgress;
                if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return BatchState.Completed;
                if (s.Equals("Failed", StringComparison.OrdinalIgnoreCase)) return BatchState.Failed;
                return BatchState.Unknown;
            }
        }
    }
}