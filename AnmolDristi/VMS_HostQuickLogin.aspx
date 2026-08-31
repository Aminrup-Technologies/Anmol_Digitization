<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VMS_HostQuickLogin.aspx.cs" Inherits="AnmolDristi.VMS_HostQuickLogin" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Instant Visitor Approval | Anmol VMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
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
        .day-window-wrap { text-align: left; margin: 16px 0 8px 0; }
        .schedule-summary { font-size: 13px; color: #333; font-weight: 600; margin-bottom: 8px; }
        .day-window-title { font-size: 11px; font-weight: 700; color: #73879C; margin-bottom: 6px; text-transform: uppercase; }
        .day-window-table { width: 100%; border-collapse: collapse; font-size: 12px; }
        .day-window-table th { text-align: left; color: #888; font-weight: 600; padding: 3px 6px 5px 0; border-bottom: 1px solid #eee; }
        .day-window-table td { padding: 5px 6px 5px 0; color: #333; border-bottom: 1px solid #f3f3f3; vertical-align: middle; }
        .badge { padding: 3px 8px; border-radius: 10px; font-weight: 600; font-size: 10px; color: #fff; }
        .badge-pending { background-color: #f0ad4e; }
        .badge-approved { background-color: #1ABB9C; }
        .badge-rejected { background-color: #d9534f; }
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

                <asp:Literal ID="litPendingVisitDays" runat="server"></asp:Literal>

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
                <asp:Literal ID="litVisitDaysRecap" runat="server"></asp:Literal>
            </asp:Panel>

            <asp:Label ID="lblMessage" runat="server" CssClass="error"></asp:Label>
        </div>
    </form>
    <script type="text/javascript">
        function parseTimeSpan(value) {
            if (!value) return null;
            var parts = value.split(":");
            if (parts.length < 2) return null;
            var h = parseInt(parts[0], 10);
            var m = parseInt(parts[1], 10);
            if (isNaN(h) || isNaN(m)) return null;
            return (h * 60) + m;
        }
        function weekdayName(dateValue) {
            var names = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
            var d = new Date(dateValue + "T00:00:00");
            return names[d.getDay()];
        }
        function formatClock(minutes) {
            if (minutes === null || minutes === undefined) return "";
            var h = Math.floor(minutes / 60);
            var m = minutes % 60;
            var suffix = h >= 12 ? "PM" : "AM";
            var hour12 = h % 12;
            if (hour12 === 0) hour12 = 12;
            return (hour12 < 10 ? "0" : "") + hour12 + ":" + (m < 10 ? "0" : "") + m + " " + suffix;
        }
        function formatNoticeDate(dateValue) {
            var d = new Date(dateValue + "T00:00:00");
            var months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
            return weekdayName(dateValue) + " " + ("0" + d.getDate()).slice(-2) + " " + months[d.getMonth()];
        }
        function classifyPlantHourWindow(startMin, endMin, timing) {
            var result = { plantClosed: false, arrivalBeforeOpen: false, arrivalAfterClose: false, departureAfterClose: false, openMin: null, closeMin: null, hasViolation: false };
            if (!timing) return result;
            result.openMin = parseTimeSpan(timing.open);
            result.closeMin = parseTimeSpan(timing.close);
            if (!timing.open && !timing.close) {
                result.plantClosed = true;
                result.hasViolation = true;
                return result;
            }
            if (result.openMin !== null && startMin < result.openMin) result.arrivalBeforeOpen = true;
            if (result.closeMin !== null) {
                if (startMin >= result.closeMin) result.arrivalAfterClose = true;
                if (endMin > result.closeMin) result.departureAfterClose = true;
            }
            result.hasViolation = result.plantClosed || result.arrivalBeforeOpen || result.arrivalAfterClose || result.departureAfterClose;
            return result;
        }
        function describeCurrentTimeViolation(nowMin, timing, context) {
            if (!timing) return "";
            var leadIn = context === "approval"
                ? "Current approval is outside today's working hours."
                : "Current registration is outside today's working hours.";
            if (!timing.open && !timing.close)
                return leadIn + " The plant is closed today.";
            var openMin = parseTimeSpan(timing.open);
            var closeMin = parseTimeSpan(timing.close);
            if (openMin !== null && nowMin < openMin)
                return leadIn + " The plant opens at " + formatClock(openMin) + ".";
            if (closeMin !== null && nowMin >= closeMin)
                return leadIn + " The plant closed at " + formatClock(closeMin) + ".";
            return "";
        }
        function formatDayViolationBlock(dateValue, startMin, endMin, classification) {
            var block = formatNoticeDate(dateValue);
            if (classification.plantClosed) {
                block += "\n• The plant is closed this day.";
                return block;
            }
            if (classification.arrivalBeforeOpen)
                block += "\n• Arrival time (" + formatClock(startMin) + ") is before plant opening (" + formatClock(classification.openMin) + ").";
            if (classification.arrivalAfterClose)
                block += "\n• Arrival time (" + formatClock(startMin) + ") is after plant closing (" + formatClock(classification.closeMin) + ").";
            if (classification.departureAfterClose)
                block += "\n• Departure time (" + formatClock(endMin) + ") is after plant closing (" + formatClock(classification.closeMin) + ").";
            return block;
        }
        function ymd(d) {
            return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2);
        }
        function confirmHostQuickApprove() {
            var timings = {};
            try {
                var raw = document.getElementById("<%= hfPlantTimingsJson.ClientID %>").value;
                if (raw) timings = JSON.parse(raw);
            } catch (e) { timings = {}; }

            var timeFromEl = document.getElementById("<%= txtApprovedTimeFrom.ClientID %>");
            var timeToEl = document.getElementById("<%= txtApprovedTimeTo.ClientID %>");
            var timeFrom = parseTimeSpan(timeFromEl ? timeFromEl.value : "");
            var timeTo = parseTimeSpan(timeToEl ? timeToEl.value : "");

            var dates = [];
            var dayList = document.getElementById("<%= cblRequestedDays.ClientID %>");
            if (dayList && dayList.style.display !== "none") {
                var checks = dayList.querySelectorAll("input[type=checkbox]:checked");
                for (var i = 0; i < checks.length; i++) dates.push(checks[i].value);
            }
            if (dates.length === 0) {
                var fromIso = document.getElementById("<%= hfVisitFromDate.ClientID %>").value;
                var toIso = document.getElementById("<%= hfVisitToDate.ClientID %>").value || fromIso;
                if (fromIso) {
                    var cursor = new Date(fromIso + "T00:00:00");
                    var end = new Date((toIso || fromIso) + "T00:00:00");
                    while (cursor <= end) {
                        dates.push(ymd(cursor));
                        cursor.setDate(cursor.getDate() + 1);
                    }
                }
            }

            var now = new Date();
            var todayName = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"][now.getDay()];
            var nowMin = now.getHours() * 60 + now.getMinutes();
            var currentViolation = describeCurrentTimeViolation(nowMin, timings[todayName], "approval");
            var dayBlocks = [];
            if (timeFrom !== null && timeTo !== null) {
                for (var j = 0; j < dates.length; j++) {
                    var classification = classifyPlantHourWindow(timeFrom, timeTo, timings[weekdayName(dates[j])]);
                    if (classification.hasViolation)
                        dayBlocks.push(formatDayViolationBlock(dates[j], timeFrom, timeTo, classification));
                }
            }

            if (!currentViolation && dayBlocks.length === 0)
                return confirm("Approve this visitor and generate Gate Pass?");

            var msg = "Warning:";
            if (currentViolation)
                msg += "\n" + currentViolation;
            if (dayBlocks.length)
                msg += (currentViolation ? "\n\n" : "\n") + dayBlocks.join("\n\n");
            msg += "\n\nApproving this visit will be recorded as an after-hours approval.";
            return confirm(msg);
        }
    </script>
</body>
</html>