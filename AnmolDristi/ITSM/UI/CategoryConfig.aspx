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
              A separate panel edits itsm.MST_SupportMapping for the
              signed-in branch. The same Manage permission is required.
     Why:     Display order is shared with Create Ticket. Typing must not
              save or delete a row. Mapping uses the existing dispatch
              table and does not change ticket assignment.
     ======================================================================== --%>
<%@ Page Title="ITSM Category Configuration" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="CategoryConfig.aspx.cs" Inherits="AnmolDristi.ITSM.UI.CategoryConfig" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="<%= ResolveUrl("~/Resources/select2/dist/css/select2.min.css") %>" rel="stylesheet" />
    <style type="text/css">
        .itsm-processing {
            position: fixed;
            top: 0;
            right: 0;
            bottom: 0;
            left: 0;
            z-index: 20000;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 16px;
            background: rgba(255, 255, 255, 0.72);
        }
        .itsm-processing[hidden] { display: none !important; }
        .itsm-processing__panel {
            width: 100%;
            max-width: 280px;
            padding: 18px 22px;
            text-align: center;
            color: #2A3F54;
            background: #fff;
            border: 1px solid #E6E9ED;
            border-radius: 4px;
            font-size: 16px;
        }
        .itsm-processing__panel i { margin-right: 8px; }
        .itsm-colhead { white-space: nowrap; }
        .itsm-colinfo {
            margin-left: 4px;
            padding: 0 2px;
            border: 0;
            background: transparent;
            color: #73879C;
            font-size: 13px;
            line-height: 1;
            vertical-align: baseline;
        }
        .itsm-colinfo:focus { outline: 1px solid #2A3F54; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div id="itsmProcessing" class="itsm-processing" hidden="hidden" role="status" aria-live="polite" aria-busy="false">
        <div class="itsm-processing__panel">
            <i class="fa fa-spinner fa-spin" aria-hidden="true"></i>
            <span>Processing...</span>
        </div>
    </div>
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
                                <asp:TemplateField ItemStyle-Width="90px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Order
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999."
                                                aria-label="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Active
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category."
                                                aria-label="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="130px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">In use
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this parent category id only. Child rows are not counted. Not Referenced means the current check found no matching ticket or support mapping."
                                                aria-label="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this parent category id only. Child rows are not counted. Not Referenced means the current check found no matching ticket or support mapping.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Style="white-space:nowrap;" Text='<%# (bool)Eval("IsReferenced") ? "Referenced" : "Not Referenced" %>' />
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
                                <asp:TemplateField ItemStyle-Width="90px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Order
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999."
                                                aria-label="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Active
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category."
                                                aria-label="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="130px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">In use
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this category id or a sub-category under it, and Referenced keeps the parent from being changed. Not Referenced means the current check found no matching ticket or support mapping."
                                                aria-label="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this category id or a sub-category under it, and Referenced keeps the parent from being changed. Not Referenced means the current check found no matching ticket or support mapping.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Style="white-space:nowrap;" Text='<%# (bool)Eval("IsReferenced") ? "Referenced" : "Not Referenced" %>' />
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
                                <asp:TemplateField ItemStyle-Width="90px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Order
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999."
                                                aria-label="Listed by this number, lowest first. The same number is ordered by id. Save stores a whole number from 0 through 999999. A new row is one higher than the highest number in its list, or 0 if the list is empty, and stops at 999999.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtSortOrder" runat="server" MaxLength="6" CssClass="form-control"
                                            Text='<%# Eval("SortOrder") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">Active
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category."
                                                aria-label="Save writes this checkbox to the catalog row. Create Ticket includes the row only when it is active and its parents are active. Clearing it does not delete the row or change tickets or support mappings. An inactive parent blocks saving an active category. An inactive category or parent blocks saving an active sub-category.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-Width="130px">
                                    <HeaderTemplate>
                                        <span class="itsm-colhead">In use
                                            <button type="button" class="itsm-colinfo" data-toggle="tooltip"
                                                data-trigger="hover focus click" data-placement="top" data-container="body"
                                                title="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this sub-category id, and Referenced keeps the category from being changed. Not Referenced means the current check found no matching ticket or support mapping."
                                                aria-label="Referenced means the record is linked to existing tickets or support mappings, and existing reference rules may restrict changes to this record or its parent. This check counts this sub-category id, and Referenced keeps the category from being changed. Not Referenced means the current check found no matching ticket or support mapping.">
                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                            </button>
                                        </span>
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:Label ID="lblInUse" runat="server" Style="white-space:nowrap;" Text='<%# (bool)Eval("IsReferenced") ? "Referenced" : "Not Referenced" %>' />
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

                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-user"></i> Support Agent Mapping</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <p>
                            Mappings apply to the signed-in branch
                            <asp:Label ID="lblMappingBranch" runat="server" Font-Bold="true"></asp:Label>.
                            A row names one active employee in your company. Branch default has no catalog entry.
                            Otherwise choose a parent category, and a category or sub-category only when that
                            level needs its own agent. An existing active row for the same selection is rejected.
                            Deactivate a row to stop new tickets from using it. Existing tickets keep their assignee.
                            The support agent lists can be searched by name or code. The agent, parent, and category filters only hide rows already on this page.
                        </p>
                        <asp:Label ID="lblMappingMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>
                        <div class="form-inline" style="margin-bottom: 12px;">
                            <asp:CheckBox ID="chkMappingBranchDefault" runat="server" Text="Branch default" />
                            <asp:DropDownList ID="ddlMappingParent" runat="server" CssClass="form-control" />
                            <asp:DropDownList ID="ddlMappingCategory" runat="server" CssClass="form-control" />
                            <asp:DropDownList ID="ddlMappingSubCategory" runat="server" CssClass="form-control" />
                            <asp:DropDownList ID="ddlMappingAgent" runat="server" CssClass="form-control mapping-agent-select" />
                            <asp:Button ID="btnAddMapping" runat="server" Text="Add mapping"
                                CssClass="btn btn-primary" CausesValidation="false"
                                OnClick="btnAddMapping_Click" />
                        </div>
                        <div class="form-inline" id="mappingFilterBar" style="margin-bottom: 8px;">
                            <label for="txtMappingAgentFilter">Support agent</label>
                            <input type="text" id="txtMappingAgentFilter" class="form-control" maxlength="100"
                                placeholder="Name or code" autocomplete="off"
                                style="margin: 0 12px 8px 6px;" />
                            <label for="ddlMappingFilterParent">Parent category</label>
                            <select id="ddlMappingFilterParent" class="form-control" style="margin: 0 12px 8px 6px;">
                                <option value="">All parent categories</option>
                            </select>
                            <label for="ddlMappingFilterCategory">Category</label>
                            <select id="ddlMappingFilterCategory" class="form-control" style="margin: 0 0 8px 6px;">
                                <option value="">All categories</option>
                            </select>
                        </div>
                        <asp:GridView ID="gvMappings" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" DataKeyNames="Id"
                            OnRowCommand="gvMappings_RowCommand"
                            OnRowDataBound="gvMappings_RowDataBound"
                            EmptyDataText="No support agent mappings have been added for this branch.">
                            <Columns>
                                <asp:BoundField DataField="Scope" HeaderText="Level" />
                                <asp:BoundField DataField="CategoryName" HeaderText="Parent Category" />
                                <asp:BoundField DataField="SubCategoryName" HeaderText="Category" />
                                <asp:BoundField DataField="ItemName" HeaderText="Sub-Category" />
                                <asp:TemplateField HeaderText="Support Agent">
                                    <ItemTemplate>
                                        <asp:DropDownList ID="ddlAgent" runat="server" CssClass="form-control mapping-agent-select" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Active" ItemStyle-Width="80px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkActive" runat="server" Checked='<%# Eval("IsActive") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="90px">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSaveMapping" runat="server" CommandName="Save"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-xs"
                                            CausesValidation="false" Text="Save"
                                            OnClientClick="return ITSM.CategoryConfig.confirmMappingSave(this);" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <p id="mappingEmpty" style="display: none; margin-top: 8px;">No support agent mappings match this filter.</p>
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

            function mappingRows() {
                var table = document.getElementById('<%= gvMappings.ClientID %>');
                var list = [];
                if (!table) {
                    return list;
                }

                var all = table.getElementsByTagName('tr');
                for (var i = 0; i < all.length; i++) {
                    if (all[i].getAttribute('data-agent') !== null) {
                        list.push(all[i]);
                    }
                }

                return list;
            }

            function fillMappingParentFilter() {
                var select = document.getElementById('ddlMappingFilterParent');
                if (!select) {
                    return;
                }

                var previous = select.value;
                select.innerHTML = '';
                addOption(select, '', 'All parent categories');
                var seen = {};
                var rows = mappingRows();
                for (var i = 0; i < rows.length; i++) {
                    var id = rows[i].getAttribute('data-parent-id') || '';
                    if (!id || seen[id]) {
                        continue;
                    }

                    seen[id] = true;
                    var label = rows[i].cells && rows[i].cells.length > 1 ? rows[i].cells[1].innerText : id;
                    addOption(select, id, label || id);
                    if (id === previous) {
                        select.value = id;
                    }
                }
            }

            function fillMappingCategoryFilter() {
                var select = document.getElementById('ddlMappingFilterCategory');
                var parentSelect = document.getElementById('ddlMappingFilterParent');
                if (!select || !parentSelect) {
                    return;
                }

                var parentId = parentSelect.value;
                var previous = select.value;
                select.innerHTML = '';
                addOption(select, '', 'All categories');
                var seen = {};
                var rows = mappingRows();
                for (var i = 0; i < rows.length; i++) {
                    if (parentId && rows[i].getAttribute('data-parent-id') !== parentId) {
                        continue;
                    }

                    var id = rows[i].getAttribute('data-category-id') || '';
                    if (!id || seen[id]) {
                        continue;
                    }

                    seen[id] = true;
                    var label = rows[i].cells && rows[i].cells.length > 2 ? rows[i].cells[2].innerText : id;
                    addOption(select, id, label || id);
                }

                select.value = previous;
                if (select.value !== previous) {
                    select.value = '';
                }
            }

            function applyMappingFilter() {
                var search = document.getElementById('txtMappingAgentFilter');
                var parentSelect = document.getElementById('ddlMappingFilterParent');
                var categorySelect = document.getElementById('ddlMappingFilterCategory');
                if (!search || !parentSelect || !categorySelect) {
                    return;
                }

                var term = (search.value || '').toLowerCase();
                var parentId = parentSelect.value;
                var categoryId = categorySelect.value;
                var rows = mappingRows();
                var any = false;
                for (var i = 0; i < rows.length; i++) {
                    var agent = (rows[i].getAttribute('data-agent') || '').toLowerCase();
                    var parentOk = !parentId || rows[i].getAttribute('data-parent-id') === parentId;
                    var categoryOk = !categoryId || rows[i].getAttribute('data-category-id') === categoryId;
                    var visible = parentOk && categoryOk && agent.indexOf(term) >= 0;
                    rows[i].style.display = visible ? '' : 'none';
                    if (visible) {
                        any = true;
                    }
                }

                var empty = document.getElementById('mappingEmpty');
                if (empty) {
                    empty.style.display = rows.length > 0 && !any ? 'block' : 'none';
                }
            }

            function optionSnapshot(select) {
                var list = [];
                for (var i = 0; i < select.options.length; i++) {
                    list.push({
                        value: select.options[i].value,
                        text: select.options[i].text,
                        parent: select.options[i].getAttribute('data-parent') || ''
                    });
                }

                return list;
            }

            function rebuildOptions(select, source, parentId) {
                var current = select.value;
                select.innerHTML = '';
                for (var i = 0; i < source.length; i++) {
                    if (source[i].value && parentId && source[i].parent !== parentId) {
                        continue;
                    }

                    var option = document.createElement('option');
                    option.value = source[i].value;
                    option.text = source[i].text;
                    if (source[i].parent) {
                        option.setAttribute('data-parent', source[i].parent);
                    }

                    select.appendChild(option);
                }

                select.value = current;
                if (select.value !== current) {
                    select.value = '';
                }
            }

            function setMappingCatalogEnabled(enabled) {
                var parent = document.getElementById('<%= ddlMappingParent.ClientID %>');
                var category = document.getElementById('<%= ddlMappingCategory.ClientID %>');
                var subCategory = document.getElementById('<%= ddlMappingSubCategory.ClientID %>');
                if (!parent || !category || !subCategory) {
                    return;
                }

                parent.disabled = !enabled;
                category.disabled = !enabled;
                subCategory.disabled = !enabled;
            }

            function initMapping() {
                var agentSearch = document.getElementById('txtMappingAgentFilter');
                var branchDefault = document.getElementById('<%= chkMappingBranchDefault.ClientID %>');
                var parent = document.getElementById('<%= ddlMappingParent.ClientID %>');
                var category = document.getElementById('<%= ddlMappingCategory.ClientID %>');
                var subCategory = document.getElementById('<%= ddlMappingSubCategory.ClientID %>');
                if (!agentSearch || !parent || !category || !subCategory) {
                    return;
                }

                var categories = optionSnapshot(category);
                var subCategories = optionSnapshot(subCategory);
                fillMappingParentFilter();
                fillMappingCategoryFilter();
                rebuildOptions(category, categories, parent.value);
                rebuildOptions(subCategory, subCategories, category.value);
                agentSearch.addEventListener('input', applyMappingFilter);
                agentSearch.addEventListener('keydown', function (event) {
                    if (event.key === 'Enter' || event.keyCode === 13) {
                        event.preventDefault();
                    }
                });
                document.getElementById('ddlMappingFilterParent').addEventListener('change', function () {
                    fillMappingCategoryFilter();
                    applyMappingFilter();
                });
                document.getElementById('ddlMappingFilterCategory').addEventListener('change', applyMappingFilter);
                parent.addEventListener('change', function () {
                    rebuildOptions(category, categories, parent.value);
                    rebuildOptions(subCategory, subCategories, category.value);
                });
                category.addEventListener('change', function () {
                    rebuildOptions(subCategory, subCategories, category.value);
                });
                if (branchDefault) {
                    branchDefault.addEventListener('click', function () {
                        setMappingCatalogEnabled(!branchDefault.checked);
                    });
                    setMappingCatalogEnabled(!branchDefault.checked);
                }

                applyMappingFilter();
            }

            function confirmMappingSave(button) {
                var row = rowOf(button);
                if (!row || row.getAttribute('data-active') !== '1') {
                    return true;
                }

                var box = row.querySelector('input[type="checkbox"]');
                if (box && !box.checked) {
                    return window.confirm('Deactivate this support agent mapping? New tickets will no longer use it. Existing tickets keep their assignee.');
                }

                return true;
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', initMapping);
            } else {
                initMapping();
            }

            return { confirmSave: confirmSave, confirmMappingSave: confirmMappingSave };
        })();

        // When: 2026-11-26
        // What: Turn the mapping agent lists into searchable Select2
        //       dropdowns after the master page loads jQuery.
        // Why:  The admin types a name or code inside the selector used
        //       to add or save a mapping. The underlying select still
        //       posts the employee code.
        function matchMappingAgent(params, data) {
            var term = (params.term || '').replace(/^\s+|\s+$/g, '').toLowerCase();
            if (!term) {
                return data;
            }

            if (!data || !data.id) {
                return null;
            }

            var text = (data.text || '').toLowerCase();
            return text.indexOf(term) >= 0 ? data : null;
        }

        function initMappingAgentSelects() {
            if (!window.jQuery || !jQuery.fn || !jQuery.fn.select2) {
                return;
            }

            jQuery('select.mapping-agent-select').each(function () {
                var list = jQuery(this);
                if (list.data('select2')) {
                    return;
                }

                list.select2({
                    width: '100%',
                    matcher: matchMappingAgent
                });
            });
        }

        function initColumnInfo() {
            if (!window.jQuery || !jQuery.fn || !jQuery.fn.tooltip) {
                return;
            }

            jQuery('.itsm-colinfo').tooltip({
                container: 'body',
                trigger: 'hover focus click',
                placement: 'top'
            });
        }

        function loadMappingAgentSearch() {
            if (window.jQuery && jQuery.fn && jQuery.fn.select2) {
                initMappingAgentSelects();
                return;
            }

            var script = document.createElement('script');
            script.src = '<%= ResolveUrl("~/Resources/select2/dist/js/select2.min.js") %>';
            script.onload = initMappingAgentSelects;
            document.body.appendChild(script);
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', loadMappingAgentSearch);
            document.addEventListener('DOMContentLoaded', initColumnInfo);
        } else {
            loadMappingAgentSearch();
            initColumnInfo();
        }

        // When: 2026-11-26
        // What: Show Processing while a full postback from this page is
        //       in flight, and ignore a second submit.
        // Why:  Add and Save post the form. Catalog filters and Select2
        //       search do not. There is no UpdatePanel on this page.
        (function () {
            if (window.__itsmCategoryProcessing) {
                return;
            }
            window.__itsmCategoryProcessing = true;
            var overlay = document.getElementById('itsmProcessing');
            var form = document.getElementById('form1') || document.forms[0];
            var pending = false;

            function showProcessing() {
                if (!overlay || pending) {
                    return;
                }
                pending = true;
                overlay.hidden = false;
                overlay.setAttribute('aria-busy', 'true');
            }

            function hideProcessing() {
                pending = false;
                if (!overlay) {
                    return;
                }
                overlay.hidden = true;
                overlay.setAttribute('aria-busy', 'false');
            }

            function bindProcessing() {
                if (form) {
                    form.addEventListener('submit', function (event) {
                        if (event.defaultPrevented) {
                            hideProcessing();
                            return;
                        }
                        if (pending) {
                            event.preventDefault();
                            return;
                        }
                        showProcessing();
                    });
                }

                if (typeof window.__doPostBack === 'function' && !window.__doPostBack.__itsmCategory) {
                    var postBack = window.__doPostBack;
                    var guarded = function (target, argument) {
                        if (pending) {
                            return;
                        }
                        showProcessing();
                        postBack(target, argument);
                    };
                    guarded.__itsmCategory = true;
                    window.__doPostBack = guarded;
                }

                if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                    var manager = Sys.WebForms.PageRequestManager.getInstance();
                    if (manager && !manager.__itsmCategoryProcessing) {
                        manager.__itsmCategoryProcessing = true;
                        manager.add_beginRequest(showProcessing);
                        manager.add_endRequest(hideProcessing);
                    }
                }
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', bindProcessing);
            } else {
                bindProcessing();
            }

            window.addEventListener('pageshow', hideProcessing);
        })();
    </script>
</asp:Content>
