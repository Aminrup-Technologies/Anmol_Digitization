<%@ Page Title="QA Form Entry" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qahome.aspx.cs" Inherits="AnmolDristi.qaqc_qahome" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .form-card {
            background: #fff;
            border: 1px solid #e6e9ed;
            border-top: 4px solid #1ABB9C; /* Gentelella Green */
            border-radius: 4px;
            padding: 15px;
            margin-bottom: 20px;
            transition: all 0.3s ease;
        }
        .form-card:hover {
            box-shadow: 0 4px 10px rgba(0,0,0,0.1);
            transform: translateY(-2px);
        }
        .form-card h4 {
            margin-top: 0;
            font-weight: 600;
            color: #2A3F54;
            font-size: 16px;
        }
        .form-card .qms-meta {
            font-size: 12px;
            color: #73879C;
            margin-bottom: 15px;
        }
        .form-card .qms-meta i {
            margin-right: 5px;
            width: 14px;
            text-align: center;
        }
        .form-card .action-btn {
            width: 100%;
            text-align: center;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">
                <div class="col-md-12 col-sm-12 col-xs-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Quality Assurance Online Formats <small>Select a form to enter data</small></h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        
                        <div class="x_content">
                            <div class="row">
                                
                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-cogs" style="color: #34495E; margin-right:8px;"></i> Process Checking</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocProcess" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevProcess" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqProcess" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_process_rpt.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-search" style="color: #9B59B6; margin-right:8px;"></i> QA Inspector</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocInspector" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevInspector" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqInspector" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_inspector_rpt.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-balance-scale" style="color: #3498DB; margin-right:8px;"></i> Packet Weights</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocPacket" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPacket" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPacket" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_packet_weight.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-line-chart" style="color: #1ABB9C; margin-right:8px;"></i> Line Weight</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocRotary" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRotary" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRotary" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_rotary_line.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-check-square-o" style="color: #F39C12; margin-right:8px;"></i> CCP Checklist</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocCCP" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevCCP" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqCCP" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="ccp_checklist.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-tint" style="color: #95A5A6; margin-right:8px;"></i> Leak Test</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocLeak" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevLeak" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqLeak" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_leaktest_rev1.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card">
                                        <h4><i class="fa fa-truck" style="color: #26B99A; margin-right:8px;"></i> Pre-Dispatch</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocDispatch" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevDispatch" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqDispatch" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="qaqc_pre_dispatch_QI_Report.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                    </div>
                                </div>

                                <div class="col-md-3 col-sm-6 col-xs-12">
                                    <div class="form-card" style="border-top-color: #E74C3C;"> <h4><i class="fa fa-exclamation-triangle" style="color: #E74C3C; margin-right:8px;"></i> Critical Incident</h4>
                                        <div class="qms-meta">
                                            <div><i class="fa fa-file-text-o"></i> Doc No: <strong><asp:Label ID="lblDocCritical" runat="server" Text="N/A"></asp:Label></strong></div>
                                            <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevCritical" runat="server" Text="0"></asp:Label></strong></div>
                                            <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-danger"><asp:Label ID="lblFreqCritical" runat="server" Text="Unknown"></asp:Label></span></div>
                                        </div>
                                        <a href="critical_quality_report.aspx" class="btn btn-sm btn-danger action-btn">Open Form</a>
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