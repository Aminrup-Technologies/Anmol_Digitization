<%@ Page Title="ADMIN | Role Permission Manager" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RolePermissionManager.aspx.cs" Inherits="AnmolDristi.RolePermissionManager" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Main Heading</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12  ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Role → Permission Manager</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <!-- First set of dropdown list -->
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Select :<span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlRoles" runat="server" AutoPostBack="true" CssClass="form-control form-control-sm rounded" OnSelectedIndexChanged="ddlRoles_SelectedIndexChanged" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6 center-margin">
                                <div class="ln_solid"></div>
                                <div class="item form-group row">
                                    <div class="col-md-6 col-sm-12">
                                        <asp:Label ID="lblMessage" runat="server" Text="Click SUBMIT to Save Data!!"></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12">
                                        <button type="button" class="btn btn-danger btn-sm collapse-link">Cancel</button>
                                        <button type="reset" class="btn btn-warning btn-sm">Reset</button>
                                        <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" OnClick="btnSave_Click" />
                                    </div>
                                </div>
                            </div>

                            <!-- Small modal -->
                            <asp:Button ID="ShowPopup" runat="server" Text="Button" class="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">

                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body">
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>

                                    </div>
                                </div>
                            </div>
                            <!-- Small Modal - END---->
                        </div>

                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Data</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvPerms" runat="server" AutoGenerateColumns="false" DataKeyNames="PermissionId">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Grant">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="cbGrant" runat="server" Checked='<%# Eval("IsGranted") != null && Eval("IsGranted") != DBNull.Value && Convert.ToBoolean(Eval("IsGranted")) %>' />
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
