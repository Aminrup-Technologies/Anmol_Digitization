using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.BLL;

namespace AnmolDristi
{
    public partial class ManagePolicyException : BasePage
    {
        private readonly PolicyExceptionService _service = new PolicyExceptionService();

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
            BindMasterDropdown(ddlSearchDesignation, "designation", true);
        }

        private void BindMasterDropdown(DropDownList ddl, string masterType, bool includeAll)
        {
            ddl.Items.Clear();
            if (includeAll) ddl.Items.Add(new ListItem("-- All --", ""));

            var dt = _service.GetMasterList(masterType);
            ddl.DataSource = dt;
            ddl.DataTextField = "Description";
            ddl.DataValueField = "Id";
            ddl.DataBind();
        }

        private PolicyExceptionSearchCriteria BuildSearchCriteria()
        {
            var criteria = new PolicyExceptionSearchCriteria
            {
                EmployeeCode = txtSearchEmployeeCode.Text.Trim(),
                EmployeeName = txtSearchEmployeeName.Text.Trim()
            };

            int tmp;
            if (int.TryParse(ddlSearchRegion.SelectedValue, out tmp)) criteria.RegionId = tmp;
            if (int.TryParse(ddlSearchBranch.SelectedValue, out tmp)) criteria.BranchId = tmp;
            if (int.TryParse(ddlSearchDesignation.SelectedValue, out tmp)) criteria.DesignationId = tmp;

            if (!string.IsNullOrWhiteSpace(ddlSearchActive.SelectedValue))
                criteria.IsActive = ddlSearchActive.SelectedValue == "1";

            DateTime effDate;
            if (DateTime.TryParse(txtSearchEffectiveDate.Text, out effDate))
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
            BindGrid();
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearchEmployeeCode.Text = string.Empty;
            txtSearchEmployeeName.Text = string.Empty;
            ddlSearchRegion.SelectedIndex = 0;
            ddlSearchBranch.SelectedIndex = 0;
            ddlSearchDesignation.SelectedIndex = 0;
            ddlSearchActive.SelectedIndex = 0;
            txtSearchEffectiveDate.Text = string.Empty;
            BindGrid();
        }

        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            hfExceptionId.Value = "0";
            lblModalTitle.Text = "Add Policy Exception";
            pnlAuditInfo.Visible = false;
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
                SetFormReadOnly(false);
                ShowModal();
            }
            else if (e.CommandName == "ViewRow")
            {
                LoadForm(exceptionId, true);
                lblModalTitle.Text = "View Policy Exception";
                SetFormReadOnly(true);
                ShowModal();
            }
            else if (e.CommandName == "ActivateRow" || e.CommandName == "DeactivateRow")
            {
                string error;
                _service.SetActiveStatus(exceptionId, e.CommandName == "ActivateRow", GetCurrentUserLogin(), out error);
                if (!string.IsNullOrEmpty(error))
                    lblFormMessage.Text = error;
                BindGrid();
            }
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
                EmployeeName = txtEmployeeName.Text.Trim(),
                RegionId = ParseNullableInt(hfRegionId.Value),
                BranchId = ParseNullableInt(hfBranchId.Value),
                GradeId = ParseNullableInt(hfGradeId.Value),
                DesignationId = ParseNullableInt(hfDesignationId.Value),
                Priority = priority,
                EffectiveFrom = effectiveFrom.Date,
                EffectiveTo = effectiveTo,
                IsActive = chkIsActive.Checked
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

            string error;
            int savedId = _service.Save(record, policyDto, GetCurrentUserLogin(), out error);
            if (!string.IsNullOrEmpty(error))
            {
                lblFormMessage.Text = error;
                ShowModal();
                return;
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
            txtMaxKM.Text = txtRatePerKm.Text = txtDA.Text = txtOutstationDA.Text = txtOwnAllowance.Text = txtHotelCap.Text = txtRemarks.Text = string.Empty;
            txtEmployeeCode.ReadOnly = false;
            btnLoadEmployee.Enabled = true;
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
            chkIsActive.Enabled = !readOnly;
            btnSave.Visible = !readOnly;
            ScriptManager.RegisterStartupScript(this, GetType(), "setReadOnly", "setPolicyExceptionReadOnly(" + (readOnly ? "true" : "false") + ");", true);
        }

        private static int? ParseNullableInt(string value)
        {
            int tmp;
            return int.TryParse(value, out tmp) ? (int?)tmp : null;
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
    }
}
