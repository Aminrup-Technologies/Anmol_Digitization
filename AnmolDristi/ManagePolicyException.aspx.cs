using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.BLL;
using ClosedXML.Excel;

namespace AnmolDristi
{
    public partial class ManagePolicyException : BasePage
    {
        private readonly PolicyExceptionService _service = new PolicyExceptionService();

        private bool? LoadedIsActive
        {
            get { return ViewState["LoadedIsActive"] as bool?; }
            set { ViewState["LoadedIsActive"] = value; }
        }

        private bool CanViewHistory
        {
            get { return HasPermission(PolicyExceptionService.HistoryPermissionKey); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindSearchDropdowns();
                BindGrid();
            }
        }

        private void BindSearchDropdowns()
        {
            BindMasterDropdown(ddlSearchRegion, "region", true);
            BindMasterDropdown(ddlSearchBranch, "branch", true);
            BindMasterDropdown(ddlSearchGrade, "grade", true);
            BindMasterDropdown(ddlSearchDesignation, "designation", true);
        }

        private void BindMasterDropdown(DropDownList ddl, string masterType, bool includeAll)
        {
            ddl.Items.Clear();

            // AppendDataBoundItems must be set before DataBind so "-- All --" is preserved.
            // Without it, DataBind replaces items and the first master Id becomes selected,
            // which unintentionally filters the search (often to zero rows).
            ddl.AppendDataBoundItems = includeAll;
            if (includeAll)
                ddl.Items.Add(new ListItem("-- All --", ""));

            var dt = _service.GetMasterList(masterType);
            ddl.DataSource = dt;
            ddl.DataTextField = "Description";
            ddl.DataValueField = "Id";
            ddl.DataBind();

            // Explicit default after every bind (initial load and any future rebind).
            if (includeAll)
                ddl.SelectedIndex = 0;
        }

        private PolicyExceptionSearchCriteria BuildSearchCriteria()
        {
            var criteria = new PolicyExceptionSearchCriteria
            {
                // Empty / whitespace text controls must remain "no filter" (null), not "".
                EmployeeCode = NormalizeOptionalText(txtSearchEmployeeCode.Text),
                EmployeeName = NormalizeOptionalText(txtSearchEmployeeName.Text),
                RegionId = ParseOptionalDropdownInt(ddlSearchRegion.SelectedValue),
                BranchId = ParseOptionalDropdownInt(ddlSearchBranch.SelectedValue),
                GradeId = ParseOptionalDropdownInt(ddlSearchGrade.SelectedValue),
                DesignationId = ParseOptionalDropdownInt(ddlSearchDesignation.SelectedValue)
            };

            string activeValue = NormalizeOptionalText(ddlSearchActive.SelectedValue);
            if (activeValue != null)
                criteria.IsActive = activeValue == "1";

            string effectiveDateText = NormalizeOptionalText(txtSearchEffectiveDate.Text);
            DateTime effDate;
            if (effectiveDateText != null && DateTime.TryParse(effectiveDateText, out effDate))
                criteria.EffectiveDate = effDate.Date;

            return criteria;
        }

        private void BindGrid()
        {
            gvExceptions.DataSource = _service.Search(BuildSearchCriteria());
            gvExceptions.DataBind();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            lblPageMessage.Text = string.Empty;
            BindGrid();
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearchEmployeeCode.Text = string.Empty;
            txtSearchEmployeeName.Text = string.Empty;
            ddlSearchRegion.SelectedIndex = 0;
            ddlSearchBranch.SelectedIndex = 0;
            ddlSearchGrade.SelectedIndex = 0;
            ddlSearchDesignation.SelectedIndex = 0;
            ddlSearchActive.SelectedIndex = 0;
            txtSearchEffectiveDate.Text = string.Empty;
            lblPageMessage.Text = string.Empty;
            BindGrid();
        }

        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            hfExceptionId.Value = "0";
            hfIsEditMode.Value = "0";
            lblModalTitle.Text = "Add Policy Exception";
            pnlAuditInfo.Visible = false;
            HideHistoryPanel();
            HideOperationalPanels();
            ConfigureChangeReasonUi(isEdit: false, readOnly: false);
            SetIsActiveForNewRecord();
            SetFormReadOnly(false);
            ShowModal();
        }

        protected void btnLoadEmployee_Click(object sender, EventArgs e)
        {
            lblFormMessage.Text = string.Empty;
            LoadEmployeeContext(txtEmployeeCode.Text.Trim());
            ShowModal();
        }

        private void LoadEmployeeContext(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                lblFormMessage.Text = "Enter an employee code.";
                return;
            }

            EmployeePolicyContext context = _service.GetEmployeeContext(employeeCode);
            if (context == null)
            {
                lblFormMessage.Text = "Employee not found.";
                return;
            }

            txtEmployeeCode.Text = context.EmployeeCode;
            txtEmployeeName.Text = context.EmployeeName;
            txtRegionName.Text = context.RegionName;
            txtBranchName.Text = context.BranchName;
            txtGradeName.Text = context.GradeName;
            txtDesignationName.Text = context.DesignationName;
            hfRegionId.Value = context.RegionId.HasValue ? context.RegionId.Value.ToString() : string.Empty;
            hfBranchId.Value = context.BranchId.HasValue ? context.BranchId.Value.ToString() : string.Empty;
            hfGradeId.Value = context.GradeId.HasValue ? context.GradeId.Value.ToString() : string.Empty;
            hfDesignationId.Value = context.DesignationId.HasValue ? context.DesignationId.Value.ToString() : string.Empty;
        }

        protected void gvExceptions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int exceptionId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out exceptionId)) return;

            if (e.CommandName == "EditRow")
            {
                LoadForm(exceptionId, false);
                lblModalTitle.Text = "Edit Policy Exception";
                hfIsEditMode.Value = "1";
                HideHistoryPanel();
                ConfigureChangeReasonUi(isEdit: true, readOnly: false);
                SetFormReadOnly(false);
                ShowModal();
            }
            else if (e.CommandName == "ViewRow")
            {
                LoadForm(exceptionId, true);
                lblModalTitle.Text = "View Policy Exception";
                hfIsEditMode.Value = "0";
                ConfigureChangeReasonUi(isEdit: false, readOnly: true);
                SetFormReadOnly(true);
                ShowModal();
            }
            else if (e.CommandName == "ActivateRow" || e.CommandName == "DeactivateRow")
            {
                string error;
                _service.SetActiveStatus(exceptionId, e.CommandName == "ActivateRow", GetCurrentUserLogin(), out error);
                lblPageMessage.Text = !string.IsNullOrEmpty(error) ? error : string.Empty;
                BindGrid();
            }
        }

        protected void gvExceptions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var litExpiry = e.Row.FindControl("litExpiryAlert") as Literal;
            if (litExpiry == null) return;

            object isActiveObj = DataBinder.Eval(e.Row.DataItem, "IsActive");
            object toObj = DataBinder.Eval(e.Row.DataItem, "EffectiveTo");
            bool isActive = isActiveObj != null && isActiveObj != DBNull.Value && Convert.ToBoolean(isActiveObj);
            DateTime? effectiveTo = toObj == null || toObj == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(toObj);
            litExpiry.Text = GetExpiryAlertHtml(isActive, effectiveTo);
        }

        protected void gvHistory_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Status badge is rendered via template Eval helpers; keep hook for future styling.
        }

        private void LoadForm(int exceptionId, bool readOnly)
        {
            lblFormMessage.Text = string.Empty;
            PolicyExceptionRecord record = _service.GetById(exceptionId);
            if (record == null)
            {
                lblFormMessage.Text = "Policy exception not found.";
                return;
            }

            hfExceptionId.Value = record.ExceptionId.ToString();
            txtEmployeeCode.Text = record.EmployeeCode;
            txtEmployeeName.Text = record.EmployeeName;
            txtRegionName.Text = record.RegionName;
            txtBranchName.Text = record.BranchName;
            txtGradeName.Text = record.GradeName;
            txtDesignationName.Text = record.DesignationName;
            hfRegionId.Value = record.RegionId.HasValue ? record.RegionId.Value.ToString() : string.Empty;
            hfBranchId.Value = record.BranchId.HasValue ? record.BranchId.Value.ToString() : string.Empty;
            hfGradeId.Value = record.GradeId.HasValue ? record.GradeId.Value.ToString() : string.Empty;
            hfDesignationId.Value = record.DesignationId.HasValue ? record.DesignationId.Value.ToString() : string.Empty;

            txtPriority.Text = record.Priority.ToString();
            txtEffectiveFrom.Text = record.EffectiveFrom.ToString("yyyy-MM-dd");
            txtEffectiveTo.Text = record.EffectiveTo.HasValue ? record.EffectiveTo.Value.ToString("yyyy-MM-dd") : string.Empty;
            chkIsActive.Checked = record.IsActive;
            LoadedIsActive = record.IsActive;
            lblIsActiveNote.Visible = false;

            PolicyExceptionDto dto = PolicyExceptionSerializer.Deserialize(record.PolicyJson);
            txtMaxKM.Text = FormatDecimal(dto.MaxKM);
            txtRatePerKm.Text = FormatDecimal(dto.RatePerKm);
            txtDA.Text = FormatDecimal(dto.DA);
            txtOutstationDA.Text = FormatDecimal(dto.OutstationDA);
            txtOwnAllowance.Text = FormatDecimal(dto.OwnAllowance);
            txtHotelCap.Text = FormatDecimal(dto.HotelCap);
            txtRemarks.Text = dto.Remarks ?? string.Empty;

            pnlAuditInfo.Visible = true;
            lblCreatedBy.Text = record.CreatedBy ?? "—";
            lblCreatedOn.Text = record.CreatedOn.HasValue ? record.CreatedOn.Value.ToString("dd-MMM-yyyy HH:mm") : "—";
            lblModifiedBy.Text = record.ModifiedBy ?? "—";
            lblModifiedOn.Text = record.ModifiedOn.HasValue ? record.ModifiedOn.Value.ToString("dd-MMM-yyyy HH:mm") : "—";

            txtEmployeeCode.ReadOnly = readOnly || record.ExceptionId > 0;
            btnLoadEmployee.Enabled = !readOnly && record.ExceptionId <= 0;

            ConfigureChangeReasonUi(isEdit: !readOnly && record.ExceptionId > 0, readOnly: readOnly);

            int? historyCount = null;
            int? currentVersion = null;
            if (CanViewHistory)
            {
                DataTable historyNewestFirst = _service.GetHistory(exceptionId);
                if (historyNewestFirst != null && historyNewestFirst.Rows.Count > 0)
                {
                    historyCount = historyNewestFirst.Rows.Count;
                    currentVersion = Convert.ToInt32(historyNewestFirst.Rows[0]["VersionNo"]);
                }
                else
                {
                    historyCount = 0;
                }
            }

            BindOperationalUi(record, dto, historyCount, currentVersion);

            if (readOnly)
                BindHistory(exceptionId);
            else
                HideHistoryPanel();
        }

        private void BindOperationalUi(PolicyExceptionRecord record, PolicyExceptionDto dto, int? historyCount, int? currentVersion)
        {
            if (record == null)
            {
                HideOperationalPanels();
                return;
            }

            string statusHtml = GetStatusBadgeHtml(record.IsActive, record.EffectiveFrom, record.EffectiveTo);
            litPolicyStatus.Text = statusHtml;
            litEffectivePeriod.Text = BuildEffectivePeriodHtml(record.IsActive, record.EffectiveFrom, record.EffectiveTo);
            pnlEffectivePeriod.Visible = true;

            BindOverrideIndicators(dto);

            pnlSummary.Visible = true;
            lblSummaryEmployee.Text = string.IsNullOrWhiteSpace(record.EmployeeCode) ? "—" : record.EmployeeCode;
            litSummaryStatus.Text = statusHtml;
            lblSummaryPriority.Text = record.Priority.ToString(CultureInfo.InvariantCulture);
            lblSummaryEffective.Text = record.EffectiveFrom.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
            lblSummaryExpiry.Text = record.EffectiveTo.HasValue
                ? record.EffectiveTo.Value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
                : "—";
            lblSummaryLastUpdated.Text = record.ModifiedOn.HasValue
                ? record.ModifiedOn.Value.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture)
                : (record.CreatedOn.HasValue ? record.CreatedOn.Value.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture) : "—");
            lblSummaryUpdatedBy.Text = !string.IsNullOrWhiteSpace(record.ModifiedBy)
                ? record.ModifiedBy
                : (!string.IsNullOrWhiteSpace(record.CreatedBy) ? record.CreatedBy : "—");
            lblSummaryVersion.Text = currentVersion.HasValue ? currentVersion.Value.ToString(CultureInfo.InvariantCulture) : "—";
            lblSummaryHistoryCount.Text = historyCount.HasValue ? historyCount.Value.ToString(CultureInfo.InvariantCulture) : "—";

            string expiryAlert = GetExpiryAlertHtml(record.IsActive, record.EffectiveTo);
            if (!string.IsNullOrEmpty(expiryAlert))
                litEffectivePeriod.Text += "<div class=\"pe-expiry-alert\" style=\"margin-top:8px;\">" + expiryAlert + "</div>";
        }

        private void BindOverrideIndicators(PolicyExceptionDto dto)
        {
            litOverrideDA.Text = GetOverrideBadgeHtml(dto != null && dto.DA.HasValue);
            litOverrideOutstationDA.Text = GetOverrideBadgeHtml(dto != null && dto.OutstationDA.HasValue);
            litOverrideOwnAllowance.Text = GetOverrideBadgeHtml(dto != null && dto.OwnAllowance.HasValue);
            litOverrideHotelCap.Text = GetOverrideBadgeHtml(dto != null && dto.HotelCap.HasValue);
            litOverrideRatePerKm.Text = GetOverrideBadgeHtml(dto != null && dto.RatePerKm.HasValue);
            litOverrideMaxKM.Text = GetOverrideBadgeHtml(dto != null && dto.MaxKM.HasValue);
        }

        private void HideOperationalPanels()
        {
            pnlSummary.Visible = false;
            pnlEffectivePeriod.Visible = false;
            litEffectivePeriod.Text = string.Empty;
            litPolicyStatus.Text = string.Empty;
            litSummaryStatus.Text = string.Empty;
            litOverrideDA.Text = litOverrideOutstationDA.Text = litOverrideOwnAllowance.Text =
                litOverrideHotelCap.Text = litOverrideRatePerKm.Text = litOverrideMaxKM.Text = string.Empty;
        }

        private void ConfigureChangeReasonUi(bool isEdit, bool readOnly)
        {
            pnlChangeReason.Visible = !readOnly;
            txtChangeReason.Text = string.Empty;
            rfvChangeReason.Enabled = isEdit && !readOnly;

            if (readOnly)
            {
                lblChangeReasonHint.Text = string.Empty;
                return;
            }

            if (isEdit)
            {
                lblChangeReasonHint.Text = "Required for updates. This reason is stored on the new history version only.";
                txtChangeReason.Attributes["placeholder"] = "Reason for this update (required)";
            }
            else
            {
                lblChangeReasonHint.Text = "Optional. Defaults to \"" + PolicyExceptionService.DefaultCreateReason + "\" when left blank.";
                txtChangeReason.Attributes["placeholder"] = PolicyExceptionService.DefaultCreateReason;
            }
        }

        private void BindHistory(int exceptionId)
        {
            if (!CanViewHistory)
            {
                HideHistoryPanel();
                // Summary version/count unknown without history permission.
                return;
            }

            DataTable historyNewestFirst = _service.GetHistory(exceptionId);
            pnlHistory.Visible = true;
            lblHistoryVersionNote.Text = "Showing current values. Select a version to view a historical snapshot (read-only).";
            lblCompareMessage.Text = string.Empty;
            pnlCompareResults.Visible = false;

            gvHistory.DataSource = historyNewestFirst;
            gvHistory.DataBind();

            DataView timelineView = historyNewestFirst.DefaultView;
            timelineView.Sort = "VersionNo ASC";
            DataTable timeline = timelineView.ToTable();
            bool hasHistory = timeline.Rows.Count > 0;
            rptHistoryTimeline.DataSource = timeline;
            rptHistoryTimeline.DataBind();
            rptHistoryTimeline.Visible = hasHistory;
            lblTimelineEmpty.Visible = !hasHistory;

            BindCompareDropdowns(timeline);
            btnExportHistory.Enabled = hasHistory;

            int historyCount = timeline.Rows.Count;
            int? currentVersion = null;
            if (historyCount > 0)
                currentVersion = Convert.ToInt32(historyNewestFirst.Rows[0]["VersionNo"]);

            lblSummaryVersion.Text = currentVersion.HasValue ? currentVersion.Value.ToString(CultureInfo.InvariantCulture) : "—";
            lblSummaryHistoryCount.Text = historyCount.ToString(CultureInfo.InvariantCulture);
        }

        private void BindCompareDropdowns(DataTable timelineOldestFirst)
        {
            ddlCompareVersionA.Items.Clear();
            ddlCompareVersionB.Items.Clear();

            if (timelineOldestFirst == null || timelineOldestFirst.Rows.Count == 0)
            {
                ddlCompareVersionA.Items.Add(new ListItem("-- None --", ""));
                ddlCompareVersionB.Items.Add(new ListItem("-- None --", ""));
                btnCompareHistory.Enabled = false;
                return;
            }

            foreach (DataRow row in timelineOldestFirst.Rows)
            {
                string version = Convert.ToString(row["VersionNo"]);
                string action = Convert.ToString(row["ActionType"]);
                string text = "Version " + version + " (" + action + ")";
                ddlCompareVersionA.Items.Add(new ListItem(text, version));
                ddlCompareVersionB.Items.Add(new ListItem(text, version));
            }

            ddlCompareVersionA.SelectedIndex = 0;
            ddlCompareVersionB.SelectedIndex = timelineOldestFirst.Rows.Count > 1
                ? timelineOldestFirst.Rows.Count - 1
                : 0;
            btnCompareHistory.Enabled = timelineOldestFirst.Rows.Count >= 2;
        }

        private void HideHistoryPanel()
        {
            pnlHistory.Visible = false;
            lblHistoryVersionNote.Text = string.Empty;
            lblCompareMessage.Text = string.Empty;
            pnlCompareResults.Visible = false;
            gvHistory.DataSource = null;
            gvHistory.DataBind();
            rptHistoryTimeline.DataSource = null;
            rptHistoryTimeline.DataBind();
            lblTimelineEmpty.Visible = false;
            ddlCompareVersionA.Items.Clear();
            ddlCompareVersionB.Items.Clear();
        }

        protected void gvHistory_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ViewHistoryVersion") return;
            ViewHistoryVersion(Convert.ToString(e.CommandArgument));
        }

        protected void rptHistoryTimeline_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "ViewHistoryVersion") return;
            ViewHistoryVersion(Convert.ToString(e.CommandArgument));
        }

        private void ViewHistoryVersion(string versionArgument)
        {
            if (!CanViewHistory) return;

            int exceptionId;
            int versionNo;
            if (!int.TryParse(hfExceptionId.Value, out exceptionId) || exceptionId <= 0) return;
            if (!int.TryParse(versionArgument, out versionNo) || versionNo <= 0) return;

            PolicyExceptionHistoryRecord history = _service.GetHistoryVersion(exceptionId, versionNo);
            if (history == null)
            {
                lblFormMessage.Text = "History version not found.";
                ShowModal();
                return;
            }

            ApplyHistorySnapshot(history);
            BindHistory(exceptionId);
            lblSummaryVersion.Text = history.VersionNo.ToString(CultureInfo.InvariantCulture);
            lblHistoryVersionNote.Text = string.Format(
                "Viewing historical version {0} ({1}) changed by {2} on {3:dd-MMM-yyyy HH:mm}. Reason: {4}. Read-only.",
                history.VersionNo,
                history.ActionType,
                string.IsNullOrWhiteSpace(history.ChangedBy) ? "—" : history.ChangedBy,
                history.ChangedOn,
                string.IsNullOrWhiteSpace(history.ChangeReason) ? "—" : history.ChangeReason);
            ConfigureChangeReasonUi(isEdit: false, readOnly: true);
            SetFormReadOnly(true);
            ShowModal();
        }

        private void ApplyHistorySnapshot(PolicyExceptionHistoryRecord history)
        {
            txtPriority.Text = history.Priority.ToString();
            txtEffectiveFrom.Text = history.EffectiveFrom.ToString("yyyy-MM-dd");
            txtEffectiveTo.Text = history.EffectiveTo.HasValue ? history.EffectiveTo.Value.ToString("yyyy-MM-dd") : string.Empty;
            chkIsActive.Checked = history.IsActive;

            PolicyExceptionDto dto = PolicyExceptionSerializer.Deserialize(history.PolicyJson);
            txtMaxKM.Text = FormatDecimal(dto.MaxKM);
            txtRatePerKm.Text = FormatDecimal(dto.RatePerKm);
            txtDA.Text = FormatDecimal(dto.DA);
            txtOutstationDA.Text = FormatDecimal(dto.OutstationDA);
            txtOwnAllowance.Text = FormatDecimal(dto.OwnAllowance);
            txtHotelCap.Text = FormatDecimal(dto.HotelCap);
            txtRemarks.Text = dto.Remarks ?? string.Empty;

            // Refresh operational display for the snapshot (status / period / overrides).
            var snapshotRecord = new PolicyExceptionRecord
            {
                EmployeeCode = txtEmployeeCode.Text.Trim(),
                Priority = history.Priority,
                EffectiveFrom = history.EffectiveFrom,
                EffectiveTo = history.EffectiveTo,
                IsActive = history.IsActive,
                ModifiedBy = history.ChangedBy,
                ModifiedOn = history.ChangedOn
            };
            BindOperationalUi(snapshotRecord, dto, historyCount: null, currentVersion: history.VersionNo);
        }

        protected void btnCompareHistory_Click(object sender, EventArgs e)
        {
            if (!CanViewHistory) return;

            int exceptionId;
            int versionA;
            int versionB;
            if (!int.TryParse(hfExceptionId.Value, out exceptionId) || exceptionId <= 0)
            {
                lblCompareMessage.Text = "Exception context is missing.";
                ShowModal();
                return;
            }

            if (!int.TryParse(ddlCompareVersionA.SelectedValue, out versionA) ||
                !int.TryParse(ddlCompareVersionB.SelectedValue, out versionB))
            {
                lblCompareMessage.Text = "Select two versions to compare.";
                ShowModal();
                return;
            }

            if (versionA == versionB)
            {
                lblCompareMessage.Text = "Select two different versions.";
                pnlCompareResults.Visible = false;
                ShowModal();
                return;
            }

            var compareRows = _service.CompareHistoryVersions(exceptionId, versionA, versionB);
            if (compareRows.Count == 0)
            {
                lblCompareMessage.Text = "Unable to load one or both versions.";
                pnlCompareResults.Visible = false;
                ShowModal();
                return;
            }

            lblCompareMessage.Text = string.Empty;
            gvHistoryCompare.Columns[1].HeaderText = "Version " + versionA;
            gvHistoryCompare.Columns[2].HeaderText = "Version " + versionB;
            gvHistoryCompare.DataSource = compareRows;
            gvHistoryCompare.DataBind();
            pnlCompareResults.Visible = true;
            SetFormReadOnly(true);
            ShowModal();
        }

        protected void gvHistoryCompare_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var row = e.Row.DataItem as PolicyExceptionHistoryCompareRow;
            if (row == null) return;
            e.Row.CssClass = row.RowCssClass;
        }

        protected void btnExportHistory_Click(object sender, EventArgs e)
        {
            if (!CanViewHistory) return;

            int exceptionId;
            if (!int.TryParse(hfExceptionId.Value, out exceptionId) || exceptionId <= 0)
            {
                lblFormMessage.Text = "Exception context is missing.";
                ShowModal();
                return;
            }

            DataTable export = _service.BuildHistoryExportTable(exceptionId);
            if (export.Rows.Count == 0)
            {
                lblHistoryVersionNote.Text = "No history available to export.";
                ShowModal();
                return;
            }

            string fileName = "PolicyException_" + exceptionId + "_History_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
            using (var workbook = new XLWorkbook())
            {
                workbook.Worksheets.Add(export, "History");
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=" + fileName);
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) { ShowModal(); return; }

            lblFormMessage.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(txtEmployeeName.Text))
                LoadEmployeeContext(txtEmployeeCode.Text.Trim());

            int exceptionId;
            int.TryParse(hfExceptionId.Value, out exceptionId);

            int priority;
            if (!int.TryParse(txtPriority.Text.Trim(), out priority))
            {
                lblFormMessage.Text = "Priority must be a valid number.";
                ShowModal();
                return;
            }

            DateTime effectiveFrom;
            if (!DateTime.TryParse(txtEffectiveFrom.Text.Trim(), out effectiveFrom))
            {
                lblFormMessage.Text = "Effective From is invalid.";
                ShowModal();
                return;
            }

            DateTime? effectiveTo = null;
            DateTime parsedTo;
            if (!string.IsNullOrWhiteSpace(txtEffectiveTo.Text) && DateTime.TryParse(txtEffectiveTo.Text.Trim(), out parsedTo))
                effectiveTo = parsedTo.Date;

            var record = new PolicyExceptionRecord
            {
                ExceptionId = exceptionId,
                EmployeeCode = txtEmployeeCode.Text.Trim(),
                Priority = priority,
                EffectiveFrom = effectiveFrom.Date,
                EffectiveTo = effectiveTo
            };

            var policyDto = new PolicyExceptionDto
            {
                MaxKM = ParseNullableDecimal(txtMaxKM.Text),
                RatePerKm = ParseNullableDecimal(txtRatePerKm.Text),
                DA = ParseNullableDecimal(txtDA.Text),
                OutstationDA = ParseNullableDecimal(txtOutstationDA.Text),
                OwnAllowance = ParseNullableDecimal(txtOwnAllowance.Text),
                HotelCap = ParseNullableDecimal(txtHotelCap.Text),
                Remarks = txtRemarks.Text.Trim()
            };

            string changeReason = NormalizeOptionalText(txtChangeReason.Text);
            string error;
            int savedId = _service.Save(record, policyDto, GetCurrentUserLogin(), out error, changeReason);
            if (!string.IsNullOrEmpty(error))
            {
                lblFormMessage.Text = error;
                ShowModal();
                return;
            }

            if (exceptionId > 0 && LoadedIsActive.HasValue && LoadedIsActive.Value != chkIsActive.Checked)
            {
                string statusError;
                _service.SetActiveStatus(savedId, chkIsActive.Checked, GetCurrentUserLogin(), out statusError);
                if (!string.IsNullOrEmpty(statusError))
                {
                    lblFormMessage.Text = "Record saved, but active status could not be updated: " + statusError;
                    ShowModal();
                    BindGrid();
                    return;
                }
            }

            if (savedId > 0) hfExceptionId.Value = savedId.ToString();

            BindGrid();
            HideModal();
        }

        private void ClearForm()
        {
            lblFormMessage.Text = string.Empty;
            txtEmployeeCode.Text = string.Empty;
            txtEmployeeName.Text = string.Empty;
            txtRegionName.Text = string.Empty;
            txtBranchName.Text = string.Empty;
            txtGradeName.Text = string.Empty;
            txtDesignationName.Text = string.Empty;
            hfRegionId.Value = hfBranchId.Value = hfGradeId.Value = hfDesignationId.Value = string.Empty;
            txtPriority.Text = "1";
            txtEffectiveFrom.Text = DateTime.Today.ToString("yyyy-MM-dd");
            txtEffectiveTo.Text = string.Empty;
            chkIsActive.Checked = true;
            LoadedIsActive = null;
            txtMaxKM.Text = txtRatePerKm.Text = txtDA.Text = txtOutstationDA.Text = txtOwnAllowance.Text = txtHotelCap.Text = txtRemarks.Text = string.Empty;
            txtChangeReason.Text = string.Empty;
            txtEmployeeCode.ReadOnly = false;
            btnLoadEmployee.Enabled = true;
            HideHistoryPanel();
            HideOperationalPanels();
            SetIsActiveForNewRecord();
        }

        private void SetIsActiveForNewRecord()
        {
            chkIsActive.Checked = true;
            chkIsActive.Enabled = false;
            lblIsActiveNote.Visible = true;
        }

        private void SetFormReadOnly(bool readOnly)
        {
            txtMaxKM.ReadOnly = readOnly;
            txtRatePerKm.ReadOnly = readOnly;
            txtDA.ReadOnly = readOnly;
            txtOutstationDA.ReadOnly = readOnly;
            txtOwnAllowance.ReadOnly = readOnly;
            txtHotelCap.ReadOnly = readOnly;
            txtRemarks.ReadOnly = readOnly;
            txtPriority.ReadOnly = readOnly;
            txtEffectiveFrom.ReadOnly = readOnly;
            txtEffectiveTo.ReadOnly = readOnly;
            txtChangeReason.ReadOnly = readOnly;

            int exceptionId;
            if (!int.TryParse(hfExceptionId.Value, out exceptionId) || exceptionId <= 0)
                SetIsActiveForNewRecord();
            else
                chkIsActive.Enabled = !readOnly;

            btnSave.Visible = !readOnly;
            pnlReadOnlyBanner.Visible = readOnly;
            lblReadOnlyBadge.Visible = readOnly;
            ScriptManager.RegisterStartupScript(this, GetType(), "setReadOnly", "setPolicyExceptionReadOnly(" + (readOnly ? "true" : "false") + ");", true);
        }

        #region Status / period / override helpers (UI only)

        protected string GetToggleConfirmScript(object isActiveObj)
        {
            bool isActive = isActiveObj != null && isActiveObj != DBNull.Value && Convert.ToBoolean(isActiveObj);
            if (isActive)
            {
                return "return confirm('This Policy Exception is currently active.\\n\\n"
                    + "Claims submitted after deactivation will use the standard policy unless another exception exists.\\n\\n"
                    + "Proceed?');";
            }

            return "return confirm('Activate this Policy Exception?');";
        }

        protected string GetStatusBadgeHtml(object isActiveObj, object effectiveFromObj, object effectiveToObj)
        {
            bool isActive = isActiveObj != null && isActiveObj != DBNull.Value && Convert.ToBoolean(isActiveObj);
            DateTime effectiveFrom = effectiveFromObj == null || effectiveFromObj == DBNull.Value
                ? DateTime.MinValue
                : Convert.ToDateTime(effectiveFromObj).Date;
            DateTime? effectiveTo = effectiveToObj == null || effectiveToObj == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(effectiveToObj).Date;

            string status = DerivePolicyStatus(isActive, effectiveFrom, effectiveTo);
            string css;
            string label;
            switch (status)
            {
                case "Active":
                    css = "pe-status-badge pe-status-active";
                    label = "🟢 Active";
                    break;
                case "Future":
                    css = "pe-status-badge pe-status-future";
                    label = "🟡 Future";
                    break;
                case "Expired":
                    css = "pe-status-badge pe-status-expired";
                    label = "🔴 Expired";
                    break;
                default:
                    css = "pe-status-badge pe-status-inactive";
                    label = "⚪ Inactive";
                    break;
            }

            return "<span class=\"" + css + "\">" + label + "</span>";
        }

        private static string DerivePolicyStatus(bool isActive, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            if (!isActive) return "Inactive";

            DateTime today = DateTime.Today;
            if (effectiveFrom > today) return "Future";
            if (effectiveTo.HasValue && effectiveTo.Value.Date < today) return "Expired";
            return "Active";
        }

        private static string GetOverrideBadgeHtml(bool overridden)
        {
            if (overridden)
                return "<span class=\"pe-override-badge pe-override-yes\">🟢 Overridden</span>";
            return "<span class=\"pe-override-badge pe-override-no\">⚪ Inherited</span>";
        }

        private static string BuildEffectivePeriodHtml(bool isActive, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            DateTime today = DateTime.Today;
            string fromText = effectiveFrom.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
            string toText = effectiveTo.HasValue
                ? effectiveTo.Value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
                : "Open-ended";

            string title;
            if (!isActive)
                title = "Inactive";
            else if (effectiveFrom.Date > today)
                title = "Starts";
            else if (effectiveTo.HasValue && effectiveTo.Value.Date < today)
                title = "Expired";
            else
                title = "Currently Effective";

            if (string.Equals(title, "Starts", StringComparison.OrdinalIgnoreCase))
            {
                return "<div class=\"pe-period-title\">" + title + "</div>"
                    + "<div class=\"pe-period-date\">" + fromText + "</div>";
            }

            if (string.Equals(title, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                return "<div class=\"pe-period-title\">" + title + "</div>"
                    + "<div class=\"pe-period-date\">" + toText + "</div>";
            }

            return "<div class=\"pe-period-title\">" + title + "</div>"
                + "<div class=\"pe-period-date\">" + fromText + "</div>"
                + "<div class=\"pe-period-arrow\">↓</div>"
                + "<div class=\"pe-period-date\">" + toText + "</div>";
        }

        private static string GetExpiryAlertHtml(bool isActive, DateTime? effectiveTo)
        {
            if (!isActive || !effectiveTo.HasValue) return string.Empty;

            DateTime today = DateTime.Today;
            DateTime to = effectiveTo.Value.Date;
            if (to < today) return string.Empty;

            int days = (to - today).Days;
            if (days > 30) return string.Empty;

            if (days == 0)
                return "<div class=\"pe-expiry-alert\">⚠ Expires today</div>";
            if (days == 1)
                return "<div class=\"pe-expiry-alert\">⚠ Expires in 1 day</div>";
            return "<div class=\"pe-expiry-alert\">⚠ Expires in " + days + " days</div>";
        }

        #endregion

        private static string NormalizeOptionalText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }

        /// <summary>
        /// Parses an optional dropdown filter value. Null, empty, and whitespace mean "no filter".
        /// </summary>
        private static int? ParseOptionalDropdownInt(string value)
        {
            string trimmed = NormalizeOptionalText(value);
            if (trimmed == null) return null;

            int tmp;
            return int.TryParse(trimmed, out tmp) ? (int?)tmp : null;
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            decimal tmp;
            return decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tmp) ? (decimal?)tmp : null;
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        private string GetCurrentUserLogin()
        {
            return Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : "SYSTEM";
        }

        private void ShowModal()
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "showPolicyExceptionModal", "showPolicyExceptionModal();", true);
        }

        private void HideModal()
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "hidePolicyExceptionModal", "$('#modalPolicyException').modal('hide');", true);
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<EmployeeSuggestionDto> SuggestEmployees(string term)
        {
            var results = new List<EmployeeSuggestionDto>();
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 1)
                return results;

            var service = new PolicyExceptionService();
            DataTable dt = service.SuggestEmployees(term.Trim(), 10);
            if (dt == null) return results;

            foreach (DataRow row in dt.Rows)
            {
                results.Add(new EmployeeSuggestionDto
                {
                    EmployeeCode = Convert.ToString(row["EmployeeCode"]),
                    EmployeeName = row.Table.Columns.Contains("EmployeeName") && row["EmployeeName"] != DBNull.Value
                        ? Convert.ToString(row["EmployeeName"])
                        : string.Empty
                });
            }

            return results;
        }
    }

    public class EmployeeSuggestionDto
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
    }
}
