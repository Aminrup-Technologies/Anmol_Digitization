<%@ Page Title="ADMIN | Role Permission Manager" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RolePermissionManager.aspx.cs" Inherits="AnmolDristi.RolePermissionManager" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Role &rarr; Permission Manager</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Select Role:<span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlRoles" runat="server" AutoPostBack="true" 
                                            CssClass="form-control form-control-sm rounded" 
                                            OnSelectedIndexChanged="ddlRoles_SelectedIndexChanged" />
                                        <asp:RequiredFieldValidator ID="rfvRoles" runat="server" ControlToValidate="ddlRoles" 
                                            InitialValue="0" ErrorMessage="Please select a role." 
                                            CssClass="text-danger" Display="Dynamic" ValidationGroup="SaveGroup" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="ln_solid"></div>
                                <div class="form-group row align-items-center">
                                    <div class="col-md-6 col-sm-12 font-weight-bold">
                                        <asp:Label ID="lblMessage" runat="server" Text="Select a role, check the desired permissions, and click Save."></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-secondary btn-sm" onclick="window.location.href=window.location.href;">Reset</button>
                                        <asp:Button ID="btnSave" runat="server" Text="Save Permissions" CssClass="btn btn-primary btn-sm" OnClick="btnSave_Click" ValidationGroup="SaveGroup" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Manage Granted Permissions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvPerms" runat="server" AutoGenerateColumns="false" DataKeyNames="PermissionId"
                                            CssClass="table table-striped table-bordered table-hover" UseAccessibleHeader="true">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Grant" ItemStyle-HorizontalAlign="Center" HeaderStyle-CssClass="text-center">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="cbGrant" runat="server" 
                                                            Checked='<%# Eval("IsGranted") != DBNull.Value && Convert.ToBoolean(Eval("IsGranted")) %>' />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField DataField="PermissionKey" HeaderText="Key" />
                                                <asp:BoundField DataField="PermissionName" HeaderText="Name" />
                                                <asp:BoundField DataField="PermissionType" HeaderText="Type" />
                                                <asp:BoundField DataField="PagePath" HeaderText="Page" />
                                                <asp:BoundField DataField="ControlId" HeaderText="ControlId" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>