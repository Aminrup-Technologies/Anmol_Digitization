<%@ Page Title="ADMIN | User Role Audit" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="UserRoleAudit.aspx.cs" Inherits="AnmolDristi.UserRoleAudit" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>User Role Audit Log</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Employee Code:</label>
                                        <asp:TextBox ID="txtEmployeeCode" runat="server" CssClass="form-control form-control-sm rounded" placeholder="Search by Emp Code" />
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Filter by Role:</label>
                                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">From Date:</label>
                                        <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">To Date:</label>
                                        <asp:TextBox ID="txtTo" runat="server" TextMode="Date" CssClass="form-control form-control-sm rounded" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="ln_solid"></div>
                                <div class="form-group row align-items-center">
                                    <div class="col-md-6 col-sm-12 font-weight-bold">
                                        <asp:Label ID="lblMessage" runat="server" Text="Select filters and click Load."></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-secondary btn-sm" onclick="window.location.href=window.location.href;">Reset Filters</button>
                                        <asp:Button ID="btnFilter" runat="server" Text="Load Audit Log" OnClick="btnFilter_Click" CssClass="btn btn-primary btn-sm"/>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Audit Results</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvAudit" runat="server" AutoGenerateColumns="true" CssClass="table table-striped table-bordered table-hover" UseAccessibleHeader="true" />
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