<%-- ========================================================================
     File:    SupportGroupConfig.aspx
     Layer:   UI — Presentation
     When:    2026-11-26
     What:    R6 support group admin (groups, members, taxonomy mappings).
     Why:     CategoryConfig does not exist on Aug26 tip; this page uses the
              closest existing admin permission ITSM.ApprovalConfig.Manage.
              Session gate is USERID + BRANCH (legacy Branch_ID).
     ======================================================================== --%>
<%@ Page Title="ITSM | Support Groups" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="SupportGroupConfig.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.SupportGroupConfig" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-users"></i> ITSM Support Groups (R6)</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>
                        <asp:HiddenField ID="hfSelectedGroupId" runat="server" />

                        <div class="form-inline" style="margin-bottom: 12px;">
                            <asp:TextBox ID="txtNewGroupName" runat="server" CssClass="form-control input-sm"
                                MaxLength="100" placeholder="New group name" Width="260px" />
                            <asp:Button ID="btnCreateGroup" runat="server" Text="Create Group"
                                CssClass="btn btn-success btn-sm" OnClick="btnCreateGroup_Click" />
                        </div>

                        <asp:GridView ID="gvGroups" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" DataKeyNames="Id"
                            OnRowCommand="gvGroups_RowCommand">
                            <Columns>
                                <asp:BoundField DataField="Name" HeaderText="Group" />
                                <asp:BoundField DataField="BranchName" HeaderText="Branch" />
                                <asp:BoundField DataField="TeamLeaderName" HeaderText="Team Leader" />
                                <asp:BoundField DataField="ActiveMemberCount" HeaderText="Active Members" />
                                <asp:TemplateField HeaderText="Active">
                                    <ItemTemplate>
                                        <%# (bool)Eval("IsActive") ? "Yes" : "No" %>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Actions">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSelect" runat="server" CommandName="SelectGroup"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-xs btn-primary"
                                            Text="Select" />
                                        <asp:LinkButton ID="lnkToggle" runat="server" CommandName="ToggleActive"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-xs btn-default"
                                            Text='<%# (bool)Eval("IsActive") ? "Deactivate" : "Activate" %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <asp:Panel ID="pnlGroupDetail" runat="server" Visible="false">
                    <div class="x_panel">
                        <div class="x_title">
                            <h3><i class="fa fa-user"></i> Members —
                                <asp:Literal ID="litSelectedGroup" runat="server" /></h3>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="form-inline" style="margin-bottom: 10px;">
                                <asp:DropDownList ID="ddlMember" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" Width="260px">
                                    <asp:ListItem Text="-- Select employee --" Value="" />
                                </asp:DropDownList>
                                <asp:CheckBox ID="chkIsTeamLeader" runat="server" Text="Team Leader" />
                                <asp:CheckBox ID="chkMemberActive" runat="server" Text="Active" Checked="true" />
                                <asp:Button ID="btnSaveMember" runat="server" Text="Save Member"
                                    CssClass="btn btn-primary btn-sm" OnClick="btnSaveMember_Click" />
                            </div>
                            <asp:GridView ID="gvMembers" runat="server" CssClass="table table-bordered"
                                AutoGenerateColumns="false">
                                <Columns>
                                    <asp:BoundField DataField="EmployeeName" HeaderText="Name" />
                                    <asp:BoundField DataField="EmployeeCode" HeaderText="Code" />
                                    <asp:TemplateField HeaderText="Team Leader">
                                        <ItemTemplate><%# (bool)Eval("IsTeamLeader") ? "Yes" : "No" %></ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Active">
                                        <ItemTemplate><%# (bool)Eval("IsActive") ? "Yes" : "No" %></ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>

                    <div class="x_panel">
                        <div class="x_title">
                            <h3><i class="fa fa-sitemap"></i> Taxonomy Mappings</h3>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="form-inline" style="margin-bottom: 10px;">
                                <asp:DropDownList ID="ddlParentCategory" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" AutoPostBack="true"
                                    OnSelectedIndexChanged="ddlParentCategory_SelectedIndexChanged" Width="220px">
                                    <asp:ListItem Text="-- Parent Category --" Value="" />
                                </asp:DropDownList>
                                <asp:DropDownList ID="ddlCategory" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" Width="220px">
                                    <asp:ListItem Text="-- Category (optional) --" Value="" />
                                </asp:DropDownList>
                                <asp:Button ID="btnSaveMapping" runat="server" Text="Save Mapping"
                                    CssClass="btn btn-primary btn-sm" OnClick="btnSaveMapping_Click"
                                    ToolTip="Leave Category blank for Parent Category level" />
                            </div>
                            <asp:GridView ID="gvMappings" runat="server" CssClass="table table-bordered"
                                AutoGenerateColumns="false" DataKeyNames="Id"
                                OnRowCommand="gvMappings_RowCommand">
                                <Columns>
                                    <asp:BoundField DataField="LevelLabel" HeaderText="Level" />
                                    <asp:BoundField DataField="CategoryName" HeaderText="Parent Category" />
                                    <asp:BoundField DataField="SubCategoryName" HeaderText="Category" />
                                    <asp:TemplateField HeaderText="Active">
                                        <ItemTemplate><%# (bool)Eval("IsActive") ? "Yes" : "No" %></ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Action">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lnkDeactivate" runat="server" CommandName="DeactivateMapping"
                                                CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-xs btn-warning"
                                                Text="Deactivate" Visible='<%# (bool)Eval("IsActive") %>' />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
