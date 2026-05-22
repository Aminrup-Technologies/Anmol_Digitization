<%@ Page Title="VMS | Admin Pending" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_AdminPending.aspx.cs" MaintainScrollPositionOnPostback="true" Inherits="AnmolDristi.VMS_AdminPending" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Typography & Layout Enhancements */
        .grid-timeline small {
            display: block;
            line-height: 1.4;
        }

        .grid-host small {
            color: #73879C;
            font-size: 11px;
            line-height: 1.3;
            display: block;
        }

        .audit-box {
            border: 1px solid #1ABB9C !important;
            background-color: #fcfcfc;
        }

        .visitor-photo {
            width: 45px;
            height: 45px;
            border-radius: 50%;
            margin-right: 12px;
            border: 1px solid #ddd;
            object-fit: cover;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-shield"></i>Central Visitor Administration</h2>
                        <div class="clearfix"></div>
                    </div>

                    <div class="x_content">
                        <asp:UpdatePanel ID="upAdminGrid" runat="server">
                            <ContentTemplate>
                                <asp:Label ID="lblMessage" runat="server" Font-Bold="true" Style="display: block; margin-bottom: 15px;"></asp:Label>

                                <ul class="nav nav-tabs" id="adminTabs" role="tablist">
                                    <li class="active"><a data-toggle="tab" href="#tabReminders" id="reminders-tab" role="tab"><i class="fa fa-bell"></i>Send Reminders</a></li>
                                    <li><a data-toggle="tab" href="#tabOnBehalf" id="onbehalf-tab" role="tab"><i class="fa fa-check-square-o"></i>On-Behalf Approvals</a></li>
                                    <li><a data-toggle="tab" href="#tabGateOverride" id="override-tab" role="tab"><i class="fa fa-edit"></i>Inline Gate Override</a></li>
                                </ul>

                                <div class="tab-content" style="padding-top: 20px;">

                                    <div id="tabReminders" class="tab-pane fade in active" role="tabpanel">
                                        <div class="table-responsive">
                                            <asp:GridView ID="gvAdminPending" runat="server" AutoGenerateColumns="False"
                                                CssClass="table table-striped table-bordered table-hover"
                                                OnRowCommand="gvAdminPending_RowCommand"
                                                EmptyDataText="No pending visitors at this time."
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    <asp:BoundField DataField="LogID" HeaderText="Log ID" ItemStyle-Width="60px" />

                                                    <asp:TemplateField HeaderText="Visitor Details">
                                                        <ItemTemplate>
                                                            <div style="display: flex; align-items: center;">
                                                                <img src='<%# Eval("PhotoPath") != DBNull.Value && !string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? ResolveUrl(Eval("PhotoPath").ToString()) : ResolveUrl("~/Images/default-avatar.png") %>' class="visitor-photo" />
                                                                <div>
                                                                    <strong style="color: #333; font-size: 13px;"><%# Eval("VisitorName") %></strong><br />
                                                                    <small class="text-muted"><%# Eval("VisitorEmail") %></small>
                                                                </div>
                                                            </div>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:BoundField DataField="Branch_Description" HeaderText="Branch / Plant" />

                                                    <asp:TemplateField HeaderText="Host Details" ItemStyle-CssClass="grid-host">
                                                        <ItemTemplate>
                                                            <strong style="color: #555;"><%# Eval("HostName") %></strong>
                                                            <small><i class="fa fa-id-badge"></i>Code: <%# Eval("HostEmployeeCode") %></small>
                                                            <small><i class="fa fa-envelope"></i><%# Eval("HostEmail") %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:BoundField DataField="CreatedDate" HeaderText="Waiting Since" DataFormatString="{0:dd MMM yyyy, hh:mm tt}" />

                                                    <asp:TemplateField HeaderText="Actions" ItemStyle-Width="150px" ItemStyle-CssClass="text-center">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="btnRetrigger" runat="server" CommandName="Retrigger"
                                                                CommandArgument='<%# Eval("LogID") %>' CssClass="btn btn-warning btn-sm"
                                                                OnClientClick="return confirm('Send high-priority reminder?');">
                                                                <i class="fa fa-bell"></i> Remind Host
                                                            </asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </div>
                                    </div>

                                    <div id="tabOnBehalf" class="tab-pane fade" role="tabpanel">
                                        <div class="alert alert-info">
                                            <strong><i class="fa fa-info-circle"></i>Note:</strong> Approving a pass here will bypass the Host and instantly send a digital pass to the visitor.
                                        </div>
                                        <div class="table-responsive">
                                            <asp:GridView ID="gvOnBehalf" runat="server" AutoGenerateColumns="False"
                                                CssClass="table table-striped table-bordered table-hover"
                                                DataKeyNames="LogID,VisitorEmail,VisitorName,HostEmail,HostName"
                                                OnRowCommand="gvOnBehalf_RowCommand"
                                                EmptyDataText="No pending visitors at this time."
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    <asp:BoundField DataField="LogID" HeaderText="Log ID" ItemStyle-Width="60px" />

                                                    <asp:TemplateField HeaderText="Visitor Details">
                                                        <ItemTemplate>
                                                            <div style="display: flex; align-items: center;">
                                                                <img src='<%# Eval("PhotoPath") != DBNull.Value && !string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? ResolveUrl(Eval("PhotoPath").ToString()) : ResolveUrl("~/Images/default-avatar.png") %>' class="visitor-photo" />
                                                                <div>
                                                                    <strong style="color: #333; font-size: 13px;"><%# Eval("VisitorName") %></strong><br />
                                                                    <small class="text-muted"><%# Eval("VisitorEmail") %></small><br />
                                                                    <small class="text-muted"><%# Eval("CreatedDate") %></small>
                                                                </div>
                                                            </div>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:BoundField DataField="Branch_Description" HeaderText="Branch / Plant" />

                                                    <asp:TemplateField HeaderText="Host Details" ItemStyle-CssClass="grid-host">
                                                        <ItemTemplate>
                                                            <strong style="color: #555;"><%# Eval("HostName") %></strong>
                                                            <small><i class="fa fa-id-badge"></i>Code: <%# Eval("HostEmployeeCode") %></small>
                                                            <small><i class="fa fa-envelope"></i><%# Eval("HostEmail") %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Schedule Date & Time" ItemStyle-Width="200px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtScheduleDate" runat="server" TextMode="DateTimeLocal" CssClass="form-control input-sm"></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Admin Remarks (Required)" ItemStyle-Width="220px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control input-sm" Placeholder="Reason for bypass..."></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Action" ItemStyle-CssClass="text-center" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="btnForceApprove" runat="server" CommandName="ForceApprove"
                                                                CommandArgument='<%# Container.DataItemIndex %>' CssClass="btn btn-success btn-sm"
                                                                OnClientClick="return confirm('WARNING: Are you sure you want to approve this visitor ON BEHALF of the host?');">
                                                                <i class="fa fa-check"></i> Approve
                                                            </asp:LinkButton>
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
                                                DataKeyNames="LogID,VisitorName"
                                                EmptyDataText="No active visitors found."
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    <asp:BoundField DataField="LogID" HeaderText="ID" ItemStyle-Width="50px" ItemStyle-Font-Bold="true" />

                                                    <asp:TemplateField HeaderText="Visitor Details" ItemStyle-Width="230px">
                                                        <ItemTemplate>
                                                            <div style="display: flex; align-items: center;">
                                                                <img src='<%# Eval("PhotoPath") != DBNull.Value && !string.IsNullOrEmpty(Eval("PhotoPath").ToString()) ? ResolveUrl(Eval("PhotoPath").ToString()) : ResolveUrl("~/Images/default-avatar.png") %>' class="visitor-photo" style="width: 40px; height: 40px;" />
                                                                <div>
                                                                    <strong style="color: #1ABB9C; font-size: 14px;"><%# Eval("VisitorName") %></strong><br />
                                                                    <span class='<%# Eval("VisitStatus").ToString() == "Scheduled" ? "label label-warning" : "label label-success" %>'>
                                                                        <%# Eval("VisitStatus") %>
                                                                    </span>
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

                                                    <asp:TemplateField HeaderText="Host" ItemStyle-CssClass="grid-host">
                                                        <ItemTemplate>
                                                            <strong style="color: #555;"><%# Eval("HostName") %></strong>
                                                            <small><i class="fa fa-id-badge"></i>Code: <%# Eval("HostEmployeeCode") %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Audit Reason" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOverrideReason" runat="server" CssClass="form-control input-sm audit-box" Placeholder="Type here first..."></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="In-Time" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtInTime" runat="server" TextMode="DateTimeLocal" AutoPostBack="true"
                                                                OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ActualInTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualInTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>'
                                                                CssClass="form-control input-sm"></asp:TextBox>

                                                            <%-- Smart Suggestion --%>
                                                            <%# RenderTimeSuggestion("IN", Eval("ActualInTime"), Eval("CreatedDate"), Eval("ApprovedOn"), Eval("ActualInTime")) %>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Out-Time" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOutTime" runat="server" TextMode="DateTimeLocal" AutoPostBack="true"
                                                                OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ActualOutTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualOutTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>'
                                                                CssClass="form-control input-sm"></asp:TextBox>

                                                            <%-- Smart Suggestion --%>
                                                            <%# RenderTimeSuggestion("OUT", Eval("ActualOutTime"), Eval("CreatedDate"), Eval("ApprovedOn"), Eval("ActualInTime")) %>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Valid Upto" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtValidUpto" runat="server" TextMode="DateTimeLocal" AutoPostBack="true"
                                                                OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ValidUpto") != DBNull.Value ? Convert.ToDateTime(Eval("ValidUpto")).ToString("yyyy-MM-ddTHH:mm") : "" %>'
                                                                CssClass="form-control input-sm"></asp:TextBox>

                                                            <%-- Smart Suggestion --%>
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
</asp:Content>
