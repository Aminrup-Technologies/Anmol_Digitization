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
            margin-top: 20px;
        }

        #reader {
            width: 100%;
            max-width: 500px;
            margin: 0 auto;
            border: 4px solid #1ABB9C;
            border-radius: 10px;
            overflow: hidden;
        }

        /* REMOVED display: none; FROM HERE */
        .result-card {
            margin-top: 20px;
            padding: 20px;
            border-radius: 8px;
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
            width: 100px;
            height: 100px;
            border-radius: 50%;
            object-fit: cover;
            border: 3px solid #fff;
            box-shadow: 0 2px 5px rgba(0,0,0,0.2);
            margin-bottom: 10px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-6 col-md-offset-3 col-sm-8 col-sm-offset-2 col-xs-12">

                <div class="scanner-container">
                    <h2 style="color: #1ABB9C; font-weight: bold;"><i class="fa fa-qrcode"></i>Security Gate Scanner</h2>
                    <p class="text-muted">Point the camera at the visitor's Digital Pass.</p>
                    <div class="ln_solid"></div>

                    <div id="reader"></div>

                    <asp:HiddenField ID="hfScannedToken" runat="server" />
                    <asp:Button ID="btnProcessScan" runat="server" Text="Process" OnClick="btnProcessScan_Click" Style="display: none;" />

                    <asp:Panel ID="pnlResult" runat="server" CssClass="result-card" Visible="false">

                        <div class="row">
                            <div class="col-xs-12 text-center" style="margin-bottom: 15px;">
                                <asp:Image ID="imgVisitor" runat="server" CssClass="visitor-img" Visible="false" />
                            </div>
                        </div>

                        <div class="row text-center" style="margin-bottom: 20px; border-bottom: 1px solid rgba(0,0,0,0.1); padding-bottom: 10px;">
                            <h3>
                                <asp:Label ID="lblScanTitle" runat="server"></asp:Label></h3>
                            <p style="font-size: 16px; font-weight: 500;">
                                <asp:Label ID="lblScanMessage" runat="server"></asp:Label>
                            </p>
                        </div>

                        <asp:Panel ID="pnlVisitDetails" runat="server" Visible="false">
                            <ul class="list-group text-left" style="font-size: 15px; color: #333;">
                                <li class="list-group-item">
                                    <i class="fa fa-user" style="width: 20px;"></i><b>Visitor:</b>
                                    <span class="pull-right">
                                        <asp:Label ID="lblDetName" runat="server"></asp:Label></span>
                                </li>
                                <li class="list-group-item">
                                    <i class="fa fa-building" style="width: 20px;"></i><b>Company:</b>
                                    <span class="pull-right">
                                        <asp:Label ID="lblDetCompany" runat="server"></asp:Label></span>
                                </li>
                                <li class="list-group-item">
                                    <i class="fa fa-phone" style="width: 20px;"></i><b>Contact:</b>
                                    <span class="pull-right">
                                        <asp:Label ID="lblDetContact" runat="server"></asp:Label></span>
                                </li>
                                <li class="list-group-item" style="background-color: #f9f9f9;">
                                    <i class="fa fa-id-badge" style="width: 20px; color: #1ABB9C;"></i><b>Host Employee:</b>
                                    <span class="pull-right" style="font-weight: bold; color: #1ABB9C;">
                                        <asp:Label ID="lblDetHost" runat="server"></asp:Label></span>
                                </li>
                                <li class="list-group-item">
                                    <i class="fa fa-briefcase" style="width: 20px;"></i><b>Host Dept:</b>
                                    <span class="pull-right">
                                        <asp:Label ID="lblDetDept" runat="server"></asp:Label></span>
                                </li>
                                <li class="list-group-item">
                                    <i class="fa fa-clock-o" style="width: 20px;"></i><b>Pass Valid Until:</b>
                                    <span class="pull-right text-danger">
                                        <asp:Label ID="lblDetValid" runat="server"></asp:Label></span>
                                </li>
                            </ul>
                        </asp:Panel>

                        <div class="row text-center mt-3">
                            <asp:Button ID="btnResetScanner" runat="server" CssClass="btn btn-primary" Text="Scan Next Visitor" OnClick="btnResetScanner_Click" />
                        </div>
                    </asp:Panel>

                </div>

            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Initialize Scanner only if the result panel is NOT showing
        var isResultShowing = '<%= pnlResult.Visible %>' === 'True';

        if (!isResultShowing) {
            function onScanSuccess(decodedText, decodedResult) {
                // Stop scanner to prevent multiple rapid fires
                html5QrcodeScanner.clear();

                // Optional: Play a beep sound
                // var audio = new Audio('/sounds/beep.mp3'); audio.play();

                // Put token in hidden field and click the invisible ASP.NET button
                document.getElementById('<%= hfScannedToken.ClientID %>').value = decodedText;
                document.getElementById('<%= btnProcessScan.ClientID %>').click();
            }

            function onScanFailure(error) {
                // handle scan failure, usually better to ignore and keep scanning
            }

            let html5QrcodeScanner = new Html5QrcodeScanner(
                "reader",
                { fps: 10, qrbox: { width: 250, height: 250 }, aspectRatio: 1.0 },
                /* verbose= */ false);

            html5QrcodeScanner.render(onScanSuccess, onScanFailure);
        }
    </script>
</asp:Content>
