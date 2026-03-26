<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Claims_Config.aspx.cs" Inherits="AnmolDristi.Claims_Config" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <%--<div class="page-title">
                <div class="title_left">
                    <h2>Quality Report Forms</h2>
                </div>
            </div>

            <div class="clearfix"></div>--%>

            <div class="row">

                <div class="col-md-6" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Upload & Process Bizom Reports</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <a class="btn btn-app" href="ImportCsv.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label3" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Bizom Import
                            </a>

                            <a class="btn btn-app" href="AdminBatches.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label4" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Process Batch
                            </a>

                            <a class="btn btn-app" href="ManageEmployeeTolerance.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label1" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Manage KM Tol.
                            </a>

                            <a class="btn btn-app" href="ManageExpenseSettings.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label2" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Exp. Settings
                            </a>

                            <a class="btn btn-app" href="PolicyMatrixAdmin.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label5" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Policy Matrix
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="Div2" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Upload & Process Bizom Reports</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <a class="btn btn-app" href="HrmsDataImport.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>ZingHR Import
                            </a>

                            <a class="btn btn-app" href="HrmsBatchManager.aspx">
                                <span class="badge bg-red">
                                    <asp:Label ID="Label7" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Process ZingHR 
                            </a>

                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
