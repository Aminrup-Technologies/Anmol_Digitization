<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qaapprovals.aspx.cs" Inherits="AnmolDristi.qaqc_qaapprovals" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">
                <div class="col-md-12" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Quality Assurance - Reports for Approval</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="Process_Approval.aspx">
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label1" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Process Checking
                            </a>
                            <a class="btn btn-app" href="app_qc_inspectorreport.aspx">
                                <span class="badge bg-green">Shift-2
                                    <asp:Label ID="Label2" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Inspector Report
                            </a>
                            
                            <a class="btn btn-app" href="qaqc_rotaryline_approval.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Line Weight
                            </a>
                            <a class="btn btn-app" href="vm_ccp_checklist.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label7" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>CCP Checklist
                            </a>
                            

                            <a class="btn btn-app" href="vm_leak_test.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label8" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Leak Test
                            </a>

                            <a class="btn btn-app" href="preDispatchVM.aspx" >
                                <span class="badge bg-green">Shift : 1
                                    <asp:Label ID="Label9" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Pre-Dispatch
                            </a>
                            
                            <a class="btn btn-app" href="Critical_Incident_ViewPage.aspx" >
                                <span class="badge bg-green">As Req.
                                    <asp:Label ID="Label10" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Critical Incident
                            </a>
                            
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
