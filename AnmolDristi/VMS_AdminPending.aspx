<%@ Page Title="VMS | Admin Pending" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_AdminPending.aspx.cs" MaintainScrollPositionOnPostback="true" Inherits="AnmolDristi.VMS_AdminPending" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Typography & Layout Enhancements */
        .grid-timeline small { display: block; line-height: 1.4; }
        .grid-host small { color: #73879C; font-size: 11px; line-height: 1.3; display: block; }
        .audit-box { border: 1px solid #1ABB9C !important; background-color: #fcfcfc; }
        .visitor-photo { width: 45px; height: 45px; border-radius: 50%; margin-right: 12px; border: 1px solid #ddd; object-fit: cover; }
        .email-body-preview { background: #f9f9f9; padding: 10px; border: 1px solid #ddd; font-size: 12px; max-height: 250px; overflow-y: auto; border-radius: 4px; margin-top: 10px; }
        .action-column .btn { margin-bottom: 5px; text-align: left; }
        .schedule-summary { font-size: 12px; color: #333; font-weight: 600; margin-bottom: 8px; }
        .day-window { margin-top: 4px; min-width: 260px; }
        .day-window-title { font-size: 11px; font-weight: 700; color: #73879C; margin-bottom: 6px; text-transform: uppercase; }
        .day-window-table { width: 100%; border-collapse: collapse; font-size: 12px; }
        .day-window-table th { text-align: left; color: #888; font-weight: 600; padding: 3px 8px 5px 0; border-bottom: 1px solid #eee; }
        .day-window-table td { padding: 5px 8px 5px 0; color: #333; border-bottom: 1px solid #f3f3f3; vertical-align: middle; white-space: nowrap; }
        .badge { padding: 3px 8px; border-radius: 10px; font-weight: 600; font-size: 10px; color: #fff; }
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
                        <h2><i class="fa fa-shield"></i> Central Visitor Administration</h2>
                        <div class="clearfix"></div>
                    </div>

                    <div class="x_content">
                        <asp:UpdatePanel ID="upAdminGrid" runat="server">
                            <ContentTemplate>
                                <asp:Label ID="lblMessage" runat="server" Font-Bold="true" Style="display: block; margin-bottom: 15px;"></asp:Label>

                                <ul class="nav nav-tabs" id="adminTabs" role="tablist">
                                    <li class="active"><a data-toggle="tab" href="#tabPending" id="pending-tab" role="tab"><i class="fa fa-clock-o"></i> Pending Actions</a></li>
                                    <li><a data-toggle="tab" href="#tabGateOverride" id="override-tab" role="tab"><i class="fa fa-edit"></i> Inline Gate Override</a></li>
                                </ul>

                                <div class="tab-content" style="padding-top: 20px;">

                                    <div id="tabPending" class="tab-pane fade in active" role="tabpanel">
                                        <div class="alert alert-info">
                                            <strong><i class="fa fa-info-circle"></i> Note:</strong> From here you can Remind Hosts, update Host Codes, or forcefully Approve visitors on their behalf.
                                        </div>
                                        <div class="table-responsive">
                                            <asp:GridView ID="gvAdminPending" runat="server" AutoGenerateColumns="False"
                                                CssClass="table table-striped table-bordered table-hover"
                                                DataKeyNames="LogID,VisitorEmail,VisitorName,HostEmail,HostName"
                                                OnRowCommand="gvAdminPending_RowCommand" OnRowDataBound="gvAdminPending_RowDataBound"
                                                EmptyDataText="No pending visitors at this time."
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    
                                                    <asp:TemplateField HeaderText="Log ID" ItemStyle-Width="60px" ItemStyle-CssClass="text-center">
                                                        <ItemTemplate>
                                                            <strong>#<%# Eval("LogID") %></strong><br />
                                                            <asp:LinkButton ID="btnViewEmails" runat="server" CommandName="ViewEmails" CssClass="btn btn-info btn-xs" ToolTip="View Email Logs" Style="margin-top: 5px;"><i class="fa fa-envelope"></i> Logs</asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Visitor Details" ItemStyle-Width="220px">
                                                        <ItemTemplate>
                                                            <div style="display: flex; align-items: center;">
                                                                <img src='<%# Eval("PhotoPath") != DBNull.Value && !string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? ResolveUrl(Eval("PhotoPath").ToString()) : ResolveUrl("~/Images/default-avatar.png") %>' class="visitor-photo" />
                                                                <div>
                                                                    <strong style="color: #333; font-size: 13px;"><%# Eval("VisitorName") %></strong><br />
                                                                    <small class="text-muted"><%# Eval("VisitorEmail") %></small><br />
                                                                    <small style="color: #73879C;"><i class="fa fa-clock-o"></i> Req: <%# Eval("CreatedDate", "{0:dd MMM HH:mm}") %></small>
                                                                </div>
                                                            </div>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Branch (ID)" ItemStyle-Width="120px">
                                                        <ItemTemplate>
                                                            <strong><%# Eval("Branch_Description") %></strong><br />
                                                            <small class="text-muted"><i class="fa fa-map-marker"></i> ID: <%# Eval("TargetBranchID") %></small>
                                                            <asp:HiddenField ID="hfTargetBranchID" runat="server" Value='<%# Eval("TargetBranchID") %>' />
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Host Code & Details" ItemStyle-Width="220px">
                                                        <ItemTemplate>
                                                            <div class="input-group" style="margin-bottom: 5px;">
                                                                <asp:TextBox ID="txtHostCode" runat="server" CssClass="form-control input-sm"
                                                                    Placeholder="Emp Code..." AutoPostBack="true"
                                                                    OnTextChanged="txtHostCode_TextChanged"
                                                                    Text='<%# Eval("HostEmployeeCode") %>'></asp:TextBox>
                                                                <span class="input-group-btn">
                                                                    <asp:LinkButton ID="btnSyncHost" runat="server" CommandName="SyncHost"
                                                                        CssClass="btn btn-success btn-sm" ToolTip="Save Host Change">
                                                                        <i class="fa fa-save"></i>
                                                                    </asp:LinkButton>
                                                                </span>
                                                            </div>
                                                            <asp:Label ID="lblHostDetails" runat="server">
                                                                <strong style="color: #555;"><%# Eval("HostName") %></strong><br />
                                                                <small class="text-muted"><i class="fa fa-envelope"></i> <%# Eval("HostEmail") %></small>
                                                            </asp:Label>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Visit Window">
                                                        <ItemTemplate>
                                                            <asp:Literal ID="litDayWindow" runat="server"></asp:Literal>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="On-Behalf Settings" ItemStyle-Width="220px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtScheduleDate" runat="server" TextMode="DateTimeLocal" CssClass="form-control input-sm" style="margin-bottom: 5px;" ToolTip="Schedule Date & Time"></asp:TextBox>
                                                            <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control input-sm" Placeholder="Admin Remarks (Required)..."></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Actions" ItemStyle-Width="120px" ItemStyle-CssClass="action-column">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="btnRetrigger" runat="server" CommandName="Retrigger" CssClass="btn btn-warning btn-sm btn-block" OnClientClick="return confirm('Send high-priority reminder?');"><i class="fa fa-bell"></i> Remind</asp:LinkButton>
                                                            <asp:LinkButton ID="btnForceApprove" runat="server" CommandName="ForceApprove" CssClass="btn btn-success btn-sm btn-block" OnClientClick="return confirm('Approve ON BEHALF of host?');"><i class="fa fa-check"></i> Approve</asp:LinkButton>
                                                            <asp:LinkButton ID="btnSoftDelete" runat="server" CommandName="SoftDelete" CssClass="btn btn-danger btn-sm btn-block" OnClientClick="return confirm('Delete this duplicate/invalid request?');"><i class="fa fa-trash"></i> Delete</asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </div>
                                    </div>

                                    <div id="tabGateOverride" class="tab-pane fade" role="tabpanel">
                                        <div class="row" style="margin-bottom: 15px;">
                                            <div class="col-md-4">
                                                <label>Filter Branch:</label>
                                                <asp:DropDownList ID="ddlBranchFilter" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlBranchFilter_SelectedIndexChanged"></asp:DropDownList>
                                            </div>
                                            <div class="col-md-8 text-right" style="padding-top: 25px;">
                                                <span class="text-danger"><i class="fa fa-bolt"></i><strong>Auto-Save Active:</strong> Type audit reason first, then change time.</span>
                                            </div>
                                        </div>

                                        <div class="table-responsive">
                                            <asp:GridView ID="gvAdminGateOverride" runat="server" AutoGenerateColumns="False"
                                                CssClass="table table-striped table-bordered table-hover"
                                                DataKeyNames="LogID,VisitorName" OnRowCommand="gvAdminGateOverride_RowCommand"
                                                EmptyDataText="No active visitors found." OnRowDataBound="gvAdminGateOverride_RowDataBound"
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    <asp:TemplateField HeaderText="Log ID" ItemStyle-Width="60px" ItemStyle-CssClass="text-center">
                                                        <ItemTemplate>
                                                            <strong style="color: #d9534f;">#<%# Eval("LogID") %></strong><br />
                                                            <asp:LinkButton ID="btnViewEmails" runat="server" CommandName="ViewEmails" CommandArgument='<%# Eval("LogID") %>' CssClass="btn btn-info btn-xs" ToolTip="View Email Logs" Style="margin-top: 5px;"><i class="fa fa-envelope"></i> Logs</asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Visitor Details" ItemStyle-Width="230px">
                                                        <ItemTemplate>
                                                            <div style="display: flex; align-items: center;">
                                                                <img src='<%# Eval("PhotoPath") != DBNull.Value && !string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? ResolveUrl(Eval("PhotoPath").ToString()) : ResolveUrl("~/Images/default-avatar.png") %>' class="visitor-photo" style="width: 40px; height: 40px;" />
                                                                <div>
                                                                    <strong style="color: #1ABB9C; font-size: 14px;"><%# Eval("VisitorName") %></strong><br />
                                                                    <span class='<%# Eval("VisitStatus").ToString() == "Scheduled" ? "label label-warning" : "label label-success" %>'><%# Eval("VisitStatus") %></span>
                                                                </div>
                                                            </div>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Timeline (Req/App)" ItemStyle-CssClass="grid-timeline" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <small style="color: #73879C;"><i class="fa fa-clock-o"></i>Req: <%# Eval("CreatedDate", "{0:dd MMM HH:mm}") %></small>
                                                            <small style="color: #1ABB9C;"><i class="fa fa-check-square-o"></i>App: <%# Eval("ApprovedOn") != DBNull.Value ? Convert.ToDateTime(Eval("ApprovedOn")).ToString("dd MMM HH:mm") : "N/A" %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Host" ItemStyle-CssClass="grid-host" ItemStyle-Width="120px">
                                                        <ItemTemplate>
                                                            <strong style="color: #555;"><%# Eval("HostName") %></strong>
                                                            <small><i class="fa fa-id-badge"></i>Code: <%# Eval("HostEmployeeCode") %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Visit Window">
                                                        <ItemTemplate>
                                                            <asp:Literal ID="litDayWindow" runat="server"></asp:Literal>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Audit Reason" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOverrideReason" runat="server" CssClass="form-control input-sm audit-box" Placeholder="Type here first..."></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="In-Time" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtInTime" runat="server" TextMode="DateTimeLocal" AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged" Text='<%# Eval("ActualInTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualInTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>' CssClass="form-control input-sm"></asp:TextBox>
                                                            <%# RenderTimeSuggestion("IN", Eval("ActualInTime"), Eval("CreatedDate"), Eval("ApprovedOn"), Eval("ActualInTime")) %>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Out-Time" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOutTime" runat="server" TextMode="DateTimeLocal" AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged" Text='<%# Eval("ActualOutTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualOutTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>' CssClass="form-control input-sm"></asp:TextBox>
                                                            <%# RenderTimeSuggestion("OUT", Eval("ActualOutTime"), Eval("CreatedDate"), Eval("ApprovedOn"), Eval("ActualInTime")) %>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Valid Upto" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtValidUpto" runat="server" TextMode="DateTimeLocal" AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged" Text='<%# Eval("ValidUpto") != DBNull.Value ? Convert.ToDateTime(Eval("ValidUpto")).ToString("yyyy-MM-ddTHH:mm") : "" %>' CssClass="form-control input-sm"></asp:TextBox>
                                                            <%# RenderTimeSuggestion("VALID", Eval("ValidUpto"), Eval("CreatedDate"), Eval("ApprovedOn"), Eval("ActualInTime")) %>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </div>
                                    </div>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="emailLogsModal" tabindex="-1" role="dialog" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header" style="background: #34495E; color: white;">
                    <button type="button" class="close" data-dismiss="modal" style="color: white;">&times;</button>
                    <h4 class="modal-title"><i class="fa fa-envelope"></i> Email Delivery Logs</h4>
                </div>
                <div class="modal-body" style="background: #f4f6f9;">
                    <asp:UpdatePanel ID="upEmailLogs" runat="server">
                        <ContentTemplate>
                            <asp:Repeater ID="rptEmailLogs" runat="server">
                                <HeaderTemplate><ul class="list-group"></HeaderTemplate>
                                <ItemTemplate>
                                    <li class="list-group-item" style="margin-bottom: 10px; border-radius: 5px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                                        <div class="row">
                                            <div class="col-md-7">
                                                <strong>To:</strong> <span class="text-primary"><%# Eval("RecipientName") %></span> (<%# Eval("RecipientEmail") %>)<br />
                                                <strong>Subject:</strong> <%# Eval("Subject") %>
                                            </div>
                                            <div class="col-md-5 text-right">
                                                <span class='label <%# Eval("Status").ToString() == "Sent" || Eval("Status").ToString() == "Processed" ? "label-success" : (Eval("Status").ToString() == "Failed" ? "label-danger" : "label-warning") %>'>
                                                    <%# Eval("Status") %>
                                                </span>
                                                <div style="margin-top: 8px; font-size: 11px; color: #73879C; text-align: right; line-height: 1.5;">
                                                    <div><i class="fa fa-clock-o"></i> Queued: <b><%# Eval("CreatedOn", "{0:dd MMM yyyy, hh:mm tt}") %></b></div>
                                                    <div><i class="fa fa-paper-plane"></i> Sent: <b><%# Eval("SentOn") != DBNull.Value ? Eval("SentOn", "{0:dd MMM yyyy, hh:mm tt}") : "<span style='color:#f0ad4e;'>Pending Dispatch...</span>" %></b></div>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="email-body-preview">
                                            <%# Eval("Body") %>
                                        </div>
                                    </li>
                                </ItemTemplate>
                                <FooterTemplate></ul></FooterTemplate>
                            </asp:Repeater>
                            <asp:Label ID="lblNoEmails" runat="server" Visible="false" CssClass="alert alert-warning display-block text-center"><i class="fa fa-info-circle"></i> No emails found for this visit.</asp:Label>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function applySmartTime(element, isoValue) {
            var container = element.closest('td');
            var inputBox = container.querySelector('input[type="datetime-local"]');

            if (inputBox) {
                inputBox.value = isoValue;
                inputBox.style.backgroundColor = "#e0f7fa";
                setTimeout(function () { inputBox.style.backgroundColor = ""; }, 1000);

                setTimeout(function () {
                    if (typeof __doPostBack === 'function') {
                        __doPostBack(inputBox.name, '');
                    } else {
                        inputBox.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                }, 100);
            }
        }
    </script>
</asp:Content>