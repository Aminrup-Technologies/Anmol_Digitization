<%@ Page Title="VMS | Admin Pending" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_AdminPending.aspx.cs" MaintainScrollPositionOnPostback="true" Inherits="AnmolDristi.VMS_AdminPending" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
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
                                    <li class="active">
                                        <a data-toggle="tab" href="#tabReminders" id="reminders-tab" role="tab"><i class="fa fa-bell"></i>Send Reminders</a>
                                    </li>
                                    <li>
                                        <a data-toggle="tab" href="#tabOnBehalf" id="onbehalf-tab" role="tab"><i class="fa fa-check-square-o"></i>On-Behalf Approvals</a>
                                    </li>
                                    <li>
                                        <a data-toggle="tab" href="#tabGateOverride" id="override-tab" role="tab"><i class="fa fa-edit"></i>Inline Gate Override</a>
                                    </li>
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
                                                    <asp:BoundField DataField="VisitorName" HeaderText="Visitor Name" />
                                                    <asp:BoundField DataField="VisitorCompany" HeaderText="Company" />
                                                    <asp:BoundField DataField="HostName" HeaderText="Host Employee" />
                                                    <asp:BoundField DataField="LocationName" HeaderText="Branch / Plant" />
                                                    <asp:BoundField DataField="CreatedDate" HeaderText="Waiting Since" DataFormatString="{0:dd MMM yyyy, hh:mm tt}" />

                                                    <asp:TemplateField HeaderText="Actions" ItemStyle-Width="150px" ItemStyle-CssClass="text-center">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="btnRetrigger" runat="server"
                                                                CommandName="Retrigger"
                                                                CommandArgument='<%# Eval("LogID") %>'
                                                                CssClass="btn btn-warning btn-sm"
                                                                OnClientClick="return confirm('Send a high-priority reminder email to the host?');">
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
                                                    <asp:BoundField DataField="VisitorName" HeaderText="Visitor Name" />
                                                    <asp:BoundField DataField="HostName" HeaderText="Host Employee" />
                                                    <asp:BoundField DataField="LocationName" HeaderText="Branch / Plant" />

                                                    <asp:TemplateField HeaderText="Admin Remarks (Required)" ItemStyle-Width="250px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control input-sm" Placeholder="Reason for override..." MaxLength="150"></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Action" ItemStyle-Width="180px" ItemStyle-CssClass="text-center">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="btnForceApprove" runat="server"
                                                                CommandName="ForceApprove"
                                                                CommandArgument='<%# Container.DataItemIndex %>'
                                                                CssClass="btn btn-success btn-sm"
                                                                OnClientClick="return confirm('Are you sure you want to approve this visitor ON BEHALF of the host?');">
                                                                <i class="fa fa-check"></i> Approve On-Behalf
                                                            </asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </div>
                                    </div>

                                    <div id="tabGateOverride" class="tab-pane fade" role="tabpanel">
                                        <div class="alert alert-danger">
                                            <strong><i class="fa fa-bolt"></i>Instant Auto-Save Active:</strong>
                                            Type your reason FIRST. Changing any Date/Time field will instantly auto-save and update the database.
                                       
                                        </div>

                                        <div class="table-responsive">
                                            <asp:GridView ID="gvAdminGateOverride" runat="server" AutoGenerateColumns="False"
                                                CssClass="table table-striped table-bordered table-hover"
                                                DataKeyNames="LogID,VisitorName"
                                                EmptyDataText="No approved/active visitors available."
                                                EmptyDataRowStyle-CssClass="alert alert-success text-center">
                                                <Columns>
                                                    <asp:BoundField DataField="LogID" HeaderText="Log ID" ItemStyle-Width="50px" ItemStyle-Font-Bold="true" />

                                                    <asp:TemplateField HeaderText="Visitor & Timeline" ItemStyle-Width="220px">
                                                        <ItemTemplate>
                                                            <strong style="color: #1ABB9C; font-size: 14px;"><%# Eval("VisitorName") %></strong>
                                                            <span class='<%# Eval("VisitStatus").ToString() == "Scheduled" ? "label label-warning pull-right" : "label label-success pull-right" %>'>
                                                                <%# Eval("VisitStatus") %>
                                                            </span>
                                                            <br />

                                                            <div style="font-size: 11px; color: #555; margin-top: 8px; line-height: 1.5; background: #f9f9f9; padding: 5px; border-radius: 4px; border: 1px solid #eee;">
                                                                <i class="fa fa-clock-o" style="color: #73879C;"></i><b>Requested:</b> <%# Eval("CreatedDate", "{0:dd MMM yyyy, HH:mm}") %><br />
                                                                <i class="fa fa-check-square-o" style="color: #1ABB9C;"></i><b>Approved:</b> <%# Eval("ApprovedOn") != DBNull.Value ? Eval("ApprovedOn", "{0:dd MMM yyyy, HH:mm}") : "N/A" %>
                                                            </div>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Audit Reason (Type First!)" ItemStyle-Width="150px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOverrideReason" runat="server" CssClass="form-control input-sm" Placeholder="Required for changes..."></asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="In-Time (Editable)" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtInTime" runat="server" TextMode="DateTimeLocal" CssClass="form-control input-sm"
                                                                AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ActualInTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualInTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>'>
                                                            </asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Out-Time (Editable)" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtOutTime" runat="server" TextMode="DateTimeLocal" CssClass="form-control input-sm"
                                                                AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ActualOutTime") != DBNull.Value ? Convert.ToDateTime(Eval("ActualOutTime")).ToString("yyyy-MM-ddTHH:mm") : "" %>'>
                                                            </asp:TextBox>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Valid Upto (Editable)" ItemStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:TextBox ID="txtValidUpto" runat="server" TextMode="DateTimeLocal" CssClass="form-control input-sm"
                                                                AutoPostBack="true" OnTextChanged="InlineEdit_TextChanged"
                                                                Text='<%# Eval("ValidUpto") != DBNull.Value ? Convert.ToDateTime(Eval("ValidUpto")).ToString("yyyy-MM-ddTHH:mm") : "" %>'>
                                                            </asp:TextBox>
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
