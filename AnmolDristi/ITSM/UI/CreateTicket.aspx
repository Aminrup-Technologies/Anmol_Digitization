<%-- ========================================================================
     File:    CreateTicket.aspx
     Layer:   UI — Presentation
     When:    2026-08-26 (submit reliability)
     What:    onSubmit() must not disable the ASP.NET submit input. A disabled
              submit control is omitted from the POST, so btnSubmit_Click never
              runs and the page stays on "Creating...". Busy state is the
              non-submit #submitSpinner plus a JS _submitting flag.
     Why:     Web Forms postback requires the clicked submit control to remain
              a successful form control until the request is sent.
     When:    2026-08-26 (semantic colour UX)
     What:    Request Type = blue, Root Category = Dristi green, Impact =
              green/amber/red, Urgency = blue/amber/red. Selected state uses
              a stronger semantic border + checkmark badge. SubCategory/Item
              stay neutral Dristi dropdowns. Visual-only CSS/JS.
     Why:     Product UX — colour encodes meaning without changing postback,
              validation, HiddenFields, or server rendering of card IDs.
     ======================================================================== --%>
<%@ Page Title="ITSM | Create Ticket" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="CreateTicket.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.CreateTicket" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ================================================================
           CreateTicket — scoped BEM CSS (.itsm-ct)
           Semantic colours: Dristi/Gentelella tokens with hex fallbacks.
           ================================================================ */
        .itsm-ct {
            --itsm-blue: #3498DB;
            --itsm-blue-dark: #2471A3;
            --itsm-green: #1ABB9C;
            --itsm-green-dark: #148F77;
            --itsm-amber: #F39C12;
            --itsm-amber-dark: #B9770E;
            --itsm-red: #E74C3C;
            --itsm-red-dark: #C0392B;
            --itsm-text: #333333;
        }

        /* --- Section layout --- */
        .itsm-ct__section { margin-bottom: 24px; }
        .itsm-ct__section--disabled { opacity: 0.45; pointer-events: none; }
        .itsm-ct__heading {
            display: flex; align-items: center; gap: 8px;
            font-size: 14px; font-weight: 700; color: #333;
            margin-bottom: 10px; padding-bottom: 6px;
            border-bottom: 2px solid #e8e8e8;
        }
        .itsm-ct__step {
            display: inline-flex; align-items: center; justify-content: center;
            width: 22px; height: 22px; border-radius: 50%;
            background: #1ABB9C; background: var(--itsm-green); color: #fff;
            font-size: 12px; font-weight: 700; flex-shrink: 0;
        }
        .itsm-ct__step--active { background: #1ABB9C; background: var(--itsm-green); }
        .itsm-ct__step--done { background: #26b99a; }
        .itsm-ct__label { font-size: 12px; color: #999; margin-left: 4px; }

        /* --- Selectable option cards --- */
        .itsm-ct__options {
            display: grid; gap: 10px;
            grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        }
        .itsm-ct__option {
            position: relative;
            display: flex; flex-direction: column; align-items: center;
            gap: 6px; padding: 14px 16px 14px 12px;
            border: 2px solid #d9d9d9; border-radius: 8px;
            background: #fafafa; cursor: pointer;
            transition: border-color 0.15s, background 0.15s, box-shadow 0.15s, transform 0.15s;
            text-align: center; user-select: none;
            min-height: 72px; justify-content: center;
        }
        .itsm-ct__option-icon { font-size: 22px; color: #6c757d; }
        .itsm-ct__option-name {
            font-size: 13px; font-weight: 600; color: #333; color: var(--itsm-text);
            line-height: 1.2;
        }
        .itsm-ct__option--selected::after,
        .itsm-ct__btn--selected::after {
            content: "\f00c";
            font-family: FontAwesome;
            position: absolute;
            top: 4px; right: 6px;
            width: 16px; height: 16px; line-height: 16px;
            font-size: 10px; text-align: center;
            border-radius: 50%;
            color: #fff;
        }

        /* Request Type — blue */
        .itsm-ct__options--request-type .itsm-ct__option-icon { color: #3498DB; color: var(--itsm-blue); }
        .itsm-ct__options--request-type .itsm-ct__option:hover {
            border-color: #3498DB; border-color: var(--itsm-blue);
            background: rgba(52, 152, 219, 0.08);
            transform: scale(1.02);
            box-shadow: 0 4px 12px rgba(52, 152, 219, 0.22);
        }
        .itsm-ct__options--request-type .itsm-ct__option:focus {
            outline: 2px solid #3498DB; outline: 2px solid var(--itsm-blue); outline-offset: 2px;
        }
        .itsm-ct__options--request-type .itsm-ct__option--selected {
            border-width: 3px;
            border-color: #2471A3; border-color: var(--itsm-blue-dark);
            background: rgba(52, 152, 219, 0.10);
            box-shadow: 0 0 0 1px rgba(52, 152, 219, 0.25);
        }
        .itsm-ct__options--request-type .itsm-ct__option--selected .itsm-ct__option-icon,
        .itsm-ct__options--request-type .itsm-ct__option--selected .itsm-ct__option-name {
            color: #2471A3; color: var(--itsm-blue-dark); font-weight: 700;
        }
        .itsm-ct__options--request-type .itsm-ct__option--selected::after {
            background: #3498DB; background: var(--itsm-blue);
        }

        /* Root Category — Dristi green */
        .itsm-ct__options--category .itsm-ct__option-icon { color: #1ABB9C; color: var(--itsm-green); }
        .itsm-ct__options--category .itsm-ct__option:hover {
            border-color: #1ABB9C; border-color: var(--itsm-green);
            background: rgba(26, 187, 156, 0.08);
            transform: scale(1.02);
            box-shadow: 0 4px 12px rgba(26, 187, 156, 0.22);
        }
        .itsm-ct__options--category .itsm-ct__option:focus {
            outline: 2px solid #1ABB9C; outline: 2px solid var(--itsm-green); outline-offset: 2px;
        }
        .itsm-ct__options--category .itsm-ct__option--selected {
            border-width: 3px;
            border-color: #148F77; border-color: var(--itsm-green-dark);
            background: rgba(26, 187, 156, 0.10);
            box-shadow: 0 0 0 1px rgba(26, 187, 156, 0.25);
        }
        .itsm-ct__options--category .itsm-ct__option--selected .itsm-ct__option-icon,
        .itsm-ct__options--category .itsm-ct__option--selected .itsm-ct__option-name {
            color: #148F77; color: var(--itsm-green-dark); font-weight: 700;
        }
        .itsm-ct__options--category .itsm-ct__option--selected::after {
            background: #1ABB9C; background: var(--itsm-green);
        }

        /* --- Impact / Urgency button groups --- */
        .itsm-ct__btn-group { display: flex; flex-wrap: wrap; gap: 8px; }
        .itsm-ct__btn {
            position: relative;
            display: inline-flex; align-items: center; gap: 6px;
            padding: 8px 22px 8px 12px; border: 2px solid #d9d9d9; border-radius: 6px;
            background: #fafafa; cursor: pointer; font-size: 13px; font-weight: 500;
            color: #555;
            transition: border-color 0.15s, background 0.15s, color 0.15s, box-shadow 0.15s, transform 0.15s;
            user-select: none;
        }
        .itsm-ct__btn-icon { font-size: 14px; }
        .itsm-ct__btn:hover { transform: scale(1.02); }
        .itsm-ct__btn:focus { outline: 2px solid #1ABB9C; outline-offset: 2px; }

        /* Impact Low / Urgency Medium-High share level tokens */
        .itsm-ct__btn-group--impact .itsm-ct__btn--low {
            border-color: rgba(26, 187, 156, 0.55); color: #148F77; color: var(--itsm-green-dark);
            background: rgba(26, 187, 156, 0.08);
        }
        .itsm-ct__btn-group--impact .itsm-ct__btn--low .itsm-ct__btn-icon { color: #1ABB9C; color: var(--itsm-green); }
        .itsm-ct__btn-group--impact .itsm-ct__btn--low:hover {
            border-color: #1ABB9C; border-color: var(--itsm-green);
            box-shadow: 0 4px 10px rgba(26, 187, 156, 0.22);
        }
        .itsm-ct__btn-group--impact .itsm-ct__btn--low:focus { outline-color: #1ABB9C; }
        .itsm-ct__btn-group--impact .itsm-ct__btn--low.itsm-ct__btn--selected {
            border-width: 3px; border-color: #148F77; border-color: var(--itsm-green-dark);
            background: rgba(26, 187, 156, 0.14); color: #0E5C4C;
        }
        .itsm-ct__btn-group--impact .itsm-ct__btn--low.itsm-ct__btn--selected::after {
            background: #1ABB9C; background: var(--itsm-green);
        }

        .itsm-ct__btn-group--urgency .itsm-ct__btn--low {
            border-color: rgba(52, 152, 219, 0.55); color: #2471A3; color: var(--itsm-blue-dark);
            background: rgba(52, 152, 219, 0.08);
        }
        .itsm-ct__btn-group--urgency .itsm-ct__btn--low .itsm-ct__btn-icon { color: #3498DB; color: var(--itsm-blue); }
        .itsm-ct__btn-group--urgency .itsm-ct__btn--low:hover {
            border-color: #3498DB; border-color: var(--itsm-blue);
            box-shadow: 0 4px 10px rgba(52, 152, 219, 0.22);
        }
        .itsm-ct__btn-group--urgency .itsm-ct__btn--low:focus { outline-color: #3498DB; }
        .itsm-ct__btn-group--urgency .itsm-ct__btn--low.itsm-ct__btn--selected {
            border-width: 3px; border-color: #2471A3; border-color: var(--itsm-blue-dark);
            background: rgba(52, 152, 219, 0.14); color: #1A5276;
        }
        .itsm-ct__btn-group--urgency .itsm-ct__btn--low.itsm-ct__btn--selected::after {
            background: #3498DB; background: var(--itsm-blue);
        }

        .itsm-ct__btn--medium {
            border-color: rgba(243, 156, 18, 0.55); color: #B9770E; color: var(--itsm-amber-dark);
            background: rgba(243, 156, 18, 0.08);
        }
        .itsm-ct__btn--medium .itsm-ct__btn-icon { color: #F39C12; color: var(--itsm-amber); }
        .itsm-ct__btn--medium:hover {
            border-color: #F39C12; border-color: var(--itsm-amber);
            box-shadow: 0 4px 10px rgba(243, 156, 18, 0.22);
        }
        .itsm-ct__btn--medium:focus { outline-color: #F39C12; }
        .itsm-ct__btn--medium.itsm-ct__btn--selected {
            border-width: 3px; border-color: #B9770E; border-color: var(--itsm-amber-dark);
            background: rgba(243, 156, 18, 0.14); color: #7D6608;
        }
        .itsm-ct__btn--medium.itsm-ct__btn--selected::after {
            background: #F39C12; background: var(--itsm-amber);
        }

        .itsm-ct__btn--high {
            border-color: rgba(231, 76, 60, 0.55); color: #C0392B; color: var(--itsm-red-dark);
            background: rgba(231, 76, 60, 0.08);
        }
        .itsm-ct__btn--high .itsm-ct__btn-icon { color: #E74C3C; color: var(--itsm-red); }
        .itsm-ct__btn--high:hover {
            border-color: #E74C3C; border-color: var(--itsm-red);
            box-shadow: 0 4px 10px rgba(231, 76, 60, 0.22);
        }
        .itsm-ct__btn--high:focus { outline-color: #E74C3C; }
        .itsm-ct__btn--high.itsm-ct__btn--selected {
            border-width: 3px; border-color: #C0392B; border-color: var(--itsm-red-dark);
            background: rgba(231, 76, 60, 0.14); color: #922B21;
        }
        .itsm-ct__btn--high.itsm-ct__btn--selected::after {
            background: #E74C3C; background: var(--itsm-red);
        }

        /* --- Form elements (SubCategory / Item stay Dristi-neutral) --- */
        .itsm-ct__field { margin-bottom: 14px; }
        .itsm-ct__field-label {
            display: block; font-size: 13px; font-weight: 600;
            color: #555; margin-bottom: 4px;
        }
        .itsm-ct__field-label .itsm-ct__required { color: #dc3545; margin-left: 2px; }
        .itsm-ct__field-input {
            width: 100%; max-width: 480px;
        }

        /* --- Error banner --- */
        .itsm-ct__error {
            background: #f8d7da; border: 1px solid #f5c6cb; color: #721c24;
            padding: 8px 14px; border-radius: 4px; font-size: 13px;
            margin-bottom: 14px; display: none;
        }

        /* --- Submit area --- */
        .itsm-ct__actions {
            display: flex; gap: 12px; align-items: center; padding-top: 8px;
        }
        .itsm-ct__submit { padding: 10px 28px; font-size: 15px; font-weight: 600; }
        .itsm-ct__cancel { padding: 10px 20px; font-size: 14px; }
        .itsm-ct__spinner {
            display: none; margin-left: 12px; color: #666; font-size: 13px;
        }

        /* --- Dependent section states --- */
        .itsm-ct__dependent--locked .itsm-ct__field { display: none; }
        .itsm-ct__dependent--locked .itsm-ct__lock-msg { display: flex; }
        .itsm-ct__dependent--locked .itsm-ct__heading { opacity: 0.45; }
        .itsm-ct__dependent--unlocked .itsm-ct__lock-msg { display: none; }
        .itsm-ct__lock-msg {
            display: none; align-items: center; gap: 6px;
            padding: 8px 12px; margin-top: 4px;
            background: #f8f9fa; border: 1px dashed #ccc; border-radius: 4px;
            font-size: 12px; color: #888;
        }
        .itsm-ct__lock-msg i { font-size: 11px; }

        @media (max-width: 767px) {
            .itsm-ct__options {
                grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
            }
            .itsm-ct__btn { width: 100%; justify-content: flex-start; }
        }
        @media (prefers-reduced-motion: reduce) {
            .itsm-ct__option, .itsm-ct__btn {
                transition: none;
            }
            .itsm-ct__option:hover, .itsm-ct__btn:hover {
                transform: none;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-ticket"></i> Create ITSM Ticket</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content itsm-ct">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 15px;"></asp:Label>

                        <div id="clientError" class="itsm-ct__error"></div>

                        <%-- ============================================================
                             STEP 1 — Request Type (segmented cards)
                             ============================================================ --%>
                        <div class="itsm-ct__section" id="secRequestType">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">1</span> Request Type
                                <span class="itsm-ct__label">* required</span>
                            </div>
                            <div class="itsm-ct__options itsm-ct__options--request-type" id="requestTypeCards" runat="server">
                                <%-- Server-rendered request-type cards --%>
                            </div>
                            <asp:HiddenField ID="hfSelectedRequestTypeId" runat="server" />
                        </div>

                        <%-- ============================================================
                             STEP 2 — Root Category (clickable cards)
                             ============================================================ --%>
                        <div class="itsm-ct__section" id="secCategory">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">2</span> Root Category
                                <span class="itsm-ct__label">* required</span>
                            </div>
                            <div class="itsm-ct__options itsm-ct__options--category" id="categoryCards" runat="server">
                                <%-- Server-rendered category cards --%>
                            </div>
                            <asp:HiddenField ID="hfSelectedCategoryId" runat="server" />
                            <asp:HiddenField ID="hfSelectedSubCategoryId" runat="server" />
                            <asp:HiddenField ID="hfSelectedItemId" runat="server" />
                        </div>

                        <%-- ============================================================
                             STEP 3 — Sub Category (client-side filtered dropdown)
                             ============================================================ --%>
                        <div class="itsm-ct__section itsm-ct__dependent itsm-ct__dependent--locked" id="secSubCategory">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">3</span> Sub Category
                                <span class="itsm-ct__label">* required</span>
                            </div>
                            <div class="itsm-ct__lock-msg"><i class="fa fa-lock"></i> Select a root category first</div>
                            <div class="itsm-ct__field">
                                <select id="ddlSubCategoryClient"
                                    class="form-control itsm-ct__field-input"
                                    onchange="ITSM.CreateTicket.onSubCategoryChange(this.value)">
                                    <option value="">-- Select Sub Category --</option>
                                </select>
                            </div>
                        </div>

                        <%-- ============================================================
                             STEP 4 — Item (client-side filtered, optional)
                             ============================================================ --%>
                        <div class="itsm-ct__section itsm-ct__dependent itsm-ct__dependent--locked" id="secItem">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">4</span> Item
                                <span class="itsm-ct__label">optional</span>
                            </div>
                            <div class="itsm-ct__lock-msg"><i class="fa fa-lock"></i> Select a sub-category first</div>
                            <div class="itsm-ct__field">
                                <select id="ddlItemClient"
                                    class="form-control itsm-ct__field-input"
                                    onchange="ITSM.CreateTicket.onItemChange(this.value)">
                                    <option value="">-- Select Item (optional) --</option>
                                </select>
                            </div>
                        </div>

                        <%-- ============================================================
                             STEP 5 — Impact & Urgency (button groups)
                             ============================================================ --%>
                        <div class="itsm-ct__section" id="secImpactUrgency">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">5</span> Impact &amp; Urgency
                                <span class="itsm-ct__label">* required</span>
                            </div>
                            <div class="row">
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="itsm-ct__field-label">Impact <span class="itsm-ct__required">*</span></div>
                                    <div class="itsm-ct__btn-group itsm-ct__btn-group--impact" id="impactButtons" runat="server"></div>
                                    <asp:HiddenField ID="hfSelectedImpactId" runat="server" />
                                </div>
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="itsm-ct__field-label">Urgency <span class="itsm-ct__required">*</span></div>
                                    <div class="itsm-ct__btn-group itsm-ct__btn-group--urgency" id="urgencyButtons" runat="server"></div>
                                    <asp:HiddenField ID="hfSelectedUrgencyId" runat="server" />
                                </div>
                            </div>
                        </div>

                        <%-- ============================================================
                             STEP 6 — Ticket Details
                             ============================================================ --%>
                        <div class="itsm-ct__section" id="secDetails">
                            <div class="itsm-ct__heading">
                                <span class="itsm-ct__step">6</span> Ticket Details
                                <span class="itsm-ct__label">* Title required</span>
                            </div>
                            <div class="itsm-ct__field">
                                <label class="itsm-ct__field-label" for="<%= txtTitle.ClientID %>">
                                    Title <span class="itsm-ct__required">*</span>
                                </label>
                                <asp:TextBox ID="txtTitle" runat="server"
                                    CssClass="form-control itsm-ct__field-input"
                                    MaxLength="200" placeholder="Brief summary of the issue" />
                                <asp:RequiredFieldValidator ID="rfvTitle" runat="server"
                                    ControlToValidate="txtTitle"
                                    ErrorMessage="Title is required."
                                    Display="Dynamic" CssClass="validation-msg"
                                    ValidationGroup="CreateTicket" />
                            </div>
                            <div class="itsm-ct__field">
                                <label class="itsm-ct__field-label" for="<%= txtDescription.ClientID %>">Description</label>
                                <asp:TextBox ID="txtDescription" runat="server"
                                    CssClass="form-control itsm-ct__field-input"
                                    TextMode="MultiLine" Rows="5"
                                    placeholder="Describe the issue, steps to reproduce, and impact" />
                            </div>
                        </div>

                        <%-- ============================================================
                             SUBMIT
                             ============================================================ --%>
                        <div class="itsm-ct__section">
                            <div class="ln_solid"></div>
                            <div class="itsm-ct__actions">
                                <asp:Button ID="btnSubmit" runat="server" Text="Create Ticket"
                                    CssClass="btn btn-success itsm-ct__submit"
                                    ValidationGroup="CreateTicket"
                                    OnClientClick="return ITSM.CreateTicket.onSubmit();"
                                    OnClick="btnSubmit_Click" />
                                <asp:Button ID="btnReset" runat="server" Text="Cancel"
                                    CssClass="btn btn-default itsm-ct__cancel"
                                    CausesValidation="false"
                                    OnClientClick="ITSM.CreateTicket.onReset(); return false;" />
                                <span id="submitSpinner" class="itsm-ct__spinner">
                                    <i class="fa fa-spinner fa-spin"></i> Creating ticket...
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="hfCategoryData" runat="server" />

    <script type="text/javascript">
        var ITSM = ITSM || {};
        ITSM.CreateTicket = (function () {
            var _catData = [];
            var _submitting = false;
            var _el = {
                catCards: '<%= categoryCards.ClientID %>',
                rtCards: '<%= requestTypeCards.ClientID %>',
                hfCat: '<%= hfSelectedCategoryId.ClientID %>',
                hfSub: '<%= hfSelectedSubCategoryId.ClientID %>',
                hfItem: '<%= hfSelectedItemId.ClientID %>',
                hfImpact: '<%= hfSelectedImpactId.ClientID %>',
                hfUrgency: '<%= hfSelectedUrgencyId.ClientID %>',
                hfRt: '<%= hfSelectedRequestTypeId.ClientID %>',
                impactBtns: '<%= impactButtons.ClientID %>',
                urgencyBtns: '<%= urgencyButtons.ClientID %>',
                btnSubmit: '<%= btnSubmit.ClientID %>'
            };

            function init() {
                var raw = document.getElementById('<%= hfCategoryData.ClientID %>');
                if (raw && raw.value) {
                    try { _catData = JSON.parse(raw.value); } catch (e) { _catData = []; }
                }
                attachHandlers();
                applySemanticStyles();
                restoreFromHidden();
            }

            function attachHandlers() {
                // Request Type cards — event delegation
                var rtContainer = document.getElementById(_el.rtCards);
                if (rtContainer) {
                    rtContainer.addEventListener('click', function (e) {
                        var card = e.target.closest('.itsm-ct__option');
                        if (card) onRequestTypeSelect(parseInt(card.getAttribute('data-id'), 10));
                    });
                    rtContainer.addEventListener('keydown', function (e) {
                        if (e.key === 'Enter' || e.key === ' ') {
                            var card = e.target.closest('.itsm-ct__option');
                            if (card) { e.preventDefault(); onRequestTypeSelect(parseInt(card.getAttribute('data-id'), 10)); }
                        }
                    });
                }
                // Category cards — event delegation
                var catContainer = document.getElementById(_el.catCards);
                if (catContainer) {
                    catContainer.addEventListener('click', function (e) {
                        var card = e.target.closest('.itsm-ct__option');
                        if (card) onCategoryClick(parseInt(card.getAttribute('data-id'), 10));
                    });
                    catContainer.addEventListener('keydown', function (e) {
                        if (e.key === 'Enter' || e.key === ' ') {
                            var card = e.target.closest('.itsm-ct__option');
                            if (card) { e.preventDefault(); onCategoryClick(parseInt(card.getAttribute('data-id'), 10)); }
                        }
                    });
                }
            }

            function lockSection(id) {
                var el = document.getElementById(id);
                if (!el) return;
                el.classList.add('itsm-ct__dependent--locked');
                el.classList.remove('itsm-ct__dependent--unlocked');
            }
            function unlockSection(id) {
                var el = document.getElementById(id);
                if (!el) return;
                el.classList.remove('itsm-ct__dependent--locked');
                el.classList.add('itsm-ct__dependent--unlocked');
            }

            function onRequestTypeSelect(id) {
                document.getElementById(_el.hfRt).value = id;
                selectOptionCards(_el.rtCards, id);
            }

            function onCategoryClick(categoryId) {
                document.getElementById(_el.hfCat).value = categoryId;
                document.getElementById(_el.hfSub).value = '';
                document.getElementById(_el.hfItem).value = '';
                selectOptionCards(_el.catCards, categoryId);
                unlockSection('secSubCategory');
                populateSubCategories(categoryId);
                lockSection('secItem');
            }

            function populateSubCategories(catId) {
                var ddl = document.getElementById('ddlSubCategoryClient');
                ddl.innerHTML = '<option value="">-- Select Sub Category --</option>';
                for (var i = 0; i < _catData.length; i++) {
                    if (_catData[i].Id === catId) {
                        var subs = _catData[i].SubCategories || [];
                        for (var j = 0; j < subs.length; j++) {
                            var o = document.createElement('option');
                            o.value = subs[j].Id; o.text = subs[j].Name;
                            ddl.appendChild(o);
                        }
                        break;
                    }
                }
            }

            function onSubCategoryChange(val) {
                document.getElementById(_el.hfSub).value = val;
                document.getElementById(_el.hfItem).value = '';
                if (val) {
                    unlockSection('secItem');
                    populateItems(parseInt(val, 10));
                } else {
                    lockSection('secItem');
                }
            }

            function populateItems(subId) {
                var ddl = document.getElementById('ddlItemClient');
                ddl.innerHTML = '<option value="">-- Select Item (optional) --</option>';
                var catId = parseInt(document.getElementById(_el.hfCat).value, 10);
                for (var i = 0; i < _catData.length; i++) {
                    if (_catData[i].Id === catId) {
                        var subs = _catData[i].SubCategories || [];
                        for (var j = 0; j < subs.length; j++) {
                            if (subs[j].Id === subId) {
                                var items = subs[j].Items || [];
                                for (var k = 0; k < items.length; k++) {
                                    var o = document.createElement('option');
                                    o.value = items[k].Id; o.text = items[k].Name;
                                    ddl.appendChild(o);
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            function onItemChange(val) { document.getElementById(_el.hfItem).value = val; }

            function semanticLevel(text, index) {
                var t = (text || '').toLowerCase();
                if (t.indexOf('high') !== -1) return 'high';
                if (t.indexOf('medium') !== -1) return 'medium';
                if (t.indexOf('low') !== -1) return 'low';
                if (index === 0) return 'low';
                if (index === 1) return 'medium';
                return 'high';
            }

            function ensureBtnIcon(el, faClass) {
                if (!el || el.querySelector('.itsm-ct__btn-icon')) return;
                var icon = document.createElement('i');
                icon.className = 'fa ' + faClass + ' itsm-ct__btn-icon';
                icon.setAttribute('aria-hidden', 'true');
                el.insertBefore(icon, el.firstChild);
            }

            function applySemanticGroup(containerId, kind) {
                var icons = kind === 'impact'
                    ? { low: 'fa-user', medium: 'fa-users', high: 'fa-building' }
                    : { low: 'fa-clock-o', medium: 'fa-line-chart', high: 'fa-hand-paper-o' };
                var btns = document.querySelectorAll('#' + containerId + ' .itsm-ct__btn');
                for (var i = 0; i < btns.length; i++) {
                    var level = semanticLevel(btns[i].textContent, i);
                    btns[i].classList.add('itsm-ct__btn--' + level);
                    ensureBtnIcon(btns[i], icons[level]);
                }
            }

            function applySemanticStyles() {
                applySemanticGroup(_el.impactBtns, 'impact');
                applySemanticGroup(_el.urgencyBtns, 'urgency');
            }

            function selectBtnGroup(containerId, hfId, id) {
                document.getElementById(hfId).value = id;
                var btns = document.querySelectorAll('#' + containerId + ' .itsm-ct__btn');
                for (var i = 0; i < btns.length; i++) {
                    if (parseInt(btns[i].getAttribute('data-id'), 10) === id)
                        btns[i].classList.add('itsm-ct__btn--selected');
                    else btns[i].classList.remove('itsm-ct__btn--selected');
                }
            }

            function showError(msg) {
                var el = document.getElementById('clientError');
                el.textContent = msg; el.style.display = 'block';
            }
            function hideError() {
                var el = document.getElementById('clientError');
                el.textContent = ''; el.style.display = 'none';
            }

            function restoreFromHidden() {
                var rtVal = document.getElementById(_el.hfRt).value;
                if (rtVal) {
                    selectOptionCards(_el.rtCards, parseInt(rtVal, 10));
                }

                var catVal = document.getElementById(_el.hfCat).value;
                if (catVal) {
                    var catId = parseInt(catVal, 10);
                    selectOptionCards(_el.catCards, catId);
                    unlockSection('secSubCategory');
                    populateSubCategories(catId);
                    var subVal = document.getElementById(_el.hfSub).value;
                    if (subVal) {
                        var subDdl = document.getElementById('ddlSubCategoryClient');
                        if (subDdl) subDdl.value = subVal;
                        unlockSection('secItem');
                        populateItems(parseInt(subVal, 10));
                        var itemVal = document.getElementById(_el.hfItem).value;
                        var itemDdl = document.getElementById('ddlItemClient');
                        if (itemVal && itemDdl) itemDdl.value = itemVal;
                    }
                }

                var impactVal = document.getElementById(_el.hfImpact).value;
                if (impactVal) {
                    selectBtnGroup(_el.impactBtns, _el.hfImpact, parseInt(impactVal, 10));
                }
                var urgencyVal = document.getElementById(_el.hfUrgency).value;
                if (urgencyVal) {
                    selectBtnGroup(_el.urgencyBtns, _el.hfUrgency, parseInt(urgencyVal, 10));
                }
            }

            function selectOptionCards(containerId, id) {
                var cards = document.querySelectorAll('#' + containerId + ' .itsm-ct__option');
                for (var i = 0; i < cards.length; i++) {
                    var cid = parseInt(cards[i].getAttribute('data-id'), 10);
                    if (cid === id) cards[i].classList.add('itsm-ct__option--selected');
                    else cards[i].classList.remove('itsm-ct__option--selected');
                }
            }

            function onSubmit() {
                hideError();
                if (_submitting) {
                    return false;
                }

                var rt = document.getElementById(_el.hfRt).value;
                var cat = document.getElementById(_el.hfCat).value;
                var sub = document.getElementById(_el.hfSub).value;
                var impact = document.getElementById(_el.hfImpact).value;
                var urgency = document.getElementById(_el.hfUrgency).value;

                if (!rt) { showError('Please select a request type.'); return false; }
                if (!cat) { showError('Please select a category.'); return false; }
                if (!sub) { showError('Please select a sub category.'); return false; }
                if (!impact) { showError('Please select an impact level.'); return false; }
                if (!urgency) { showError('Please select an urgency level.'); return false; }

                // ASP.NET client validators (rfvTitle) must run BEFORE any busy
                // state. Failure: no POST, button stays a successful submit control.
                if (typeof Page_ClientValidate === 'function' &&
                    !Page_ClientValidate('CreateTicket')) {
                    return false;
                }

                // Do NOT disable the submit input before the postback. A
                // disabled submit is omitted from the POST, so ASP.NET never
                // raises btnSubmit_Click. After this handler returns true the
                // current submit is already in progress; delay-disable only
                // to block a second click on the still-visible page.
                _submitting = true;
                var sp = document.getElementById('submitSpinner');
                if (sp) sp.style.display = 'inline';
                var btn = document.getElementById(_el.btnSubmit);
                if (btn) {
                    window.setTimeout(function () { btn.disabled = true; }, 0);
                }
                return true;
            }

            function onReset() {
                _submitting = false;
                hideError();
                // Clear hidden fields
                var hfs = [_el.hfRt, _el.hfCat, _el.hfSub, _el.hfItem, _el.hfImpact, _el.hfUrgency];
                for (var i = 0; i < hfs.length; i++) document.getElementById(hfs[i]).value = '';
                // Remove active from all option cards
                var opts = document.querySelectorAll('.itsm-ct__option--selected');
                for (var j = 0; j < opts.length; j++) opts[j].classList.remove('itsm-ct__option--selected');
                // Remove active from btn groups
                var btns = document.querySelectorAll('.itsm-ct__btn--selected');
                for (var k = 0; k < btns.length; k++) btns[k].classList.remove('itsm-ct__btn--selected');
                // Lock dependent sections
                lockSection('secSubCategory');
                lockSection('secItem');
                // Reset selects
                var subDdl = document.getElementById('ddlSubCategoryClient');
                if (subDdl) subDdl.innerHTML = '<option value="">-- Select Sub Category --</option>';
                var itemDdl = document.getElementById('ddlItemClient');
                if (itemDdl) itemDdl.innerHTML = '<option value="">-- Select Item (optional) --</option>';
            }

            return {
                init: init,
                onSubCategoryChange: onSubCategoryChange,
                onItemChange: onItemChange,
                onSubmit: onSubmit,
                onReset: onReset,
                onImpactSelect: function (id) { selectBtnGroup(_el.impactBtns, _el.hfImpact, id); },
                onUrgencySelect: function (id) { selectBtnGroup(_el.urgencyBtns, _el.hfUrgency, id); }
            };
        })();

        if (document.readyState === 'loading')
            document.addEventListener('DOMContentLoaded', ITSM.CreateTicket.init);
        else ITSM.CreateTicket.init();
    </script>
</asp:Content>
