<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qahome_rpt.aspx.cs" Inherits="AnmolDristi.qaqc_qahome_rpt" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">
                <div class="col-md-12" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Quality Assurance Online Reports</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="Process_Report.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblProcessChecking" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Process Checking
                            </a>
                            <a class="btn btn-app" href="qaqc_qcinspector_rpt_.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblQCInspector" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Inspector Report

                            </a>

                            <a class="btn btn-app" href="qaqc_rotaryline_view.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblRotaryLine" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Line Weight
                            </a>
                            <a class="btn btn-app" href="vm_ccp_checklist.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblCCPChecklist" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>CCP Checklist
                            </a>


                            <a class="btn btn-app" href="vm_leak_test.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblLeakSeal" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Leak/ Slanted
                            </a>

                            <a class="btn btn-app" href="preDispatchVM.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblPreDispatch" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Pre-Dispatch
                            </a>
                            <a class="btn btn-app" href="Critical_Incident_ViewPage.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="lblCriticalIncident" runat="server" Text="0" Visible="true" Font-Bold="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Critical Incident
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
