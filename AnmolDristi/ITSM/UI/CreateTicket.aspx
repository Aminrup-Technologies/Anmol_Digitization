<%-- ========================================================================
     File:    CreateTicket.aspx
     Layer:   UI — Presentation
     When:    2026-08-24
     What:    Post/Redirect/Get after successful ticket creation; session
              gatekeeping now validates WORKMAN+USERID+BRANCH on every request.
     Why:     The post-success BindInitialLookups() rebind caused the POST to
              hang indefinitely. PRG eliminates the unnecessary re-render and
              navigates the user directly to the new ticket detail page.
     When:    2026-08-26 (UX refactor)
     What:    Eliminated postbacks for Category/SubCategory/Item selection.
              Category hierarchy loaded once as strongly typed DTOs, serialized
              via JavaScriptSerializer. Category cards rendered server-side once;
              JS uses event delegation for clicks and JSON for sub/item filtering.
              Impact/Urgency rendered as compact button groups.
     Why:     Product decision — faster ticket creation (5 fewer postbacks),
              better UX with clear category cards, zero unnecessary DB
              round-trips for dependent selections.
     ======================================================================== --%>
<%@ Page Title="ITSM | Create Ticket" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="CreateTicket.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.CreateTicket" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* --- Section layout --- */
        .ticket-section { margin-bottom: 28px; }
        .ticket-section-title {
            font-size: 14px; font-weight: 700; color: #333;
            margin-bottom: 10px; padding-bottom: 6px;
            border-bottom: 2px solid #e8e8e8;
            display: flex; align-items: center; gap: 8px;
        }
        .ticket-section-title .step-num {
            display: inline-flex; align-items: center; justify-content: center;
            width: 22px; height: 22px; border-radius: 50%;
            background: #17a2b8; color: #fff; font-size: 12px; font-weight: 700;
        }

        /* --- Category cards --- */
        .category-cards {
            display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 10px;
        }
        .category-card {
            display: flex; flex-direction: column; align-items: center; gap: 6px;
            padding: 14px 10px; border: 2px solid #d9d9d9; border-radius: 8px;
            background: #fafafa; cursor: pointer; transition: all 0.15s ease;
            text-align: center; user-select: none; min-height: 80px; justify-content: center;
        }
        .category-card:hover { border-color: #17a2b8; background: #e8f8fb; }
        .category-card:focus { outline: 2px solid #17a2b8; outline-offset: 2px; }
        .category-card.active {
            border-color: #17a2b8; background: #d1ecf1;
            box-shadow: 0 0 0 2px rgba(23, 162, 184, 0.25);
        }
        .category-card.active .category-icon { color: #0c5460; }
        .category-card .category-icon { font-size: 24px; color: #6c757d; }
        .category-card .category-name { font-size: 13px; font-weight: 600; color: #333; line-height: 1.2; }
        .category-card.active .category-name { color: #0c5460; }

        /* --- Impact / Urgency button groups --- */
        .btn-group-flat { display: flex; flex-wrap: wrap; gap: 8px; }
        .btn-group-flat .btn-flat {
            display: inline-flex; align-items: center; gap: 4px;
            padding: 7px 14px; border: 2px solid #d9d9d9; border-radius: 6px;
            background: #fafafa; cursor: pointer; font-size: 13px; font-weight: 500;
            color: #555; transition: all 0.15s ease; user-select: none;
        }
        .btn-group-flat .btn-flat:hover { border-color: #17a2b8; background: #e8f8fb; }
        .btn-group-flat .btn-flat:focus { outline: 2px solid #17a2b8; outline-offset: 2px; }
        .btn-group-flat .btn-flat.active { border-color: #17a2b8; background: #17a2b8; color: #fff; }

        /* --- Form styling --- */
        .ticket-form-label { font-size: 13px; font-weight: 600; color: #555; margin-bottom: 4px; }
        .ticket-form-label .required { color: #dc3545; margin-left: 2px; }
        .ticket-form-control {
            border-radius: 6px; border: 1px solid #ccc; padding: 8px 12px;
            font-size: 14px; transition: border-color 0.15s;
        }
        .ticket-form-control:focus { border-color: #17a2b8; box-shadow: 0 0 0 2px rgba(23, 162, 184, 0.15); }
        .validation-msg { font-size: 12px; margin-top: 3px; color: #dc3545; }

        /* --- Submit area --- */
        .submit-area { display: flex; gap: 12px; align-items: center; padding-top: 8px; }
        .btn-create-ticket { padding: 10px 28px; font-size: 15px; font-weight: 600; border-radius: 6px; }
        .btn-cancel-ticket { padding: 10px 20px; font-size: 14px; border-radius: 6px; }

        /* --- Dependent sections (shown/hidden by JS) --- */
        .dependent-section { display: none; }
        .dependent-section.visible { display: block; }

        /* --- Client-side error banner --- */
        .client-error-msg {
            background: #f8d7da; border: 1px solid #f5c6cb; color: #721c24;
            padding: 8px 14px; border-radius: 4px; font-size: 13px; margin-bottom: 12px;
            display: none;
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
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 15px;"></asp:Label>

                        <%-- Client-side validation error banner (replaces alert()) --%>
                        <div id="clientError" class="client-error-msg"></div>

                        <%-- ============================================================
                             SECTION 1 — Category (server-rendered cards, event delegation)
                             ============================================================ --%>
                        <div class="ticket-section" id="secCategory">
                            <div class="ticket-section-title">
                                <span class="step-num">1</span> Category
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div class="category-cards" id="categoryCards" runat="server">
                                <%-- Server-rendered category cards. JS attaches click handlers
                                     via event delegation — no client-side card recreation. --%>
                            </div>
                            <asp:HiddenField ID="hfSelectedCategoryId" runat="server" />
                            <asp:HiddenField ID="hfSelectedSubCategoryId" runat="server" />
                            <asp:HiddenField ID="hfSelectedItemId" runat="server" />
                            <asp:RequiredFieldValidator ID="rfvCategory" runat="server"
                                ControlToValidate="hfSelectedCategoryId" InitialValue=""
                                ErrorMessage="Please select a category."
                                Display="Dynamic" CssClass="validation-msg"
                                ValidationGroup="CreateTicket" />
                        </div>

                        <%-- SubCategory (appears after category selection, client-side filtered) --%>
                        <div class="ticket-section dependent-section" id="secSubCategory">
                            <div class="ticket-section-title">
                                <span class="step-num">2</span> Sub Category
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div style="max-width: 400px;">
                                <select id="ddlSubCategoryClient" class="form-control ticket-form-control"
                                    onchange="ITSM.CreateTicket.onSubCategoryChange(this.value)">
                                    <option value="">-- Select Sub Category --</option>
                                </select>
                            </div>
                            <asp:RequiredFieldValidator ID="rfvSubCategory" runat="server"
                                ControlToValidate="hfSelectedSubCategoryId" InitialValue=""
                                ErrorMessage="Please select a sub category."
                                Display="Dynamic" CssClass="validation-msg"
                                ValidationGroup="CreateTicket" />
                        </div>

                        <%-- Item (appears after subcategory selection, optional) --%>
                        <div class="ticket-section dependent-section" id="secItem">
                            <div class="ticket-section-title">
                                <span class="step-num">3</span> Item
                                <span style="font-size: 12px; color: #888;">optional</span>
                            </div>
                            <div style="max-width: 400px;">
                                <select id="ddlItemClient" class="form-control ticket-form-control"
                                    onchange="ITSM.CreateTicket.onItemChange(this.value)">
                                    <option value="">-- Select Item (optional) --</option>
                                </select>
                            </div>
                        </div>

                        <%-- ============================================================
                             SECTION 2 — Impact & Urgency (compact button groups)
                             ============================================================ --%>
                        <div class="ticket-section">
                            <div class="ticket-section-title">
                                <span class="step-num">4</span> Impact &amp; Urgency
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div class="row">
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="ticket-form-label">Impact <span class="required">*</span></div>
                                    <div class="btn-group-flat" id="impactButtons" runat="server">
                                        <%-- Server-rendered impact buttons --%>
                                    </div>
                                    <asp:HiddenField ID="hfSelectedImpactId" runat="server" />
                                    <asp:RequiredFieldValidator ID="rfvImpact" runat="server"
                                        ControlToValidate="hfSelectedImpactId" InitialValue=""
                                        ErrorMessage="Please select an impact level."
                                        Display="Dynamic" CssClass="validation-msg"
                                        ValidationGroup="CreateTicket" />
                                </div>
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="ticket-form-label">Urgency <span class="required">*</span></div>
                                    <div class="btn-group-flat" id="urgencyButtons" runat="server">
                                        <%-- Server-rendered urgency buttons --%>
                                    </div>
                                    <asp:HiddenField ID="hfSelectedUrgencyId" runat="server" />
                                    <asp:RequiredFieldValidator ID="rfvUrgency" runat="server"
                                        ControlToValidate="hfSelectedUrgencyId" InitialValue=""
                                        ErrorMessage="Please select an urgency level."
                                        Display="Dynamic" CssClass="validation-msg"
                                        ValidationGroup="CreateTicket" />
                                </div>
                            </div>
                        </div>

                        <%-- ============================================================
                             SECTION 3 — Request Type
                             ============================================================ --%>
                        <div class="ticket-section">
                            <div class="ticket-section-title">
                                <span class="step-num">5</span> Request Type
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div style="max-width: 400px;">
                                <asp:DropDownList ID="ddlRequestType" runat="server"
                                    CssClass="form-control ticket-form-control"
                                    AppendDataBoundItems="true">
                                    <asp:ListItem Text="-- Select Request Type --" Value="" />
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="rfvRequestType" runat="server"
                                    ControlToValidate="ddlRequestType" InitialValue=""
                                    ErrorMessage="Request Type is required."
                                    Display="Dynamic" CssClass="validation-msg"
                                    ValidationGroup="CreateTicket" />
                            </div>
                        </div>

                        <%-- ============================================================
                             SECTION 4 — Ticket Details
                             ============================================================ --%>
                        <div class="ticket-section">
                            <div class="ticket-section-title">
                                <span class="step-num">6</span> Ticket Details
                                <span class="text-danger" style="font-size: 12px;">* Title required</span>
                            </div>
                            <div class="row">
                                <div class="col-md-12 col-sm-12 col-xs-12" style="margin-bottom: 14px;">
                                    <div class="ticket-form-label">Title <span class="required">*</span></div>
                                    <asp:TextBox ID="txtTitle" runat="server"
                                        CssClass="form-control ticket-form-control"
                                        MaxLength="200"
                                        placeholder="Brief summary of the issue" />
                                    <asp:RequiredFieldValidator ID="rfvTitle" runat="server"
                                        ControlToValidate="txtTitle"
                                        ErrorMessage="Title is required."
                                        Display="Dynamic" CssClass="validation-msg"
                                        ValidationGroup="CreateTicket" />
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12 col-sm-12 col-xs-12">
                                    <div class="ticket-form-label">Description</div>
                                    <asp:TextBox ID="txtDescription" runat="server"
                                        CssClass="form-control ticket-form-control"
                                        TextMode="MultiLine" Rows="5"
                                        placeholder="Describe the issue, steps to reproduce, and impact" />
                                </div>
                            </div>
                        </div>

                        <%-- ============================================================
                             SECTION 5 — Submit
                             ============================================================ --%>
                        <div class="ticket-section">
                            <div class="ln_solid"></div>
                            <div class="submit-area">
                                <asp:Button ID="btnSubmit" runat="server" Text="Create Ticket"
                                    CssClass="btn btn-success btn-create-ticket"
                                    ValidationGroup="CreateTicket"
                                    OnClientClick="return ITSM.CreateTicket.onSubmit();"
                                    OnClick="btnSubmit_Click" />
                                <asp:Button ID="btnReset" runat="server" Text="Cancel"
                                    CssClass="btn btn-default btn-cancel-ticket"
                                    CausesValidation="false"
                                    OnClick="btnReset_Click" />
                                <span id="submitSpinner" style="display: none; margin-left: 12px; color: #666;">
                                    <i class="fa fa-spinner fa-spin"></i> Creating ticket...
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <%-- JSON data for client-side subcategory/item filtering only.
         Category cards are server-rendered; this JSON is NOT used to
         recreate them. --%>
    <asp:HiddenField ID="hfCategoryData" runat="server" />

    <script type="text/javascript">
        // ======================================================================
        // ITSM.CreateTicket — client-side dependent selection.
        //
        // Architecture:
        //   - Category cards: server-rendered, event-delegated click handlers
        //   - SubCategory/Item: filtered client-side from JSON in hfCategoryData
        //   - Impact/Urgency: server-rendered button groups, JS toggles active
        //   - No postbacks for any selection before submit
        //
        // Security: client-side filtering is UX only. Server validates all IDs
        // and hierarchy relationships on submit.
        // ======================================================================
        var ITSM = ITSM || {};
        ITSM.CreateTicket = (function () {
            var categoryData = [];

            function init() {
                var raw = document.getElementById('<%= hfCategoryData.ClientID %>');
                if (raw && raw.value) {
                    try { categoryData = JSON.parse(raw.value); } catch (e) { categoryData = []; }
                }
                attachCategoryCardHandlers();
            }

            // Event delegation: single handler on the container, not per-card.
            function attachCategoryCardHandlers() {
                var container = document.getElementById('<%= categoryCards.ClientID %>');
                if (!container) return;
                container.addEventListener('click', function (e) {
                    var card = e.target.closest('.category-card');
                    if (card) {
                        var id = parseInt(card.getAttribute('data-id'), 10);
                        if (!isNaN(id)) onCategoryClick(id);
                    }
                });
                container.addEventListener('keydown', function (e) {
                    if (e.key === 'Enter' || e.key === ' ') {
                        var card = e.target.closest('.category-card');
                        if (card) {
                            e.preventDefault();
                            var id = parseInt(card.getAttribute('data-id'), 10);
                            if (!isNaN(id)) onCategoryClick(id);
                        }
                    }
                });
            }

            function onCategoryClick(categoryId) {
                document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value = categoryId;
                document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = '';

                // Toggle active class on server-rendered cards
                var cards = document.querySelectorAll('#<%= categoryCards.ClientID %> .category-card');
                for (var i = 0; i < cards.length; i++) {
                    var id = parseInt(cards[i].getAttribute('data-id'), 10);
                    if (id === categoryId) {
                        cards[i].classList.add('active');
                    } else {
                        cards[i].classList.remove('active');
                    }
                }

                // Show subcategory section, populate from JSON
                document.getElementById('secSubCategory').classList.add('visible');
                populateSubCategories(categoryId);

                // Hide item section
                document.getElementById('secItem').classList.remove('visible');
            }

            function populateSubCategories(categoryId) {
                var ddl = document.getElementById('ddlSubCategoryClient');
                ddl.innerHTML = '<option value="">-- Select Sub Category --</option>';
                for (var i = 0; i < categoryData.length; i++) {
                    if (categoryData[i].Id === categoryId) {
                        var subs = categoryData[i].SubCategories || [];
                        for (var j = 0; j < subs.length; j++) {
                            var opt = document.createElement('option');
                            opt.value = subs[j].Id;
                            opt.text = subs[j].Name;
                            ddl.appendChild(opt);
                        }
                        break;
                    }
                }
            }

            function onSubCategoryChange(value) {
                document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value = value;
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = '';

                if (value) {
                    document.getElementById('secItem').classList.add('visible');
                    populateItems(parseInt(value, 10));
                } else {
                    document.getElementById('secItem').classList.remove('visible');
                }
            }

            function populateItems(subCategoryId) {
                var ddl = document.getElementById('ddlItemClient');
                ddl.innerHTML = '<option value="">-- Select Item (optional) --</option>';
                var catId = parseInt(document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value, 10);
                for (var i = 0; i < categoryData.length; i++) {
                    if (categoryData[i].Id === catId) {
                        var subs = categoryData[i].SubCategories || [];
                        for (var j = 0; j < subs.length; j++) {
                            if (subs[j].Id === subCategoryId) {
                                var items = subs[j].Items || [];
                                for (var k = 0; k < items.length; k++) {
                                    var opt = document.createElement('option');
                                    opt.value = items[k].Id;
                                    opt.text = items[k].Name;
                                    ddl.appendChild(opt);
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            function onItemChange(value) {
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = value;
            }

            // Impact / Urgency button group selection
            function selectButtonGroup(containerId, hiddenFieldId, id) {
                document.getElementById(hiddenFieldId).value = id;
                var btns = document.querySelectorAll('#' + containerId + ' .btn-flat');
                for (var i = 0; i < btns.length; i++) {
                    var btnId = parseInt(btns[i].getAttribute('data-id'), 10);
                    if (btnId === id) {
                        btns[i].classList.add('active');
                    } else {
                        btns[i].classList.remove('active');
                    }
                }
            }

            function showError(msg) {
                var el = document.getElementById('clientError');
                el.textContent = msg;
                el.style.display = 'block';
            }

            function hideError() {
                var el = document.getElementById('clientError');
                el.textContent = '';
                el.style.display = 'none';
            }

            // Submit handler — validates client-side, disables button, returns true to postback
            function onSubmit() {
                hideError();
                var cat = document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value;
                var sub = document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value;
                var impact = document.getElementById('<%= hfSelectedImpactId.ClientID %>').value;
                var urgency = document.getElementById('<%= hfSelectedUrgencyId.ClientID %>').value;

                if (!cat) { showError('Please select a category.'); return false; }
                if (!sub) { showError('Please select a sub category.'); return false; }
                if (!impact) { showError('Please select an impact level.'); return false; }
                if (!urgency) { showError('Please select an urgency level.'); return false; }

                // Prevent duplicate submission
                var btn = document.getElementById('<%= btnSubmit.ClientID %>');
                if (btn) { btn.disabled = true; btn.value = 'Creating...'; }
                var spinner = document.getElementById('submitSpinner');
                if (spinner) spinner.style.display = 'inline';
                return true;
            }

            // Reset — restores page to initial state without DB reload
            function onReset() {
                hideError();
                // Clear hidden fields
                document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedImpactId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedUrgencyId.ClientID %>').value = '';
                // Remove active states from cards
                var cards = document.querySelectorAll('#<%= categoryCards.ClientID %> .category-card');
                for (var i = 0; i < cards.length; i++) cards[i].classList.remove('active');
                // Remove active states from button groups
                var flatBtns = document.querySelectorAll('.btn-group-flat .btn-flat');
                for (var j = 0; j < flatBtns.length; j++) flatBtns[j].classList.remove('active');
                // Hide dependent sections
                document.getElementById('secSubCategory').classList.remove('visible');
                document.getElementById('secItem').classList.remove('visible');
                // Reset client-side selects
                var subDdl = document.getElementById('ddlSubCategoryClient');
                if (subDdl) subDdl.innerHTML = '<option value="">-- Select Sub Category --</option>';
                var itemDdl = document.getElementById('ddlItemClient');
                if (itemDdl) itemDdl.innerHTML = '<option value="">-- Select Item (optional) --</option>';
            }

            // Public API
            return {
                init: init,
                onSubCategoryChange: onSubCategoryChange,
                onItemChange: onItemChange,
                onSubmit: onSubmit,
                onReset: onReset,
                onImpactSelect: function (id) { selectButtonGroup('<%= impactButtons.ClientID %>', '<%= hfSelectedImpactId.ClientID %>', id); },
                onUrgencySelect: function (id) { selectButtonGroup('<%= urgencyButtons.ClientID %>', '<%= hfSelectedUrgencyId.ClientID %>', id); }
            };
        })();

        // Initialize on DOM ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', ITSM.CreateTicket.init);
        } else {
            ITSM.CreateTicket.init();
        }
    </script>
</asp:Content>
