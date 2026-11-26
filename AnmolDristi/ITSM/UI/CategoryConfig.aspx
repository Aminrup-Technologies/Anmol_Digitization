<%-- ========================================================================
     File:    CategoryConfig.aspx
     Layer:   UI — Presentation
     When:    2026-11-25
     What:    ITSM admin page for the global catalog. Parent Category is
              itsm.MST_Category, Category is itsm.MST_SubCategory, and
              Sub-Category is itsm.MST_Item. Sub-Category stays optional
              on ticket create. Entries are deactivated, not deleted.
     Why:     Phase 1B R1. Administration is a separate page from ticket
              create so existing selection control IDs stay in place.
              The page requires ITSM.CategoryConfig.Manage.
     When:    2026-11-26
     What:    Browser filter for the loaded grids, and an Order box on
              each row. Save still posts the name, order, and active flag.
              Search does not post back. Deactivate asks before posting.
     Why:     Display order is shared with Create Ticket. Typing must not
              save or delete a row.
     ======================================================================== --%>
<%@ Page Title="ITSM Category Configuration" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="CategoryConfig.aspx.cs" Inherits="AnmolDristi.ITSM.UI.CategoryConfig" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">

                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-sitemap"></i> ITSM Category Configuration</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <p>
                            The catalog is shared by every company. Deactivate an entry to hide it from new tickets.
                            Existing tickets keep the entry they already stored. A category or sub-category that is
                            already used by a ticket or a support mapping cannot be moved to another parent.
                            Search and the parent and category lists only hide rows already on this page. They do not
                            save or remove entries. Order is a whole number from 0 through 999999. Lower numbers appear
                            first, and the same number stays in id order. Save stores the name, the order, and whether
                            the entry is active.
                        </p>
                        <div class="form-inline" id="catalogFilterBar" style="margin-bottom: 4px;">
                            <label for="txtCatalogFilter">Search</label>
                            <input type="text" id="txtCatalogFilter" class="form-control" maxlength="100"
                                placeholder="Type to narrow the catalog" autocomplete="off"
                                style="margin: 0 12px 8px 6px;" />
                            <label for="ddlFilterParent">Parent category</label>
                            <select id="ddlFilterParent" class="form-control" style="margin: 0 12px 8px 6px;">
                                <option value="">All parent categories</option>
                            </select>
                            <label for="ddlFilterCategory">Category</label>
                            <select id="ddlFilterCategory" class="form-control" style="margin: 0 0 8px 6px;">
                                <option value="">All categories</option>
                            </select>
                        </div>
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>
                    </div>
                </div>

                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-folder"></i> Parent Categories</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <div class="form-inline" style="margin-bottom: 12px;">
                            <asp:TextBox ID="txtNewParentName" runat="server" MaxLength="100"
                                CssClass="form-control" placeholder="New parent category" />
                            <asp:Button ID="btnAddParent" runat="server" Text="Add parent category"
                                CssClass="btn btn-primary" CausesValidation="false"
                                OnClick="btnAddParent_Click" />
                        </div>
                        <asp:GridView ID="gvParents" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" DataKeyNames="Id"
                            OnRowCommand="gvParents_RowCommand"
                            OnRowDataBound="gvParents_RowDataBound"
                            EmptyDataText="No parent categories have been added.">
                            <Columns>
                                <asp:TemplateField HeaderText="Parent Category">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtName" runat="server" MaxLength="100" CssClass="form-control"
                                            Text='<%# Eval("Name") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Order" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Active" ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="In use" ItemStyle-Width="80px">
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Text='<%# (bool)Eval("IsReferenced") ? "Yes" : "No" %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSaveParent" runat="server" CommandName="Save"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-xs"
                                            CausesValidation="false" Text="Save"
                                            OnClientClick="return ITSM.CategoryConfig.confirmSave(this);" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <p id="catalogEmptyParents" style="display: none; margin-top: 8px;">No parent categories match this filter.</p>
                    </div>
                </div>

                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-folder-open"></i> Categories</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <div class="form-inline" style="margin-bottom: 12px;">
                            <asp:DropDownList ID="ddlNewCategoryParent" runat="server" CssClass="form-control" />
                            <asp:TextBox ID="txtNewCategoryName" runat="server" MaxLength="100"
                                CssClass="form-control" placeholder="New category" />
                            <asp:Button ID="btnAddCategory" runat="server" Text="Add category"
                                CssClass="btn btn-primary" CausesValidation="false"
                                OnClick="btnAddCategory_Click" />
                        </div>
                        <asp:GridView ID="gvCategories" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" DataKeyNames="Id"
                            OnRowCommand="gvCategories_RowCommand"
                            OnRowDataBound="gvChild_RowDataBound"
                            EmptyDataText="No categories have been added.">
                            <Columns>
                                <asp:TemplateField HeaderText="Parent Category">
                                    <ItemTemplate>
                                        <asp:HiddenField ID="hfParentId" runat="server" Value='<%# Eval("ParentId") %>' />
                                        <asp:Label ID="lblParent" runat="server" />
                                        <asp:DropDownList ID="ddlParent" runat="server" CssClass="form-control" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Category">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtName" runat="server" MaxLength="100" CssClass="form-control"
                                            Text='<%# Eval("Name") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Order" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Active" ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="In use" ItemStyle-Width="80px">
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Text='<%# (bool)Eval("IsReferenced") ? "Yes" : "No" %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSaveCategory" runat="server" CommandName="Save"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-xs"
                                            CausesValidation="false" Text="Save"
                                            OnClientClick="return ITSM.CategoryConfig.confirmSave(this);" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <p id="catalogEmptyCategories" style="display: none; margin-top: 8px;">No categories match this filter.</p>
                    </div>
                </div>

                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-tag"></i> Sub-Categories</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <p>Sub-category is optional when a ticket is created.</p>
                        <div class="form-inline" style="margin-bottom: 12px;">
                            <asp:DropDownList ID="ddlNewSubCategoryParent" runat="server" CssClass="form-control" />
                            <asp:TextBox ID="txtNewSubCategoryName" runat="server" MaxLength="100"
                                CssClass="form-control" placeholder="New sub-category" />
                            <asp:Button ID="btnAddSubCategory" runat="server" Text="Add sub-category"
                                CssClass="btn btn-primary" CausesValidation="false"
                                OnClick="btnAddSubCategory_Click" />
                        </div>
                        <asp:GridView ID="gvSubCategories" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" DataKeyNames="Id"
                            OnRowCommand="gvSubCategories_RowCommand"
                            OnRowDataBound="gvChild_RowDataBound"
                            EmptyDataText="No sub-categories have been added.">
                            <Columns>
                                <asp:TemplateField HeaderText="Category">
                                    <ItemTemplate>
                                        <asp:HiddenField ID="hfParentId" runat="server" Value='<%# Eval("ParentId") %>' />
                                        <asp:Label ID="lblParent" runat="server" />
                                        <asp:DropDownList ID="ddlParent" runat="server" CssClass="form-control" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Sub-Category">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtName" runat="server" MaxLength="100" CssClass="form-control"
                                            Text='<%# Eval("Name") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Order" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Active" ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="In use" ItemStyle-Width="80px">
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Text='<%# (bool)Eval("IsReferenced") ? "Yes" : "No" %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSaveSubCategory" runat="server" CommandName="Save"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-xs"
                                            CausesValidation="false" Text="Save"
                                            OnClientClick="return ITSM.CategoryConfig.confirmSave(this);" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <p id="catalogEmptySubCategories" style="display: none; margin-top: 8px;">No sub-categories match this filter.</p>
                    </div>
                </div>

            </div>
        </div>
    </div>
    <script type="text/javascript">
        // When: 2026-11-26
        // What: Hide already-rendered catalog rows. Parent and category
        //       selects constrain children. A name match keeps its parent
        //       and its children. Nothing here posts or writes.
        // Why:  Search must not save, delete, or reorder stored rows.
        window.ITSM = window.ITSM || {};
        ITSM.CategoryConfig = (function () {
            var parentTableId = '<%= gvParents.ClientID %>';
            var categoryTableId = '<%= gvCategories.ClientID %>';
            var subCategoryTableId = '<%= gvSubCategories.ClientID %>';

            function rows(level) {
                var tableId = level === 'parent'
                    ? parentTableId
                    : (level === 'category' ? categoryTableId : subCategoryTableId);
                var table = document.getElementById(tableId);
                var list = [];
                if (!table) {
                    return list;
                }

                var all = table.getElementsByTagName('tr');
                for (var i = 0; i < all.length; i++) {
                    if (all[i].getAttribute('data-level') === level) {
                        list.push(all[i]);
                    }
                }

                return list;
            }

            function rowOf(button) {
                if (button && button.closest) {
                    return button.closest('tr');
                }

                var row = button;
                while (row && row.tagName !== 'TR') {
                    row = row.parentNode;
                }

                return row;
            }

            function confirmSave(button) {
                var row = rowOf(button);
                if (!row || row.getAttribute('data-active') !== '1') {
                    return true;
                }

                var box = row.querySelector('input[type="checkbox"]');
                if (box && !box.checked) {
                    return window.confirm('Deactivate this entry? New tickets will not offer it. Existing tickets keep the entry they already stored.');
                }

                return true;
            }

            function addOption(select, value, text) {
                var option = document.createElement('option');
                option.value = value;
                option.text = text;
                select.appendChild(option);
            }

            function fillParentFilter() {
                var select = document.getElementById('ddlFilterParent');
                var previous = select.value;
                var keep = false;
                select.innerHTML = '';
                addOption(select, '', 'All parent categories');
                var parents = rows('parent');
                for (var i = 0; i < parents.length; i++) {
                    var id = parents[i].getAttribute('data-id');
                    addOption(select, id, parents[i].getAttribute('data-name') || '');
                    if (id === previous) {
                        keep = true;
                    }
                }

                select.value = keep ? previous : '';
            }

            function fillCategoryFilter() {
                var select = document.getElementById('ddlFilterCategory');
                var previous = select.value;
                var parentId = document.getElementById('ddlFilterParent').value;
                var keep = false;
                select.innerHTML = '';
                addOption(select, '', 'All categories');
                var categories = rows('category');
                for (var i = 0; i < categories.length; i++) {
                    if (parentId && categories[i].getAttribute('data-root-id') !== parentId) {
                        continue;
                    }

                    var id = categories[i].getAttribute('data-id');
                    var parentName = categories[i].getAttribute('data-parent-name') || '';
                    var name = categories[i].getAttribute('data-name') || '';
                    addOption(select, id, parentName ? (parentName + ' / ' + name) : name);
                    if (id === previous) {
                        keep = true;
                    }
                }

                select.value = keep ? previous : '';
            }

            function key(row) {
                return row.getAttribute('data-level') + ':' + row.getAttribute('data-id');
            }

            function findRow(list, level, id) {
                for (var i = 0; i < list.length; i++) {
                    if (list[i].getAttribute('data-level') === level
                        && list[i].getAttribute('data-id') === id) {
                        return list[i];
                    }
                }

                return null;
            }

            function applyFilter() {
                var text = (document.getElementById('txtCatalogFilter').value || '').replace(/^\s+|\s+$/g, '').toLowerCase();
                var parentId = document.getElementById('ddlFilterParent').value;
                var categoryId = document.getElementById('ddlFilterCategory').value;
                var parents = rows('parent');
                var categories = rows('category');
                var items = rows('subcategory');
                var selectedCategory = categoryId ? findRow(categories, 'category', categoryId) : null;

                function selected(row) {
                    if (parentId) {
                        var level = row.getAttribute('data-level');
                        var matchesParent = level === 'parent'
                            ? row.getAttribute('data-id') === parentId
                            : row.getAttribute('data-root-id') === parentId;
                        if (!matchesParent) {
                            return false;
                        }
                    }

                    if (!categoryId) {
                        return true;
                    }

                    if (row.getAttribute('data-level') === 'subcategory') {
                        return row.getAttribute('data-parent-id') === categoryId;
                    }

                    if (row.getAttribute('data-level') === 'category') {
                        return row.getAttribute('data-id') === categoryId;
                    }

                    return selectedCategory
                        && row.getAttribute('data-id') === selectedCategory.getAttribute('data-root-id');
                }

                function nameMatches(row) {
                    var name = (row.getAttribute('data-name') || '').toLowerCase();
                    return name.indexOf(text) !== -1;
                }

                var candidate = [];
                function take(list) {
                    for (var i = 0; i < list.length; i++) {
                        if (selected(list[i])) {
                            candidate.push(list[i]);
                        }
                    }
                }

                take(parents);
                take(categories);
                take(items);

                var show = {};
                function mark(row) {
                    if (row) {
                        show[key(row)] = true;
                    }
                }

                function markChildrenOfParent(id) {
                    for (var i = 0; i < candidate.length; i++) {
                        var level = candidate[i].getAttribute('data-level');
                        if (level === 'category' && candidate[i].getAttribute('data-root-id') === id) {
                            mark(candidate[i]);
                            markItems(candidate[i].getAttribute('data-id'));
                        }
                    }
                }

                function markItems(categoryRowId) {
                    for (var i = 0; i < candidate.length; i++) {
                        if (candidate[i].getAttribute('data-level') === 'subcategory'
                            && candidate[i].getAttribute('data-parent-id') === categoryRowId) {
                            mark(candidate[i]);
                        }
                    }
                }

                if (!text) {
                    for (var i = 0; i < candidate.length; i++) {
                        mark(candidate[i]);
                    }
                } else {
                    for (var n = 0; n < candidate.length; n++) {
                        if (!nameMatches(candidate[n])) {
                            continue;
                        }

                        var row = candidate[n];
                        var level = row.getAttribute('data-level');
                        mark(row);
                        if (level === 'parent') {
                            markChildrenOfParent(row.getAttribute('data-id'));
                        } else if (level === 'category') {
                            mark(findRow(candidate, 'parent', row.getAttribute('data-root-id')));
                            markItems(row.getAttribute('data-id'));
                        } else {
                            var categoryRow = findRow(candidate, 'category', row.getAttribute('data-parent-id'));
                            mark(categoryRow);
                            if (categoryRow) {
                                mark(findRow(candidate, 'parent', categoryRow.getAttribute('data-root-id')));
                            }
                        }
                    }
                }

                paint(parents, show, 'catalogEmptyParents');
                paint(categories, show, 'catalogEmptyCategories');
                paint(items, show, 'catalogEmptySubCategories');
            }

            function paint(list, show, emptyId) {
                var any = false;
                for (var i = 0; i < list.length; i++) {
                    var visible = !!show[key(list[i])];
                    list[i].style.display = visible ? '' : 'none';
                    if (visible) {
                        any = true;
                    }
                }

                var empty = document.getElementById(emptyId);
                if (!empty) {
                    return;
                }

                empty.style.display = list.length > 0 && !any ? 'block' : 'none';
            }

            function init() {
                var search = document.getElementById('txtCatalogFilter');
                if (!search) {
                    return;
                }

                fillParentFilter();
                fillCategoryFilter();
                search.addEventListener('input', applyFilter);
                search.addEventListener('keydown', function (event) {
                    if (event.key === 'Enter' || event.keyCode === 13) {
                        event.preventDefault();
                    }
                });
                document.getElementById('ddlFilterParent').addEventListener('change', function () {
                    fillCategoryFilter();
                    applyFilter();
                });
                document.getElementById('ddlFilterCategory').addEventListener('change', applyFilter);
                applyFilter();
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', init);
            } else {
                init();
            }

            return { confirmSave: confirmSave };
        })();
    </script>
</asp:Content>
