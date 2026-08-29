<%@ Page Title="My Visitors" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_MyVisitors.aspx.cs" Inherits="AnmolDristi.VMS_MyVisitors" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .visitor-img { width: 50px; height: 50px; border-radius: 50%; object-fit: cover; border: 2px solid #1ABB9C; }
        .badge-pending { background-color: #f0ad4e; }
        .badge-approved { background-color: #1ABB9C; }
        .badge-rejected { background-color: #d9534f; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>My Visitors <small>Manage your appointments</small></h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">

                        <div class="" role="tabpanel" data-example-id="togglable-tabs">
                            <ul id="myTab" class="nav nav-tabs bar_tabs" role="tablist">
                                <li role="presentation" class="active"><a href="#tab_pending" id="home-tab" role="tab" data-toggle="tab" aria-expanded="true">Pending Approvals <span class="badge bg-red" id="badgeCount" runat="server">0</span></a></li>
                                <li role="presentation" class=""><a href="#tab_history" role="tab" id="profile-tab" data-toggle="tab" aria-expanded="false">Recent History</a></li>
                            </ul>
                            
                            <div id="myTabContent" class="tab-content">
                                
                                <div role="tabpanel" class="tab-pane fade active in" id="tab_pending" aria-labelledby="home-tab">
                                    <div class="table-responsive" style="margin-top:15px;">
                                        <asp:GridView ID="gvPending" runat="server" AutoGenerateColumns="False" CssClass="table table-striped jambo_table bulk_action" OnRowCommand="gvPending_RowCommand" OnRowDataBound="gvPending_RowDataBound" DataKeyNames="LogID,VisitorEmail,VisitorMobile,VisitorName">
                                            <EmptyDataTemplate>
                                                <div class="text-center" style="padding: 30px; color: #73879C;">
                                                    <i class="fa fa-coffee fa-3x"></i>
                                                    <h4>No pending visitors at the moment.</h4>
                                                </div>
                                            </EmptyDataTemplate>
                                            <Columns>
                                                <asp:TemplateField HeaderText="Photo">
                                                    <ItemTemplate>
                                                        <img src='<%# Eval("PhotoPath") %>' class="visitor-img" alt="Visitor" onerror="this.src='/images/user.png';" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField DataField="VisitorName" HeaderText="Visitor Name" />
                                                <asp:BoundField DataField="VisitorCompany" HeaderText="Company" />
                                                <asp:BoundField DataField="PurposeOfVisit" HeaderText="Purpose" />
                                                <asp:BoundField DataField="CreatedDate" HeaderText="Arrived At" DataFormatString="{0:dd MMM hh:mm tt}" />
                                                <asp:TemplateField HeaderText="Action">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="btnApprove" runat="server" CommandName="ApproveVisit" CommandArgument='<%# Container.DataItemIndex %>' CssClass="btn btn-success btn-xs"><i class="fa fa-check"></i> Approve</asp:LinkButton>
                                                        <asp:LinkButton ID="btnReject" runat="server" CommandName="RejectVisit" CommandArgument='<%# Container.DataItemIndex %>' CssClass="btn btn-danger btn-xs" OnClientClick="return confirm('Are you sure you want to reject this visitor?');"><i class="fa fa-times"></i> Reject</asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                    <asp:Label ID="lblPendingMsg" runat="server" Font-Bold="true"></asp:Label>
                                </div>

                                <div role="tabpanel" class="tab-pane fade" id="tab_history" aria-labelledby="profile-tab">
                                    <div class="table-responsive" style="margin-top:15px;">
                                        <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-bordered" DataKeyNames="LogID" OnRowCommand="gvHistory_RowCommand">
                                            <Columns>
                                                <asp:BoundField DataField="VisitorName" HeaderText="Visitor Name" />
                                                <asp:BoundField DataField="VisitorCompany" HeaderText="Company" />
                                                <asp:BoundField DataField="CreatedDate" HeaderText="Date" DataFormatString="{0:dd MMM yyyy}" />
                                                <asp:TemplateField HeaderText="Status">
                                                    <ItemTemplate>
                                                        <span class='badge <%# Eval("ApprovalStatus").ToString() == "Approved" ? "badge-approved" : "badge-rejected" %>'>
                                                            <%# Eval("ApprovalStatus") %>
                                                        </span>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:BoundField DataField="VisitStatus" HeaderText="Gate Status" />
                                                
                                                <asp:TemplateField HeaderText="Host Actions">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="btnManualCheckIn" runat="server" CommandName="ManualCheckIn" CommandArgument='<%# Eval("LogID") %>' 
                                                            CssClass="btn btn-info btn-xs" 
                                                            Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Approved" && Convert.ToString(Eval("VisitStatus")) != "Checked-In" && Convert.ToString(Eval("VisitStatus")) != "Checked-Out" %>' 
                                                            OnClientClick="return confirm('Are you sure you want to manually Check-IN this visitor?');">
                                                            <i class="fa fa-sign-in"></i> Check-In
                                                        </asp:LinkButton>
                                                        
                                                        <asp:LinkButton ID="btnManualCheckOut" runat="server" CommandName="ManualCheckOut" CommandArgument='<%# Eval("LogID") %>' 
                                                            CssClass="btn btn-warning btn-xs" 
                                                            Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Approved" && Convert.ToString(Eval("VisitStatus")) == "Checked-In" %>' 
                                                            OnClientClick="return confirm('Are you sure you want to manually Check-OUT this visitor?');">
                                                            <i class="fa fa-sign-out"></i> Check-Out
                                                        </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                    <asp:Label ID="lblHistoryMsg" runat="server" Font-Bold="true"></asp:Label>
                                </div>

                            </div>
                        </div>

                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="ScheduleApprovalModal" tabindex="-1" role="dialog" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <asp:UpdatePanel ID="upScheduleApproval" runat="server">
                    <ContentTemplate>
                        <div class="modal-header">
                            <button type="button" class="close" data-dismiss="modal"><span aria-hidden="true">&times;</span></button>
                            <h4 class="modal-title">Select Days to Approve</h4>
                        </div>
                        <div class="modal-body">
                            <p>Choose which days to approve for this multi-day visit:</p>
                            <asp:HiddenField ID="hfModalLogId" runat="server" />
                            <asp:CheckBoxList ID="cblModalDays" runat="server" RepeatDirection="Vertical" CssClass="checkbox" />

                            <div class="row" style="margin-top: 15px;">
                                <div class="col-xs-12">
                                    <label class="control-label">Adjust the valid access window for this visitor if required.</label>
                                </div>
                                <div class="col-xs-6">
                                    <label for="<%= txtApprovedTimeFrom.ClientID %>">Valid From</label>
                                    <asp:TextBox ID="txtApprovedTimeFrom" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                                </div>
                                <div class="col-xs-6">
                                    <label for="<%= txtApprovedTimeTo.ClientID %>">Valid Until</label>
                                    <asp:TextBox ID="txtApprovedTimeTo" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                                </div>
                            </div>

                            <asp:Label ID="lblModalMsg" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-default" data-dismiss="modal">Cancel</button>
                            <asp:HiddenField ID="hfPlantTimingsJson" runat="server" />
                            <asp:Button ID="btnConfirmSchedule" runat="server" Text="Confirm Approval" CssClass="btn btn-success" OnClick="btnConfirmSchedule_Click" OnClientClick="return confirmHostScheduleApproval();" />
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnConfirmSchedule" />
                    </Triggers>
                </asp:UpdatePanel>
            </div>
        </div>
    </div>

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
        function describeCurrentTimeViolation(nowMin, timing) {
            if (!timing) return "";
            if (!timing.open && !timing.close)
                return "Current registration is outside today's working hours. The plant is closed today.";
            var openMin = parseTimeSpan(timing.open);
            var closeMin = parseTimeSpan(timing.close);
            if (openMin !== null && nowMin < openMin)
                return "Current registration is outside today's working hours. The plant opens at " + formatClock(openMin) + ".";
            if (closeMin !== null && nowMin >= closeMin)
                return "Current registration is outside today's working hours. The plant closed at " + formatClock(closeMin) + ".";
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
        function buildHostHoursWarning(timings, timeFrom, timeTo, dateIsos) {
            var now = new Date();
            var todayName = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"][now.getDay()];
            var nowMin = now.getHours() * 60 + now.getMinutes();
            var currentViolation = describeCurrentTimeViolation(nowMin, timings[todayName]);
            var dayBlocks = [];
            if (timeFrom !== null && timeTo !== null) {
                for (var i = 0; i < dateIsos.length; i++) {
                    var classification = classifyPlantHourWindow(timeFrom, timeTo, timings[weekdayName(dateIsos[i])]);
                    if (classification.hasViolation)
                        dayBlocks.push(formatDayViolationBlock(dateIsos[i], timeFrom, timeTo, classification));
                }
            }
            if (!currentViolation && dayBlocks.length === 0)
                return "";
            var msg = "Warning:";
            if (currentViolation)
                msg += "\n" + currentViolation;
            if (dayBlocks.length)
                msg += (currentViolation ? "\n\n" : "\n") + dayBlocks.join("\n\n");
            msg += "\n\nApproving this visit will be recorded as an after-hours approval.";
            return msg;
        }
        function confirmHostScheduleApproval() {
            var timings = {};
            try {
                var raw = $("#<%= hfPlantTimingsJson.ClientID %>").val();
                if (raw) timings = JSON.parse(raw);
            } catch (e) { timings = {}; }

            var timeFrom = parseTimeSpan($("#<%= txtApprovedTimeFrom.ClientID %>").val());
            var timeTo = parseTimeSpan($("#<%= txtApprovedTimeTo.ClientID %>").val());
            var dates = [];
            $("#<%= cblModalDays.ClientID %> input:checked").each(function () {
                dates.push($(this).val());
            });
            var warning = buildHostHoursWarning(timings, timeFrom, timeTo, dates);
            if (!warning)
                return true;
            return confirm(warning);
        }
    </script>
</asp:Content>