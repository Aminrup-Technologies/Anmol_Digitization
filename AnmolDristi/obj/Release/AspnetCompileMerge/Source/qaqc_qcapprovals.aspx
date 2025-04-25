<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qcapprovals.aspx.cs" Inherits="AnmolDristi.qaqc_qcapprovals" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">

                <div class="col-md-6" id="QC_Panel" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Quality Control - Reports for Approvals</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="PM_Laminate_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:label id="Label1" runat="server" text="0" visible="false"></asp:label>
                                </span>
                                <i class="fa fa-edit"></i>Daily Laminate

                            </a>
                            <a class="btn btn-app" href="overwrap_approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:label id="Label2" runat="server" text="0" visible="false"></asp:label>
                                </span>
                                <i class="fa fa-edit"></i>HM / PP Bag
                            </a>
                            <a class="btn btn-app" href="CorrugatedBoardBox_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:label id="Label3" runat="server" text="0" visible="false"></asp:label>
                                </span>
                                <i class="fa fa-edit"></i>CBB Report
                            </a>
                            <a class="btn btn-app" href="bopp_tape_approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:label id="Label4" runat="server" text="0" visible="false"></asp:label>
                                </span>
                                <i class="fa fa-edit"></i>BOPP Tape
                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:label id="Label5" runat="server" text="0" visible="false"></asp:label>
                                </span>
                                <i class="fa fa-edit"></i>PVC Tray
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>QC - Raw Materials Test Reports</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="Aata_Maida_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label11" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Atta / Maida
                            </a>
                            <a class="btn btn-app" href="RM_Class_1_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Class 1

                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-orange">WIP
                                    <asp:Label ID="Label7" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Class 2
                            </a>
                            <a class="btn btn-app" href="RM_Class_3_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label8" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Class 3
                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-orange">WIP
                                    <asp:Label ID="Label9" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Class 4
                            </a>
                            <a class="btn btn-app" href="RM_Class_5_Approval.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label10" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Class 5
                            </a>
                            
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
