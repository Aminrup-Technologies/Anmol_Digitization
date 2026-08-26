// ========================================================================
// When: 2026-08-24
// What: Post/Redirect/Get after successful ticket creation; session
//       gatekeeping now validates WORKMAN+USERID+BRANCH on every request.
// Why: The post-success BindInitialLookups() rebind caused the POST to
//      hang indefinitely. PRG eliminates the unnecessary re-render and
//      navigates the user directly to the new ticket detail page.
// ========================================================================
// When: 2026-08-26 (UX refactor)
// What: Eliminated postbacks for Category/SubCategory/Item selection.
//       All category hierarchy data is loaded once as strongly typed DTOs,
//       serialized via JavaScriptSerializer for client-side filtering.
//       Impact/Urgency rendered as button groups. Client-side JS filters
//       subcategories and items without postbacks.
// Why:  Product decision — faster ticket creation (5 fewer postbacks),
//       better UX with clear category cards, zero unnecessary DB
//       round-trips for dependent selections.
// ========================================================================
// When: 2026-08-26 (architecture hardening)
// What: Removed all direct EF6/ItsmDbContext access from code-behind.
//       All master-data queries delegated to TicketMasterDataService (BLL).
//       Hierarchy validation uses single consolidated BLL query.
//       Broad catch(Exception) removed — only specific business exceptions
//       are caught. Static JavaScriptSerializer replaced with instance.
//       Company/tenant isolation enforced via BLL service.
// Why:  Architecture enforcement: UI contains zero database access.
//       UI → BLL → DAL. Master data reads are company-scoped.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    /// <summary>
    /// Presentation-only New Ticket form. All master-data queries are
    /// delegated to <see cref="TicketMasterDataService"/>. Client-side JS
    /// filters dependent selections without postbacks. Ticket creation is
    /// delegated entirely to <see cref="TicketService"/>.
    /// </summary>
    public partial class CreateTicket : BasePage
    {
        /// <summary>
        /// Per-request serializer instance. JavaScriptSerializer is safe for
        /// concurrent use, but an instance avoids any shared-state concern.
        /// </summary>
        private readonly JavaScriptSerializer _serializer =
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

            // Server-side hierarchy validation via BLL (single consolidated query).
            int? itemId = null;
            int itemIdParsed;
            if (int.TryParse(hfSelectedItemId.Value, out itemIdParsed))
            {
                itemId = itemIdParsed;
            }

            var masterService = new TicketMasterDataService();
            var hierarchyResult = masterService.ValidateHierarchy(
                categoryId, subCategoryId, itemId);

            if (!hierarchyResult.IsValid)
            {
                ShowMessage(hierarchyResult.Error, false);
                return;
            }

            int requestTypeId;
            if (!int.TryParse(hfSelectedRequestTypeId.Value, out requestTypeId))
            {
                ShowMessage("Please select a valid request type.", false);
                return;
            }

            // Validate impact, urgency, and request type exist and are active.
            var masterDataResult = masterService.ValidateMasterDataIds(
                impactId, urgencyId, requestTypeId);

            if (!masterDataResult.IsValid)
            {
                ShowMessage(masterDataResult.Error, false);
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
            catch (InvalidOperationException ex)
            {
                // Known business exception from TicketService — show message only.
                ShowMessage(ex.Message, false);
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ClearMessage();
            ResetFormInputs();
            // Reset hidden fields — master data is already on the page,
            // no DB reload needed. Client-side JS resets visual state.
            hfSelectedCategoryId.Value = string.Empty;
            hfSelectedSubCategoryId.Value = string.Empty;
            hfSelectedItemId.Value = string.Empty;
            hfSelectedImpactId.Value = string.Empty;
            hfSelectedUrgencyId.Value = string.Empty;
        }

        // ====================================================================
        // Initial data loading — runs once on GET via BLL service.
        // ====================================================================

        /// <summary>
        /// Loads all master data once on initial GET via
        /// <see cref="TicketMasterDataService"/>. Category/subcategory/item
        /// hierarchy is projected into strongly typed DTOs and serialized
        /// via <see cref="JavaScriptSerializer"/> for client-side filtering.
        /// </summary>
        private void BindAllMasterData()
        {
            var masterService = new TicketMasterDataService();

            // Resolve company for tenant-scoped reads.
            var workman = Session["WORKMAN"] != null
                ? Session["WORKMAN"].ToString()
                : string.Empty;
            int companyId = masterService.ResolveCompanyId(workman);

            // 1. Category → SubCategory → Item hierarchy (strongly typed DTOs).
            List<TicketCategoryDto> categories = masterService
                .GetCategoryHierarchy(companyId);
            hfCategoryData.Value = _serializer.Serialize(categories);

            // Render category cards server-side (single render, no duplication).
            RenderCategoryCards(categories);

            // 2. Request Types (small list, rendered as selectable cards).
            List<TicketLookupItemDto> requestTypes = masterService.GetRequestTypes();
            RenderRequestTypeCards(requestTypes);

            // 3. Impacts (small list, rendered as button group).
            List<TicketLookupItemDto> impacts = masterService.GetImpacts();
            BindImpacts(impacts);

            // 4. Urgencies (small list, rendered as button group).
            List<TicketLookupItemDto> urgencies = masterService.GetUrgencies();
            BindUrgencies(urgencies);
        }

        private void BindImpacts(List<TicketLookupItemDto> impacts)
        {
            impactButtons.Controls.Clear();
            foreach (var impact in impacts)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = "itsm-ct__btn";
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

        private void BindUrgencies(List<TicketLookupItemDto> urgencies)
        {
            urgencyButtons.Controls.Clear();
            foreach (var urgency in urgencies)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = "itsm-ct__btn";
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
                card.Attributes["class"] = "itsm-ct__option";
                card.Attributes["tabindex"] = "0";
                card.Attributes["role"] = "button";
                card.Attributes["aria-label"] = "Select category: " + cat.Name;
                card.Attributes["data-id"] = cat.Id.ToString(CultureInfo.InvariantCulture);

                var icon = new HtmlGenericControl("span");
                icon.Attributes["class"] = "itsm-ct__option-icon";
                icon.InnerHtml = "<i class='fa fa-folder'></i>";

                var name = new HtmlGenericControl("span");
                name.Attributes["class"] = "itsm-ct__option-name";
                name.InnerText = cat.Name;

                card.Controls.Add(icon);
                card.Controls.Add(name);
                categoryCards.Controls.Add(card);
            }
        }

        // ====================================================================
        // Form helpers.
        // ====================================================================

        private void RenderRequestTypeCards(List<TicketLookupItemDto> types)
        {
            requestTypeCards.Controls.Clear();
            foreach (var rt in types)
            {
                var card = new HtmlGenericControl("div");
                card.Attributes["class"] = "itsm-ct__option";
                card.Attributes["tabindex"] = "0";
                card.Attributes["role"] = "button";
                card.Attributes["aria-label"] = "Select request type: " + rt.Name;
                card.Attributes["data-id"] = rt.Id.ToString(CultureInfo.InvariantCulture);

                var icon = new HtmlGenericControl("span");
                icon.Attributes["class"] = "itsm-ct__option-icon";
                icon.InnerHtml = "<i class='fa fa-file-text-o'></i>";

                var name = new HtmlGenericControl("span");
                name.Attributes["class"] = "itsm-ct__option-name";
                name.InnerText = rt.Name;

                card.Controls.Add(icon);
                card.Controls.Add(name);
                requestTypeCards.Controls.Add(card);
            }
        }

        private void ResetFormInputs()
        {
            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            hfSelectedRequestTypeId.Value = string.Empty;
        }

        private void ClearMessage()
        {
            lblMessage.Text = string.Empty;
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = message;
            lblMessage.ForeColor = success ? System.Drawing.Color.Green : System.Drawing.Color.Red;
        }
    }
}
