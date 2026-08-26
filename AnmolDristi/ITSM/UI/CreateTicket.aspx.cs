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
//       is loaded once on GET as JSON and embedded in a hidden field.
//       Impact/Urgency are rendered as button groups from initial data.
//       Client-side JS filters subcategories and items without postbacks.
// Why:  Eliminates 5 unnecessary postbacks (category select, subcategory
//       select, and their round-trips). Reduces total DB queries from 6
//       to 4 (categories+subs+items, request types, impacts, urgencies).
//       Server validates submitted IDs on submit to ensure data integrity.
// ========================================================================

using System;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.UI
{
    /// <summary>
    /// Presentation-only New Ticket form. All category/subcategory/item
    /// hierarchy data is loaded once as JSON on GET. Client-side JS filters
    /// dependent selections without postbacks. Ticket creation is delegated
    /// entirely to <see cref="TicketService"/>.
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

            // Read IDs from hidden fields (set by client-side JS).
            var requesterCode = Session["WORKMAN"].ToString();

            int categoryId;
            int subCategoryId;
            int impactId;
            int urgencyId;
            int? itemId = null;

            if (!int.TryParse(hfSelectedCategoryId.Value, out categoryId)
                || !int.TryParse(hfSelectedSubCategoryId.Value, out subCategoryId)
                || !int.TryParse(hfSelectedImpactId.Value, out impactId)
                || !int.TryParse(hfSelectedUrgencyId.Value, out urgencyId))
            {
                ShowMessage("Please complete all required fields.", false);
                return;
            }

            // Validate category/subcategory relationship server-side.
            if (!ValidateCategoryHierarchy(categoryId, subCategoryId))
            {
                ShowMessage("The selected sub category does not belong to the chosen category.", false);
                return;
            }

            int itemIdParsed;
            if (int.TryParse(hfSelectedItemId.Value, out itemIdParsed))
            {
                // Validate item belongs to the subcategory.
                if (!ValidateItemHierarchy(subCategoryId, itemIdParsed))
                {
                    ShowMessage("The selected item does not belong to the chosen sub category.", false);
                    return;
                }
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
            // Reset hidden fields
            hfSelectedCategoryId.Value = string.Empty;
            hfSelectedSubCategoryId.Value = string.Empty;
            hfSelectedItemId.Value = string.Empty;
            hfSelectedImpactId.Value = string.Empty;
            hfSelectedUrgencyId.Value = string.Empty;
        }

        /// <summary>
        /// Loads ALL master data once on initial GET. Category/subcategory/item
        /// hierarchy is serialized as JSON for client-side filtering.
        /// </summary>
        private void BindAllMasterData()
        {
            using (var context = new ItsmDbContext())
            {
                // 1. Category → SubCategory → Item hierarchy (as JSON).
                var categories = context.Categories.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        SubCategories = context.SubCategories
                            .Where(s => s.IsActive && s.CategoryId == c.Id)
                            .OrderBy(s => s.Name)
                            .Select(s => new
                            {
                                s.Id,
                                s.Name,
                                Items = context.Items
                                    .Where(i => i.IsActive && i.SubCategoryId == s.Id)
                                    .OrderBy(i => i.Name)
                                    .Select(i => new { i.Id, i.Name })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList();

                // Serialize hierarchy as JSON for client-side filtering.
                var json = SerializeCategories(categories);
                hfCategoryData.Value = json;

                // Render category cards server-side.
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
                btn.Attributes["onclick"] = "onImpactSelect(" + impact.Id + ")";
                btn.Attributes["onkeydown"] =
                    "if(event.key==='Enter'||event.key===' '){event.preventDefault();onImpactSelect(" + impact.Id + ");}";
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
                btn.Attributes["onclick"] = "onUrgencySelect(" + urgency.Id + ")";
                btn.Attributes["onkeydown"] =
                    "if(event.key==='Enter'||event.key===' '){event.preventDefault();onUrgencySelect(" + urgency.Id + ");}";
                urgencyButtons.Controls.Add(btn);
            }
        }

        private void RenderCategoryCards(System.Collections.Generic.List<dynamic> categories)
        {
            categoryCards.Controls.Clear();
            foreach (var cat in categories)
            {
                int catId = cat.Id;
                string catName = cat.Name;

                var card = new HtmlGenericControl("div");
                card.Attributes["class"] = "category-card";
                card.Attributes["tabindex"] = "0";
                card.Attributes["role"] = "button";
                card.Attributes["aria-label"] = "Select category: " + catName;
                card.Attributes["data-id"] = catId.ToString(CultureInfo.InvariantCulture);

                var icon = new HtmlGenericControl("span");
                icon.Attributes["class"] = "category-icon";
                icon.InnerHtml = "<i class='fa fa-folder'></i>";

                var name = new HtmlGenericControl("span");
                name.Attributes["class"] = "category-name";
                name.InnerText = catName;

                card.Controls.Add(icon);
                card.Controls.Add(name);

                // Client-side click is handled by the JS init that reads from JSON.
                categoryCards.Controls.Add(card);
            }
        }

        /// <summary>
        /// Validates that the submitted subcategory belongs to the submitted category.
        /// Server-side integrity check — client-side filtering is UX only.
        /// </summary>
        private bool ValidateCategoryHierarchy(int categoryId, int subCategoryId)
        {
            using (var context = new ItsmDbContext())
            {
                return context.SubCategories.AsNoTracking()
                    .Any(s => s.Id == subCategoryId
                             && s.CategoryId == categoryId
                             && s.IsActive);
            }
        }

        /// <summary>
        /// Validates that the submitted item belongs to the submitted subcategory.
        /// </summary>
        private bool ValidateItemHierarchy(int subCategoryId, int itemId)
        {
            using (var context = new ItsmDbContext())
            {
                return context.Items.AsNoTracking()
                    .Any(i => i.Id == itemId
                             && i.SubCategoryId == subCategoryId
                             && i.IsActive);
            }
        }

        /// <summary>
        /// Serializes the category/subcategory/item hierarchy as a JSON string
        /// for client-side filtering. Uses manual JSON construction to avoid
        /// introducing a JSON library dependency.
        /// </summary>
        private static string SerializeCategories(System.Collections.Generic.List<dynamic> categories)
        {
            var sb = new StringBuilder();
            sb.Append('[');

            bool firstCat = true;
            foreach (var cat in categories)
            {
                if (!firstCat) sb.Append(',');
                firstCat = false;

                sb.Append('{');
                sb.AppendFormat("\"Id\":{0},\"Name\":\"{1}\",\"SubCategories\":[",
                    cat.Id, EscapeJson(cat.Name));

                bool firstSub = true;
                foreach (var sub in cat.SubCategories)
                {
                    if (!firstSub) sb.Append(',');
                    firstSub = false;

                    sb.Append('{');
                    sb.AppendFormat("\"Id\":{0},\"Name\":\"{1}\",\"Items\":[",
                        sub.Id, EscapeJson(sub.Name));

                    bool firstItem = true;
                    foreach (var item in sub.Items)
                    {
                        if (!firstItem) sb.Append(',');
                        firstItem = false;

                        sb.AppendFormat("{{\"Id\":{0},\"Name\":\"{1}\"}}",
                            item.Id, EscapeJson(item.Name));
                    }

                    sb.Append("]}");
                }

                sb.Append("]}");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

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
