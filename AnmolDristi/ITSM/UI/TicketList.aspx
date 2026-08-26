<%-- ========================================================================
     File:    TicketList.aspx
     Layer:   UI — Presentation
     When:    2026-08-21
     What:    Agent ticket queues bound to TicketListItemDto. Session gate is
              WORKMAN + USERID + BRANCH. Queues are not filtered by actor company.
     Why:     Shared-service agents work tickets across companies. Ticket.CompanyId
              is the tenant; BranchId remains a routing filter only.
     ======================================================================== --%>
<%@ Page Title="ITSM | Ticket Queue" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="TicketList.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.TicketList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .queue-header { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 15px; }
        .queue-filters { display: flex; flex-wrap: wrap; gap: 10px; align-items: flex-end; margin-bottom: 15px; }
        .queue-filters .form-group { margin-bottom: 0; }
        .badge-priority { display: inline-block; padding: 2px 8px; border-radius: 3px; font-size: 11px; font-weight: 600; color: #fff; }
        .badge-p1 { background: #dc3545; }
        .badge-p2 { background: #fd7e14; }
        .badge-p3 { background: #ffc107; color: #333; }
        .badge-p4 { background: #28a745; }
        .badge-status { display: inline-block; padding: 2px 8px; border-radius: 3px; font-size: 11px; font-weight: 600; }
        .badge-queue { background: #17a2b8; color: #fff; }
        .badge-progress { background: #007bff; color: #fff; }
        .badge-resolved { background: #6f42c1; color: #fff; }
        .badge-closed { background: #6c757d; color: #fff; }
        .badge-pending { background: #ffc107; color: #333; }
        .badge-hold { background: #e83e8c; color: #fff; }
        .badge-cancelled { background: #343a40; color: #fff; }
        .ticket-link { color: #007bff; text-decoration: none; font-weight: 600; }
        .ticket-link:hover { text-decoration: underline; }
        .gvQueue { width: 100%; }
        .gvQueue th { background: #f8f9fa; border-bottom: 2px solid #dee2e6; font-size: 13px; white-space: nowrap; }
        .gvQueue td { vertical-align: middle; font-size: 13px; }
        .gvQueue tr:hover { background: #f0f4ff; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <div class="queue-header">
                            <h2><i class="fa fa-list"></i> ITSM Ticket Queue</h2>
                            <div>
                                <asp:Label ID="lblTicketCount" runat="server" CssClass="label label-default"
                                    Style="font-size: 13px;"></asp:Label>
                                <a href="CreateTicket.aspx" class="btn btn-success btn-sm" style="margin-left: 8px;">
                                    <i class="fa fa-plus"></i> New Ticket
                                </a>
                            </div>
                        </div>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>

                        <!-- Queue Tabs
                             MyQueue / AllOpen: visible when ITSM.Queue.View permission is granted.
                             MyTickets: visible to all authenticated employees.
                             Approvals: visible when ITSM.Approval.View permission is granted.
                             Server-side visibility ensures tabs cannot be exposed via URL manipulation.
                             -->
                        <ul class="nav nav-tabs" role="tablist">
                            <li id="tabMyQueue" runat="server" visible="false" role="presentation" class="active">
                                <asp:LinkButton ID="lnkMyQueue" runat="server" OnClick="lnkMyQueue_Click"
                                    Text="My Queue" />
                            </li>
                            <li id="tabAllOpen" runat="server" visible="false" role="presentation">
                                <asp:LinkButton ID="lnkAllOpen" runat="server" OnClick="lnkAllOpen_Click"
                                    Text="All Open" />
                            </li>
                            <li id="tabMyTickets" runat="server" visible="false" role="presentation">
                                <asp:LinkButton ID="lnkMyTickets" runat="server" OnClick="lnkMyTickets_Click"
                                    Text="My Tickets" />
                            </li>
                            <li id="tabApprovals" runat="server" visible="false" role="presentation">
                                <asp:LinkButton ID="lnkApprovals" runat="server" OnClick="lnkApprovals_Click"
                                    Text="Pending Approvals" />
                            </li>
                        </ul>

                        <!-- Server-Side Filters -->
                        <div class="queue-filters" style="margin-top: 15px;">
                            <div class="form-group">
                                <label>Status</label>
                                <asp:DropDownList ID="ddlFilterStatus" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" AutoPostBack="true"
                                    OnSelectedIndexChanged="Filter_Changed">
                                    <asp:ListItem Text="All Statuses" Value="" />
                                </asp:DropDownList>
                            </div>
                            <div class="form-group">
                                <label>Priority</label>
                                <asp:DropDownList ID="ddlFilterPriority" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" AutoPostBack="true"
                                    OnSelectedIndexChanged="Filter_Changed">
                                    <asp:ListItem Text="All Priorities" Value="" />
                                </asp:DropDownList>
                            </div>
                            <div class="form-group">
                                <label>Branch</label>
                                <asp:DropDownList ID="ddlFilterBranch" runat="server" CssClass="form-control input-sm"
                                    AppendDataBoundItems="true" AutoPostBack="true"
                                    OnSelectedIndexChanged="Filter_Changed">
                                    <asp:ListItem Text="All Branches" Value="" />
                                </asp:DropDownList>
                            </div>
                            <div class="form-group">
                                <label>Search</label>
                                <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control input-sm"
                                    placeholder="Ticket #, title..." MaxLength="100" />
                            </div>
                            <div class="form-group" style="padding-top: 22px;">
                                <asp:Button ID="btnSearch" runat="server" Text="Search"
                                    CssClass="btn btn-primary btn-sm"
                                    OnClick="btnSearch_Click" />
                                <asp:Button ID="btnClearFilter" runat="server" Text="Clear"
                                    CssClass="btn btn-default btn-sm" CausesValidation="false"
                                    OnClick="btnClearFilter_Click" />
                            </div>
                        </div>

                        <!-- Ticket Grid -->
                        <asp:GridView ID="gvTickets" runat="server"
                            CssClass="table table-striped table-bordered gvQueue"
                            AutoGenerateColumns="false"
                            EmptyDataText="No tickets found for the selected filters."
                            AllowPaging="true" PageSize="25"
                            OnPageIndexChanging="gvTickets_PageIndexChanging"
                            OnRowDataBound="gvTickets_RowDataBound">
                            <Columns>
                                <asp:TemplateField HeaderText="Ticket #">
                                    <ItemTemplate>
                                        <a href='TicketDetail.aspx?id=<%# Eval("Id") %>' class="ticket-link">
                                            <%# Eval("TicketNumber") %>
                                        </a>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Title" HeaderText="Title"
                                    ItemStyle-CssClass="text-truncate" />
                                <asp:BoundField DataField="RequestTypeName" HeaderText="Type" />
                                <asp:TemplateField HeaderText="Priority">
                                    <ItemTemplate>
                                        <asp:Label ID="lblPriorityBadge" runat="server" CssClass="badge-priority"
                                            Text='<%# Eval("PriorityName") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <asp:Label ID="lblStatusBadge" runat="server" CssClass="badge-status"
                                            Text='<%# Eval("StatusName") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="RequesterName" HeaderText="Requester" />
                                <asp:BoundField DataField="AssigneeName" HeaderText="Assignee" />
                                <asp:BoundField DataField="CategoryName" HeaderText="Category" />
                                <asp:BoundField DataField="BranchName" HeaderText="Branch" />
                                <asp:BoundField DataField="CreatedOn" HeaderText="Created"
                                    DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                            </Columns>
                            <PagerSettings Mode="NumericFirstLast" PageButtonCount="5"
                                FirstPageText="&laquo;" LastPageText="&raquo;" />
                            <PagerStyle CssClass="pagination-sm" />
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
