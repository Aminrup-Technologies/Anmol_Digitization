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
                        </p>
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
                            EmptyDataText="No parent categories have been added.">
                            <Columns>
                                <asp:TemplateField HeaderText="Parent Category">
                                    <ItemTemplate>
                                        <asp:TextBox ID="txtName" runat="server" MaxLength="100" CssClass="form-control"
                                            Text='<%# Eval("Name") %>' />
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
                                            CausesValidation="false" Text="Save" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
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
                                            CausesValidation="false" Text="Save" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
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
                                            CausesValidation="false" Text="Save" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
