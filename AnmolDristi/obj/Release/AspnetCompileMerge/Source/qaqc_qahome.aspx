<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qahome.aspx.cs" Inherits="AnmolDristi.qaqc_qahome" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">
                <div class="col-md-12" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Quality Assurance Online Forms</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <a class="btn btn-app" href="qaqc_process_rpt.aspx">
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label3" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Process Checking
                            </a>
                            <a class="btn btn-app" href="qaqc_inspector_rpt.aspx">
                                <span class="badge bg-green">Shift-2
                                    <asp:Label ID="lbl_tbttodaycount" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Inspector Report
                            </a>
                            
                            <a class="btn btn-app" href="qaqc_rotary_line.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label5" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Line Weight
                            </a>
                            <a class="btn btn-app" href="ccp_checklist.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label1" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>CCP Checklist
                            </a>
                            

                            <a class="btn btn-app" href="qaqc_leaktest_rev1.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Leak Test
                            </a>

                            <a class="btn btn-app" href="qaqc_pre_dispatch_QI_Report.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label7" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Pre-Dispatch
                            </a>
                            
                            <a class="btn btn-app" href="critical_quality_report.aspx" >
                                <span class="badge bg-green">As Req.
                                    <asp:Label ID="Label8" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Critical Incident
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
