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
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main" style="background-color: #f7f7f7; min-height: 100vh;">
        <div class="row">
            <div class="col-md-6 col-md-offset-3 col-sm-8 col-sm-offset-2 col-xs-12" style="margin-top: 20px;">

                <div class="text-center" style="margin-bottom: 25px;">
                    <h3 style="color: #333; font-weight: bold;">My Visits</h3>
                    <p class="text-muted">History for the last 6 months</p>
                </div>

                <asp:Repeater ID="rptVisits" runat="server" OnItemDataBound="rptVisits_ItemDataBound">
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
                                <div class="detail-row" style="margin-top: 12px;">
                                    <span class="detail-label">Status:</span>
                                    <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("ApprovalStatus") %>'></asp:Label>
                                </div>
                            </div>

                            <asp:Panel ID="pnlFooter" runat="server" CssClass="card-footer" Visible="false">
                                <a href='VMS_DigitalPass.aspx?token=<%# Eval("QRToken") %>' class="btn btn-pass">
                                    <i class="fa fa-qrcode"></i>View Digital Pass
                                </a>
                            </asp:Panel>

                        </div>
                    </ItemTemplate>
                </asp:Repeater>

            </div>
        </div>
    </div>
</asp:Content>
