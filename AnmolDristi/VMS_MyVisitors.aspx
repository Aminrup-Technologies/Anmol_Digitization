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
                                        <asp:GridView ID="gvPending" runat="server" AutoGenerateColumns="False" CssClass="table table-striped jambo_table bulk_action" OnRowCommand="gvPending_RowCommand" DataKeyNames="LogID,VisitorEmail,VisitorMobile,VisitorName">
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
</asp:Content>