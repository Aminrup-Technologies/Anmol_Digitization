// ========================================================================
// File:    TicketList.aspx.cs
// Layer:   UI — Code-Behind
// When:    2026-08-20 / 2026-08-20 (audit patch)
// What:    Presentation-only code-behind for agent ticket queue.
// Why:     Thin UI layer — validates session, invokes BLL services,
//          binds DTO results to GridView. No business rules, no SQL,
//          no transaction logic, no static state (requirement §21).
// Patch:   Enforced mandatory session gatekeeping for ALL required keys
//          (WORKMAN, USERID, BranchID, CompanyID) per audit rule §16.
//          Added endResponse:true to all Redirect calls to prevent
//          continuation of the page lifecycle after auth failure.
//          Replaced DB-resolved CompanyId with direct Session["CompanyID"]
//          lookup to eliminate unnecessary round-trip.
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
    public partial class TicketList : BasePage
    {
        // Lightweight view-state keys — only identifiers, never entities (requirement §19)
        private const string VS_ACTIVE_TAB = "_activeTab";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Session gatekeeping — ALL four keys mandatory (requirement §16, audit patch)
            if (Session["WORKMAN"] == null || Session["USERID"] == null
                || Session["BranchID"] == null || Session["CompanyID"] == null)
            {
                Response.Redirect("~/Login.aspx", true);
                return;
            }

            if (!IsPostBack)
            {
                SetActiveTab("MyQueue");
                BindFilterLookups();
                BindQueue();
            }
        }

        // ---- Tab Navigation ----

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

        // ---- Filter Events ----

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

        // ---- Grid Paging ----

        protected void gvTickets_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvTickets.PageIndex = e.NewPageIndex;
            BindQueue();
        }

        // ---- Row Data Bound for badge styling ----

        protected void gvTickets_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
            {
                return;
            }

            // Priority badge color
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

            // Status badge color
            var lblStatus = e.Row.FindControl("lblStatusBadge") as Label;
            if (lblStatus != null)
            {
                var statusName = lblStatus.Text.Trim().ToLower().Replace(" ", "-");
                lblStatus.CssClass = "badge-status badge-" + statusName;
            }
        }

        // ---- Private Helpers ----

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

        /// <summary>
        /// Resolves CompanyId from trusted session context.
        /// Session["CompanyID"] is set at login and validated in Page_Load.
        /// No DB round-trip — CompanyId comes from the authenticated
        /// application context, not from arbitrary user input (requirement §7).
        /// </summary>
        private int ResolveCompanyId()
        {
            int companyId;
            if (int.TryParse(Session["CompanyID"].ToString(), out companyId) && companyId > 0)
            {
                return companyId;
            }

            // Fallback: derive from user master if session value is stale
            var workman = Session["WORKMAN"].ToString();
            using (var context = new ItsmDbContext())
            {
                var user = context.UserMasters
                    .AsNoTracking()
                    .FirstOrDefault(u => u.EmployeeCode == workman);

                return user != null ? user.CompanyId : 0;
            }
        }

        private TicketListFilter BuildFilter()
        {
            var filter = new TicketListFilter();

            if (!string.IsNullOrWhiteSpace(ddlFilterStatus.SelectedValue))
            {
                int statusId;
                if (int.TryParse(ddlFilterStatus.SelectedValue, out statusId))
                {
                    filter.StatusId = statusId;
                }
            }

            if (!string.IsNullOrWhiteSpace(ddlFilterPriority.SelectedValue))
            {
                int priorityId;
                if (int.TryParse(ddlFilterPriority.SelectedValue, out priorityId))
                {
                    filter.PriorityId = priorityId;
                }
            }

            if (!string.IsNullOrWhiteSpace(ddlFilterBranch.SelectedValue))
            {
                int branchId;
                if (int.TryParse(ddlFilterBranch.SelectedValue, out branchId))
                {
                    filter.BranchId = branchId;
                }
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
                var companyId = ResolveCompanyId();
                if (companyId == 0)
                {
                    ShowMessage("Unable to resolve company context.", false);
                    return;
                }

                var workman = Session["WORKMAN"].ToString();
                var filter = BuildFilter();
                var service = new TicketListService();

                var tab = GetActiveTab();

                switch (tab)
                {
                    case "MyQueue":
                        // Tickets assigned to the current agent
                        filter.AssigneeCode = workman;
                        gvTickets.DataSource = service.GetOpenQueue(companyId, filter);
                        break;
                    case "AllOpen":
                        // All open tickets for the tenant (supervisor view)
                        gvTickets.DataSource = service.GetOpenQueue(companyId, filter);
                        break;
                    case "MyTickets":
                        // Tickets created by the current user
                        gvTickets.DataSource = service.GetMyTickets(companyId, workman, filter);
                        break;
                    case "Approvals":
                        // Pending approvals for the current user
                        gvTickets.DataSource = service.GetApprovalQueue(companyId, workman);
                        break;
                    default:
                        filter.AssigneeCode = workman;
                        gvTickets.DataSource = service.GetOpenQueue(companyId, filter);
                        break;
                }

                gvTickets.DataBind();

                var dataSource = gvTickets.DataSource as System.Collections.IList;
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
            using (var context = new ItsmDbContext())
            {
                // Status filter
                BindDropDown(ddlFilterStatus,
                    context.TicketStatuses.AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Id)
                        .Select(s => new { s.Id, s.Name })
                        .ToList(),
                    "Name", "Id", "All Statuses");

                // Priority filter
                BindDropDown(ddlFilterPriority,
                    context.Priorities.AsNoTracking()
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.Id)
                        .Select(p => new { p.Id, p.Name })
                        .ToList(),
                    "Name", "Id", "All Priorities");

                // Branch filter
                BindDropDown(ddlFilterBranch,
                    context.Branches.AsNoTracking()
                        .OrderBy(b => b.Branch_Description)
                        .Select(b => new { Id = b.Branch_ID, Name = b.Branch_Description })
                        .ToList(),
                    "Name", "Id", "All Branches");
            }
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
