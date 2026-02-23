<%@ Page Title="ADMIN | User Role Manager" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="UserRoleManager.aspx.cs" Inherits="AnmolDristi.UserRoleManager" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Makes the CheckBoxList look better in Bootstrap */
        .chk-list label { margin-left: 8px; font-weight: normal; cursor: pointer; }
        .chk-list td { padding: 5px 15px 5px 0; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>User &rarr; Role Manager</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Employee Code :<span class="text text-danger">*</span></label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtEmployeeCode" runat="server" CssClass="form-control form-control-sm" placeholder="Enter Employee Code" />
                                            <span class="input-group-btn">
                                                <asp:Button ID="btnLoad" runat="server" Text="Load Roles" OnClick="btnLoad_Click" CssClass="btn btn-primary btn-sm" ValidationGroup="LoadGroup" />
                                            </span>
                                        </div>
                                        <asp:RequiredFieldValidator ID="rfvEmpCode" runat="server" ControlToValidate="txtEmployeeCode" 
                                            ErrorMessage="Employee Code is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="LoadGroup" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="ln_solid"></div>
                                <div class="form-group row align-items-center">
                                    <div class="col-md-6 col-sm-12 font-weight-bold">
                                        <asp:Label ID="lblMessage" runat="server" Text="Enter Employee Code and click Load Roles."></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-secondary btn-sm" onclick="window.location.href=window.location.href;">Reset</button>
                                        <%-- Fixed the text to say 'Save' --%>
                                        <asp:Button ID="btnSaveRoles" runat="server" Text="Save Assignments" OnClick="btnSaveRoles_Click" CssClass="btn btn-success btn-sm" ValidationGroup="LoadGroup"/>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Assign Roles</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box">
                                        <%-- Added a custom CSS class for better spacing --%>
                                        <asp:CheckBoxList ID="cblRoles" runat="server" CssClass="chk-list" RepeatColumns="3" RepeatDirection="Horizontal" />
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