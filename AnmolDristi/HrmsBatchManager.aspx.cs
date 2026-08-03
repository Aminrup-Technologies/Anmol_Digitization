using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Linq;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Web.UI;

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
            ScriptManager sm = ScriptManager.GetCurrent(this);

            if (sm != null)
            {
                sm.RegisterPostBackControl(btnExportReport);
            }

            // 1. Enforce Security Gateway
            if (Session["UserId"] == null || string.IsNullOrEmpty(Session["UserId"].ToString()))
            {
                Response.Redirect("~/Login.aspx?msg=session_expired", true);
                return;
            }

            if (!IsPostBack)
            {
                bool batchSelectedFromQuery = TrySelectBatchFromQueryString();
                LoadBatches();
                LoadOverwriteFields();

                if (batchSelectedFromQuery)
                {
                    RegisterScrollToSyncConfiguration();
                }
            }
        }

        /// <summary>
        /// Reads BatchId from the query string (Upload → BatchManager handoff).
        /// On success, populates selection controls. Invalid/missing values leave the page unchanged.
        /// </summary>
        private bool TrySelectBatchFromQueryString()
        {
            string batchIdRaw = Request.QueryString["BatchId"];
            if (string.IsNullOrWhiteSpace(batchIdRaw))
                return false;

            int batchId;
            if (!int.TryParse(batchIdRaw.Trim(), out batchId) || batchId <= 0)
                return false;

            if (!BatchExists(batchId))
                return false;

            string batchIdText = batchId.ToString();
            hfSelectedBatch.Value = batchIdText;
            lblSelectedBatch.Text = batchIdText;
            return true;
        }

        private bool BatchExists(int batchId)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                int count = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                return count > 0;
            }
        }

        private void RegisterScrollToSyncConfiguration()
        {
            const string script = @"
(function () {
    function scrollToSyncConfiguration() {
        var el = document.getElementById('syncConfigurationSection');
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
    if (window.addEventListener) {
        window.addEventListener('load', scrollToSyncConfiguration);
    } else if (window.attachEvent) {
        window.attachEvent('onload', scrollToSyncConfiguration);
    } else {
        scrollToSyncConfiguration();
    }
})();";

            ClientScript.RegisterStartupScript(
                this.GetType(),
                "ScrollToSyncConfiguration",
                script,
                true);
        }

        protected void gvBatches_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if (string.IsNullOrEmpty(hfSelectedBatch.Value))
                    return;

                object batchValue = DataBinder.Eval(e.Row.DataItem, "ImportBatchId");
                if (batchValue == null)
                    return;

                if (string.Equals(batchValue.ToString(), hfSelectedBatch.Value, StringComparison.Ordinal))
                {
                    string existing = e.Row.CssClass ?? string.Empty;
                    if (existing.IndexOf("selected-row", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        e.Row.CssClass = (existing + " selected-row").Trim();
                    }
                }
            }
        }

        private void LoadBatches()
        {
            lblPageError.Text = string.Empty;
            lblRunMessage.Text = string.Empty;

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
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                string sql = @"SELECT * FROM MST_HRMSImportField WHERE IsVisible=1 ORDER BY Category, DisplayOrder";

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    rptOverwriteFields.DataSource = dt;
                    rptOverwriteFields.DataBind();
                }
            }
        }

        protected void rblProcessingMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            divOverwriteFields.Visible = rblProcessingMode.SelectedValue == "2";
        }

        protected void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk = item.FindControl("chkField") as CheckBox;
                if (chk != null) chk.Checked = true;
            }
        }

        protected void btnClearAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk = item.FindControl("chkField") as CheckBox;
                if (chk != null) chk.Checked = false;
            }
        }

        private string GetSelectedOverwriteFields()
        {
            List<string> fields = new List<string>();

            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk = item.FindControl("chkField") as CheckBox;
                HiddenField hf = item.FindControl("hfFieldCode") as HiddenField;

                if (chk != null && hf != null && chk.Checked)
                {
                    fields.Add(hf.Value.Trim());
                }
            }

            return string.Join("|", fields);
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
            string executedBy = Session["UserId"] != null ? Session["UserId"].ToString() : "SYSTEM";

            try
            {
                // STEP 1: Validate batch before synchronization
                bool isValid;
                DataTable dtValidation;
                string validationMessage;

                ExecuteHseValidateBatch(
                    batchId,
                    processingMode,
                    modules,
                    out isValid,
                    out dtValidation,
                    out validationMessage);

                // STEP 2: Validation failed — do not synchronize
                if (!isValid)
                {
                    gvResult.DataSource = dtValidation;
                    gvResult.DataBind();

                    lblRunMessage.Text = string.Empty;
                    lblPageError.Text = string.IsNullOrWhiteSpace(validationMessage)
                        ? "Batch validation failed. Synchronization was not executed. Review the validation summary and staging ErrorMessage values."
                        : validationMessage;

                    LoadBatches();
                    return;
                }

                // STEP 3: Validation passed — existing synchronization flow unchanged
                using (SqlConnection conn =
                    new SqlConnection(GetConnectionString()))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_RunHRMSSynchronization", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 300;

                        cmd.Parameters.AddWithValue("@BatchId", batchId);
                        cmd.Parameters.AddWithValue("@AllowOverwrite", allowOverwrite);
                        cmd.Parameters.AddWithValue("@ProcessingMode", processingMode);
                        cmd.Parameters.AddWithValue("@Modules", modules);
                        cmd.Parameters.AddWithValue("@OverwriteFields", string.IsNullOrEmpty(overwriteFields) ? (object)DBNull.Value : overwriteFields);
                        cmd.Parameters.AddWithValue("@ExecutedBy", executedBy);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            gvResult.DataSource = dt;
                            gvResult.DataBind();
                        }

                        lblRunMessage.Text = "Synchronization completed successfully.";
                    }
                }

                LoadBatches();
            }
            catch (Exception ex)
            {
                lblPageError.Text = ex.Message;
            }
        }

        /// <summary>
        /// Executes dbo.usp_HSE_ValidateBatch and returns the OUTPUT IsValid flag plus summary result set.
        /// Parameter name is @Modules (procedure contract). Callers pass the same Modules string used for sync.
        /// </summary>
        private void ExecuteHseValidateBatch(
            int batchId,
            int processingMode,
            string modules,
            out bool isValid,
            out DataTable validationSummary,
            out string validationMessage)
        {
            isValid = false;
            validationMessage = string.Empty;
            validationSummary = new DataTable();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_HSE_ValidateBatch", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;

                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.Parameters.AddWithValue("@ProcessingMode", processingMode);
                cmd.Parameters.AddWithValue(
                    "@Modules",
                    string.IsNullOrWhiteSpace(modules) ? (object)DBNull.Value : modules);

                SqlParameter pIsValid = new SqlParameter("@IsValid", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(pIsValid);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(validationSummary);
                }

                if (pIsValid.Value != null && pIsValid.Value != DBNull.Value)
                    isValid = Convert.ToBoolean(pIsValid.Value);

                if (validationSummary.Rows.Count > 0
                    && validationSummary.Columns.Contains("ValidationMessage")
                    && validationSummary.Rows[0]["ValidationMessage"] != DBNull.Value)
                {
                    validationMessage = validationSummary.Rows[0]["ValidationMessage"].ToString();
                }
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

        // Bulletproof CSV Field Escaper to prevent layout shifts
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                return string.Format("\"{0}\"", field.Replace("\"", "\"\""));
            }

            return field;
        }

        protected void btnExportReport_Click(object sender, EventArgs e)
        {
            if (ScriptManager.GetCurrent(this).IsInAsyncPostBack)
            {
                throw new Exception("Export is running inside Async PostBack.");
            }

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
                            string code = EscapeCsvField(reader["EmpCode"] != DBNull.Value ? reader["EmpCode"].ToString() : "");
                            string name = EscapeCsvField(reader["EmpName"] != DBNull.Value ? reader["EmpName"].ToString() : "");
                            string email = EscapeCsvField(reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "");
                            string mobile = EscapeCsvField(reader["MobileNo"] != DBNull.Value ? reader["MobileNo"].ToString() : "");
                            string status = EscapeCsvField(reader["ImportStatus"] != DBNull.Value ? reader["ImportStatus"].ToString() : "");
                            string error = EscapeCsvField(reader["ErrorMessage"] != DBNull.Value ? reader["ErrorMessage"].ToString() : "");

                            sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", code, name, email, mobile, status, error));
                        }

                        Response.Clear();
                        Response.ClearHeaders();
                        Response.ClearContent();

                        Response.Buffer = true;
                        Response.ContentType = "text/csv";
                        Response.ContentEncoding = Encoding.UTF8;

                        Response.AddHeader(
                            "Content-Disposition",
                            $"attachment; filename=HRMS_BatchReport_{batchId}.csv");

                        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

                        Response.BinaryWrite(bytes);

                        Response.Flush();

                        Response.End();

                        Response.Flush();

                        Response.SuppressContent = true;
                        HttpContext.Current.ApplicationInstance.CompleteRequest();

                        return;
                    }
                }
            }
        }
    }
}