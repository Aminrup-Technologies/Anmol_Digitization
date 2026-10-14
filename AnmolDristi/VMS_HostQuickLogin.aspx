<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VMS_HostQuickLogin.aspx.cs" Inherits="AnmolDristi.VMS_HostQuickLogin" %>
<%@ Register Src="~/Controls/VisitWindowControl.ascx" TagPrefix="vms" TagName="VisitWindow" %>
<%@ Register Src="~/Controls/ApprovalSummaryControl.ascx" TagPrefix="vms" TagName="ApprovalSummary" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Instant Visitor Approval | Anmol VMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="css/vms-ui.css" rel="stylesheet" />
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F7F7F7; display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 20px 0; box-sizing: border-box; }
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
        .day-checklist { text-align: left; margin: 0 0 20px 0; color: #73879C; font-size: 14px; }
        .day-checklist td { padding: 4px 0; }
        .validity-window { text-align: left; margin: 0 0 20px 0; }
        .validity-window .hint { color: #73879C; font-size: 13px; margin: 0 0 10px 0; display: block; }
        .validity-row { display: flex; gap: 10px; }
        .validity-row .field { flex: 1; min-width: 0; }
        .validity-row label { display: block; font-size: 12px; color: #73879C; font-weight: 600; margin-bottom: 4px; }
        .validity-row input { width: 100%; box-sizing: border-box; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 16px; }
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
                    <b>Requested Schedule:</b> <asp:Label ID="lblSchedule" runat="server"></asp:Label><br />
                    <b>Contact:</b> <asp:Label ID="lblContact" runat="server"></asp:Label>
                </div>

                <vms:VisitWindow ID="ucPendingVisitDays" runat="server" />

                <asp:CheckBoxList ID="cblRequestedDays" runat="server" Visible="false"
                    CssClass="day-checklist" RepeatDirection="Vertical" />

                <div class="validity-window">
                    <span class="hint">Adjust the valid access window for this visitor if required.</span>
                    <asp:HiddenField ID="hfPlantTimingsJson" runat="server" />
                    <asp:HiddenField ID="hfVisitFromDate" runat="server" />
                    <asp:HiddenField ID="hfVisitToDate" runat="server" />
                    <div class="validity-row">
                        <div class="field">
                            <label for="<%= txtApprovedTimeFrom.ClientID %>">Valid From</label>
                            <asp:TextBox ID="txtApprovedTimeFrom" runat="server" TextMode="Time"></asp:TextBox>
                        </div>
                        <div class="field">
                            <label for="<%= txtApprovedTimeTo.ClientID %>">Valid Until</label>
                            <asp:TextBox ID="txtApprovedTimeTo" runat="server" TextMode="Time"></asp:TextBox>
                        </div>
                    </div>
                </div>

                <div>
                    <asp:Button ID="btnApprove" runat="server" Text="✓ Approve" CssClass="btn btn-approve" OnClick="btnApprove_Click" OnClientClick="return confirmHostQuickApprove();" />
                    <asp:Button ID="btnReject" runat="server" Text="✗ Reject" CssClass="btn btn-reject" OnClick="btnReject_Click" OnClientClick="return confirm('Reject this visitor?');" />
                    <div class="clearfix"></div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlVisitDaysRecap" runat="server" Visible="false" CssClass="day-window-wrap">
                <vms:ApprovalSummary ID="ucVisitDaysRecap" runat="server" />
            </asp:Panel>

            <asp:Label ID="lblMessage" runat="server" CssClass="error"></asp:Label>
        </div>
    </form>
    <script src="Scripts/VMS_UI.js"></script>
    <script type="text/javascript">
        function confirmHostQuickApprove() {
            var timings = VmsUI.parseTimingsJson(document.getElementById("<%= hfPlantTimingsJson.ClientID %>").value);
            var timeFromEl = document.getElementById("<%= txtApprovedTimeFrom.ClientID %>");
            var timeToEl = document.getElementById("<%= txtApprovedTimeTo.ClientID %>");
            var timeFrom = VmsUI.parseTimeSpan(timeFromEl ? timeFromEl.value : "");
            var timeTo = VmsUI.parseTimeSpan(timeToEl ? timeToEl.value : "");

            var dates = [];
            var dayList = document.getElementById("<%= cblRequestedDays.ClientID %>");
            if (dayList && dayList.style.display !== "none") {
                var checks = dayList.querySelectorAll("input[type=checkbox]:checked");
                for (var i = 0; i < checks.length; i++) dates.push(checks[i].value);
            }
            if (dates.length === 0) {
                var fromIso = document.getElementById("<%= hfVisitFromDate.ClientID %>").value;
                var toIso = document.getElementById("<%= hfVisitToDate.ClientID %>").value || fromIso;
                dates = VmsUI.enumerateInclusiveDates(fromIso, toIso);
            }

            var warning = VmsUI.buildHostHoursWarning(timings, timeFrom, timeTo, dates);
            if (!warning)
                return confirm("Approve this visitor and generate Gate Pass?");
            return confirm(warning);
        }
    </script>
</body>
</html>
