<%@ Page Title="ADMIN | Role List" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RoleList.aspx.cs" Inherits="AnmolDristi.RoleList" %>

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
                                <%-- Hidden field to track Edit/Update state --%>
                                <asp:HiddenField ID="hfRoleId" runat="server" Value="0" />

                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Key :<span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtKey" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <%-- Add the Validator here --%>
                                        <asp:RequiredFieldValidator ID="rfvKey" runat="server"
                                            ControlToValidate="txtKey"
                                            ErrorMessage="Role Key is required."
                                            CssClass="text-danger"
                                            Display="Dynamic"
                                            ValidationGroup="RoleGroup">
                                        </asp:RequiredFieldValidator>
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Name :<span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtName" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <%-- Add the Validator here --%>
                                        <asp:RequiredFieldValidator ID="rfvName" runat="server"
                                            ControlToValidate="txtName"
                                            ErrorMessage="Role Name is required."
                                            CssClass="text-danger"
                                            Display="Dynamic"
                                            ValidationGroup="RoleGroup">
                                        </asp:RequiredFieldValidator>
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align" style="display: block;">IsSystem :</label>
                                        <%-- Removed form-control to fix checkbox rendering --%>
                                        <asp:CheckBox ID="chkIsSystem" runat="server" Text=" Mark as System Role" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="ln_solid"></div>
                                <div class="form-group row align-items-center">
                                    <div class="col-md-6 col-sm-12 font-weight-bold">
                                        <asp:Label ID="lblMessage" runat="server" Text="Enter details and click Create to save data."></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-secondary btn-sm" onclick="window.location.href=window.location.href;">Cancel</button>
                                        <asp:Button ID="btnCreate" runat="server" Text="Create"
                                            CssClass="btn btn-primary btn-sm"
                                            OnClick="btnCreate_Click"
                                            ValidationGroup="RoleGroup" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Roles</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <%-- Added Bootstrap Table Classes --%>
                                        <asp:GridView ID="gvRoles" runat="server" AutoGenerateColumns="false" DataKeyNames="RoleId"
                                            OnRowCommand="gvRoles_RowCommand" OnRowDataBound="gvRoles_RowDataBound"
                                            CssClass="table table-striped table-bordered table-hover" UseAccessibleHeader="true">
                                            <Columns>
                                                <asp:BoundField DataField="RoleId" HeaderText="ID" />
                                                <asp:BoundField DataField="RoleKey" HeaderText="Role Key" />
                                                <asp:BoundField DataField="RoleName" HeaderText="Role Name" />
                                                <asp:CheckBoxField DataField="IsSystem" HeaderText="Is System?" ReadOnly="true" ItemStyle-HorizontalAlign="Center" />
                                                <asp:ButtonField ButtonType="Button" Text="Edit" CommandName="EditRole" ControlStyle-CssClass="btn btn-info btn-sm" />
                                                <asp:ButtonField ButtonType="Button" Text="Delete" CommandName="DeleteRole" ControlStyle-CssClass="btn btn-danger btn-sm" />
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
