<%@ Page Title="Gate Scanner" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_GateScanner.aspx.cs" Inherits="AnmolDristi.VMS_GateScanner" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://unpkg.com/html5-qrcode" type="text/javascript"></script>
    <style>
        .scanner-container {
            background: #fff;
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            text-align: center;
        }

        .gate-location-banner {
            background-color: #34495E;
            color: white;
            padding: 10px;
            border-radius: 5px;
            margin-bottom: 20px;
            font-size: 16px;
            font-weight: bold;
        }

        #reader {
            width: 100%;
            max-width: 500px;
            margin: 0 auto;
            border: 4px solid #1ABB9C;
            border-radius: 10px;
            overflow: hidden;
        }

        .result-card {
            margin-top: 20px;
            padding: 20px;
            border-radius: 8px;
        }

        .result-warning {
            background-color: #fcf8e3;
            border: 1px solid #faebcc;
            color: #8a6d3b;
        }

        .result-info {
            background-color: #f9f9f9;
            border: 1px solid #ddd;
            color: #333;
        }

        .result-success {
            background-color: #dff0d8;
            border: 1px solid #d6e9c6;
            color: #3c763d;
        }

        .result-danger {
            background-color: #f2dede;
            border: 1px solid #ebccd1;
            color: #a94442;
        }

        .visitor-img {
            width: 120px;
            height: 120px;
            border-radius: 50%;
            object-fit: cover;
            border: 4px solid #1ABB9C;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2);
            margin-bottom: 10px;
        }
        
        .action-buttons {
            margin-top: 20px;
            border-top: 1px solid #eee;
            padding-top: 15px;
        }

        /* Responsive Card Styles for Repeater */
        .visitor-card {
            border: 1px solid #ddd; 
            border-radius: 8px; 
            overflow: hidden; 
            height: 100%; 
            display: flex; 
            flex-direction: column;
            background: #fff;
            transition: box-shadow 0.3s ease;
        }
        .visitor-card:hover {
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-10 col-md-offset-1 col-sm-12 col-xs-12">

                <div class="gate-location-banner text-center">
                    <i class="fa fa-map-marker"></i> Branch / Gate Location: <asp:Label ID="lblGateLocation" runat="server" Text="Fetching Location..."></asp:Label>
                </div>

                <div class="x_panel">
                    <div class="x_content">

                        <div class="" role="tabpanel" data-example-id="togglable-tabs">
                            <ul id="myTab" class="nav nav-tabs bar_tabs" role="tablist">
                                <li role="presentation" class="active">
                                    <a href="#tab_scanner" id="scanner-tab" role="tab" data-toggle="tab" aria-expanded="true"><i class="fa fa-qrcode"></i> Live Scanner</a>
                                </li>
                                <li role="presentation" class="">
                                    <a href="#tab_inside" role="tab" id="inside-tab" data-toggle="tab" aria-expanded="false">
                                        <i class="fa fa-users"></i> Inside Premises 
                                        <asp:UpdatePanel ID="upBadge" runat="server" UpdateMode="Conditional" RenderMode="Inline">
                                            <ContentTemplate>
                                                <span class="badge bg-green"><asp:Label ID="lblInsideCount" runat="server" Text="0"></asp:Label></span>
                                            </ContentTemplate>
                                        </asp:UpdatePanel>
                                     </a>
                                </li>
                            </ul>

                            <div id="myTabContent" class="tab-content">

                                <div role="tabpanel" class="tab-pane fade active in" id="tab_scanner" aria-labelledby="scanner-tab">
                                    <div class="row">
                                        <div class="col-md-8 col-md-offset-2 col-sm-10 col-sm-offset-1 col-xs-12">
                                            <div class="scanner-container">
                                                
                                                <asp:Panel ID="pnlCamera" runat="server">
                                                    <h2 style="color: #1ABB9C; font-weight: bold;"><i class="fa fa-search"></i> Digital Pass Scanner</h2>
                                                    <p class="text-muted">Point the camera at the visitor's Digital Pass.</p>
                                                    <div class="ln_solid"></div>
                                                    <div id="reader"></div>
                                                </asp:Panel>

                                                <asp:HiddenField ID="hfScannedToken" runat="server" />
                                                <asp:HiddenField ID="hfCurrentLogId" runat="server" />
                                                <asp:HiddenField ID="hfHostEmail" runat="server" />
                                                <asp:HiddenField ID="hfHostName" runat="server" />
                                                <asp:HiddenField ID="hfVisName" runat="server" />
                                                <asp:HiddenField ID="hfCompany" runat="server" />

                                                <asp:Button ID="btnProcessScan" runat="server" Text="Process" OnClick="btnProcessScan_Click" Style="display: none;" />

                                                <asp:Panel ID="pnlResult" runat="server" CssClass="result-card" Visible="false">
                                                    <div class="row">
                                                        <div class="col-xs-12 text-center" style="margin-bottom: 15px;">
                                                            <asp:Image ID="imgVisitor" runat="server" CssClass="visitor-img" Visible="false" />
                                                        </div>
                                                    </div>

                                                    <div class="row text-center" style="margin-bottom: 20px; border-bottom: 1px solid rgba(0,0,0,0.1); padding-bottom: 10px;">
                                                        <h3><asp:Label ID="lblScanTitle" runat="server"></asp:Label></h3>
                                                        <p style="font-size: 16px; font-weight: 500;">
                                                            <asp:Label ID="lblScanMessage" runat="server"></asp:Label>
                                                        </p>
                                                    </div>

                                                    <asp:Panel ID="pnlVisitDetails" runat="server" Visible="false">
                                                        <ul class="list-group text-left" style="font-size: 15px; color: #333;">
                                                            <li class="list-group-item">
                                                                <i class="fa fa-user" style="width: 20px;"></i><b>Visitor:</b>
                                                                <span class="pull-right"><asp:Label ID="lblDetName" runat="server"></asp:Label></span>
                                                            </li>
                                                            <li class="list-group-item">
                                                                <i class="fa fa-building" style="width: 20px;"></i><b>Company:</b>
                                                                <span class="pull-right"><asp:Label ID="lblDetCompany" runat="server"></asp:Label></span>
                                                            </li>
                                                            <li class="list-group-item">
                                                                <i class="fa fa-phone" style="width: 20px;"></i><b>Contact:</b>
                                                                <span class="pull-right"><asp:Label ID="lblDetContact" runat="server"></asp:Label></span>
                                                            </li>
                                                            <li class="list-group-item" style="background-color: #f9f9f9;">
                                                                <i class="fa fa-id-badge" style="width: 20px; color: #1ABB9C;"></i><b>Host Employee:</b>
                                                                <span class="pull-right" style="font-weight: bold; color: #1ABB9C;">
                                                                    <asp:Label ID="lblDetHost" runat="server"></asp:Label>
                                                                </span>
                                                            </li>
                                                            <li class="list-group-item">
                                                                <i class="fa fa-briefcase" style="width: 20px;"></i><b>Host Dept:</b>
                                                                <span class="pull-right"><asp:Label ID="lblDetDept" runat="server"></asp:Label></span>
                                                            </li>
                                                            <li class="list-group-item">
                                                                <i class="fa fa-clock-o" style="width: 20px;"></i><b>Pass Valid Until:</b>
                                                                <span class="pull-right text-danger"><asp:Label ID="lblDetValid" runat="server"></asp:Label></span>
                                                            </li>
                                                            <li class="list-group-item" id="liCheckInTime" runat="server" visible="false">
                                                                <i class="fa fa-sign-in" style="width: 20px; color: green;"></i><b>Actual In-Time:</b>
                                                                <span class="pull-right" style="color: green; font-weight: bold;"><asp:Label ID="lblDetInTime" runat="server"></asp:Label></span>
                                                            </li>
                                                        </ul>
                                                    </asp:Panel>

                                                    <div class="row text-center action-buttons" id="divActionButtons" runat="server" visible="false">
                                                        <asp:Button ID="btnManualCheckIn" runat="server" CssClass="btn btn-success btn-lg" Text="Confirm Check-IN" OnClick="btnManualCheckIn_Click" />
                                                        <asp:Button ID="btnManualCheckOut" runat="server" CssClass="btn btn-warning btn-lg" Text="Confirm Check-OUT" OnClick="btnManualCheckOut_Click" />
                                                        <br /><br />
                                                        <asp:Button ID="btnCancelProcess" runat="server" CssClass="btn btn-default" Text="Cancel / Scan Next Visitor" OnClick="btnResetScanner_Click" />
                                                    </div>

                                                    <div class="row text-center action-buttons" id="divFinishButtons" runat="server" visible="false">
                                                        <asp:Button ID="btnFinish" runat="server" CssClass="btn btn-primary" Text="Scan Next Visitor" OnClick="btnResetScanner_Click" />
                                                    </div>

                                                </asp:Panel>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div role="tabpanel" class="tab-pane fade" id="tab_inside" aria-labelledby="inside-tab">
                                    <div class="row">
                                        <div class="col-xs-12">
                                            <h3 style="margin-top:20px; color:#333;">Currently Inside Premises</h3>
                                            <p class="text-muted">Awaiting Check-Out</p>
                                            
                                            <asp:UpdatePanel ID="upGrid" runat="server" UpdateMode="Conditional">
                                                <ContentTemplate>
                                                    <asp:Label ID="lblGridMsg" runat="server" Font-Bold="true" ForeColor="Green" EnableViewState="false"></asp:Label>

                                                    <div class="row" style="margin-top: 15px;">
                                                        <asp:Repeater ID="rptVisitorLogs" runat="server" OnItemCommand="rptVisitorLogs_ItemCommand">
                                                            <ItemTemplate>
                                                                <div class="col-12 col-md-6 col-lg-4" style="margin-bottom: 20px;">
                                                                    <div class="visitor-card shadow-sm">
                                                                        <div class="card-body" style="padding: 15px; flex-grow: 1;">
                                                                            
                                                                            <div class="d-flex align-items-center" style="margin-bottom: 15px; display: flex;">
                                                                                <img src='<%# Eval("PhotoPath") %>' onerror="this.src='/images/user.png';" style="width: 60px; height: 60px; border-radius: 50%; object-fit: cover; border: 2px solid #1ABB9C; margin-right: 15px;" />
                                                                                <div>
                                                                                    <h4 style="margin: 0; font-weight: bold; color: #333; font-size:16px;"><%# Eval("VisitorName") %></h4>
                                                                                    <small class="text-muted"><i class="fa fa-building"></i> <%# Eval("VisitorCompany") %></small>
                                                                                </div>
                                                                            </div>
                                                                            
                                                                            <p style="margin: 0 0 5px 0; font-size: 13px;"><i class="fa fa-phone"></i> <%# Eval("ContactInfo") %></p>
                                                                            <p style="margin: 0 0 5px 0; font-size: 13px;"><i class="fa fa-user"></i> Host: <b><%# Eval("HostName") %></b></p>
                                                                            <p style="margin: 0 0 5px 0; font-size: 13px; color: green;"><i class="fa fa-sign-in"></i> In: <%# Convert.ToDateTime(Eval("ActualInTime")).ToString("dd MMM, hh:mm tt") %></p>
                                                                            <p style="margin: 0 0 10px 0; font-size: 13px; color: #d9534f;"><i class="fa fa-clock-o"></i> Valid Until: <%# Convert.ToDateTime(Eval("ValidUpto")).ToString("dd MMM, hh:mm tt") %></p>
                                                                        </div>
                                                                        
                                                                        <div style="padding: 10px 15px; background-color: #f9f9f9; border-top: 1px solid #eee;">
                                                                            <asp:LinkButton ID="btnForceCheckOut" runat="server" CommandName="ForceCheckOut" 
                                                                                CommandArgument='<%# Eval("LogID") %>' 
                                                                                CssClass='<%# GetButtonCss(Eval("ValidUpto")) %>' 
                                                                                OnClientClick="return confirm('Are you sure you want to manually Check-Out this visitor?');">
                                                                                <%# GetButtonText(Eval("ValidUpto")) %>
                                                                            </asp:LinkButton>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                </ContentTemplate>
                                            </asp:UpdatePanel>

                                        </div>
                                    </div>
                                </div>

                            </div>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Inject lower-case string from C# boolean to control camera UI
        var isCameraHidden = <%= (!pnlCamera.Visible).ToString().ToLower() %>;

        if (!isCameraHidden) {
            function onScanSuccess(decodedText, decodedResult) {
                // Instantly clear the scanner to prevent double-firing before postback completes
                if (html5QrcodeScanner) {
                    html5QrcodeScanner.clear();
                }
                document.getElementById('<%= hfScannedToken.ClientID %>').value = decodedText;
                document.getElementById('<%= btnProcessScan.ClientID %>').click();
            }

            function onScanFailure(error) { }

            let html5QrcodeScanner = new Html5QrcodeScanner(
                "reader",
                { fps: 10, qrbox: { width: 250, height: 250 }, aspectRatio: 1.0 },
                false);

            html5QrcodeScanner.render(onScanSuccess, onScanFailure);
        }
    </script>
</asp:Content>