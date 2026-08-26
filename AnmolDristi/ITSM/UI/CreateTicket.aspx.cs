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
// When: 2026-08-26 (submit reliability)
// What: Recreate dynamic master-data controls on every request (including
//       postback). Never disable btnSubmit. Restore selected card/button
//       classes from HiddenFields after a failed POST.
// Why:  HtmlGenericControl children are not rebuilt from ViewState. A
//       disabled submit input is omitted from the POST, so Click never
//       fires. Enabled=false at the start of Click left the button stuck
//       after any server-side validation or exception.
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

            RestoreSubmitButton();

            // Dynamic HtmlGenericControl cards/buttons are not restored from
            // ViewState. Recreate them on every request, including postback,
            // so a failed submit does not render an empty form.
            EnsureMasterDataControls();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            var diag = new CreateTicketDiagnostics();

            // Do not disable the submit control. A failed request must leave
            // it usable. Page.Trace is TraceContext — use System.Diagnostics.Trace.
            RestoreSubmitButton();

            try
            {
                diag.BeginStage("Request received");
                ClearMessage();

                if (!Page.IsValid)
                {
                    diag.EndStage();
                    System.Diagnostics.Trace.TraceInformation(diag.GetSummary());
                    RestoreSubmitButton();
                    return;
                }

                if (Session["WORKMAN"] == null)
                {
                    diag.EndStage();
                    Response.Redirect("~/login.aspx");
                    return;
                }

                var requesterCode = Session["WORKMAN"].ToString();
                diag.EndStage();

                // ── Parse hidden fields ──────────────────────────────────────
                diag.BeginStage("Field parsing");
                int categoryId;
                int subCategoryId;
                int impactId;
                int urgencyId;

                if (!int.TryParse(hfSelectedCategoryId.Value, out categoryId)
                    || !int.TryParse(hfSelectedSubCategoryId.Value, out subCategoryId)
                    || !int.TryParse(hfSelectedImpactId.Value, out impactId)
                    || !int.TryParse(hfSelectedUrgencyId.Value, out urgencyId))
                {
                    diag.FailStage(new InvalidOperationException("Required field parse failed"));
                    System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                    RestoreSubmitButton();
                    ShowMessage("Please complete all required fields.", false);
                    return;
                }
                diag.EndStage();

                // ── Hierarchy validation ─────────────────────────────────────
                diag.BeginStage("ValidateHierarchy");
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
                    diag.FailStage(new InvalidOperationException(hierarchyResult.Error));
                    System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                    RestoreSubmitButton();
                    ShowMessage(hierarchyResult.Error, false);
                    return;
                }
                diag.EndStage();

                // ── Master-data validation ───────────────────────────────────
                diag.BeginStage("ValidateMasterDataIds");
                int requestTypeId;
                if (!int.TryParse(hfSelectedRequestTypeId.Value, out requestTypeId))
                {
                    diag.FailStage(new InvalidOperationException("RequestType parse failed"));
                    System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                    RestoreSubmitButton();
                    ShowMessage("Please select a valid request type.", false);
                    return;
                }

                var masterDataResult = masterService.ValidateMasterDataIds(
                    impactId, urgencyId, requestTypeId);

                if (!masterDataResult.IsValid)
                {
                    diag.FailStage(new InvalidOperationException(masterDataResult.Error));
                    System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                    RestoreSubmitButton();
                    ShowMessage(masterDataResult.Error, false);
                    return;
                }
                diag.EndStage();

                // ── TicketService.CreateTicket ────────────────────────────────
                diag.BeginStage("TicketService.CreateTicket");
                var ticketService = new TicketService();

                Action<string> onBllBegin = stage => diag.BeginStage(stage);
                Action<string, long> onBllEnd = (stage, ms) => diag.EndStage();

                int newTicketId = ticketService.CreateTicket(
                    requesterCode,
                    requestTypeId,
                    txtTitle.Text.Trim(),
                    txtDescription.Text.Trim(),
                    categoryId,
                    subCategoryId,
                    itemId,
                    impactId,
                    urgencyId,
                    onBllBegin,
                    onBllEnd);

                diag.EndStage();

                diag.Complete();
                System.Diagnostics.Trace.TraceInformation(diag.GetSummary());

                string redirectUrl = ResolveUrl("~/ITSM/UI/TicketDetail.aspx?id="
                    + newTicketId.ToString(CultureInfo.InvariantCulture));
                Response.Redirect(redirectUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            catch (InvalidOperationException ex)
            {
                RestoreSubmitButton();
                diag.FailStage(ex);
                System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                ShowMessage(ex.Message + " Reference: " + diag.CorrelationId, false);
            }
            catch (ArgumentException ex)
            {
                RestoreSubmitButton();
                diag.FailStage(ex);
                System.Diagnostics.Trace.TraceWarning(diag.GetSummary());
                ShowMessage(ex.Message + " Reference: " + diag.CorrelationId, false);
            }
            catch (Exception ex)
            {
                RestoreSubmitButton();
                diag.FailStage(ex);
                System.Diagnostics.Trace.TraceError(diag.GetSummary());
                ShowMessage(diag.GetUserFacingMessage(), false);
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
        // Master-data controls — recreated on GET and POST.
        // ====================================================================

        /// <summary>
        /// Rebuilds Request Type / Category / Impact / Urgency controls.
        /// Category hierarchy JSON is reused from <see cref="hfCategoryData"/>
        /// on postback to avoid repeating the nested master-data query.
        /// Lookup lists (request types, impacts, urgencies) are small and
        /// re-read via BLL. HiddenField values remain the selection source.
        /// </summary>
        private void EnsureMasterDataControls()
        {
            var masterService = new TicketMasterDataService();

            List<TicketCategoryDto> categories = TryReadPostedCategoryData();
            if (categories == null)
            {
                var workman = Session["WORKMAN"] != null
                    ? Session["WORKMAN"].ToString()
                    : string.Empty;
                int companyId = masterService.ResolveCompanyId(workman);
                categories = masterService.GetCategoryHierarchy(companyId);
                hfCategoryData.Value = _serializer.Serialize(categories);
            }

            int selectedCategoryId = ParseHiddenId(hfSelectedCategoryId.Value);
            int selectedRequestTypeId = ParseHiddenId(hfSelectedRequestTypeId.Value);
            int selectedImpactId = ParseHiddenId(hfSelectedImpactId.Value);
            int selectedUrgencyId = ParseHiddenId(hfSelectedUrgencyId.Value);

            RenderCategoryCards(categories, selectedCategoryId);
            RenderRequestTypeCards(masterService.GetRequestTypes(), selectedRequestTypeId);
            BindImpacts(masterService.GetImpacts(), selectedImpactId);
            BindUrgencies(masterService.GetUrgencies(), selectedUrgencyId);
        }

        private List<TicketCategoryDto> TryReadPostedCategoryData()
        {
            if (!IsPostBack || string.IsNullOrEmpty(hfCategoryData.Value))
            {
                return null;
            }

            try
            {
                var parsed = _serializer.Deserialize<List<TicketCategoryDto>>(
                    hfCategoryData.Value);
                if (parsed == null || parsed.Count == 0)
                {
                    return null;
                }

                return parsed;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (Exception)
            {
                // Malformed posted JSON — reload hierarchy from BLL.
                return null;
            }
        }

        private static int ParseHiddenId(string value)
        {
            int id;
            if (int.TryParse(value, out id) && id > 0)
            {
                return id;
            }

            return 0;
        }

        private void BindImpacts(List<TicketLookupItemDto> impacts, int selectedId)
        {
            impactButtons.Controls.Clear();
            foreach (var impact in impacts)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = ButtonClass(impact.Id, selectedId);
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

        private void BindUrgencies(List<TicketLookupItemDto> urgencies, int selectedId)
        {
            urgencyButtons.Controls.Clear();
            foreach (var urgency in urgencies)
            {
                var btn = new HtmlGenericControl("span");
                btn.Attributes["class"] = ButtonClass(urgency.Id, selectedId);
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

        private void RenderCategoryCards(List<TicketCategoryDto> categories, int selectedId)
        {
            categoryCards.Controls.Clear();
            foreach (var cat in categories)
            {
                var card = new HtmlGenericControl("div");
                card.Attributes["class"] = OptionClass(cat.Id, selectedId);
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

        private void RenderRequestTypeCards(List<TicketLookupItemDto> types, int selectedId)
        {
            requestTypeCards.Controls.Clear();
            foreach (var rt in types)
            {
                var card = new HtmlGenericControl("div");
                card.Attributes["class"] = OptionClass(rt.Id, selectedId);
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

        private static string OptionClass(int id, int selectedId)
        {
            if (selectedId > 0 && id == selectedId)
            {
                return "itsm-ct__option itsm-ct__option--selected";
            }

            return "itsm-ct__option";
        }

        private static string ButtonClass(int id, int selectedId)
        {
            if (selectedId > 0 && id == selectedId)
            {
                return "itsm-ct__btn itsm-ct__btn--selected";
            }

            return "itsm-ct__btn";
        }

        private void RestoreSubmitButton()
        {
            btnSubmit.Enabled = true;
            btnSubmit.Text = "Create Ticket";
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
