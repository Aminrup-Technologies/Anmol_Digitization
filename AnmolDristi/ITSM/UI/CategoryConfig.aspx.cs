// ========================================================================
// When: 2026-11-25
// What: Category administration page. Session gate plus
//       ITSM.CategoryConfig.Manage on load and on every add or save.
// Why:  Phase 1B R1. The catalog is global. This page does not create
//       tickets and does not write ticket status or audit rows.
// When: 2026-11-26
// What: Page_Load requires USERID and the login branch id. Save reads
//       the order box. The browser filter is not a server event.
//       Support agent mapping uses the same permission and session keys.
// Why:  Display order is saved only by the existing Save and Add buttons.
//       Search must not post back. BRANCH is the BranchId from login;
//       this application has no Session key named BranchID. Mapping
//       writes stay on the signed-in branch and company.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    public partial class CategoryConfig : BasePage
    {
        private const string ManagePermission = "ITSM.CategoryConfig.Manage";

        private List<CatalogOptionDto> _parentCategoryOptions = new List<CatalogOptionDto>();
        private List<CatalogOptionDto> _categoryOptions = new List<CatalogOptionDto>();
        private List<MappingEmployeeDto> _mappingEmployees = new List<MappingEmployeeDto>();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Catalog lists are not stored in ViewState. GridView keeps the
            // posted row editors (id, name, order, active, parent id).
            if (!HasSignedInUserAndBranch())
            {
                RedirectToLogin();
                return;
            }

            if (!DemandManagePermission())
            {
                return;
            }

            if (!IsPostBack)
            {
                BindAll();
            }
        }

        protected void btnAddParent_Click(object sender, EventArgs e)
        {
            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            var result = new CategoryAdministrationService()
                .AddParentCategory(txtNewParentName.Text, CurrentEmployeeCode);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, false);
                return;
            }

            txtNewParentName.Text = string.Empty;
            ShowMessage("Parent category added.", true);
            BindAll();
        }

        protected void btnAddCategory_Click(object sender, EventArgs e)
        {
            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            int parentId;
            if (!int.TryParse(ddlNewCategoryParent.SelectedValue, out parentId))
            {
                ShowMessage("Select a parent category.", false);
                return;
            }

            var result = new CategoryAdministrationService()
                .AddCategory(parentId, txtNewCategoryName.Text, CurrentEmployeeCode);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, false);
                return;
            }

            txtNewCategoryName.Text = string.Empty;
            ShowMessage("Category added.", true);
            BindAll();
        }

        protected void btnAddSubCategory_Click(object sender, EventArgs e)
        {
            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            int categoryId;
            if (!int.TryParse(ddlNewSubCategoryParent.SelectedValue, out categoryId))
            {
                ShowMessage("Select a category.", false);
                return;
            }

            var result = new CategoryAdministrationService()
                .AddSubCategory(categoryId, txtNewSubCategoryName.Text, CurrentEmployeeCode);
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, false);
                return;
            }

            txtNewSubCategoryName.Text = string.Empty;
            ShowMessage("Sub-category added.", true);
            BindAll();
        }

        protected void gvParents_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Save")
            {
                return;
            }

            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            var row = FindCommandRow(gvParents, e);
            if (row == null)
            {
                ShowMessage("Unable to read the catalog row.", false);
                return;
            }

            int id;
            string name;
            bool isActive;
            int sortOrder;
            string sortError;
            if (!TryReadId(e, out id) || !TryReadEditableRow(row, out name, out isActive))
            {
                ShowMessage("Unable to read the catalog row.", false);
                return;
            }

            if (!TryReadSortOrder(row, out sortOrder, out sortError))
            {
                ShowMessage(sortError, false);
                return;
            }

            var result = new CategoryAdministrationService()
                .SaveParentCategory(id, name, isActive, sortOrder, CurrentEmployeeCode);
            FinishSave(result, "Parent category saved.");
        }

        protected void gvCategories_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Save")
            {
                return;
            }

            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            SaveChildRow(gvCategories, e, true);
        }

        protected void gvSubCategories_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Save")
            {
                return;
            }

            if (!DemandManagePermission())
            {
                return;
            }

            ClearMessage();
            SaveChildRow(gvSubCategories, e, false);
        }

        protected void gvChild_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var entry = e.Row.DataItem as CatalogEntryDto;
            if (entry == null)
            {
                return;
            }

            var options = sender == gvCategories
                ? _parentCategoryOptions
                : _categoryOptions;
            BindChildParentCell(e.Row, entry, options);
            StampCatalogRow(
                e.Row,
                sender == gvCategories ? "category" : "subcategory",
                entry);
        }

        protected void gvParents_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var entry = e.Row.DataItem as CatalogEntryDto;
            if (entry == null)
            {
                return;
            }

            StampCatalogRow(e.Row, "parent", entry);
        }

        private void SaveChildRow(GridView grid, GridViewCommandEventArgs e, bool categoryLevel)
        {
            var row = FindCommandRow(grid, e);
            if (row == null)
            {
                ShowMessage("Unable to read the catalog row.", false);
                return;
            }

            int id;
            string name;
            bool isActive;
            int parentId;
            int sortOrder;
            string parentError = null;
            string sortError;
            if (!TryReadId(e, out id)
                || !TryReadEditableRow(row, out name, out isActive)
                || !TryReadParentId(row, out parentId, out parentError))
            {
                ShowMessage(parentError ?? "Unable to read the catalog row.", false);
                return;
            }

            if (!TryReadSortOrder(row, out sortOrder, out sortError))
            {
                ShowMessage(sortError, false);
                return;
            }

            var service = new CategoryAdministrationService();
            var result = categoryLevel
                ? service.SaveCategory(id, parentId, name, isActive, sortOrder, CurrentEmployeeCode)
                : service.SaveSubCategory(id, parentId, name, isActive, sortOrder, CurrentEmployeeCode);
            FinishSave(result, categoryLevel ? "Category saved." : "Sub-category saved.");
        }

        private void FinishSave(CatalogSaveResult result, string successMessage)
        {
            if (!result.Succeeded)
            {
                ShowMessage(result.Error, false);
                return;
            }

            ShowMessage(successMessage, true);
            BindAll();
        }

        private void BindAll()
        {
            var service = new CategoryAdministrationService();
            _parentCategoryOptions = service.GetActiveParentCategoryOptions();
            _categoryOptions = service.GetActiveCategoryOptions();

            BindAddList(ddlNewCategoryParent, _parentCategoryOptions, "-- Select parent category --");
            BindAddList(ddlNewSubCategoryParent, _categoryOptions, "-- Select category --");

            gvParents.DataSource = service.GetParentCategories();
            gvParents.DataBind();
            gvCategories.DataSource = service.GetCategories();
            gvCategories.DataBind();
            gvSubCategories.DataSource = service.GetSubCategories();
            gvSubCategories.DataBind();
            BindMappings();
        }

        protected void btnAddMapping_Click(object sender, EventArgs e)
        {
            if (!DemandManagePermission())
            {
                return;
            }

            ClearMappingMessage();
            int legacyBranchId;
            if (!TryReadSessionBranch(out legacyBranchId))
            {
                ShowMappingMessage("The signed-in branch is missing.", false);
                return;
            }

            int? categoryId;
            int? subCategoryId;
            int? itemId;
            string selectionError;
            if (!TryReadNewMappingTarget(out categoryId, out subCategoryId, out itemId, out selectionError))
            {
                ShowMappingMessage(selectionError, false);
                return;
            }

            var result = new SupportMappingAdminService().Add(
                CurrentEmployeeCode,
                legacyBranchId,
                chkMappingBranchDefault.Checked,
                categoryId,
                subCategoryId,
                itemId,
                ddlMappingAgent.SelectedValue);
            if (!result.Succeeded)
            {
                ShowMappingMessage(result.Error, false);
                return;
            }

            chkMappingBranchDefault.Checked = false;
            ShowMappingMessage("Support agent mapping saved.", true);
            BindAll();
        }

        protected void gvMappings_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Save")
            {
                return;
            }

            if (!DemandManagePermission())
            {
                return;
            }

            ClearMappingMessage();
            int legacyBranchId;
            int id;
            if (!TryReadSessionBranch(out legacyBranchId) || !TryReadId(e, out id))
            {
                ShowMappingMessage("Unable to read the mapping row.", false);
                return;
            }

            var row = FindCommandRow(gvMappings, e);
            var agentList = row == null ? null : row.FindControl("ddlAgent") as DropDownList;
            var activeBox = row == null ? null : row.FindControl("chkActive") as CheckBox;
            if (agentList == null || activeBox == null)
            {
                ShowMappingMessage("Unable to read the mapping row.", false);
                return;
            }

            var result = new SupportMappingAdminService().Save(
                CurrentEmployeeCode,
                legacyBranchId,
                id,
                agentList.SelectedValue,
                activeBox.Checked);
            if (!result.Succeeded)
            {
                ShowMappingMessage(result.Error, false);
                return;
            }

            ShowMappingMessage("Support agent mapping saved.", true);
            BindAll();
        }

        protected void gvMappings_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var entry = (SupportMappingRowDto)e.Row.DataItem;
            var agentList = (DropDownList)e.Row.FindControl("ddlAgent");
            agentList.Items.Clear();
            var found = false;
            foreach (var employee in _mappingEmployees)
            {
                agentList.Items.Add(new ListItem(
                    EmployeeLabel(employee.EmployeeName, employee.EmployeeCode),
                    employee.EmployeeCode));
                if (string.Equals(employee.EmployeeCode, entry.AgentCode, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }

            if (!found)
            {
                agentList.Items.Insert(0, new ListItem("-- Select support agent --", string.Empty));
            }
            else
            {
                var selected = agentList.Items.FindByValue(entry.AgentCode);
                if (selected != null)
                {
                    agentList.ClearSelection();
                    selected.Selected = true;
                }
            }

            e.Row.Attributes["data-parent-id"] = entry.CategoryId.HasValue
                ? entry.CategoryId.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            e.Row.Attributes["data-category-id"] = entry.SubCategoryId.HasValue
                ? entry.SubCategoryId.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            e.Row.Attributes["data-agent"] = EmployeeLabel(entry.AgentName, entry.AgentCode);
            e.Row.Attributes["data-active"] = entry.IsActive ? "1" : "0";
            e.Row.Attributes["data-branch-default"] = entry.IsBranchDefault ? "1" : "0";
        }

        private void BindMappings()
        {
            int legacyBranchId;
            if (!TryReadSessionBranch(out legacyBranchId))
            {
                ShowMappingMessage("The signed-in branch is missing.", false);
                gvMappings.DataSource = new List<SupportMappingRowDto>();
                gvMappings.DataBind();
                return;
            }

            var page = new SupportMappingAdminService().GetForBranch(CurrentEmployeeCode, legacyBranchId);
            _mappingEmployees = page.Employees ?? new List<MappingEmployeeDto>();
            lblMappingBranch.Text = page.BranchName ?? string.Empty;
            if (!string.IsNullOrEmpty(page.Error))
            {
                ShowMappingMessage(page.Error, false);
            }

            BindMappingChoices(ddlMappingParent, page.Parents, "-- Select parent category --");
            BindMappingChoices(ddlMappingCategory, page.Categories, "-- Parent category only --");
            BindMappingChoices(ddlMappingSubCategory, page.SubCategories, "-- Category only --");
            ddlMappingAgent.Items.Clear();
            ddlMappingAgent.Items.Add(new ListItem("-- Select support agent --", string.Empty));
            foreach (var employee in _mappingEmployees)
            {
                ddlMappingAgent.Items.Add(new ListItem(
                    EmployeeLabel(employee.EmployeeName, employee.EmployeeCode),
                    employee.EmployeeCode));
            }

            gvMappings.DataSource = page.Rows ?? new List<SupportMappingRowDto>();
            gvMappings.DataBind();
        }

        private static void BindMappingChoices(
            DropDownList list,
            IList<MappingChoiceDto> choices,
            string placeholder)
        {
            list.Items.Clear();
            list.Items.Add(new ListItem(placeholder, string.Empty));
            if (choices == null)
            {
                return;
            }

            foreach (var choice in choices)
            {
                var item = new ListItem(choice.Name, choice.Id.ToString(CultureInfo.InvariantCulture));
                if (choice.ParentId > 0)
                {
                    item.Attributes["data-parent"] = choice.ParentId.ToString(CultureInfo.InvariantCulture);
                }

                list.Items.Add(item);
            }
        }

        private bool TryReadNewMappingTarget(
            out int? categoryId,
            out int? subCategoryId,
            out int? itemId,
            out string error)
        {
            categoryId = null;
            subCategoryId = null;
            itemId = null;
            error = null;
            if (chkMappingBranchDefault.Checked)
            {
                return true;
            }

            int parentId;
            if (!int.TryParse(ddlMappingParent.SelectedValue, out parentId) || parentId <= 0)
            {
                error = "Select a parent category, or choose the branch default.";
                return false;
            }

            categoryId = parentId;
            int categorySelection;
            if (int.TryParse(ddlMappingCategory.SelectedValue, out categorySelection) && categorySelection > 0)
            {
                subCategoryId = categorySelection;
            }

            int subCategorySelection;
            if (int.TryParse(ddlMappingSubCategory.SelectedValue, out subCategorySelection) && subCategorySelection > 0)
            {
                if (!subCategoryId.HasValue)
                {
                    error = "Select a category before a sub-category.";
                    return false;
                }

                itemId = subCategorySelection;
            }

            return true;
        }

        private bool TryReadSessionBranch(out int legacyBranchId)
        {
            legacyBranchId = 0;
            return Session["BRANCH"] != null
                && int.TryParse(
                    Convert.ToString(Session["BRANCH"]),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out legacyBranchId)
                && legacyBranchId > 0;
        }

        private static string EmployeeLabel(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return code ?? string.Empty;
            }

            return name + " (" + code + ")";
        }

        private void ClearMappingMessage()
        {
            lblMappingMessage.Text = string.Empty;
        }

        private void ShowMappingMessage(string message, bool success)
        {
            lblMappingMessage.Text = message ?? string.Empty;
            lblMappingMessage.ForeColor = success ? Color.Green : Color.Red;
        }

        private static void BindAddList(DropDownList list, IList<CatalogOptionDto> options, string placeholder)
        {
            list.Items.Clear();
            list.Items.Add(new ListItem(placeholder, string.Empty));
            foreach (var option in options)
            {
                list.Items.Add(new ListItem(
                    option.Name,
                    option.Id.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static void BindChildParentCell(
            GridViewRow row,
            CatalogEntryDto entry,
            IList<CatalogOptionDto> options)
        {
            var label = row.FindControl("lblParent") as Label;
            var dropdown = row.FindControl("ddlParent") as DropDownList;
            if (label == null || dropdown == null)
            {
                return;
            }

            if (entry.IsReferenced)
            {
                label.Visible = true;
                label.Text = FormatParent(entry.ParentName, entry.ParentIsActive);
                dropdown.Visible = false;
                return;
            }

            label.Visible = false;
            dropdown.Visible = true;
            dropdown.Items.Clear();

            var found = false;
            if (options != null)
            {
                foreach (var option in options)
                {
                    dropdown.Items.Add(new ListItem(
                        option.Name,
                        option.Id.ToString(CultureInfo.InvariantCulture)));
                    if (option.Id == entry.ParentId)
                    {
                        found = true;
                    }
                }
            }

            if (!found)
            {
                var inactiveName = string.IsNullOrEmpty(entry.ParentName)
                    ? "Current parent (inactive)"
                    : entry.ParentName + " (inactive)";
                dropdown.Items.Insert(0, new ListItem(
                    inactiveName,
                    entry.ParentId.ToString(CultureInfo.InvariantCulture)));
            }

            dropdown.SelectedValue = entry.ParentId.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatParent(string name, bool parentIsActive)
        {
            var text = HttpUtility.HtmlEncode(name ?? string.Empty);
            return parentIsActive ? text : text + " (inactive)";
        }

        private static GridViewRow FindCommandRow(GridView grid, GridViewCommandEventArgs e)
        {
            var source = e.CommandSource as Control;
            if (source == null)
            {
                return null;
            }

            var row = source.NamingContainer as GridViewRow;
            if (row == null || row.RowIndex < 0 || row.RowIndex >= grid.Rows.Count)
            {
                return null;
            }

            return grid.Rows[row.RowIndex];
        }

        private static bool TryReadId(GridViewCommandEventArgs e, out int id)
        {
            return int.TryParse(Convert.ToString(e.CommandArgument), out id);
        }

        private static bool TryReadEditableRow(GridViewRow row, out string name, out bool isActive)
        {
            name = null;
            isActive = false;
            var textBox = row.FindControl("txtName") as TextBox;
            var checkBox = row.FindControl("chkActive") as CheckBox;
            if (textBox == null || checkBox == null)
            {
                return false;
            }

            name = textBox.Text;
            isActive = checkBox.Checked;
            return true;
        }

        private static bool TryReadSortOrder(GridViewRow row, out int sortOrder, out string error)
        {
            sortOrder = 0;
            error = null;
            var textBox = row.FindControl("txtSortOrder") as TextBox;
            if (textBox == null)
            {
                error = "Unable to read the display order.";
                return false;
            }

            var text = (textBox.Text ?? string.Empty).Trim();
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out sortOrder)
                || sortOrder < 0
                || sortOrder > 999999)
            {
                error = "Enter a whole number from 0 through 999999 for the display order.";
                return false;
            }

            return true;
        }

        private static void StampCatalogRow(GridViewRow row, string level, CatalogEntryDto entry)
        {
            if (row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            row.Attributes["data-level"] = level;
            row.Attributes["data-id"] = entry.Id.ToString(CultureInfo.InvariantCulture);
            row.Attributes["data-name"] = entry.Name ?? string.Empty;
            row.Attributes["data-active"] = entry.IsActive ? "1" : "0";
            row.Attributes["data-root-id"] = entry.RootCategoryId.ToString(CultureInfo.InvariantCulture);
            if (level != "parent")
            {
                row.Attributes["data-parent-id"] = entry.ParentId.ToString(CultureInfo.InvariantCulture);
                row.Attributes["data-parent-name"] = entry.ParentName ?? string.Empty;
            }
        }

        private bool HasSignedInUserAndBranch()
        {
            if (Session["WORKMAN"] == null
                || string.IsNullOrWhiteSpace(Convert.ToString(Session["WORKMAN"])))
            {
                return false;
            }

            int userId;
            int branchId;
            return Session["USERID"] != null
                && int.TryParse(
                    Convert.ToString(Session["USERID"]),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out userId)
                && userId > 0
                && Session["BRANCH"] != null
                && int.TryParse(
                    Convert.ToString(Session["BRANCH"]),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out branchId)
                && branchId > 0;
        }

        private static bool TryReadParentId(GridViewRow row, out int parentId, out string error)
        {
            parentId = 0;
            error = null;
            // A referenced row does not render the parent dropdown, so its items
            // are absent on postback. Read the dropdown only when it posted a choice.
            // Otherwise keep the hidden parent id. The service still refuses a move
            // when the row is referenced.
            var dropdown = row.FindControl("ddlParent") as DropDownList;
            if (dropdown != null
                && dropdown.Visible
                && dropdown.Items.Count > 0
                && int.TryParse(dropdown.SelectedValue, out parentId))
            {
                return true;
            }

            var hidden = row.FindControl("hfParentId") as HiddenField;
            if (hidden != null && int.TryParse(hidden.Value, out parentId))
            {
                return true;
            }

            error = "Unable to read the parent entry.";
            return false;
        }

        private bool DemandManagePermission()
        {
            RequirePermission(ManagePermission);
            return HasPermission(ManagePermission);
        }

        private void ClearMessage()
        {
            lblMessage.Text = string.Empty;
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = message ?? string.Empty;
            lblMessage.ForeColor = success ? Color.Green : Color.Red;
        }
    }
}
