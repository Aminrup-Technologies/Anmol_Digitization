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
     ======================================================================== --%>
<%@ Page Title="ITSM | Create Ticket" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="CreateTicket.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.CreateTicket" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ================================================================
           CreateTicket — scoped BEM CSS
           Root: .itsm-ct
           Theme-consistent: uses Dristi panel, button, form-control classes
           ================================================================ */

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
            background: #1ABB9C; color: #fff; font-size: 12px; font-weight: 700;
            flex-shrink: 0;
        }
        .itsm-ct__step--active { background: #1ABB9C; }
        .itsm-ct__step--done { background: #26b99a; }
        .itsm-ct__label { font-size: 12px; color: #999; margin-left: 4px; }

        /* --- Selectable option cards (Request Type + Category) --- */
        .itsm-ct__options {
            display: grid; gap: 10px;
            grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        }
        .itsm-ct__option {
            display: flex; flex-direction: column; align-items: center;
            gap: 6px; padding: 14px 12px;
            border: 2px solid #d9d9d9; border-radius: 8px;
            background: #fafafa; cursor: pointer;
            transition: border-color 0.15s, background 0.15s, box-shadow 0.15s;
            text-align: center; user-select: none;
            min-height: 72px; justify-content: center;
        }
        .itsm-ct__option:hover {
            border-color: #1ABB9C; background: #e8f8f2;
        }
        .itsm-ct__option:focus {
            outline: 2px solid #1ABB9C; outline-offset: 2px;
        }
        .itsm-ct__option--selected {
            border-color: #1ABB9C; background: #d4edda;
            box-shadow: 0 0 0 2px rgba(26, 187, 156, 0.25);
        }
        .itsm-ct__option--selected .itsm-ct__option-icon { color: #147a5e; }
        .itsm-ct__option--selected .itsm-ct__option-name { color: #147a5e; font-weight: 700; }
        .itsm-ct__option-icon { font-size: 22px; color: #6c757d; }
        .itsm-ct__option-name {
            font-size: 13px; font-weight: 600; color: #333; line-height: 1.2;
        }

        /* --- Impact / Urgency button groups --- */
        .itsm-ct__btn-group { display: flex; flex-wrap: wrap; gap: 8px; }
        .itsm-ct__btn {
            display: inline-flex; align-items: center; gap: 4px;
            padding: 7px 14px; border: 2px solid #d9d9d9; border-radius: 6px;
            background: #fafafa; cursor: pointer; font-size: 13px; font-weight: 500;
            color: #555; transition: border-color 0.15s, background 0.15s, color 0.15s;
            user-select: none;
        }
        .itsm-ct__btn:hover { border-color: #1ABB9C; background: #e8f8f2; }
        .itsm-ct__btn:focus { outline: 2px solid #1ABB9C; outline-offset: 2px; }
        .itsm-ct__btn--selected {
            border-color: #1ABB9C; background: #1ABB9C; color: #fff;
        }
        .itsm-ct__btn--selected:hover { background: #17a689; }

        /* --- Form elements --- */
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
                    <div class="x_content">
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
                            <div class="itsm-ct__options" id="requestTypeCards" runat="server">
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
                            <div class="itsm-ct__options" id="categoryCards" runat="server">
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
                                    <div class="itsm-ct__btn-group" id="impactButtons" runat="server"></div>
                                    <asp:HiddenField ID="hfSelectedImpactId" runat="server" />
                                </div>
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="itsm-ct__field-label">Urgency <span class="itsm-ct__required">*</span></div>
                                    <div class="itsm-ct__btn-group" id="urgencyButtons" runat="server"></div>
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
