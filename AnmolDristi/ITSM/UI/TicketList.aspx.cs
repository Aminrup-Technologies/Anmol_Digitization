// ========================================================================
// When: 2026-08-21
// What: Session gate WORKMAN+USERID+BRANCH. MyTickets/Approvals/branch
//       lookup no longer use actor CompanyId. Queues stay DTO-bound.
// Why: Shared-service queues must not be restricted by agent company.
//      Session CompanyID is not the authentication model.
// ========================================================================

using System;
using System.Collections;
using System.Drawing;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    public partial class TicketList : BasePage
    {
        private const string VS_ACTIVE_TAB = "_activeTab";

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
                SetActiveTab("MyQueue");
                BindFilterLookups();
                BindQueue();
            }
        }

        protected void lnkMyQueue_Click(object sender, EventArgs e)
        {
            SetActiveTab("MyQueue");
            BindQueue();
        }

        protected void lnkAllOpen_Click(object sender, EventArgs e)
        {
            SetActiveTab("AllOpen");
            BindQueue();
        }

        protected void lnkMyTickets_Click(object sender, EventArgs e)
        {
            SetActiveTab("MyTickets");
            BindQueue();
        }

        protected void lnkApprovals_Click(object sender, EventArgs e)
        {
            SetActiveTab("Approvals");
            BindQueue();
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            BindQueue();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindQueue();
        }

        protected void btnClearFilter_Click(object sender, EventArgs e)
        {
            ddlFilterStatus.SelectedIndex = 0;
            ddlFilterPriority.SelectedIndex = 0;
            ddlFilterBranch.SelectedIndex = 0;
            txtSearch.Text = string.Empty;
            BindQueue();
        }

        protected void gvTickets_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvTickets.PageIndex = e.NewPageIndex;
            BindQueue();
        }

        protected void gvTickets_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            var lblPriority = e.Row.FindControl("lblPriorityBadge") as Label;
            if (lblPriority != null)
            {
                var priorityName = lblPriority.Text.Trim().ToUpper();
                if (priorityName.Contains("P1") || priorityName.Contains("CRITICAL"))
                {
                    lblPriority.CssClass = "badge-priority badge-p1";
                }
                else if (priorityName.Contains("P2") || priorityName.Contains("HIGH"))
                {
                    lblPriority.CssClass = "badge-priority badge-p2";
                }
                else if (priorityName.Contains("P3") || priorityName.Contains("MEDIUM"))
                {
                    lblPriority.CssClass = "badge-priority badge-p3";
                }
                else
                {
                    lblPriority.CssClass = "badge-priority badge-p4";
                }
            }

            var lblStatus = e.Row.FindControl("lblStatusBadge") as Label;
            if (lblStatus != null)
            {
                var statusName = lblStatus.Text.Trim().ToLower().Replace(" ", "-");
                lblStatus.CssClass = "badge-status badge-" + statusName;
            }
        }

        private void SetActiveTab(string tab)
        {
            ViewState[VS_ACTIVE_TAB] = tab;
            lnkMyQueue.CssClass = tab == "MyQueue" ? "active" : "";
            lnkAllOpen.CssClass = tab == "AllOpen" ? "active" : "";
            lnkMyTickets.CssClass = tab == "MyTickets" ? "active" : "";
            lnkApprovals.CssClass = tab == "Approvals" ? "active" : "";
        }

        private string GetActiveTab()
        {
            return ViewState[VS_ACTIVE_TAB] as string ?? "MyQueue";
        }

        private TicketListFilter BuildFilter()
        {
            var filter = new TicketListFilter();

            int statusId;
            if (!string.IsNullOrWhiteSpace(ddlFilterStatus.SelectedValue)
                && int.TryParse(ddlFilterStatus.SelectedValue, out statusId))
            {
                filter.StatusId = statusId;
            }

            int priorityId;
            if (!string.IsNullOrWhiteSpace(ddlFilterPriority.SelectedValue)
                && int.TryParse(ddlFilterPriority.SelectedValue, out priorityId))
            {
                filter.PriorityId = priorityId;
            }

            int branchId;
            if (!string.IsNullOrWhiteSpace(ddlFilterBranch.SelectedValue)
                && int.TryParse(ddlFilterBranch.SelectedValue, out branchId))
            {
                filter.BranchId = branchId;
            }

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                filter.SearchText = txtSearch.Text.Trim();
            }

            return filter;
        }

        private void BindQueue()
        {
            ClearMessage();

            try
            {
                var workman = Session["WORKMAN"].ToString();
                var filter = BuildFilter();
                var service = new TicketListService();
                var tab = GetActiveTab();

                switch (tab)
                {
                    case "MyQueue":
                        gvTickets.DataSource = service.GetAgentQueue(workman, filter);
                        break;
                    case "AllOpen":
                        gvTickets.DataSource = service.GetOpenQueue(filter);
                        break;
                    case "MyTickets":
                        gvTickets.DataSource = service.GetMyTickets(workman, filter);
                        break;
                    case "Approvals":
                        gvTickets.DataSource = service.GetApprovalQueue(workman);
                        break;
                    default:
                        gvTickets.DataSource = service.GetAgentQueue(workman, filter);
                        break;
                }

                gvTickets.DataBind();

                var dataSource = gvTickets.DataSource as IList;
                if (dataSource != null)
                {
                    lblTicketCount.Text = dataSource.Count + " ticket(s)";
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading ticket queue: " + ex.Message, false);
            }
        }

        private void BindFilterLookups()
        {
            var service = new TicketListService();
            BindDropDown(ddlFilterStatus, service.GetActiveStatuses(), "Name", "Id", "All Statuses");
            BindDropDown(ddlFilterPriority, service.GetActivePriorities(), "Name", "Id", "All Priorities");
            BindDropDown(ddlFilterBranch, service.GetBranches(), "Name", "Id", "All Branches");
        }

        private static void BindDropDown(
            DropDownList dropdown,
            object dataSource,
            string textField,
            string valueField,
            string placeholder)
        {
            dropdown.Items.Clear();
            dropdown.AppendDataBoundItems = true;
            dropdown.Items.Add(new ListItem(placeholder, string.Empty));
            dropdown.DataSource = dataSource;
            dropdown.DataTextField = textField;
            dropdown.DataValueField = valueField;
            dropdown.DataBind();
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
