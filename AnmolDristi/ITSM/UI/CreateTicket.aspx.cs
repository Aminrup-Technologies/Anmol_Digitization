// ========================================================================
// When: 2026-08-24
// What: Post/Redirect/Get after successful ticket creation; session
//       gatekeeping now validates WORKMAN+USERID+BRANCH on every request.
// Why: The post-success BindInitialLookups() rebind caused the POST to
//      hang indefinitely. PRG eliminates the unnecessary re-render and
//      navigates the user directly to the new ticket detail page.
// ========================================================================

using System;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.UI
{
    /// <summary>
    /// Presentation-only New Ticket form. Dropdown lookups use ItsmDbContext;
    /// ticket creation is delegated entirely to <see cref="TicketService"/>.
    /// </summary>
    public partial class CreateTicket : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Session gatekeeping on every request (not only !IsPostBack).
            if (Session["WORKMAN"] == null
                || Session["USERID"] == null
                || Session["BRANCH"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindInitialLookups();
                ResetChildDropdowns();
            }
        }

        protected void ddlCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearMessage();
            BindSubCategories();
            ResetItemDropdown();
        }

        protected void ddlSubCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearMessage();
            BindItems();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            ClearMessage();

            if (!Page.IsValid)
            {
                return;
            }

            if (Session["WORKMAN"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            var requesterCode = Session["WORKMAN"].ToString();

            try
            {
                int requestTypeId = int.Parse(ddlRequestType.SelectedValue);
                int categoryId = int.Parse(ddlCategory.SelectedValue);
                int subCategoryId = int.Parse(ddlSubCategory.SelectedValue);
                int impactId = int.Parse(ddlImpact.SelectedValue);
                int urgencyId = int.Parse(ddlUrgency.SelectedValue);

                int? itemId = null;
                if (!string.IsNullOrWhiteSpace(ddlItem.SelectedValue))
                {
                    itemId = int.Parse(ddlItem.SelectedValue);
                }

                var ticketService = new TicketService();
                int newTicketId = ticketService.CreateTicket(
                    requesterCode,
                    requestTypeId,
                    txtTitle.Text.Trim(),
                    txtDescription.Text.Trim(),
                    categoryId,
                    subCategoryId,
                    itemId,
                    impactId,
                    urgencyId);

                // Post/Redirect/Get: navigate to TicketDetail so the browser
                // issues a fresh GET instead of repeating the POST.
                // When: 2026-08-24 — prevents indefinite POST hang.
                string redirectUrl = ResolveUrl("~/ITSM/UI/TicketDetail.aspx?id="
                    + newTicketId.ToString(CultureInfo.InvariantCulture));
                Response.Redirect(redirectUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            catch (Exception ex)
            {
                ShowMessage("Unable to create ticket: " + ex.Message, false);
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ClearMessage();
            ResetFormInputs();
            BindInitialLookups();
            ResetChildDropdowns();
        }

        /// <summary>
        /// Binds Request Type, Category, Impact, and Urgency on first load / reset.
        /// Read-only lookup queries only — no business rules.
        /// </summary>
        private void BindInitialLookups()
        {
            using (var context = new ItsmDbContext())
            {
                BindDropDown(
                    ddlRequestType,
                    context.RequestTypes.AsNoTracking()
                        .Where(r => r.IsActive)
                        .OrderBy(r => r.Name)
                        .Select(r => new { r.Id, Display = r.Code + " - " + r.Name })
                        .ToList(),
                    "Display",
                    "Id",
                    "-- Select Request Type --");

                BindDropDown(
                    ddlCategory,
                    context.Categories.AsNoTracking()
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => new { c.Id, c.Name })
                        .ToList(),
                    "Name",
                    "Id",
                    "-- Select Category --");

                BindDropDown(
                    ddlImpact,
                    context.Impacts.AsNoTracking()
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => new { i.Id, i.Name })
                        .ToList(),
                    "Name",
                    "Id",
                    "-- Select Impact --");

                BindDropDown(
                    ddlUrgency,
                    context.Urgencies.AsNoTracking()
                        .Where(u => u.IsActive)
                        .OrderBy(u => u.SortOrder)
                        .Select(u => new { u.Id, u.Name })
                        .ToList(),
                    "Name",
                    "Id",
                    "-- Select Urgency --");
            }
        }

        private void BindSubCategories()
        {
            ResetSubCategoryDropdown();
            ResetItemDropdown();

            if (string.IsNullOrWhiteSpace(ddlCategory.SelectedValue))
            {
                return;
            }

            int categoryId = int.Parse(ddlCategory.SelectedValue);

            using (var context = new ItsmDbContext())
            {
                BindDropDown(
                    ddlSubCategory,
                    context.SubCategories.AsNoTracking()
                        .Where(s => s.IsActive && s.CategoryId == categoryId)
                        .OrderBy(s => s.Name)
                        .Select(s => new { s.Id, s.Name })
                        .ToList(),
                    "Name",
                    "Id",
                    "-- Select Sub Category --");
            }
        }

        private void BindItems()
        {
            ResetItemDropdown();

            if (string.IsNullOrWhiteSpace(ddlSubCategory.SelectedValue))
            {
                return;
            }

            int subCategoryId = int.Parse(ddlSubCategory.SelectedValue);

            using (var context = new ItsmDbContext())
            {
                BindDropDown(
                    ddlItem,
                    context.Items.AsNoTracking()
                        .Where(i => i.IsActive && i.SubCategoryId == subCategoryId)
                        .OrderBy(i => i.Name)
                        .Select(i => new { i.Id, i.Name })
                        .ToList(),
                    "Name",
                    "Id",
                    "-- Select Item (optional) --");
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

        private void ResetChildDropdowns()
        {
            ResetSubCategoryDropdown();
            ResetItemDropdown();
        }

        private void ResetSubCategoryDropdown()
        {
            ddlSubCategory.Items.Clear();
            ddlSubCategory.Items.Add(new ListItem("-- Select Sub Category --", string.Empty));
        }

        private void ResetItemDropdown()
        {
            ddlItem.Items.Clear();
            ddlItem.Items.Add(new ListItem("-- Select Item (optional) --", string.Empty));
        }

        private void ResetFormInputs()
        {
            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            if (ddlRequestType.Items.Count > 0) ddlRequestType.SelectedIndex = 0;
            if (ddlCategory.Items.Count > 0) ddlCategory.SelectedIndex = 0;
            if (ddlImpact.Items.Count > 0) ddlImpact.SelectedIndex = 0;
            if (ddlUrgency.Items.Count > 0) ddlUrgency.SelectedIndex = 0;
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
