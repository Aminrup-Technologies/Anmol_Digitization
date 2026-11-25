// ========================================================================
// When: 2026-11-25
// What: R5 moves status mutation to dropdown selection with explicit
//       client confirmation and a post-success confirmation popup.
// Why: Remove the redundant Apply button while retaining BLL validation.
// ========================================================================
// When: 2026-08-21
// What: Session gate WORKMAN+USERID+BRANCH; detail/update without actor
//       CompanyId; tenant badge = MST_Company.Company_Description;
//       AddComment passes RowVersion; reassign agents are all Active users.
//       2026-08-24: Added ITSM.Ticket.View permission check with tenant
//       isolation — permission grants view only for tickets in the
//       user's authorized company (ItsmTenantContext.ResolveCompanyId).
// Why: Ticket.CompanyId is the tenant. Session CompanyID / actor company
//      must not gate shared-service processing. The permission gate
//      prevents cross-company access via ?id=.
// ========================================================================

using System;
using System.Drawing;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    public partial class TicketDetail : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["WORKMAN"] == null
                || Session["USERID"] == null
                || Session["BRANCH"] == null)
            {
                Response.Redirect("~/login.aspx", true);
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

        // ====================================================================
        // When: 2026-11-25
        // What: R5 executes the selected status transition directly from the
        //       dropdown postback and displays a completion confirmation.
        // Why: Remove the separate Apply button while retaining server-side
        //       transition, reason, authorization, tenant and RowVersion gates.
        // ====================================================================
        protected void ddlTargetStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearMessage();
            int ticketId;
            int targetStatusId;
            if (!TryGetTicketId(out ticketId)
                || !int.TryParse(ddlTargetStatus.SelectedValue, out targetStatusId))
            {
                ShowMessage("Invalid ticket or status action.", false);
                return;
            }

            var result = new TicketUpdateService().ChangeStatus(
                ticketId,
                targetStatusId,
                GetActorCode(),
                txtResolution.Text,
                GetRowVersion(),
                ddlHoldClass.SelectedValue);

            if (!result.Success)
            {
                LoadTicket(ticketId);
                ShowMessage(result.Error, false);
                return;
            }

            ApplyResult(result, ticketId, "Status updated successfully.");
            ClientScript.RegisterStartupScript(
                GetType(),
                "ItsmR5ActionConfirmation",
                "window.alert('Action executed successfully.');",
                true);
        }

        protected void btnTakeOwnership_Click(object sender, EventArgs e)
        {
            ClearMessage();
            int ticketId;
            if (!TryGetTicketId(out ticketId))
            {
                return;
            }

            var result = new TicketUpdateService().TakeOwnership(
                ticketId,
                GetActorCode(),
                GetRowVersion());
            ApplyResult(result, ticketId, "You are now the assignee.");
        }

        protected void btnReassign_Click(object sender, EventArgs e)
        {
            ClearMessage();
            if (string.IsNullOrWhiteSpace(ddlReassignTo.SelectedValue))
            {
                ShowMessage("Please select an agent to reassign to.", false);
                return;
            }

            int ticketId;
            if (!TryGetTicketId(out ticketId))
            {
                return;
            }

            var result = new TicketUpdateService().ReassignTicket(
                ticketId,
                ddlReassignTo.SelectedValue,
                GetActorCode(),
                GetRowVersion(),
                txtReassignReason.Text);

            if (result.Success)
            {
                txtReassignReason.Text = string.Empty;
            }

            ApplyResult(result, ticketId, "Ticket reassigned successfully.");
        }

        protected void btnOverridePriority_Click(object sender, EventArgs e)
        {
            ClearMessage();
            if (string.IsNullOrWhiteSpace(ddlOverridePriority.SelectedValue))
            {
                ShowMessage("Please select a priority.", false);
                return;
            }

            int ticketId;
            int newPriorityId;
            if (!TryGetTicketId(out ticketId)
                || !int.TryParse(ddlOverridePriority.SelectedValue, out newPriorityId))
            {
                return;
            }

            var result = new TicketUpdateService().OverridePriority(
                ticketId,
                newPriorityId,
                GetActorCode(),
                txtOverrideReason.Text,
                GetRowVersion());

            if (result.Success)
            {
                txtOverrideReason.Text = string.Empty;
            }

            ApplyResult(result, ticketId, "Priority override recorded with audit justification.");
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            ClearMessage();
            int ticketId;
            if (!TryGetTicketId(out ticketId))
            {
                return;
            }

            var result = new TicketUpdateService().ApproveTicket(
                ticketId,
                GetActorCode(),
                null,
                GetRowVersion());

            ApplyResult(result, ticketId, "Ticket approved successfully.");
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            ClearMessage();
            int ticketId;
            if (!TryGetTicketId(out ticketId))
            {
                return;
            }

            var result = new TicketUpdateService().RejectTicket(
                ticketId,
                GetActorCode(),
                txtRejectionReason.Text,
                GetRowVersion());

            if (result.Success)
            {
                txtRejectionReason.Text = string.Empty;
            }

            ApplyResult(result, ticketId, "Ticket rejected.");
        }

        protected void btnAddComment_Click(object sender, EventArgs e)
        {
            ClearMessage();
            if (string.IsNullOrWhiteSpace(txtComment.Text))
            {
                ShowMessage("Comment text is required.", false);
                return;
            }

            int ticketId;
            if (!TryGetTicketId(out ticketId))
            {
                return;
            }

            var result = new TicketUpdateService().AddComment(
                ticketId,
                txtComment.Text,
                GetActorCode(),
                chkInternal.Checked,
                GetRowVersion());

            if (result.Success)
            {
                txtComment.Text = string.Empty;
                chkInternal.Checked = false;
            }

            ApplyResult(result, ticketId, "Comment added.");
        }

        private void LoadTicket(int ticketId)
        {
            // RBAC: resolve permission and authorized tenant at the UI layer.
            // BLL remains System.Web-agnostic.
            var hasTicketViewPermission = HasPermission("ITSM.Ticket.View");
            var authorizedCompanyId = ItsmTenantContext.ResolveCompanyId(GetActorCode());

            var dto = new TicketDetailService().GetTicketDetail(
                ticketId,
                GetActorCode(),
                hasTicketViewPermission,
                authorizedCompanyId);
            if (dto == null)
            {
                ShowNotFound();
                return;
            }

            pnlTicketContent.Visible = true;
            pnlNotFound.Visible = false;
            pnlTenantBadge.Visible = true;
            lblTenantBadge.Text = Enc(FormatTenantBadge(dto.CompanyName, dto.CompanyId));

            lblTicketNumber.Text = Enc("#" + dto.TicketNumber);
            lblTitle.Text = Enc(dto.Title);
            lblRequestType.Text = Enc(dto.RequestTypeName);
            lblRequester.Text = Enc(dto.RequesterName);
            lblAssignee.Text = Enc(dto.AssigneeName);
            lblCategory.Text = Enc(dto.CategoryName)
                + (string.IsNullOrEmpty(dto.SubCategoryName) ? "" : " > " + Enc(dto.SubCategoryName))
                + (string.IsNullOrEmpty(dto.ItemName) ? "" : " > " + Enc(dto.ItemName));
            lblBranch.Text = Enc(dto.BranchName);
            lblImpactUrgency.Text = Enc(dto.ImpactName) + " / " + Enc(dto.UrgencyName);
            lblCreatedOn.Text = dto.CreatedOn.ToString("dd-MMM-yyyy HH:mm");
            lblDescription.Text = Enc(string.IsNullOrEmpty(dto.Description)
                ? "(No description provided)"
                : dto.Description);

            pnlResolution.Visible = !string.IsNullOrEmpty(dto.ResolutionText);
            lblResolution.Text = Enc(dto.ResolutionText);

            lblStatusBadge.Text = Enc(dto.StatusName);
            lblStatusBadge.CssClass = StatusCss(dto.StatusName);
            lblPriorityBadge.Text = Enc(dto.PriorityName);
            hfStatusName.Value = dto.StatusName ?? string.Empty;

            if (dto.CsatScore.HasValue)
            {
                pnlCsat.Visible = true;
                lblCsatScore.Text = dto.CsatScore.Value + " / 5";
                lblCsatFeedback.Text = Enc(string.IsNullOrEmpty(dto.CsatFeedback)
                    ? "(No feedback)"
                    : dto.CsatFeedback);
            }
            else
            {
                pnlCsat.Visible = false;
            }

            ddlTargetStatus.Items.Clear();
            ddlTargetStatus.AppendDataBoundItems = true;
            ddlTargetStatus.Items.Add(new ListItem("-- Select Action --", string.Empty));
            foreach (var status in dto.AvailableStatuses)
            {
                ddlTargetStatus.Items.Add(new ListItem(status.Name, status.Id.ToString()));
            }

            divResolution.Visible = false;
            divHoldClass.Visible = false;
            txtResolution.Text = string.Empty;
            ddlHoldClass.SelectedIndex = 0;

            BindAgentDropdown();
            BindPriorityDropdown();

            var hasStatusActions = dto.AvailableStatuses != null && dto.AvailableStatuses.Count > 0;
            pnlStatusAction.Visible = hasStatusActions;
            pnlAssigneeTools.Visible = dto.CanProcess;
            pnlTakeOwnership.Visible = dto.CanTakeOwnership;
            pnlWorkActions.Visible = hasStatusActions || dto.CanProcess || dto.CanTakeOwnership;
            pnlApprovalActions.Visible = dto.CanApprove || dto.CanReject;
            btnApprove.Visible = dto.CanApprove;
            btnReject.Visible = dto.CanReject;
            txtRejectionReason.Visible = dto.CanReject;
            pnlActions.Visible = pnlWorkActions.Visible || pnlApprovalActions.Visible;
            txtComment.Visible = dto.CanComment;
            btnAddComment.Visible = dto.CanComment;
            chkInternal.Visible = dto.CanAddInternalNote;
            if (!dto.CanAddInternalNote)
            {
                chkInternal.Checked = false;
            }

            rptComments.DataSource = dto.Comments;
            rptComments.DataBind();
            rptApprovals.DataSource = dto.Approvals;
            rptApprovals.DataBind();
            gvAudit.DataSource = dto.AuditHistory;
            gvAudit.DataBind();

            var attachments = dto.Attachments ?? new System.Collections.Generic.List<TicketAttachmentDto>();
            lblNoAttachments.Visible = attachments.Count == 0;
            rptAttachments.DataSource = attachments;
            rptAttachments.DataBind();
            ShowPendingAttachmentFailures(ticketId);

            hfRowVersion.Value = dto.RowVersion != null
                ? Convert.ToBase64String(dto.RowVersion)
                : string.Empty;
        }

        private static string FormatTenantBadge(string companyName, int? companyId)
        {
            if (!string.IsNullOrWhiteSpace(companyName))
            {
                return companyName.Trim();
            }

            if (companyId.HasValue)
            {
                return "Company " + companyId.Value;
            }

            return "Tenant not recorded";
        }

        private void BindAgentDropdown()
        {
            ddlReassignTo.Items.Clear();
            ddlReassignTo.AppendDataBoundItems = true;
            ddlReassignTo.Items.Add(new ListItem("-- Select Agent --", string.Empty));
            ddlReassignTo.DataSource = new TicketListService().GetActiveAgents();
            ddlReassignTo.DataTextField = "EmployeeName";
            ddlReassignTo.DataValueField = "EmployeeCode";
            ddlReassignTo.DataBind();
        }

        private void BindPriorityDropdown()
        {
            ddlOverridePriority.Items.Clear();
            ddlOverridePriority.AppendDataBoundItems = true;
            ddlOverridePriority.Items.Add(new ListItem("-- Select Priority --", string.Empty));
            ddlOverridePriority.DataSource = new TicketListService().GetActivePriorities();
            ddlOverridePriority.DataTextField = "Name";
            ddlOverridePriority.DataValueField = "Id";
            ddlOverridePriority.DataBind();
        }

        private static string Enc(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string StatusCss(string statusName)
        {
            switch (statusName)
            {
                case "New":
                    return "badge-status badge-queue";
                case "In Progress":
                    return "badge-status badge-progress";
                case "Resolved":
                    return "badge-status badge-resolved";
                case "Closed":
                    return "badge-status badge-closed";
                case "Pending Approval":
                    return "badge-status badge-pending";
                case "On Hold":
                    return "badge-status badge-hold";
                case "Cancelled":
                case "Rejected":
                    return "badge-status badge-cancelled";
                default:
                    return "badge-status";
            }
        }

        private string GetActorCode()
        {
            return Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : string.Empty;
        }

        private bool TryGetTicketId(out int ticketId)
        {
            return int.TryParse(hfTicketId.Value, out ticketId);
        }

        private byte[] GetRowVersion()
        {
            if (string.IsNullOrWhiteSpace(hfRowVersion.Value))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(hfRowVersion.Value);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyResult(TicketUpdateResult result, int ticketId, string successMessage)
        {
            if (result.Success)
            {
                ShowMessage(successMessage, true);
                LoadTicket(ticketId);
            }
            else
            {
                ShowMessage(result.Error, false);
            }
        }

        private void ShowPendingAttachmentFailures(int ticketId)
        {
            var notice = Session[ItsmAttachmentFeedback.FailureSessionKey] as AttachmentFailureNotice;
            if (notice == null || notice.TicketId != ticketId || notice.Failures == null)
            {
                return;
            }

            Session.Remove(ItsmAttachmentFeedback.FailureSessionKey);
            if (notice.Failures.Count == 0)
            {
                return;
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (var failure in notice.Failures)
            {
                var name = string.IsNullOrWhiteSpace(failure.FileName) ? "attachment" : failure.FileName;
                var reason = string.IsNullOrWhiteSpace(failure.Reason)
                    ? "The file could not be saved."
                    : failure.Reason;
                parts.Add(name + ": " + reason);
            }

            ShowMessage(string.Join(" | ", parts), false);
        }

        private void ShowNotFound()
        {
            pnlNotFound.Visible = true;
            pnlTicketContent.Visible = false;
            pnlTenantBadge.Visible = false;
        }

        private void ClearMessage()
        {
            lblMessage.Text = string.Empty;
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = Enc(message);
            lblMessage.ForeColor = success ? Color.Green : Color.Red;
        }
    }
}
