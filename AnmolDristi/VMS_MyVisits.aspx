<%@ Page Title="My Visits" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_MyVisits.aspx.cs" Inherits="AnmolDristi.VMS_MyVisits" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .visit-card {
            background: #fff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.08);
            margin-bottom: 20px;
            border-left: 5px solid #1ABB9C;
            overflow: hidden;
        }

            .visit-card.rejected {
                border-left-color: #E74C3C;
            }

            .visit-card.pending {
                border-left-color: #F39C12;
            }

        .card-header {
            background: #fbfbfb;
            padding: 12px 15px;
            border-bottom: 1px solid #eee;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .card-title {
            margin: 0;
            font-size: 15px;
            font-weight: bold;
            color: #333;
        }

        .visit-date {
            font-size: 12px;
            color: #777;
        }

        .card-body {
            padding: 15px;
        }

        .detail-row {
            margin-bottom: 8px;
            font-size: 13px;
        }

        .detail-label {
            color: #888;
            font-weight: 600;
            width: 80px;
            display: inline-block;
        }

        .card-footer {
            padding: 10px 15px;
            background: #fff;
            border-top: 1px solid #eee;
            text-align: right;
        }

        .badge {
            padding: 5px 10px;
            border-radius: 12px;
            font-weight: normal;
            font-size: 11px;
        }

        .badge-pending {
            background-color: #F39C12;
            color: #fff;
        }

        .badge-approved {
            background-color: #1ABB9C;
            color: #fff;
        }

        .badge-rejected {
            background-color: #E74C3C;
            color: #fff;
        }

        .btn-pass {
            background-color: #1ABB9C;
            color: white;
            border-radius: 20px;
            font-size: 12px;
            padding: 5px 15px;
            font-weight: bold;
        }

            .btn-pass:hover {
                background-color: #149b82;
                color: white;
            }

        .visit-status-tabs {
            margin-bottom: 20px;
        }

            .visit-status-tabs .nav-link {
                font-size: 13px;
                font-weight: 600;
                color: #73879C;
            }

            .visit-status-tabs .nav-link.active {
                color: #1ABB9C;
                font-weight: 700;
            }

        .visit-empty-state {
            text-align: center;
            color: #888;
            padding: 30px 15px;
            background: #fff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.05);
            font-size: 14px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main" style="background-color: #f7f7f7; min-height: 100vh;">
        <div class="row">
            <div class="col-md-6 col-md-offset-3 col-sm-8 col-sm-offset-2 col-xs-12" style="margin-top: 20px;">

                <div class="text-center" style="margin-bottom: 25px;">
                    <h3 style="color: #333; font-weight: bold;">My Visits</h3>
                    <p class="text-muted">History for the last 6 months</p>
                    <asp:Panel ID="pnlAnotherHost" runat="server" Visible="false" style="margin-top: 12px;">
                        <asp:HyperLink ID="lnkAnotherHost" runat="server" CssClass="btn btn-pass">
                            Request visit with another host
                        </asp:HyperLink>
                    </asp:Panel>
                </div>

                <ul class="nav nav-tabs visit-status-tabs" role="tablist">
                    <li class="nav-item">
                        <a class="nav-link active" id="tabActivePasses-tab" data-toggle="tab" href="#tabActivePasses" role="tab" aria-controls="tabActivePasses" aria-selected="true">Active Passes</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" id="tabPendingApproval-tab" data-toggle="tab" href="#tabPendingApproval" role="tab" aria-controls="tabPendingApproval" aria-selected="false">Pending Approval</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" id="tabRejected-tab" data-toggle="tab" href="#tabRejected" role="tab" aria-controls="tabRejected" aria-selected="false">Rejected</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" id="tabExpiredPasses-tab" data-toggle="tab" href="#tabExpiredPasses" role="tab" aria-controls="tabExpiredPasses" aria-selected="false">Expired Passes</a>
                    </li>
                </ul>

                <div class="tab-content" style="padding-top: 15px;">
                    <div role="tabpanel" class="tab-pane active" id="tabActivePasses">
                        <asp:Label ID="lblEmptyActivePasses" runat="server" CssClass="visit-empty-state" Text="No active passes found." Visible="false" />
                        <asp:Repeater ID="rptVisitsActivePasses" runat="server" OnItemDataBound="rptVisits_ItemDataBound">
                            <ItemTemplate>
                                <div class='visit-card <%# Eval("ApprovalStatus").ToString().ToLower() %>'>
                                    <div class="card-header">
                                        <h4 class="card-title"><i class="fa fa-building-o"></i><%# Eval("AliasName") %></h4>
                                        <span class="visit-date"><%# Convert.ToDateTime(Eval("CreatedDate")).ToString("dd MMM yyyy") %></span>
                                    </div>
                                    <div class="card-body">
                                        <div class="detail-row">
                                            <span class="detail-label">Host:</span>
                                            <span style="color: #333; font-weight: bold;"><%# Eval("HostName") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Purpose:</span>
                                            <span><%# Eval("PurposeOfVisit") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Schedule:</span>
                                            <span><%# FormatVisitSchedule(Eval("ScheduleType"), Eval("RequestedValidFrom"), Eval("RequestedValidUpto")) %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Today's Visit:</span>
                                            <span><%# Eval("VisitStatus") %></span>
                                        </div>
                                        <div class="detail-row" style="margin-top: 12px;">
                                            <span class="detail-label">Status:</span>
                                            <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("ApprovalStatus") %>'></asp:Label>
                                        </div>
                                    </div>
                                    <asp:Panel ID="pnlFooter" runat="server" CssClass="card-footer text-center" Visible="false">
                                        <asp:Label ID="lblQRMessage" runat="server" Visible="false" CssClass="text-muted" Style="font-size: 12px; display: block; margin-bottom: 8px;"></asp:Label>
                                        <asp:Panel ID="pnlPassLink" runat="server" Visible="false">
                                            <a href='VMS_DigitalPass.aspx?token=<%# Eval("QRToken") %>' class="btn btn-pass" style="margin-top: 5px; display: inline-block;">
                                                <i class="fa fa-qrcode"></i>Full Screen Pass
                                            </a>
                                        </asp:Panel>
                                    </asp:Panel>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div role="tabpanel" class="tab-pane" id="tabPendingApproval">
                        <asp:Label ID="lblEmptyPending" runat="server" CssClass="visit-empty-state" Text="No pending visits found." Visible="false" />
                        <asp:Repeater ID="rptVisitsPending" runat="server" OnItemDataBound="rptVisits_ItemDataBound">
                            <ItemTemplate>
                                <div class='visit-card <%# Eval("ApprovalStatus").ToString().ToLower() %>'>
                                    <div class="card-header">
                                        <h4 class="card-title"><i class="fa fa-building-o"></i><%# Eval("AliasName") %></h4>
                                        <span class="visit-date"><%# Convert.ToDateTime(Eval("CreatedDate")).ToString("dd MMM yyyy") %></span>
                                    </div>
                                    <div class="card-body">
                                        <div class="detail-row">
                                            <span class="detail-label">Host:</span>
                                            <span style="color: #333; font-weight: bold;"><%# Eval("HostName") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Purpose:</span>
                                            <span><%# Eval("PurposeOfVisit") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Schedule:</span>
                                            <span><%# FormatVisitSchedule(Eval("ScheduleType"), Eval("RequestedValidFrom"), Eval("RequestedValidUpto")) %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Today's Visit:</span>
                                            <span><%# Eval("VisitStatus") %></span>
                                        </div>
                                        <div class="detail-row" style="margin-top: 12px;">
                                            <span class="detail-label">Status:</span>
                                            <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("ApprovalStatus") %>'></asp:Label>
                                        </div>
                                    </div>
                                    <asp:Panel ID="pnlFooter" runat="server" CssClass="card-footer text-center" Visible="false">
                                        <asp:Label ID="lblQRMessage" runat="server" Visible="false" CssClass="text-muted" Style="font-size: 12px; display: block; margin-bottom: 8px;"></asp:Label>
                                        <asp:Panel ID="pnlPassLink" runat="server" Visible="false">
                                            <a href='VMS_DigitalPass.aspx?token=<%# Eval("QRToken") %>' class="btn btn-pass" style="margin-top: 5px; display: inline-block;">
                                                <i class="fa fa-qrcode"></i>Full Screen Pass
                                            </a>
                                        </asp:Panel>
                                    </asp:Panel>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div role="tabpanel" class="tab-pane" id="tabRejected">
                        <asp:Label ID="lblEmptyRejected" runat="server" CssClass="visit-empty-state" Text="No rejected visits found." Visible="false" />
                        <asp:Repeater ID="rptVisitsRejected" runat="server" OnItemDataBound="rptVisits_ItemDataBound">
                            <ItemTemplate>
                                <div class='visit-card <%# Eval("ApprovalStatus").ToString().ToLower() %>'>
                                    <div class="card-header">
                                        <h4 class="card-title"><i class="fa fa-building-o"></i><%# Eval("AliasName") %></h4>
                                        <span class="visit-date"><%# Convert.ToDateTime(Eval("CreatedDate")).ToString("dd MMM yyyy") %></span>
                                    </div>
                                    <div class="card-body">
                                        <div class="detail-row">
                                            <span class="detail-label">Host:</span>
                                            <span style="color: #333; font-weight: bold;"><%# Eval("HostName") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Purpose:</span>
                                            <span><%# Eval("PurposeOfVisit") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Schedule:</span>
                                            <span><%# FormatVisitSchedule(Eval("ScheduleType"), Eval("RequestedValidFrom"), Eval("RequestedValidUpto")) %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Today's Visit:</span>
                                            <span><%# Eval("VisitStatus") %></span>
                                        </div>
                                        <div class="detail-row" style="margin-top: 12px;">
                                            <span class="detail-label">Status:</span>
                                            <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("ApprovalStatus") %>'></asp:Label>
                                        </div>
                                    </div>
                                    <asp:Panel ID="pnlFooter" runat="server" CssClass="card-footer text-center" Visible="false">
                                        <asp:Label ID="lblQRMessage" runat="server" Visible="false" CssClass="text-muted" Style="font-size: 12px; display: block; margin-bottom: 8px;"></asp:Label>
                                        <asp:Panel ID="pnlPassLink" runat="server" Visible="false">
                                            <a href='VMS_DigitalPass.aspx?token=<%# Eval("QRToken") %>' class="btn btn-pass" style="margin-top: 5px; display: inline-block;">
                                                <i class="fa fa-qrcode"></i>Full Screen Pass
                                            </a>
                                        </asp:Panel>
                                    </asp:Panel>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <div role="tabpanel" class="tab-pane" id="tabExpiredPasses">
                        <asp:Label ID="lblEmptyExpiredPasses" runat="server" CssClass="visit-empty-state" Text="No expired passes found." Visible="false" />
                        <asp:Repeater ID="rptVisitsExpiredPasses" runat="server" OnItemDataBound="rptVisits_ItemDataBound">
                            <ItemTemplate>
                                <div class='visit-card <%# Eval("ApprovalStatus").ToString().ToLower() %>'>
                                    <div class="card-header">
                                        <h4 class="card-title"><i class="fa fa-building-o"></i><%# Eval("AliasName") %></h4>
                                        <span class="visit-date"><%# Convert.ToDateTime(Eval("CreatedDate")).ToString("dd MMM yyyy") %></span>
                                    </div>
                                    <div class="card-body">
                                        <div class="detail-row">
                                            <span class="detail-label">Host:</span>
                                            <span style="color: #333; font-weight: bold;"><%# Eval("HostName") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Purpose:</span>
                                            <span><%# Eval("PurposeOfVisit") %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Schedule:</span>
                                            <span><%# FormatVisitSchedule(Eval("ScheduleType"), Eval("RequestedValidFrom"), Eval("RequestedValidUpto")) %></span>
                                        </div>
                                        <div class="detail-row">
                                            <span class="detail-label">Today's Visit:</span>
                                            <span><%# Eval("VisitStatus") %></span>
                                        </div>
                                        <div class="detail-row" style="margin-top: 12px;">
                                            <span class="detail-label">Status:</span>
                                            <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("ApprovalStatus") %>'></asp:Label>
                                        </div>
                                    </div>
                                    <asp:Panel ID="pnlFooter" runat="server" CssClass="card-footer text-center" Visible="false">
                                        <asp:Label ID="lblQRMessage" runat="server" Visible="false" CssClass="text-muted" Style="font-size: 12px; display: block; margin-bottom: 8px;"></asp:Label>
                                        <asp:Panel ID="pnlPassLink" runat="server" Visible="false">
                                            <a href='VMS_DigitalPass.aspx?token=<%# Eval("QRToken") %>' class="btn btn-pass" style="margin-top: 5px; display: inline-block;">
                                                <i class="fa fa-qrcode"></i>Full Screen Pass
                                            </a>
                                        </asp:Panel>
                                    </asp:Panel>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                </div>

            </div>
        </div>
    </div>
</asp:Content>
