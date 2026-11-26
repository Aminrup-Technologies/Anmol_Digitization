// ========================================================================
// When: 2026-11-26
// What: R6 support group admin UI. Permission ITSM.ApprovalConfig.Manage
//       (no group-specific key in catalog). Session BRANCH = legacy Branch_ID.
// Why: Closest existing admin permission already granted to ITSMAdmin.
// ========================================================================

using System;
using System.Drawing;
using System.Linq;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;
using AnmolDristi.ITSM.DAL;
using System.Data.Entity;

namespace AnmolDristi.ITSM.UI
{
    public partial class SupportGroupConfig : BasePage
    {
        private const string ManagePermission = "ITSM.ApprovalConfig.Manage";
        private const string VS_GROUP_ID = "_sgId";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["USERID"] == null || Session["BRANCH"] == null)
            {
                RedirectToLogin();
                return;
            }

            RequirePermission(ManagePermission);

            if (!IsPostBack)
            {
                BindGroups();
                BindParentCategories();
                BindEmployeeDropdown();
            }
            else
            {
                RestoreSelectedGroupPanel();
            }
        }

        protected void btnCreateGroup_Click(object sender, EventArgs e)
        {
            RequirePermission(ManagePermission);
            ClearMessage();

            int companyId;
            int branchId;
            if (!TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            var result = new SupportGroupAdminService().CreateGroup(
                companyId, branchId, txtNewGroupName.Text, CurrentEmployeeCode);
            if (!result.Success)
            {
                ShowMessage(result.Error, false);
                return;
            }

            txtNewGroupName.Text = string.Empty;
            ShowMessage("Group created (inactive until a Team Leader is set and activated).", true);
            BindGroups();
            SelectGroup(result.Id.Value);
        }

        protected void gvGroups_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            RequirePermission(ManagePermission);
            ClearMessage();

            int groupId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out groupId))
            {
                return;
            }

            if (e.CommandName == "SelectGroup")
            {
                SelectGroup(groupId);
                return;
            }

            if (e.CommandName != "ToggleActive")
            {
                return;
            }

            int companyId;
            int branchId;
            if (!TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            var groups = new SupportGroupAdminService().ListGroups(companyId, branchId);
            var current = groups.FirstOrDefault(g => g.Id == groupId);
            if (current == null)
            {
                ShowMessage("Group not found.", false);
                return;
            }

            var result = new SupportGroupAdminService().SetGroupActive(
                companyId, branchId, groupId, !current.IsActive, CurrentEmployeeCode);
            if (!result.Success)
            {
                ShowMessage(result.Error, false);
                return;
            }

            ShowMessage(current.IsActive ? "Group deactivated." : "Group activated.", true);
            BindGroups();
            SelectGroup(groupId);
        }

        protected void btnSaveMember_Click(object sender, EventArgs e)
        {
            RequirePermission(ManagePermission);
            ClearMessage();

            int groupId;
            int companyId;
            int branchId;
            if (!TryGetSelectedGroupId(out groupId) || !TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ddlMember.SelectedValue))
            {
                ShowMessage("Select an employee.", false);
                return;
            }

            var result = new SupportGroupAdminService().UpsertMember(
                companyId,
                branchId,
                groupId,
                ddlMember.SelectedValue,
                chkIsTeamLeader.Checked,
                chkMemberActive.Checked,
                CurrentEmployeeCode);

            if (!result.Success)
            {
                ShowMessage(result.Error, false);
                return;
            }

            ShowMessage("Member saved.", true);
            BindGroupDetail(groupId);
            BindGroups();
        }

        protected void ddlParentCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindCategoryDropdown();
        }

        protected void btnSaveMapping_Click(object sender, EventArgs e)
        {
            RequirePermission(ManagePermission);
            ClearMessage();

            int groupId;
            int companyId;
            int branchId;
            int categoryId;
            if (!TryGetSelectedGroupId(out groupId) || !TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            if (!int.TryParse(ddlParentCategory.SelectedValue, out categoryId))
            {
                ShowMessage("Select a Parent Category.", false);
                return;
            }

            int? subCategoryId = null;
            int parsedSub;
            if (!string.IsNullOrWhiteSpace(ddlCategory.SelectedValue)
                && int.TryParse(ddlCategory.SelectedValue, out parsedSub))
            {
                subCategoryId = parsedSub;
            }

            var result = new SupportGroupAdminService().SaveMapping(
                companyId, branchId, groupId, categoryId, subCategoryId, CurrentEmployeeCode);
            if (!result.Success)
            {
                ShowMessage(result.Error, false);
                return;
            }

            ShowMessage("Mapping saved.", true);
            BindGroupDetail(groupId);
        }

        protected void gvMappings_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "DeactivateMapping")
            {
                return;
            }

            RequirePermission(ManagePermission);
            ClearMessage();

            int mappingId;
            int companyId;
            int branchId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out mappingId)
                || !TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            var result = new SupportGroupAdminService().DeactivateMapping(
                companyId, branchId, mappingId, CurrentEmployeeCode);
            if (!result.Success)
            {
                ShowMessage(result.Error, false);
                return;
            }

            int groupId;
            if (TryGetSelectedGroupId(out groupId))
            {
                BindGroupDetail(groupId);
            }

            ShowMessage("Mapping deactivated.", true);
        }

        private void BindGroups()
        {
            int companyId;
            int branchId;
            if (!TryResolveScope(out companyId, out branchId))
            {
                gvGroups.DataSource = null;
                gvGroups.DataBind();
                return;
            }

            gvGroups.DataSource = new SupportGroupAdminService().ListGroups(companyId, branchId);
            gvGroups.DataBind();
        }

        private void SelectGroup(int groupId)
        {
            ViewState[VS_GROUP_ID] = groupId;
            hfSelectedGroupId.Value = groupId.ToString();
            BindGroupDetail(groupId);
        }

        private void RestoreSelectedGroupPanel()
        {
            int groupId;
            if (TryGetSelectedGroupId(out groupId))
            {
                pnlGroupDetail.Visible = true;
            }
        }

        private void BindGroupDetail(int groupId)
        {
            int companyId;
            int branchId;
            if (!TryResolveScope(out companyId, out branchId))
            {
                pnlGroupDetail.Visible = false;
                return;
            }

            var service = new SupportGroupAdminService();
            var group = service.ListGroups(companyId, branchId).FirstOrDefault(g => g.Id == groupId);
            if (group == null)
            {
                pnlGroupDetail.Visible = false;
                return;
            }

            pnlGroupDetail.Visible = true;
            litSelectedGroup.Text = group.Name;
            ViewState[VS_GROUP_ID] = groupId;
            hfSelectedGroupId.Value = groupId.ToString();

            gvMembers.DataSource = service.ListMembers(companyId, branchId, groupId);
            gvMembers.DataBind();
            gvMappings.DataSource = service.ListMappings(companyId, branchId, groupId);
            gvMappings.DataBind();
        }

        private void BindEmployeeDropdown()
        {
            int companyId;
            int branchId;
            ddlMember.Items.Clear();
            ddlMember.Items.Add(new ListItem("-- Select employee --", string.Empty));
            if (!TryResolveScope(out companyId, out branchId))
            {
                return;
            }

            ddlMember.DataSource = new SupportGroupAdminService()
                .ListCompanyActiveAgents(companyId, branchId);
            ddlMember.DataTextField = "EmployeeName";
            ddlMember.DataValueField = "EmployeeCode";
            ddlMember.DataBind();
        }

        private void BindParentCategories()
        {
            var hierarchy = new TicketMasterDataService().GetCategoryHierarchy(0);
            ddlParentCategory.Items.Clear();
            ddlParentCategory.Items.Add(new ListItem("-- Parent Category --", string.Empty));
            foreach (var cat in hierarchy)
            {
                ddlParentCategory.Items.Add(new ListItem(cat.Name, cat.Id.ToString()));
            }

            BindCategoryDropdown();
        }

        private void BindCategoryDropdown()
        {
            ddlCategory.Items.Clear();
            ddlCategory.Items.Add(new ListItem("-- Category (optional) --", string.Empty));

            int parentId;
            if (!int.TryParse(ddlParentCategory.SelectedValue, out parentId))
            {
                return;
            }

            var parent = new TicketMasterDataService()
                .GetCategoryHierarchy(0)
                .FirstOrDefault(c => c.Id == parentId);
            if (parent == null || parent.SubCategories == null)
            {
                return;
            }

            foreach (var sub in parent.SubCategories)
            {
                ddlCategory.Items.Add(new ListItem(sub.Name, sub.Id.ToString()));
            }
        }

        private bool TryGetSelectedGroupId(out int groupId)
        {
            if (ViewState[VS_GROUP_ID] != null && int.TryParse(ViewState[VS_GROUP_ID].ToString(), out groupId))
            {
                return true;
            }

            return int.TryParse(hfSelectedGroupId.Value, out groupId);
        }

        private bool TryResolveScope(out int companyId, out int branchId)
        {
            companyId = ItsmTenantContext.ResolveCompanyId(CurrentEmployeeCode);
            branchId = 0;

            if (companyId <= 0)
            {
                ShowMessage("Your user has no CompanyId; support group admin is blocked.", false);
                return false;
            }

            int legacyBranchId;
            if (Session["BRANCH"] == null
                || !int.TryParse(Convert.ToString(Session["BRANCH"]), out legacyBranchId))
            {
                ShowMessage("Session BRANCH is missing.", false);
                return false;
            }

            using (var context = new ItsmDbContext())
            {
                branchId = context.Branches.AsNoTracking()
                    .Where(b => b.Branch_ID == legacyBranchId)
                    .Select(b => b.Id)
                    .FirstOrDefault();
            }

            if (branchId <= 0)
            {
                ShowMessage("Session BRANCH does not resolve to MST_Branch.", false);
                return false;
            }

            return true;
        }

        private void ClearMessage()
        {
            lblMessage.Text = string.Empty;
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = message;
            lblMessage.ForeColor = success ? Color.Green : Color.Red;
        }
    }
}
