// ========================================================================
// When: 2026-08-24
// What: Post/Redirect/Get after successful ticket creation; session
//       gatekeeping now validates WORKMAN+USERID+BRANCH on every request.
// Why: The post-success BindInitialLookups() rebind caused the POST to
//      hang indefinitely. PRG eliminates the unnecessary re-render and
//      navigates the user directly to the new ticket detail page.
// ========================================================================
// When: 2026-08-26 (UX refactor)
// What: Single initial data load — all category/subcategory/item hierarchy
//       loaded once on GET as strongly typed DTOs, serialized via
//       JavaScriptSerializer for client-side filtering. Impact/Urgency
//       rendered as button groups. Client-side JS filters subcategories
//       and items without postbacks.
// Why:  Eliminates 5 unnecessary postbacks. Server validates all IDs and
//       hierarchy relationships on submit via a single consolidated query.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.UI
{
    // ====================================================================
    // Strongly typed DTOs for the category/subcategory/item hierarchy.
    // These are presentation-only projection DTOs — not EF entities.
    // ====================================================================

    /// <summary>
    /// Presentation DTO for an ITSM Item, projected from
    /// <c>itsm.MST_Item</c>. Used only for client-side filtering JSON.
    /// </summary>
    public class TicketItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Presentation DTO for an ITSM SubCategory, projected from
    /// <c>itsm.MST_SubCategory</c>. Contains child items.
    /// </summary>
    public class TicketSubCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TicketItemDto> Items { get; set; }
    }

    /// <summary>
    /// Presentation DTO for an ITSM Category, projected from
    /// <c>itsm.MST_Category</c>. Contains child subcategories.
    /// </summary>
    public class TicketCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TicketSubCategoryDto> SubCategories { get; set; }
    }

    /// <summary>
    /// Presentation-only New Ticket form. All category/subcategory/item
    /// hierarchy data is loaded once as strongly typed DTOs on GET and
    /// serialized via JavaScriptSerializer. Client-side JS filters
    /// dependent selections without postbacks. Ticket creation is
    /// delegated entirely to <see cref="TicketService"/>.
    /// </summary>
    public partial class CreateTicket : BasePage
    {
        /// <summary>
        /// Reusable serializer instance (thread-safe for concurrent requests).
        /// </summary>
        private static readonly JavaScriptSerializer _serializer =
            new JavaScriptSerializer();

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
                BindAllMasterData();
            }
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

            int categoryId;
            int subCategoryId;
            int impactId;
            int urgencyId;

            if (!int.TryParse(hfSelectedCategoryId.Value, out categoryId)
                || !int.TryParse(hfSelectedSubCategoryId.Value, out subCategoryId)
                || !int.TryParse(hfSelectedImpactId.Value, out impactId)
                || !int.TryParse(hfSelectedUrgencyId.Value, out urgencyId))
            {
                ShowMessage("Please complete all required fields.", false);
                return;
            }

            // Consolidated server-side hierarchy validation.
            // Single query checks: subcategory→category, item→subcategory,
            // and active flags for all three.
            int? itemId = null;
            string validationError = ValidateHierarchy(categoryId, subCategoryId, hfSelectedItemId.Value);
            if (validationError != null)
            {
                ShowMessage(validationError, false);
                return;
            }

            int itemIdParsed;
            if (int.TryParse(hfSelectedItemId.Value, out itemIdParsed))
            {
                itemId = itemIdParsed;
            }

            int requestTypeId;
            if (!int.TryParse(ddlRequestType.SelectedValue, out requestTypeId))
            {
                ShowMessage("Please select a valid request type.", false);
                return;
            }

            try
            {
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
            // Reset hidden fields — master data is already on the page,
            // no DB reload needed.
            hfSelectedCategoryId.Value = string.Empty;
            hfSelectedSubCategoryId.Value = string.Empty;
            hfSelectedItemId.Value = string.Empty;
            hfSelectedImpactId.Value = string.Empty;
            hfSelectedUrgencyId.Value = string.Empty;
        }

        // ====================================================================
        // Initial data loading — runs once on GET.
        // ====================================================================

        /// <summary>
        /// Loads all master data once on initial GET. Category/subcategory/item
        /// hierarchy is projected into strongly typed DTOs and serialized via
        /// <see cref="JavaScriptSerializer"/> for client-side filtering.
        /// </summary>
        private void BindAllMasterData()
        {
            using (var context = new ItsmDbContext())
            {
                // 1. Category → SubCategory → Item hierarchy (strongly typed).
                var categories = context.Categories.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new TicketCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SubCategories = context.SubCategories
                            .Where(s => s.IsActive && s.CategoryId == c.Id)
                            .OrderBy(s => s.Name)
                            .Select(s => new TicketSubCategoryDto
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Items = context.Items
                                    .Where(i => i.IsActive && i.SubCategoryId == s.Id)
                                    .OrderBy(i => i.Name)
                                    .Select(i => new TicketItemDto
                                    {
                                        Id = i.Id,
                                        Name = i.Name
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList();

                // Serialize hierarchy via JavaScriptSerializer (handles all
                // JSON escaping: quotes, Unicode, special characters).
                hfCategoryData.Value = _serializer.Serialize(categories);

                // Render category cards server-side (single render, no duplication).
                RenderCategoryCards(categories);

                // 2. Request Types (small list, bound to dropdown).
                BindRequestTypes(context);

                // 3. Impacts (small list, rendered as button group).
                BindImpacts(context);

                // 4. Urgencies (small list, rendered as button group).
                BindUrgencies(context);
            }
        }

        private void BindRequestTypes(ItsmDbContext context)
        {
            var types = context.RequestTypes.AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, Display = r.Code + " - " + r.Name })
                .ToList();

            ddlRequestType.Items.Clear();
            ddlRequestType.AppendDataBoundItems = true;
            ddlRequestType.Items.Add(new ListItem("-- Select Request Type --", string.Empty));
            ddlRequestType.DataSource = types;
            ddlRequestType.DataTextField = "Display";
            ddlRequestType.DataValueField = "Id";
            ddlRequestType.DataBind();
        }

        private void BindImpacts(ItsmDbContext context)
        {
            var impacts = context.Impacts.AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.Name })
                .ToList();

            impactButtons.Controls.Clear();
            foreach (var impact in impacts)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = "btn-flat";
                btn.Attributes["data-id"] = impact.Id.ToString(CultureInfo.InvariantCulture);
                btn.Attributes["tabindex"] = "0";
                btn.Attributes["role"] = "button";
                btn.Attributes["aria-label"] = "Select impact: " + impact.Name;
                btn.InnerText = impact.Name;
                btn.Attributes["onclick"] = "ITSM.CreateTicket.onImpactSelect(" + impact.Id + ")";
                btn.Attributes["onkeydown"] =
                    "if(event.key==='Enter'||event.key===' '){event.preventDefault();ITSM.CreateTicket.onImpactSelect(" + impact.Id + ");}";
                impactButtons.Controls.Add(btn);
            }
        }

        private void BindUrgencies(ItsmDbContext context)
        {
            var urgencies = context.Urgencies.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.SortOrder)
                .Select(u => new { u.Id, u.Name })
                .ToList();

            urgencyButtons.Controls.Clear();
            foreach (var urgency in urgencies)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = "btn-flat";
                btn.Attributes["data-id"] = urgency.Id.ToString(CultureInfo.InvariantCulture);
                btn.Attributes["tabindex"] = "0";
                btn.Attributes["role"] = "button";
                btn.Attributes["aria-label"] = "Select urgency: " + urgency.Name;
                btn.InnerText = urgency.Name;
                btn.Attributes["onclick"] = "ITSM.CreateTicket.onUrgencySelect(" + urgency.Id + ")";
                btn.Attributes["onkeydown"] =
                    "if(event.key==='Enter'||event.key===' '){event.preventDefault();ITSM.CreateTicket.onUrgencySelect(" + urgency.Id + ");}";
                urgencyButtons.Controls.Add(btn);
            }
        }

        private void RenderCategoryCards(List<TicketCategoryDto> categories)
        {
            categoryCards.Controls.Clear();
            foreach (var cat in categories)
            {
                var card = new HtmlGenericControl("div");
                card.Attributes["class"] = "category-card";
                card.Attributes["tabindex"] = "0";
                card.Attributes["role"] = "button";
                card.Attributes["aria-label"] = "Select category: " + cat.Name;
                card.Attributes["data-id"] = cat.Id.ToString(CultureInfo.InvariantCulture);

                var icon = new HtmlGenericControl("span");
                icon.Attributes["class"] = "category-icon";
                icon.InnerHtml = "<i class='fa fa-folder'></i>";

                var name = new HtmlGenericControl("span");
                name.Attributes["class"] = "category-name";
                name.InnerText = cat.Name;

                card.Controls.Add(icon);
                card.Controls.Add(name);
                categoryCards.Controls.Add(card);
            }
        }

        // ====================================================================
        // Server-side validation — consolidated into a single query.
        // ====================================================================

        /// <summary>
        /// Validates the submitted category/subcategory/item hierarchy in a
        /// single database query. Returns null on success, or an error message.
        /// Client-side filtering is UX only; this is the data-integrity gate.
        /// </summary>
        private string ValidateHierarchy(int categoryId, int subCategoryId, string itemValue)
        {
            using (var context = new ItsmDbContext())
            {
                // Single query: verify subcategory belongs to category and is active.
                bool subValid = context.SubCategories.AsNoTracking()
                    .Any(s => s.Id == subCategoryId
                             && s.CategoryId == categoryId
                             && s.IsActive);

                if (!subValid)
                {
                    return "The selected sub category does not belong to the chosen category.";
                }

                // If an item was selected, verify it belongs to the subcategory.
                int itemIdParsed;
                if (int.TryParse(itemValue, out itemIdParsed))
                {
                    bool itemValid = context.Items.AsNoTracking()
                        .Any(i => i.Id == itemIdParsed
                                 && i.SubCategoryId == subCategoryId
                                 && i.IsActive);

                    if (!itemValid)
                    {
                        return "The selected item does not belong to the chosen sub category.";
                    }
                }
            }

            return null;
        }

        // ====================================================================
        // Form helpers.
        // ====================================================================

        private void ResetFormInputs()
        {
            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            if (ddlRequestType.Items.Count > 0) ddlRequestType.SelectedIndex = 0;
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
