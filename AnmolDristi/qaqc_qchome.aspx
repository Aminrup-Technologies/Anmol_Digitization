<%@ Page Title="QAQC | QC Forms" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qchome.aspx.cs" Inherits="AnmolDristi.qaqc_qchome" %>

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
            height: 100%;
        }
        .form-card:hover {
            box-shadow: 0 4px 10px rgba(0,0,0,0.1);
            transform: translateY(-2px);
        }
        .form-card h4 {
            margin-top: 0;
            font-weight: 600;
            color: #2A3F54;
            font-size: 15px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
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
        .row-flex {
            display: flex;
            flex-wrap: wrap;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">

            <div class="col-md-6 col-sm-12 col-xs-12" id="QC_Panel" runat="server" visible="true">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>QC - Packing Materials Test Reports</h2>
                        <ul class="nav navbar-right panel_toolbox">
                            <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                        </ul>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <div class="row row-flex">

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card">
                                    <h4><i class="fa fa-file-image-o" style="color: #34495E; margin-right:5px;"></i> Daily Laminate</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocPMLaminate" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPMLaminate" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPMLaminate" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="PM_LaminateTesting.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card">
                                    <h4><i class="fa fa-shopping-bag" style="color: #9B59B6; margin-right:5px;"></i> HM / PP Bag</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocPMOverwrap" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPMOverwrap" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPMOverwrap" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="qaqc_overwrap.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card">
                                    <h4><i class="fa fa-archive" style="color: #3498DB; margin-right:5px;"></i> CBB Report</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocPMCBB" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPMCBB" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPMCBB" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="CorrugatedBoardBoxReport.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card">
                                    <h4><i class="fa fa-bars" style="color: #1ABB9C; margin-right:5px;"></i> BOPP Tape</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocPMTape" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPMTape" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPMTape" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="bopp_tape.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card">
                                    <h4><i class="fa fa-inbox" style="color: #F39C12; margin-right:5px;"></i> PVC Tray</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocPMTray" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevPMTray" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqPMTray" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="PVC_Tray.aspx" class="btn btn-sm btn-primary action-btn">Open Form</a>
                                </div>
                            </div>

                        </div>
                    </div>
                </div>
            </div>

            <div class="col-md-6 col-sm-12 col-xs-12" id="Div1" runat="server" visible="true">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>QC - Raw Materials Test Reports</h2>
                        <ul class="nav navbar-right panel_toolbox">
                            <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                        </ul>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <div class="row row-flex">

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #E74C3C;">
                                    <h4><i class="fa fa-flask" style="color: #E74C3C; margin-right:5px;"></i> Atta / Maida</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRMAtta" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRMAtta" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRMAtta" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="Aata_Maida_Form.aspx" class="btn btn-sm btn-danger action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #26B99A;">
                                    <h4><i class="fa fa-cube" style="color: #26B99A; margin-right:5px;"></i> RM Class 1</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRM1" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRM1" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRM1" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="RM_Class_1.aspx" class="btn btn-sm btn-success action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #26B99A;">
                                    <h4><i class="fa fa-cube" style="color: #26B99A; margin-right:5px;"></i> RM Class 2</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRM2" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRM2" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRM2" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="RM_Class2.aspx" class="btn btn-sm btn-success action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #26B99A;">
                                    <h4><i class="fa fa-cube" style="color: #26B99A; margin-right:5px;"></i> RM Class 3</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRM3" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRM3" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRM3" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="RM_Class_3.aspx" class="btn btn-sm btn-success action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #26B99A;">
                                    <h4><i class="fa fa-cube" style="color: #26B99A; margin-right:5px;"></i> RM Class 4</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRM4" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRM4" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRM4" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="RM_Class_4.aspx" class="btn btn-sm btn-success action-btn">Open Form</a>
                                </div>
                            </div>

                            <div class="col-lg-6 col-md-6 col-sm-6 col-xs-12">
                                <div class="form-card" style="border-top-color: #26B99A;">
                                    <h4><i class="fa fa-cube" style="color: #26B99A; margin-right:5px;"></i> RM Class 5</h4>
                                    <div class="qms-meta">
                                        <div><i class="fa fa-file-text-o"></i> Doc: <strong><asp:Label ID="lblDocRM5" runat="server" Text="N/A"></asp:Label></strong></div>
                                        <div><i class="fa fa-history"></i> Rev: <strong><asp:Label ID="lblRevRM5" runat="server" Text="0"></asp:Label></strong></div>
                                        <div><i class="fa fa-clock-o"></i> Freq: <span class="label label-success"><asp:Label ID="lblFreqRM5" runat="server" Text="Unknown"></asp:Label></span></div>
                                    </div>
                                    <a href="RM_Class_5.aspx" class="btn btn-sm btn-success action-btn">Open Form</a>
                                </div>
                            </div>

                        </div>
                    </div>
                </div>
            </div>

        </div>
    </div>
</asp:Content>