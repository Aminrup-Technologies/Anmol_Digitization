<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VMS_DigitalPass.aspx.cs" Inherits="AnmolDristi.VMS_DigitalPass" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Your Digital Gate Pass</title>
    <link href="vendors/bootstrap/dist/css/css/bootstrap.min.css" rel="stylesheet">
    <link href="vendors/font-awesome/css/font-awesome.min.css" rel="stylesheet">
    
    <style>
        body { background-color: #f7f7f7; font-family: 'Helvetica Neue', Arial, sans-serif; }
        .pass-container { max-width: 400px; margin: 30px auto; }
        .ticket-card { background: #fff; border-radius: 12px; box-shadow: 0 8px 20px rgba(0,0,0,0.1); overflow: hidden; }
        .ticket-header { background: #1ABB9C; color: #fff; padding: 20px; text-align: center; }
        .ticket-header h3 { margin: 0; font-weight: 700; font-size: 24px; }
        .ticket-header p { margin: 5px 0 0 0; opacity: 0.9; }
        .ticket-body { padding: 25px 20px; text-align: center; border-bottom: 2px dashed #eee; }
        .visitor-photo { width: 120px; height: 120px; border-radius: 50%; object-fit: cover; border: 4px solid #1ABB9C; margin-bottom: 15px; }
        .qr-box { padding: 20px; text-align: center; background: #fafafa; }
        .qr-box img { max-width: 200px; height: auto; margin: 0 auto; }
        .status-badge { display: inline-block; padding: 8px 20px; border-radius: 20px; font-weight: bold; font-size: 14px; margin-top: 10px; }
        .status-valid { background-color: #e8f5e9; color: #2e7d32; }
        .status-expired { background-color: #ffebee; color: #c62828; }
        .status-pending { background-color: #fff8e1; color: #f57f17; }
        .status-inside { background-color: #e3f2fd; color: #1565c0; }
        .status-completed { background-color: #eceff1; color: #455a64; }
        .status-message { display: block; margin-top: 8px; font-size: 13px; color: #555; }
        .policy-box { padding: 15px 20px; font-size: 12px; color: #666; background: #fff; }
        .info-row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; text-align: left; }
        .info-label { color: #777; font-weight: 600; }
        .info-value { color: #333; font-weight: bold; text-align: right; }
        .day-window { margin-top: 14px; text-align: left; }
        .day-window-title { font-size: 12px; font-weight: 700; color: #73879C; margin-bottom: 8px; text-transform: uppercase; }
        .day-window-table { width: 100%; border-collapse: collapse; font-size: 12px; }
        .day-window-table th { text-align: left; color: #888; font-weight: 600; padding: 4px 4px 6px 0; border-bottom: 1px solid #eee; }
        .day-window-table td { padding: 6px 4px 6px 0; color: #333; border-bottom: 1px solid #f3f3f3; vertical-align: middle; }
        .badge { padding: 3px 8px; border-radius: 10px; font-weight: 600; font-size: 10px; }
        .badge-pending { background-color: #F39C12; color: #fff; }
        .badge-approved { background-color: #1ABB9C; color: #fff; }
        .badge-rejected { background-color: #E74C3C; color: #fff; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container pass-container">
            
            <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger text-center">
                <h4><i class="fa fa-warning"></i> Access Denied</h4>
                <asp:Label ID="lblError" runat="server"></asp:Label>
            </asp:Panel>

            <asp:Panel ID="pnlPass" runat="server" CssClass="ticket-card" Visible="false">
                
                <div class="ticket-header">
                    <h3><asp:Label ID="lblPlantAlias" runat="server"></asp:Label></h3>
                    <p>OFFICIAL VISITOR PASS</p>
                </div>

                <div class="ticket-body">
                    <asp:Image ID="imgVisitorPhoto" runat="server" CssClass="visitor-photo" />
                    <h2 style="margin: 0; font-weight: bold; color: #333;"><asp:Label ID="lblVisitorName" runat="server"></asp:Label></h2>
                    <p class="text-muted" style="margin-bottom: 20px;"><asp:Label ID="lblCompany" runat="server"></asp:Label></p>

                    <div class="info-row">
                        <span class="info-label">Host:</span>
                        <span class="info-value"><asp:Label ID="lblHostName" runat="server"></asp:Label></span>
                    </div>
                    <div class="info-row">
                        <span class="info-label">Valid From:</span>
                        <span class="info-value"><asp:Label ID="lblValidFrom" runat="server"></asp:Label></span>
                    </div>
                    <div class="info-row">
                        <span class="info-label">Valid Until:</span>
                        <span class="info-value"><asp:Label ID="lblValidUpto" runat="server"></asp:Label></span>
                    </div>
                    <div class="info-row" id="divTodayWindow" runat="server" visible="false">
                        <span class="info-label">Today's Window:</span>
                        <span class="info-value"><asp:Label ID="lblTodayWindow" runat="server"></asp:Label></span>
                    </div>
                    <div class="info-row" id="divApprovedDates" runat="server" visible="false">
                        <span class="info-label">Approved Dates:</span>
                        <span class="info-value"><asp:Label ID="lblApprovedDates" runat="server"></asp:Label></span>
                    </div>
                    <asp:Panel ID="pnlVisitDays" runat="server" Visible="false">
                        <asp:Literal ID="litVisitDays" runat="server"></asp:Literal>
                    </asp:Panel>

                    <asp:Label ID="lblStatusBadge" runat="server"></asp:Label>
                    <asp:Label ID="lblStatusMessage" runat="server" CssClass="status-message"></asp:Label>
                </div>

                <div class="qr-box" id="divQrBox" runat="server">
                    <p class="text-muted"><small>Show this QR code at the security gate</small></p>
                    <asp:Image ID="imgScanQR" runat="server" />
                </div>

                <div class="policy-box">
                    <strong>Plant Safety Rules:</strong><br />
                    <asp:Literal ID="litPolicyText" runat="server"></asp:Literal>
                </div>

            </asp:Panel>

        </div>
    </form>
</body>
</html>