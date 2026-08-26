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
              All category hierarchy data is loaded once as JSON on initial GET
              and filtered client-side. Category is now clickable cards instead
              of a dropdown. Impact/Urgency are compact button groups.
     Why:     Product decision — faster ticket creation (5 fewer postbacks),
              better UX with clear category cards, and zero unnecessary DB
              round-trips for dependent selections.
     ======================================================================== --%>
<%@ Page Title="ITSM | Create Ticket" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="CreateTicket.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.CreateTicket" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* --- Section layout --- */
        .ticket-section {
            margin-bottom: 28px;
        }
        .ticket-section-title {
            font-size: 14px;
            font-weight: 700;
            color: #333;
            margin-bottom: 10px;
            padding-bottom: 6px;
            border-bottom: 2px solid #e8e8e8;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .ticket-section-title .step-num {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 22px;
            height: 22px;
            border-radius: 50%;
            background: #17a2b8;
            color: #fff;
            font-size: 12px;
            font-weight: 700;
        }

        /* --- Category cards --- */
        .category-cards {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
            gap: 10px;
        }
        .category-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
            padding: 14px 10px;
            border: 2px solid #d9d9d9;
            border-radius: 8px;
            background: #fafafa;
            cursor: pointer;
            transition: all 0.15s ease;
            text-align: center;
            user-select: none;
            min-height: 80px;
            justify-content: center;
        }
        .category-card:hover {
            border-color: #17a2b8;
            background: #e8f8fb;
        }
        .category-card:focus {
            outline: 2px solid #17a2b8;
            outline-offset: 2px;
        }
        .category-card.active {
            border-color: #17a2b8;
            background: #d1ecf1;
            box-shadow: 0 0 0 2px rgba(23, 162, 184, 0.25);
        }
        .category-card.active .category-icon {
            color: #0c5460;
        }
        .category-card .category-icon {
            font-size: 24px;
            color: #6c757d;
        }
        .category-card .category-name {
            font-size: 13px;
            font-weight: 600;
            color: #333;
            line-height: 1.2;
        }
        .category-card.active .category-name {
            color: #0c5460;
        }

        /* --- Impact / Urgency button groups --- */
        .btn-group-flat {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
        }
        .btn-group-flat .btn-flat {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            padding: 7px 14px;
            border: 2px solid #d9d9d9;
            border-radius: 6px;
            background: #fafafa;
            cursor: pointer;
            font-size: 13px;
            font-weight: 500;
            color: #555;
            transition: all 0.15s ease;
            user-select: none;
        }
        .btn-group-flat .btn-flat:hover {
            border-color: #17a2b8;
            background: #e8f8fb;
        }
        .btn-group-flat .btn-flat:focus {
            outline: 2px solid #17a2b8;
            outline-offset: 2px;
        }
        .btn-group-flat .btn-flat.active {
            border-color: #17a2b8;
            background: #17a2b8;
            color: #fff;
        }

        /* --- Form styling --- */
        .ticket-form-label {
            font-size: 13px;
            font-weight: 600;
            color: #555;
            margin-bottom: 4px;
        }
        .ticket-form-label .required {
            color: #dc3545;
            margin-left: 2px;
        }
        .ticket-form-control {
            border-radius: 6px;
            border: 1px solid #ccc;
            padding: 8px 12px;
            font-size: 14px;
            transition: border-color 0.15s;
        }
        .ticket-form-control:focus {
            border-color: #17a2b8;
            box-shadow: 0 0 0 2px rgba(23, 162, 184, 0.15);
        }
        .validation-msg {
            font-size: 12px;
            margin-top: 3px;
            color: #dc3545;
        }

        /* --- Submit area --- */
        .submit-area {
            display: flex;
            gap: 12px;
            align-items: center;
            padding-top: 8px;
        }
        .btn-create-ticket {
            padding: 10px 28px;
            font-size: 15px;
            font-weight: 600;
            border-radius: 6px;
        }
        .btn-cancel-ticket {
            padding: 10px 20px;
            font-size: 14px;
            border-radius: 6px;
        }

        /* --- Subcategory/Item section --- */
        .dependent-section {
            display: none;
        }
        .dependent-section.visible {
            display: block;
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

                        <%-- ============================================================
                             SECTION 1 — Category (clickable cards, client-side filtered)
                             ============================================================ --%>
                        <div class="ticket-section" id="secCategory">
                            <div class="ticket-section-title">
                                <span class="step-num">1</span> Category
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div class="category-cards" id="categoryCards" runat="server">
                                <%-- Populated server-side as <div> elements --%>
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

                        <%-- SubCategory (appears after category selection) --%>
                        <div class="ticket-section dependent-section" id="secSubCategory">
                            <div class="ticket-section-title">
                                <span class="step-num">2</span> Sub Category
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div style="max-width: 400px;">
                                <select id="ddlSubCategoryClient" class="form-control ticket-form-control"
                                    onchange="onSubCategoryChange()">
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
                                    onchange="onItemChange()">
                                    <option value="">-- Select Item (optional) --</option>
                                </select>
                            </div>
                        </div>

                        <%-- ============================================================
                             SECTION 2 — Impact & Urgency (compact button groups)
                             ============================================================ --%>
                        <div class="ticket-section">
                            <div class="ticket-section-title">
                                <span class="step-num" id="stepPriority">4</span> Impact &amp; Urgency
                                <span class="text-danger" style="font-size: 12px;">* required</span>
                            </div>
                            <div class="row">
                                <div class="col-md-6 col-sm-6 col-xs-12" style="margin-bottom: 12px;">
                                    <div class="ticket-form-label">Impact <span class="required">*</span></div>
                                    <div class="btn-group-flat" id="impactButtons" runat="server">
                                        <%-- Populated server-side as <span> elements --%>
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
                                        <%-- Populated server-side as <span> elements --%>
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
                                <span class="step-num" id="stepRequestType">5</span> Request Type
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
                                <span class="step-num" id="stepDetails">6</span> Ticket Details
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
                                    OnClientClick="if (!validateForm()) return false;"
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

    <%-- JSON data for client-side filtering --%>
    <asp:HiddenField ID="hfCategoryData" runat="server" />

    <script type="text/javascript">
        // ======================================================================
        // Client-side category/subcategory/item filtering.
        // Data is loaded once on GET as JSON in hfCategoryData.
        // No postbacks for selection filtering.
        // ======================================================================
        (function () {
            var categoryData = [];
            var selectedCategoryId = '';
            var selectedSubCategoryId = '';
            var selectedItemId = '';

            function init() {
                var raw = document.getElementById('<%= hfCategoryData.ClientID %>');
                if (!raw || !raw.value) return;

                try {
                    categoryData = JSON.parse(raw.value);
                } catch (e) {
                    return;
                }

                renderCategoryCards();
            }

            function renderCategoryCards() {
                var container = document.getElementById('<%= categoryCards.ClientID %>');
                if (!container) return;
                container.innerHTML = '';

                for (var i = 0; i < categoryData.length; i++) {
                    var cat = categoryData[i];
                    var card = document.createElement('div');
                    card.className = 'category-card';
                    card.setAttribute('tabindex', '0');
                    card.setAttribute('role', 'button');
                    card.setAttribute('aria-label', 'Select category: ' + cat.Name);
                    card.setAttribute('data-id', cat.Id);

                    card.innerHTML =
                        '<span class="category-icon"><i class="fa ' + (cat.Icon || 'fa-folder') + '"></i></span>' +
                        '<span class="category-name">' + escapeHtml(cat.Name) + '</span>';

                    card.onclick = (function (id) {
                        return function () { onCategoryClick(id); };
                    })(cat.Id);

                    card.onkeydown = (function (id) {
                        return function (e) {
                            if (e.key === 'Enter' || e.key === ' ') {
                                e.preventDefault();
                                onCategoryClick(id);
                            }
                        };
                    })(cat.Id);

                    container.appendChild(card);
                }

                // Restore previous selection if any
                var hfCat = document.getElementById('<%= hfSelectedCategoryId.ClientID %>');
                if (hfCat && hfCat.value) {
                    onCategoryClick(parseInt(hfCat.value, 10));
                }
            }

            function onCategoryClick(categoryId) {
                selectedCategoryId = categoryId;
                selectedSubCategoryId = '';
                selectedItemId = '';

                // Update hidden fields
                document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value = categoryId;
                document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value = '';
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = '';

                // Update card active states
                var cards = document.querySelectorAll('#<%= categoryCards.ClientID %> .category-card');
                for (var i = 0; i < cards.length; i++) {
                    if (parseInt(cards[i].getAttribute('data-id'), 10) === categoryId) {
                        cards[i].classList.add('active');
                    } else {
                        cards[i].classList.remove('active');
                    }
                }

                // Show and populate subcategory section
                var secSub = document.getElementById('secSubCategory');
                secSub.classList.add('visible');
                populateSubCategories(categoryId);

                // Hide item section
                var secItem = document.getElementById('secItem');
                secItem.classList.remove('visible');
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

            function onSubCategoryChange() {
                var ddl = document.getElementById('ddlSubCategoryClient');
                selectedSubCategoryId = ddl.value;
                selectedItemId = '';

                document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value = selectedSubCategoryId;
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = '';

                if (selectedSubCategoryId) {
                    var secItem = document.getElementById('secItem');
                    secItem.classList.add('visible');
                    populateItems(selectedCategoryId, parseInt(selectedSubCategoryId, 10));
                } else {
                    document.getElementById('secItem').classList.remove('visible');
                }
            }

            function populateItems(categoryId, subCategoryId) {
                var ddl = document.getElementById('ddlItemClient');
                ddl.innerHTML = '<option value="">-- Select Item (optional) --</option>';

                for (var i = 0; i < categoryData.length; i++) {
                    if (categoryData[i].Id === categoryId) {
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

            function onItemChange() {
                var ddl = document.getElementById('ddlItemClient');
                selectedItemId = ddl.value;
                document.getElementById('<%= hfSelectedItemId.ClientID %>').value = selectedItemId;
            }

            // Impact / Urgency button group handlers
            window.onImpactSelect = function (id) {
                document.getElementById('<%= hfSelectedImpactId.ClientID %>').value = id;
                var btns = document.querySelectorAll('#<%= impactButtons.ClientID %> .btn-flat');
                for (var i = 0; i < btns.length; i++) {
                    if (parseInt(btns[i].getAttribute('data-id'), 10) === id) {
                        btns[i].classList.add('active');
                    } else {
                        btns[i].classList.remove('active');
                    }
                }
            };

            window.onUrgencySelect = function (id) {
                document.getElementById('<%= hfSelectedUrgencyId.ClientID %>').value = id;
                var btns = document.querySelectorAll('#<%= urgencyButtons.ClientID %> .btn-flat');
                for (var i = 0; i < btns.length; i++) {
                    if (parseInt(btns[i].getAttribute('data-id'), 10) === id) {
                        btns[i].classList.add('active');
                    } else {
                        btns[i].classList.remove('active');
                    }
                }
            };

            // Form validation (client-side)
            window.validateForm = function () {
                var cat = document.getElementById('<%= hfSelectedCategoryId.ClientID %>').value;
                var sub = document.getElementById('<%= hfSelectedSubCategoryId.ClientID %>').value;
                var impact = document.getElementById('<%= hfSelectedImpactId.ClientID %>').value;
                var urgency = document.getElementById('<%= hfSelectedUrgencyId.ClientID %>').value;

                if (!cat) { alert('Please select a category.'); return false; }
                if (!sub) { alert('Please select a sub category.'); return false; }
                if (!impact) { alert('Please select an impact level.'); return false; }
                if (!urgency) { alert('Please select an urgency level.'); return false; }

                // Disable submit button to prevent duplicate submission
                var btn = document.getElementById('<%= btnSubmit.ClientID %>');
                if (btn) {
                    btn.disabled = true;
                    btn.value = 'Creating...';
                }
                var spinner = document.getElementById('submitSpinner');
                if (spinner) spinner.style.display = 'inline';

                return true;
            };

            function escapeHtml(text) {
                var div = document.createElement('div');
                div.appendChild(document.createTextNode(text));
                return div.innerHTML;
            }

            // Initialize on DOM ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', init);
            } else {
                init();
            }
        })();
    </script>
</asp:Content>
