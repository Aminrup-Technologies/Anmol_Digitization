<%@ Page Title="ADMIN | Permission List" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="PermissionList.aspx.cs" Inherits="AnmolDristi.PermissionList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Permission Manager</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <%-- Hidden field for Edit tracking --%>
                                <asp:HiddenField ID="hfPermissionId" runat="server" Value="0" />

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align">Key :<span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtPKey" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="rfvPKey" runat="server" ControlToValidate="txtPKey" 
                                            ErrorMessage="Key is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="PermGroup" />
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align">Name :<span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtPName" runat="server" CssClass="form-control form-control-sm rounded" />
                                        <asp:RequiredFieldValidator ID="rfvPName" runat="server" ControlToValidate="txtPName" 
                                            ErrorMessage="Name is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="PermGroup" />
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align">Type :</label>
                                        <asp:TextBox ID="txtPType" runat="server" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align">Page Path :</label>
                                        <asp:TextBox ID="txtPagePath" runat="server" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align">Control Id :</label>
                                        <asp:TextBox ID="txtCtrl" runat="server" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="label-align" style="display:block;">Status :</label>
                                        <asp:CheckBox ID="chkIsObsolete" runat="server" Text=" Mark as Obsolete" />
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
                                        <asp:Button ID="btnCreatePerm" runat="server" Text="Create" CssClass="btn btn-primary btn-sm" OnClick="btnCreatePerm_Click" ValidationGroup="PermGroup" />
                                    </div>
                                </div>
                            </div>

                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Permissions</h2>
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
                                                      CssClass="table table-striped table-bordered table-hover" UseAccessibleHeader="true"
                                                      OnRowCommand="gvPerms_RowCommand">
                                            <Columns>
                                                <asp:BoundField DataField="PermissionId" HeaderText="ID" />
                                                <asp:BoundField DataField="PermissionKey" HeaderText="Key" />
                                                <asp:BoundField DataField="PermissionName" HeaderText="Name" />
                                                <asp:BoundField DataField="PermissionType" HeaderText="Type" />
                                                <asp:BoundField DataField="PagePath" HeaderText="PagePath" />
                                                <asp:BoundField DataField="ControlId" HeaderText="ControlId" />
                                                <asp:CheckBoxField DataField="IsObsolete" HeaderText="Obsolete" ReadOnly="true" ItemStyle-HorizontalAlign="Center" />
                                                <asp:ButtonField ButtonType="Button" Text="Edit" CommandName="EditPerm" ControlStyle-CssClass="btn btn-info btn-sm" />
                                                <asp:ButtonField ButtonType="Button" Text="Delete" CommandName="DeletePerm" ControlStyle-CssClass="btn btn-danger btn-sm" />
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