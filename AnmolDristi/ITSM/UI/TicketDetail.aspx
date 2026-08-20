<%-- ========================================================================
     File:    TicketDetail.aspx
     Layer:   UI — Presentation
     When:    2026-08-20 / 2026-08-20 (audit patch)
     What:    Ticket processing surface — status transitions, reassignment,
              priority override, approval, comments, and audit history.
     Why:     Phase 1B — the agent-facing detail view. All mutations are
              delegated to TicketUpdateService; code-behind contains only
              session validation, control reads, and BLL invocation (requirement §15).
     Patch:   Added tenant badge (§23) showing ticket's CompanyId at top of
              primary CSS Grid layout. Replaced rigid layout with CSS Grid/Flexbox
              (§24). Session gate now validates USERID + BranchID only (§5-6).
              CompanyId comes from loaded ticket, not session (§7).
     ======================================================================== --%>
<%@ Page Title="ITSM | Ticket Detail" Language="C#" MasterPageFile="~/Dristi.Master"
    AutoEventWireup="true" CodeBehind="TicketDetail.aspx.cs"
    Inherits="AnmolDristi.ITSM.UI.TicketDetail" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* --- Primary CSS Grid layout (requirement §24) --- */
        .ticket-detail-grid {
            display: grid;
            grid-template-columns: 1fr;
            gap: 16px;
        }
        /* --- Tenant badge (requirement §23) --- */
        .tenant-badge-bar {
            display: flex;
            align-items: center;
            gap: 10px;
            background: #e8f4fd;
            border: 1px solid #b6d4fe;
            border-left: 4px solid #0d6efd;
            border-radius: 4px;
            padding: 8px 14px;
            font-size: 13px;
            font-weight: 600;
            color: #084298;
        }
        .tenant-badge-bar .tenant-label {
            text-transform: uppercase;
            font-size: 11px;
            letter-spacing: 0.5px;
            color: #6c757d;
            font-weight: 700;
        }
        .tenant-badge-bar .tenant-value {
            font-size: 14px;
            font-weight: 700;
            color: #084298;
        }
        /* --- Header row --- */
        .ticket-detail-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 10px;
        }
        .ticket-number {
            font-size: 18px;
            font-weight: 700;
            color: #333;
        }
        /* --- Metadata grid (CSS Grid, requirement §24) --- */
        .ticket-meta {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 12px;
        }
        .meta-card {
            background: #f8f9fa;
            border-radius: 4px;
            padding: 10px 14px;
            border-left: 3px solid #007bff;
        }
        .meta-card .meta-label {
            font-size: 11px;
            text-transform: uppercase;
            color: #666;
            font-weight: 600;
            margin-bottom: 2px;
        }
        .meta-card .meta-value {
            font-size: 14px;
            font-weight: 600;
            color: #333;
        }
        /* --- Action panel (CSS Flexbox) --- */
        .action-panel {
            background: #f0f4ff;
            border: 1px solid #d0d7e8;
            border-radius: 4px;
            padding: 15px;
        }
        .action-panel h4 {
            margin-top: 0;
            font-size: 15px;
        }
        .action-row {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            align-items: flex-end;
        }
        /* --- Comments, approvals, audit --- */
        .section-title {
            font-size: 14px;
            font-weight: 600;
            color: #495057;
            margin: 15px 0 8px 0;
            border-bottom: 1px solid #dee2e6;
            padding-bottom: 5px;
        }
        .audit-table {
            font-size: 12px;
        }
        .audit-table td {
            vertical-align: middle !important;
        }
        .comment-box {
            margin-top: 10px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="ticket-detail-grid">

            <!-- Tenant Badge (requirement §23) — populated from ticket data, read-only -->
            <asp:Panel ID="pnlTenantBadge" runat="server" Visible="false">
                <div class="tenant-badge-bar">
                    <span class="tenant-label">Tenant:</span>
                    <span class="tenant-value">
                        <asp:Label ID="lblTenantBadge" runat="server" />
                    </span>
                </div>
            </asp:Panel>

            <!-- Header Row (CSS Flexbox) -->
            <div class="x_panel" style="margin-bottom: 0;">
                <div class="x_title">
                    <div class="ticket-detail-header">
                        <div>
                            <a href="TicketList.aspx" class="btn btn-default btn-sm">
                                <i class="fa fa-arrow-left"></i> Back to Queue
                            </a>
                            <span class="ticket-number" style="margin-left: 10px;">
                                <asp:Label ID="lblTicketNumber" runat="server" />
                            </span>
                            <asp:Label ID="lblStatusBadge" runat="server" CssClass="badge-status"
                                Style="margin-left: 8px; font-size: 12px;" />
                        </div>
                        <div>
                            <asp:Label ID="lblPriorityBadge" runat="server" CssClass="badge-priority" />
                        </div>
                    </div>
                    <div class="clearfix"></div>
                </div>
                <div class="x_content">
                    <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                        Style="display: block; margin-bottom: 10px;"></asp:Label>

                    <!-- Not Found -->
                    <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
                        <div class="alert alert-warning">Ticket not found or you do not have access.</div>
                    </asp:Panel>

                    <!-- Ticket Content -->
                    <asp:Panel ID="pnlTicketContent" runat="server" Visible="false">

                        <!-- Metadata Grid (CSS Grid, requirement §24) -->
                        <div class="ticket-meta">
                            <div class="meta-card">
                                <div class="meta-label">Title</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblTitle" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Request Type</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblRequestType" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Requester</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblRequester" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Assignee</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblAssignee" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Category</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblCategory" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Branch</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblBranch" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Impact / Urgency</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblImpactUrgency" runat="server" />
                                </div>
                            </div>
                            <div class="meta-card">
                                <div class="meta-label">Created</div>
                                <div class="meta-value">
                                    <asp:Label ID="lblCreatedOn" runat="server" />
                                </div>
                            </div>
                        </div>

                        <!-- Description -->
                        <div class="section-title">Description</div>
                        <div style="background: #f8f9fa; padding: 12px; border-radius: 4px; margin-bottom: 15px;">
                            <asp:Label ID="lblDescription" runat="server" />
                        </div>

                        <!-- Action Panel — Status Change -->
                        <asp:Panel ID="pnlActions" runat="server" Visible="true">
                            <div class="action-panel">
                                <h4><i class="fa fa-cog"></i> Actions</h4>
                                <div class="action-row">
                                    <div class="form-group">
                                        <label>Status</label>
                                        <asp:DropDownList ID="ddlTargetStatus" runat="server"
                                            CssClass="form-control input-sm"
                                            AppendDataBoundItems="true" Width="180px">
                                            <asp:ListItem Text="-- Select Action --" Value="" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="form-group" id="divResolution" runat="server" visible="false">
                                        <label>Resolution Notes <span class="text-danger">*</span></label>
                                        <asp:TextBox ID="txtResolution" runat="server" CssClass="form-control input-sm"
                                            TextMode="MultiLine" Rows="3" Width="350px"
                                            placeholder="Describe how the issue was resolved" />
                                    </div>
                                    <div class="form-group" style="padding-top: 22px;">
                                        <asp:Button ID="btnChangeStatus" runat="server" Text="Apply"
                                            CssClass="btn btn-primary btn-sm"
                                            OnClick="btnChangeStatus_Click" />
                                    </div>
                                </div>

                                <!-- Reassignment -->
                                <div class="action-row" style="margin-top: 10px;">
                                    <div class="form-group">
                                        <label>Reassign To</label>
                                        <asp:DropDownList ID="ddlReassignTo" runat="server"
                                            CssClass="form-control input-sm"
                                            AppendDataBoundItems="true" Width="220px">
                                            <asp:ListItem Text="-- Select Agent --" Value="" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="form-group" style="padding-top: 22px;">
                                        <asp:Button ID="btnReassign" runat="server" Text="Reassign"
                                            CssClass="btn btn-warning btn-sm"
                                            OnClick="btnReassign_Click" />
                                    </div>
                                </div>

                                <!-- Priority Override -->
                                <div class="action-row" style="margin-top: 10px;">
                                    <div class="form-group">
                                        <label>Override Priority</label>
                                        <asp:DropDownList ID="ddlOverridePriority" runat="server"
                                            CssClass="form-control input-sm"
                                            AppendDataBoundItems="true" Width="180px">
                                            <asp:ListItem Text="-- Select Priority --" Value="" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="form-group" style="padding-top: 22px;">
                                        <asp:Button ID="btnOverridePriority" runat="server" Text="Override"
                                            CssClass="btn btn-info btn-sm"
                                            OnClick="btnOverridePriority_Click" />
                                    </div>
                                </div>

                                <!-- Approval Actions -->
                                <asp:Panel ID="pnlApprovalActions" runat="server" Visible="false"
                                    style="margin-top: 10px; padding-top: 10px; border-top: 1px dashed #ccc;">
                                    <div class="action-row">
                                        <div class="form-group">
                                            <asp:Button ID="btnApprove" runat="server" Text="Approve"
                                                CssClass="btn btn-success btn-sm"
                                                OnClick="btnApprove_Click" />
                                        </div>
                                        <div class="form-group">
                                            <label>Rejection Reason <span class="text-danger">*</span></label>
                                            <asp:TextBox ID="txtRejectionReason" runat="server"
                                                CssClass="form-control input-sm" Width="300px"
                                                placeholder="Mandatory for rejection" />
                                        </div>
                                        <div class="form-group" style="padding-top: 22px;">
                                            <asp:Button ID="btnReject" runat="server" Text="Reject"
                                                CssClass="btn btn-danger btn-sm"
                                                OnClick="btnReject_Click" />
                                        </div>
                                    </div>
                                </asp:Panel>
                            </div>
                        </asp:Panel>

                        <!-- Comment Section -->
                        <div class="section-title">Add Comment</div>
                        <div class="comment-box">
                            <div class="row">
                                <div class="col-md-8">
                                    <asp:TextBox ID="txtComment" runat="server" CssClass="form-control"
                                        TextMode="MultiLine" Rows="3"
                                        placeholder="Add a comment..." MaxLength="2000" />
                                </div>
                                <div class="col-md-4" style="padding-top: 5px;">
                                    <asp:CheckBox ID="chkInternal" runat="server" Text="Internal note" />
                                    <br />
                                    <asp:Button ID="btnAddComment" runat="server" Text="Add Comment"
                                        CssClass="btn btn-primary btn-sm" style="margin-top: 5px;"
                                        OnClick="btnAddComment_Click" />
                                </div>
                            </div>
                        </div>

                        <!-- Comments List -->
                        <asp:Repeater ID="rptComments" runat="server">
                            <HeaderTemplate>
                                <div class="section-title">Comments</div>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="background: #f8f9fa; border-left: 3px solid
                                    <%# Eval("IsInternal").ToString() == "True" ? "#ffc107" : "#007bff" %>;
                                    padding: 10px; margin-bottom: 8px; border-radius: 2px;">
                                    <div style="font-size: 12px; color: #666;">
                                        <strong><%# Eval("CreatedByName") %></strong>
                                        on <%# Eval("CreatedOn", "{0:dd-MMM-yyyy HH:mm}") %>
                                        <%# Eval("IsInternal").ToString() == "True"
                                            ? " <span style='color:#856404;'>(Internal)</span>" : "" %>
                                    </div>
                                    <div style="margin-top: 4px; font-size: 13px;">
                                        <%# Eval("CommentText") %>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <!-- Approvals -->
                        <asp:Repeater ID="rptApprovals" runat="server">
                            <HeaderTemplate>
                                <div class="section-title">Approval History</div>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div style="background: #f8f9fa; padding: 10px; margin-bottom: 8px; border-radius: 2px;">
                                    <div style="font-size: 12px; color: #666;">
                                        <strong><%# Eval("ApprovalLevel") %></strong> —
                                        <%# Eval("ApproverName") %> —
                                        <span class="label label-<%# Eval("ApprovalStatus").ToString() == "Approved"
                                            ? "success" : Eval("ApprovalStatus").ToString() == "Rejected"
                                            ? "danger" : "warning" %>">
                                            <%# Eval("ApprovalStatus") %>
                                        </span>
                                        <%# Eval("ActionedOn") != null
                                            ? " on " + Eval("ActionedOn", "{0:dd-MMM-yyyy HH:mm}")
                                            : "" %>
                                    </div>
                                    <%# !string.IsNullOrEmpty(Eval("Comments") as string)
                                        ? "<div style='margin-top:4px;font-size:12px;color:#555;'>"
                                          + Eval("Comments") + "</div>" : "" %>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <!-- Audit History (GridView — semantic tabular data, §24) -->
                        <div class="section-title">Audit Trail</div>
                        <asp:GridView ID="gvAudit" runat="server"
                            CssClass="table table-bordered table-sm audit-table"
                            AutoGenerateColumns="false"
                            EmptyDataText="No audit records.">
                            <Columns>
                                <asp:BoundField DataField="ActionedOn" HeaderText="When"
                                    DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                                <asp:BoundField DataField="ActionType" HeaderText="Action" />
                                <asp:BoundField DataField="ActorName" HeaderText="Actor" />
                                <asp:BoundField DataField="OldValue" HeaderText="From" />
                                <asp:BoundField DataField="NewValue" HeaderText="To" />
                                <asp:BoundField DataField="Detail" HeaderText="Detail" />
                            </Columns>
                        </asp:GridView>

                        <!-- CSAT (only for Closed/Resolved) -->
                        <asp:Panel ID="pnlCsat" runat="server" Visible="false">
                            <div class="section-title">Customer Satisfaction</div>
                            <div style="background: #f8f9fa; padding: 12px; border-radius: 4px;">
                                <div style="display: grid; grid-template-columns: 120px 1fr; gap: 12px;">
                                    <div>
                                        <label>Score</label>
                                        <asp:Label ID="lblCsatScore" runat="server" CssClass="label label-info"
                                            Style="font-size: 14px;" />
                                    </div>
                                    <div>
                                        <label>Feedback</label>
                                        <div><asp:Label ID="lblCsatFeedback" runat="server" /></div>
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>

                        <!-- Hidden fields for TicketId and RowVersion (lightweight ViewState, §13) -->
                        <asp:HiddenField ID="hfTicketId" runat="server" />
                        <asp:HiddenField ID="hfRowVersion" runat="server" />

                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
