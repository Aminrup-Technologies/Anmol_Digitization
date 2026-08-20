// ========================================================================
// File:    TicketDetail.aspx.cs
// Layer:   UI — Code-Behind
// When:    2026-08-20
// What:    Presentation-only code-behind for ticket processing surface.
// Why:     Thin UI layer — validates session, reads control values,
//          invokes TicketUpdateService for all mutations. No business rules,
//          no SQL, no transaction logic, no static state (requirement §21).
// ========================================================================

using System;
using System.Drawing;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.UI
{
    public partial class TicketDetail : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Session gatekeeping (requirement §16)
            if (Session["WORKMAN"] == null || Session["USERID"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int ticketId;
                if (!int.TryParse(Request.QueryString["id"], out ticketId))
                {
                    ShowNotFound();
                    return;
                }

                hfTicketId.Value = ticketId.ToString();
                LoadTicket(ticketId);
            }
        }

        // ---- Status Change ----

        protected void btnChangeStatus_Click(object sender, EventArgs e)
        {
            ClearMessage();

            if (string.IsNullOrWhiteSpace(ddlTargetStatus.SelectedValue))
            {
                ShowMessage("Please select a target status action.", false);
                return;
            }

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var actorCode = Session["WORKMAN"].ToString();
            int targetStatusId = int.Parse(ddlTargetStatus.SelectedValue);

            var service = new TicketUpdateService();
            var result = service.ChangeStatus(
                companyId,
                ticketId,
                targetStatusId,
                actorCode,
                txtResolution.Text);

            if (result.Success)
            {
                ShowMessage("Status updated successfully.", true);
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Reassignment ----

        protected void btnReassign_Click(object sender, EventArgs e)
        {
            ClearMessage();

            if (string.IsNullOrWhiteSpace(ddlReassignTo.SelectedValue))
            {
                ShowMessage("Please select an agent to reassign to.", false);
                return;
            }

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var actorCode = Session["WORKMAN"].ToString();
            var newAssigneeCode = ddlReassignTo.SelectedValue;

            var service = new TicketUpdateService();
            var result = service.ReassignTicket(companyId, ticketId, newAssigneeCode, actorCode);

            if (result.Success)
            {
                ShowMessage("Ticket reassigned successfully.", true);
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Priority Override ----

        protected void btnOverridePriority_Click(object sender, EventArgs e)
        {
            ClearMessage();

            if (string.IsNullOrWhiteSpace(ddlOverridePriority.SelectedValue))
            {
                ShowMessage("Please select a priority.", false);
                return;
            }

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var actorCode = Session["WORKMAN"].ToString();
            int newPriorityId = int.Parse(ddlOverridePriority.SelectedValue);

            var service = new TicketUpdateService();
            var result = service.OverridePriority(companyId, ticketId, newPriorityId, actorCode);

            if (result.Success)
            {
                ShowMessage("Priority overridden successfully.", true);
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Approval ----

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            ClearMessage();

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var approverCode = Session["WORKMAN"].ToString();

            var service = new TicketUpdateService();
            var result = service.ApproveTicket(companyId, ticketId, approverCode);

            if (result.Success)
            {
                ShowMessage("Ticket approved successfully.", true);
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Rejection (mandatory reason enforced by BLL, requirement §10) ----

        protected void btnReject_Click(object sender, EventArgs e)
        {
            ClearMessage();

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var approverCode = Session["WORKMAN"].ToString();
            var reason = txtRejectionReason.Text;

            // BLL enforces non-whitespace requirement (requirement §10)
            var service = new TicketUpdateService();
            var result = service.RejectTicket(companyId, ticketId, approverCode, reason);

            if (result.Success)
            {
                ShowMessage("Ticket rejected.", true);
                txtRejectionReason.Text = string.Empty;
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Comment ----

        protected void btnAddComment_Click(object sender, EventArgs e)
        {
            ClearMessage();

            if (string.IsNullOrWhiteSpace(txtComment.Text))
            {
                ShowMessage("Comment text is required.", false);
                return;
            }

            var ticketId = int.Parse(hfTicketId.Value);
            var companyId = ResolveCompanyId();
            var actorCode = Session["WORKMAN"].ToString();

            var service = new TicketUpdateService();
            var result = service.AddComment(
                companyId, ticketId, txtComment.Text, actorCode, chkInternal.Checked);

            if (result.Success)
            {
                ShowMessage("Comment added.", true);
                txtComment.Text = string.Empty;
                chkInternal.Checked = false;
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        // ---- Private Helpers ----

        private void LoadTicket(int ticketId)
        {
            var companyId = ResolveCompanyId();
            var service = new TicketDetailService();
            var dto = service.GetTicketDetail(companyId, ticketId);

            if (dto == null)
            {
                ShowNotFound();
                return;
            }

            pnlTicketContent.Visible = true;
            pnlNotFound.Visible = false;

            // Bind metadata
            lblTicketNumber.Text = "#" + dto.TicketNumber;
            lblTitle.Text = dto.Title;
            lblRequestType.Text = dto.RequestTypeName;
            lblRequester.Text = dto.RequesterName;
            lblAssignee.Text = dto.AssigneeName;
            lblCategory.Text = dto.CategoryName + (string.IsNullOrEmpty(dto.SubCategoryName)
                ? "" : " > " + dto.SubCategoryName)
                + (string.IsNullOrEmpty(dto.ItemName) ? "" : " > " + dto.ItemName);
            lblBranch.Text = dto.BranchName;
            lblImpactUrgency.Text = dto.ImpactName + " / " + dto.UrgencyName;
            lblDescription.Text = string.IsNullOrEmpty(dto.Description)
                ? "(No description provided)"
                : dto.Description;
            lblCreatedOn.Text = dto.CreatedOn.ToString("dd-MMM-yyyy HH:mm");

            // Status badge
            lblStatusBadge.Text = dto.StatusName;
            var statusLower = dto.StatusName.ToLower().Replace(" ", "-");
            lblStatusBadge.CssClass = "badge-status badge-" + statusLower;

            // Priority badge
            lblPriorityBadge.Text = dto.PriorityName;

            // Resolution text
            if (!string.IsNullOrEmpty(dto.ResolutionText))
            {
                lblDescription.Text += "<br/><br/><strong>Resolution:</strong> " + dto.ResolutionText;
            }

            // CSAT
            if (dto.CsatScore.HasValue)
            {
                pnlCsat.Visible = true;
                lblCsatScore.Text = dto.CsatScore.Value + " / 5";
                lblCsatFeedback.Text = dto.CsatFeedback ?? "(No feedback)";
            }

            // Bind available statuses to dropdown
            ddlTargetStatus.Items.Clear();
            ddlTargetStatus.AppendDataBoundItems = true;
            ddlTargetStatus.Items.Add(new ListItem("-- Select Action --", string.Empty));
            foreach (var status in dto.AvailableStatuses)
            {
                ddlTargetStatus.Items.Add(new ListItem(status.Name, status.Id.ToString()));
            }

            // Show/hide resolution text based on selected status
            divResolution.Visible = false;
            txtResolution.Text = string.Empty;

            // Bind reassignment dropdown (all active users)
            BindAgentDropdown();

            // Bind priority override dropdown
            BindPriorityDropdown();

            // Show approval buttons if ticket is Pending Approval
            var currentStatus = dto.StatusName;
            pnlApprovalActions.Visible = currentStatus == "Pending Approval";

            // Bind comments
            rptComments.DataSource = dto.Comments;
            rptComments.DataBind();

            // Bind approvals
            rptApprovals.DataSource = dto.Approvals;
            rptApprovals.DataBind();

            // Bind audit history
            gvAudit.DataSource = dto.AuditHistory;
            gvAudit.DataBind();

            // Store RowVersion for concurrency
            if (dto.RowVersion != null)
            {
                hfRowVersion.Value = Convert.ToBase64String(dto.RowVersion);
            }

            // Show/hide action panel for terminal status
            bool isTerminal = currentStatus == "Closed" || currentStatus == "Cancelled";
            pnlActions.Visible = !isTerminal;
        }

        private void BindAgentDropdown()
        {
            ddlReassignTo.Items.Clear();
            ddlReassignTo.AppendDataBoundItems = true;
            ddlReassignTo.Items.Add(new ListItem("-- Select Agent --", string.Empty));

            using (var context = new ItsmDbContext())
            {
                ddlReassignTo.DataSource = context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.EmployeeName)
                    .Select(u => new { u.EmployeeCode, u.EmployeeName })
                    .ToList();

                ddlReassignTo.DataTextField = "EmployeeName";
                ddlReassignTo.DataValueField = "EmployeeCode";
                ddlReassignTo.DataBind();
            }
        }

        private void BindPriorityDropdown()
        {
            ddlOverridePriority.Items.Clear();
            ddlOverridePriority.AppendDataBoundItems = true;
            ddlOverridePriority.Items.Add(new ListItem("-- Select Priority --", string.Empty));

            using (var context = new ItsmDbContext())
            {
                ddlOverridePriority.DataSource = context.Priorities
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Id)
                    .Select(p => new { p.Id, p.Name })
                    .ToList();

                ddlOverridePriority.DataTextField = "Name";
                ddlOverridePriority.DataValueField = "Id";
                ddlOverridePriority.DataBind();
            }
        }

        private int ResolveCompanyId()
        {
            var workman = Session["WORKMAN"].ToString();
            using (var context = new ItsmDbContext())
            {
                var user = context.UserMasters
                    .AsNoTracking()
                    .FirstOrDefault(u => u.EmployeeCode == workman);

                return user != null ? user.CompanyId : 0;
            }
        }

        private void ShowNotFound()
        {
            pnlNotFound.Visible = true;
            pnlTicketContent.Visible = false;
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
