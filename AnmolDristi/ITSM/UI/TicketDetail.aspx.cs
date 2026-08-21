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
            if (Session["WORKMAN"] == null || Session["USERID"] == null)
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

        protected void ddlTargetStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedText = ddlTargetStatus.SelectedItem != null
                ? ddlTargetStatus.SelectedItem.Text
                : string.Empty;
            divResolution.Visible = string.Equals(selectedText, "Resolved", StringComparison.OrdinalIgnoreCase);
        }

        protected void btnChangeStatus_Click(object sender, EventArgs e)
        {
            ClearMessage();
            if (string.IsNullOrWhiteSpace(ddlTargetStatus.SelectedValue))
            {
                ShowMessage("Please select a target status action.", false);
                return;
            }

            int ticketId;
            int targetStatusId;
            if (!TryGetTicketId(out ticketId) || !int.TryParse(ddlTargetStatus.SelectedValue, out targetStatusId))
            {
                ShowMessage("Invalid ticket or status.", false);
                return;
            }

            var result = new TicketUpdateService().ChangeStatus(
                ResolveCompanyId(),
                ticketId,
                targetStatusId,
                GetActorCode(),
                txtResolution.Text,
                GetRowVersion());

            ApplyResult(result, ticketId, "Status updated successfully.");
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
                ResolveCompanyId(),
                ticketId,
                ddlReassignTo.SelectedValue,
                GetActorCode(),
                GetRowVersion());

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
                ResolveCompanyId(),
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
                ResolveCompanyId(),
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
                ResolveCompanyId(),
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
                ResolveCompanyId(),
                ticketId,
                txtComment.Text,
                GetActorCode(),
                chkInternal.Checked);

            if (result.Success)
            {
                txtComment.Text = string.Empty;
                chkInternal.Checked = false;
            }

            ApplyResult(result, ticketId, "Comment added.");
        }

        private void LoadTicket(int ticketId)
        {
            var companyId = ResolveCompanyId();
            if (companyId == 0)
            {
                ShowNotFound();
                return;
            }

            var dto = new TicketDetailService().GetTicketDetail(companyId, ticketId, GetActorCode());
            if (dto == null)
            {
                ShowNotFound();
                return;
            }

            pnlTicketContent.Visible = true;
            pnlNotFound.Visible = false;
            pnlTenantBadge.Visible = true;
            lblTenantBadge.Text = "Company " + dto.CompanyId;

            lblTicketNumber.Text = "#" + dto.TicketNumber;
            lblTitle.Text = dto.Title;
            lblRequestType.Text = dto.RequestTypeName;
            lblRequester.Text = dto.RequesterName;
            lblAssignee.Text = dto.AssigneeName;
            lblCategory.Text = dto.CategoryName
                + (string.IsNullOrEmpty(dto.SubCategoryName) ? "" : " > " + dto.SubCategoryName)
                + (string.IsNullOrEmpty(dto.ItemName) ? "" : " > " + dto.ItemName);
            lblBranch.Text = dto.BranchName;
            lblImpactUrgency.Text = dto.ImpactName + " / " + dto.UrgencyName;
            lblCreatedOn.Text = dto.CreatedOn.ToString("dd-MMM-yyyy HH:mm");
            lblDescription.Text = string.IsNullOrEmpty(dto.Description)
                ? "(No description provided)"
                : dto.Description;

            pnlResolution.Visible = !string.IsNullOrEmpty(dto.ResolutionText);
            lblResolution.Text = dto.ResolutionText ?? string.Empty;

            lblStatusBadge.Text = dto.StatusName;
            lblStatusBadge.CssClass = "badge-status badge-"
                + dto.StatusName.ToLower().Replace(" ", "-");
            lblPriorityBadge.Text = dto.PriorityName;

            if (dto.CsatScore.HasValue)
            {
                pnlCsat.Visible = true;
                lblCsatScore.Text = dto.CsatScore.Value + " / 5";
                lblCsatFeedback.Text = dto.CsatFeedback ?? "(No feedback)";
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
            txtResolution.Text = string.Empty;

            BindAgentDropdown(companyId);
            BindPriorityDropdown();

            pnlApprovalActions.Visible = dto.CanApprove;
            pnlWorkActions.Visible = !dto.IsTerminal && dto.StatusName != "Pending Approval";
            pnlActions.Visible = !dto.IsTerminal && (pnlWorkActions.Visible || pnlApprovalActions.Visible);
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

            hfRowVersion.Value = dto.RowVersion != null
                ? Convert.ToBase64String(dto.RowVersion)
                : string.Empty;
        }

        private void BindAgentDropdown(int companyId)
        {
            ddlReassignTo.Items.Clear();
            ddlReassignTo.AppendDataBoundItems = true;
            ddlReassignTo.Items.Add(new ListItem("-- Select Agent --", string.Empty));
            ddlReassignTo.DataSource = new TicketListService().GetActiveAgents(companyId);
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

        private int ResolveCompanyId()
        {
            return ItsmTenantContext.ResolveCompanyId(GetActorCode());
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
            lblMessage.Text = message;
            lblMessage.ForeColor = success ? Color.Green : Color.Red;
        }
    }
}
