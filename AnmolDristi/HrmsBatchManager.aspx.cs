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

namespace AnmolDristi
{
    public partial class HrmsBatchManager : AnmolDristi.BasePage
    {
        private const string ActionMaster = "MASTER";
        private const string ActionInsert = "INSERT";
        private const string ActionUpdate = "UPDATE";
        private const string ActionHierarchy = "HIERARCHY";

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
                    int batchId;
                    if (int.TryParse(hfSelectedBatch.Value, out batchId))
                        LoadSelectedBatchDetails(batchId);

                    RegisterScrollToSyncConfiguration();
                }

                ApplyActionUiState();
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

            hfSelectedBatch.Value = batchId.ToString();
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

        protected void gvBatches_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!string.Equals(e.CommandName, "SelectBatch", StringComparison.OrdinalIgnoreCase))
                return;

            int batchId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out batchId) || batchId <= 0)
            {
                lblPageError.Text = "Invalid batch selection.";
                return;
            }

            hfSelectedBatch.Value = batchId.ToString();
            LoadSelectedBatchDetails(batchId);
            LoadBatches();
            ApplyActionUiState();
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

            string query = @"
                SELECT 
                    b.BatchId AS ImportBatchId, 
                    b.FileName AS AnyFile,
                    COUNT(s.StagingId) AS TotalRows,
                    SUM(CASE WHEN s.ImportStatus = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
                    SUM(CASE WHEN s.ImportStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
                    SUM(CASE WHEN s.ImportStatus = 'Pending' THEN 1 ELSE 0 END) AS WaitingCount,
                    b.UploadDate AS LastModifiedIST,
                    b.Status,
                    CASE WHEN ISNULL(b.MasterSyncCompleted, 0) = 1 THEN N'Synced' ELSE N'Not synced' END AS MasterSyncLabel
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

            query += " GROUP BY b.BatchId, b.FileName, b.UploadDate, b.Status, b.MasterSyncCompleted ORDER BY b.UploadDate DESC";

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

        private void LoadSelectedBatchDetails(int batchId)
        {
            const string sql = @"
                SELECT
                    b.BatchId,
                    b.FileName,
                    b.UploadDate,
                    b.Status,
                    ISNULL(b.MasterSyncCompleted, 0) AS MasterSyncCompleted,
                    b.MasterSyncDate,
                    COUNT(s.StagingId) AS TotalRows
                FROM dbo.ImportBatchMaster b
                LEFT JOIN dbo.Staging_EmployeeData s ON s.BatchId = b.BatchId
                WHERE b.BatchId = @BatchId
                GROUP BY b.BatchId, b.FileName, b.UploadDate, b.Status, b.MasterSyncCompleted, b.MasterSyncDate";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        ClearSelection();
                        lblPageError.Text = "The selected batch was not found.";
                        return;
                    }

                    hfSelectedBatch.Value = Convert.ToInt32(reader["BatchId"]).ToString();
                    lblSelectedBatch.Text = hfSelectedBatch.Value;
                    lblBatchFileName.Text = reader["FileName"] == DBNull.Value ? "—" : reader["FileName"].ToString();
                    lblBatchUploadDate.Text = reader["UploadDate"] == DBNull.Value
                        ? "—"
                        : Convert.ToDateTime(reader["UploadDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                    lblBatchStatus.Text = reader["Status"] == DBNull.Value ? "—" : reader["Status"].ToString();
                    lblBatchTotalRows.Text = reader["TotalRows"] == DBNull.Value ? "0" : Convert.ToInt32(reader["TotalRows"]).ToString();

                    bool masterSynced = Convert.ToBoolean(reader["MasterSyncCompleted"]);
                    hfMasterSyncCompleted.Value = masterSynced ? "1" : "0";

                    if (masterSynced)
                    {
                        string syncedAt = reader["MasterSyncDate"] == DBNull.Value
                            ? ""
                            : " at " + Convert.ToDateTime(reader["MasterSyncDate"]).ToString("yyyy-MM-dd HH:mm:ss") + " IST";
                        lblMasterSyncStatus.Text = "Completed" + syncedAt;
                        lblMasterSyncStatus.CssClass = "hrms-meta-value text-success";
                    }
                    else
                    {
                        lblMasterSyncStatus.Text = "Not completed";
                        lblMasterSyncStatus.CssClass = "hrms-meta-value text-danger";
                    }
                }
            }
        }

        /// <summary>
        /// Re-reads MasterSyncCompleted from the database. UI flags are not trusted for the gate.
        /// </summary>
        private bool IsMasterSyncCompleted(int batchId)
        {
            const string sql = @"
                SELECT ISNULL(MasterSyncCompleted, 0)
                FROM dbo.ImportBatchMaster
                WHERE BatchId = @BatchId";

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return false;
                return Convert.ToBoolean(result);
            }
        }

        /// <summary>
        /// Field catalog for UPDATE (@OverwriteFields). Must match FieldCodes accepted by
        /// dbo.usp_HSE_UpdateChangedEmployees. Hierarchy columns are omitted so the
        /// Update action cannot silently perform hierarchy processing.
        /// Bound only on !IsPostBack so checkbox state is preserved via ViewState.
        /// </summary>
        private static readonly string[,] OverwriteFieldDefinitions = new string[,]
        {
            { "EmployeeName", "Employee Name", "Employee", "1" },
            { "EmployeeStatus", "Employee Status", "Employee", "1" },
            { "Email", "Email", "Employee", "1" },
            { "Mobile", "Mobile", "Employee", "1" },
            { "Gender", "Gender", "Employee", "1" },
            { "DOJ", "Date of Joining (DOJ)", "Employee", "1" },
            { "Branch", "Branch", "Organization", "1" },
            { "Department", "Department", "Organization", "1" },
            { "Designation", "Designation", "Organization", "1" },
            { "Grade", "Grade", "Organization", "1" },
            { "Plant", "Plant", "Organization", "1" },
            { "Region", "Region", "Organization", "1" },
            { "Division", "Division", "Organization", "1" },
            { "Category", "Category", "Organization", "1" },
            { "SubDepartment", "Sub Department", "Organization", "1" }
        };

        private void LoadOverwriteFields()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("FieldCode", typeof(string));
            dt.Columns.Add("DisplayName", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("IsDefault", typeof(bool));
            dt.Columns.Add("DisplayOrder", typeof(int));

            int order = 1;
            for (int i = 0; i < OverwriteFieldDefinitions.GetLength(0); i++)
            {
                DataRow row = dt.NewRow();
                row["FieldCode"] = OverwriteFieldDefinitions[i, 0];
                row["DisplayName"] = OverwriteFieldDefinitions[i, 1];
                row["Category"] = OverwriteFieldDefinitions[i, 2];
                row["IsDefault"] = OverwriteFieldDefinitions[i, 3] == "1";
                row["DisplayOrder"] = order++;
                dt.Rows.Add(row);
            }

            rptOverwriteFields.DataSource = dt;
            rptOverwriteFields.DataBind();
        }

        protected void rblAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyActionUiState();
        }

        private void ApplyActionUiState()
        {
            bool batchSelected = !string.IsNullOrWhiteSpace(hfSelectedBatch.Value);
            btnSyncMasters.Enabled = batchSelected;

            bool masterSynced = string.Equals(hfMasterSyncCompleted.Value, "1", StringComparison.Ordinal);
            bool employeeEnabled = batchSelected && masterSynced;

            lblEmployeeActionsLocked.Visible = batchSelected && !masterSynced;
            rblAction.Enabled = employeeEnabled;

            string action = rblAction.SelectedValue;
            if (!employeeEnabled)
                action = string.Empty;

            pnlInsertConfig.Visible = employeeEnabled && action == ActionInsert;
            pnlUpdateConfig.Visible = employeeEnabled && action == ActionUpdate;
            pnlHierarchyConfig.Visible = employeeEnabled && action == ActionHierarchy;
            pnlNoActionConfig.Visible = !employeeEnabled || string.IsNullOrEmpty(action);

            btnExecuteInsert.Visible = employeeEnabled && action == ActionInsert;
            btnExecuteUpdate.Visible = employeeEnabled && action == ActionUpdate;
            btnExecuteHierarchy.Visible = employeeEnabled && action == ActionHierarchy;

            btnExecuteInsert.Enabled = employeeEnabled && action == ActionInsert;
            btnExecuteUpdate.Enabled = employeeEnabled && action == ActionUpdate;
            btnExecuteHierarchy.Enabled = employeeEnabled && action == ActionHierarchy;
        }

        protected void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk = item.FindControl("chkField") as CheckBox;
                if (chk != null) chk.Checked = true;
            }
            ApplyActionUiState();
        }

        protected void btnClearAll_Click(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptOverwriteFields.Items)
            {
                CheckBox chk = item.FindControl("chkField") as CheckBox;
                if (chk != null) chk.Checked = false;
            }
            ApplyActionUiState();
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
            ApplyActionUiState();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtFrom.Text = string.Empty;
            txtTo.Text = string.Empty;
            ddlStatus.SelectedIndex = 0;
            ClearSelection();
            LoadBatches();
            ApplyActionUiState();
        }

        private void ClearSelection()
        {
            hfSelectedBatch.Value = string.Empty;
            hfMasterSyncCompleted.Value = "0";
            lblSelectedBatch.Text = "(none selected)";
            lblBatchFileName.Text = "—";
            lblBatchUploadDate.Text = "—";
            lblBatchTotalRows.Text = "—";
            lblBatchStatus.Text = "—";
            lblMasterSyncStatus.Text = "Not completed";
            lblMasterSyncStatus.CssClass = "hrms-meta-value";
            lblRunMessage.Text = string.Empty;
            lblEmployeeActionsLocked.Visible = false;
            rblAction.ClearSelection();
            gvResult.DataSource = null;
            gvResult.DataBind();
        }

        protected void btnSyncMasters_Click(object sender, EventArgs e)
        {
            ExecuteIsolatedAction(ActionMaster);
        }

        protected void btnExecuteInsert_Click(object sender, EventArgs e)
        {
            ExecuteIsolatedAction(ActionInsert);
        }

        protected void btnExecuteUpdate_Click(object sender, EventArgs e)
        {
            ExecuteIsolatedAction(ActionUpdate);
        }

        protected void btnExecuteHierarchy_Click(object sender, EventArgs e)
        {
            ExecuteIsolatedAction(ActionHierarchy);
        }

        private bool TryGetSelectedBatchId(out int batchId)
        {
            batchId = 0;
            if (string.IsNullOrWhiteSpace(hfSelectedBatch.Value))
            {
                lblPageError.Text = "Please select a batch first.";
                return false;
            }

            if (!int.TryParse(hfSelectedBatch.Value, out batchId) || batchId <= 0)
            {
                lblPageError.Text = "The selected batch is invalid.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates, then runs exactly one orchestrator action. Never combines MASTER/INSERT/UPDATE/HIERARCHY.
        /// </summary>
        private void ExecuteIsolatedAction(string action)
        {
            lblPageError.Text = "";
            lblRunMessage.Text = "";
            lblRunMessage.CssClass = "font-weight-bold text-success";

            int batchId;
            if (!TryGetSelectedBatchId(out batchId))
            {
                ApplyActionUiState();
                return;
            }

            if (!BatchExists(batchId))
            {
                lblPageError.Text = "The selected batch was not found. No action was executed.";
                ApplyActionUiState();
                return;
            }

            if (action != ActionMaster && !IsMasterSyncCompleted(batchId))
            {
                lblPageError.Text = "Master synchronization must complete successfully for this batch before employee actions can run.";
                hfMasterSyncCompleted.Value = "0";
                LoadSelectedBatchDetails(batchId);
                LoadBatches();
                ApplyActionUiState();
                return;
            }

            if (action == ActionUpdate)
            {
                string selectedFields = GetSelectedOverwriteFields();
                if (string.IsNullOrWhiteSpace(selectedFields))
                {
                    lblPageError.Text = "Select at least one field to update.";
                    ApplyActionUiState();
                    return;
                }
            }

            int processingMode;
            string modules;
            string overwriteFields;
            GetActionParameters(action, out processingMode, out modules, out overwriteFields);

            try
            {
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

                if (!isValid)
                {
                    gvResult.DataSource = dtValidation;
                    gvResult.DataBind();

                    lblRunMessage.Text = string.Empty;
                    lblPageError.Text = string.IsNullOrWhiteSpace(validationMessage)
                        ? "Batch validation failed. The selected action was not executed. Review the validation summary and staging ErrorMessage values."
                        : validationMessage;

                    LoadSelectedBatchDetails(batchId);
                    LoadBatches();
                    ApplyActionUiState();
                    return;
                }

                DataTable dtResult = ExecuteOrchestrator(batchId, action, processingMode, modules, overwriteFields);
                gvResult.DataSource = dtResult;
                gvResult.DataBind();
                ApplyRunOutcomeMessage(dtResult, action);

                LoadSelectedBatchDetails(batchId);
                LoadBatches();
                ApplyActionUiState();
            }
            catch (Exception ex)
            {
                lblPageError.Text = ex.Message;
                lblRunMessage.Text = string.Empty;
                try
                {
                    LoadSelectedBatchDetails(batchId);
                    LoadBatches();
                }
                catch (Exception refreshEx)
                {
                    lblPageError.Text = ex.Message + " (Also failed to refresh batch details: " + refreshEx.Message + ")";
                }
                ApplyActionUiState();
            }
        }

        private static void GetActionParameters(
            string action,
            out int processingMode,
            out string modules,
            out string overwriteFields)
        {
            processingMode = 0;
            modules = action;
            overwriteFields = null;

            if (action == ActionMaster)
            {
                processingMode = 0;
                modules = ActionMaster;
            }
            else if (action == ActionInsert)
            {
                processingMode = 0;
                modules = "EMPLOYEE";
            }
            else if (action == ActionUpdate)
            {
                processingMode = 2;
                modules = "EMPLOYEE";
            }
            else if (action == ActionHierarchy)
            {
                processingMode = 0;
                modules = ActionHierarchy;
            }
        }

        private DataTable ExecuteOrchestrator(
            int batchId,
            string action,
            int processingMode,
            string modules,
            string overwriteFields)
        {
            string executedBy = Session["UserId"] != null ? Session["UserId"].ToString() : "SYSTEM";
            if (action == ActionUpdate)
                overwriteFields = GetSelectedOverwriteFields();

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            using (SqlCommand cmd = new SqlCommand("usp_RunHRMSSynchronization", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300;

                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.Parameters.AddWithValue("@AllowOverwrite", action == ActionUpdate ? 1 : 0);
                cmd.Parameters.AddWithValue("@ProcessingMode", processingMode);
                cmd.Parameters.AddWithValue("@Modules", string.IsNullOrWhiteSpace(modules) ? (object)DBNull.Value : modules);
                cmd.Parameters.AddWithValue(
                    "@OverwriteFields",
                    string.IsNullOrEmpty(overwriteFields) ? (object)DBNull.Value : overwriteFields);
                cmd.Parameters.AddWithValue("@ExecutedBy", executedBy);
                cmd.Parameters.AddWithValue("@Action", action);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private void ApplyRunOutcomeMessage(DataTable dtResult, string action)
        {
            if (dtResult == null || dtResult.Rows.Count == 0)
            {
                lblRunMessage.CssClass = "font-weight-bold text-danger";
                lblRunMessage.Text = "The action returned no result. Verify database execution.";
                return;
            }

            DataRow row = dtResult.Rows[0];
            int errorCount = 0;
            if (dtResult.Columns.Contains("ErrorCount") && row["ErrorCount"] != DBNull.Value)
                errorCount = Convert.ToInt32(row["ErrorCount"]);

            string status = dtResult.Columns.Contains("Status") && row["Status"] != DBNull.Value
                ? row["Status"].ToString()
                : string.Empty;

            string message = dtResult.Columns.Contains("Message") && row["Message"] != DBNull.Value
                ? row["Message"].ToString()
                : "Action completed.";

            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) || errorCount > 0)
            {
                lblRunMessage.CssClass = "font-weight-bold text-warning";
                lblRunMessage.Text = string.Format(
                    "{0} finished with errors (ErrorCount={1}, Status={2}). Review the result grid and batch report.",
                    DescribeAction(action),
                    errorCount,
                    string.IsNullOrEmpty(status) ? "unknown" : status);
            }
            else
            {
                lblRunMessage.CssClass = "font-weight-bold text-success";
                lblRunMessage.Text = string.IsNullOrWhiteSpace(message)
                    ? DescribeAction(action) + " completed successfully."
                    : message;
            }
        }

        private static string DescribeAction(string action)
        {
            if (action == ActionMaster) return "Master synchronization";
            if (action == ActionInsert) return "Insert new employees";
            if (action == ActionUpdate) return "Update existing employees";
            if (action == ActionHierarchy) return "Update reporting hierarchy";
            return "Action";
        }

        /// <summary>
        /// Executes dbo.usp_HSE_ValidateBatch and returns the OUTPUT IsValid flag plus summary result set.
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
                ApplyActionUiState();
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
