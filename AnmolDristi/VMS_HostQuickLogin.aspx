<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VMS_HostQuickLogin.aspx.cs" Inherits="AnmolDristi.VMS_HostQuickLogin" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Instant Visitor Approval | Anmol VMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F7F7F7; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
        .card { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.1); width: 100%; max-width: 450px; text-align: center; }
        .visitor-name { color: #1ABB9C; font-size: 24px; font-weight: bold; margin-bottom: 5px; }
        .visitor-details { color: #73879C; font-size: 16px; margin-bottom: 25px; line-height: 1.6; }
        .btn { border: none; padding: 15px 20px; border-radius: 4px; cursor: pointer; width: 48%; font-size: 16px; font-weight: bold; color: white; }
        .btn-approve { background: #1ABB9C; float: left; }
        .btn-approve:hover { background: #149b82; }
        .btn-reject { background: #d9534f; float: right; }
        .btn-reject:hover { background: #c9302c; }
        .error { color: #d9534f; margin-top: 15px; font-size: 16px; font-weight: bold; display: block; }
        .success { color: #1ABB9C; margin-top: 15px; font-size: 18px; font-weight: bold; display: block; }
        .clearfix { clear: both; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="card">
            <h2 style="color:#333; margin-top:0;">Visitor Request</h2>
            <hr style="border: 0; border-top: 1px solid #eee; margin-bottom: 20px;" />

            <asp:Panel ID="pnlAction" runat="server" Visible="false">
                <div class="visitor-name">
                    <asp:Label ID="lblVisName" runat="server"></asp:Label>
                </div>
                <div class="visitor-details">
                    <b>Company:</b> <asp:Label ID="lblCompany" runat="server"></asp:Label><br />
                    <b>Purpose:</b> <asp:Label ID="lblPurpose" runat="server"></asp:Label><br />
                    <b>Contact:</b> <asp:Label ID="lblContact" runat="server"></asp:Label>
                </div>

                <div>
                    <asp:Button ID="btnApprove" runat="server" Text="✓ Approve" CssClass="btn btn-approve" OnClick="btnApprove_Click" OnClientClick="return confirm('Approve this visitor and generate Gate Pass?');" />
                    <asp:Button ID="btnReject" runat="server" Text="✗ Reject" CssClass="btn btn-reject" OnClick="btnReject_Click" OnClientClick="return confirm('Reject this visitor?');" />
                    <div class="clearfix"></div>
                </div>
            </asp:Panel>

            <asp:Label ID="lblMessage" runat="server" CssClass="error"></asp:Label>
        </div>
    </form>
</body>
</html>