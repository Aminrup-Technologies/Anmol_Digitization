<%@ Page Title="AIL | Claims Approval V2" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="ClaimsApproval_v2.aspx.cs" Inherits="AnmolDristi.ClaimsApproval_v2" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://cdn.datatables.net/1.10.20/css/dataTables.bootstrap4.min.css" rel="stylesheet" />
    <link href="https://cdn.datatables.net/responsive/2.2.3/css/responsive.bootstrap4.min.css" rel="stylesheet" />
    <style type="text/css">
        .btn-group .btn i {
            margin-right: 4px;
        }

        .gentilia-table {
            font-size: 0.92rem;
        }

        .gentilia-table-header {
            background: #37474F;
            color: #fff;
            font-weight: 600;
        }

        .gentilia-row {
            background: #ffffff;
        }

        .gentilia-row-alt {
            background: #f8fafb;
        }

        .status-badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 12px;
            font-size: 0.82rem;
            font-weight: 600;
        }

        .status-pending {
            background: #FFF4E5;
            color: #B26A00;
            border: 1px solid #F3D6A0;
        }

        .status-approved {
            background: #E6F4EA;
            color: #1B7A3A;
            border: 1px solid #B9E1C0;
        }

        .status-rejected {
            background: #FDECEA;
            color: #A61B1B;
            border: 1px solid #F7C6C6;
        }

        .dashboard-box {
            min-width: 160px;
            padding: 10px;
            border-radius: 6px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }

        .dashboard-label {
            font-size: 0.75rem;
            text-transform: uppercase;
            font-weight: 700;
            display: block;
        }

        .dashboard-value {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 0;
        }

        .table-warning {
            background-color: #fff3cd !important;
        }

        .table-success {
            background-color: #d4edda !important;
        }

        .table-danger {
            background-color: #f8d7da !important;
        }

        #detailsModal .table-responsive {
            max-height: 60vh;
            overflow-y: auto;
        }

        #detailsModal .table thead th {
            position: sticky;
            top: 0;
            z-index: 1020;
            background-color: #f8f9fa;
            border-bottom: 2px solid #dee2e6;
        }

        #detailsModal .modal-body {
            padding: 16px 18px;
            background: #ffffff;
        }

        #detailsModal .table-responsive {
            max-height: 55vh;
            overflow-y: auto;
            overflow-x: auto;
            margin-bottom: 15px;
            border-bottom: 1px solid #dee2e6;
        }

        #detailsModal .table thead th {
            position: sticky;
            top: 0;
            z-index: 1020;
            background-color: #f8f9fa !important;
            border-bottom: 2px solid #dee2e6;
            box-shadow: 0 2px 2px -1px rgba(0, 0, 0, 0.1);
        }

        #detailsModal .table tfoot td {
            position: sticky;
            bottom: 0;
            z-index: 1020;
            background-color: #f8f9fa !important;
            border-top: 2px solid #dee2e6 !important;
            box-shadow: 0 -2px 4px -1px rgba(0, 0, 0, 0.08);
        }

        #detailsModal .table tfoot tr:hover td {
            background-color: #f8f9fa !important;
        }

        @keyframes pulse-red {
            0% {
                box-shadow: 0 0 0 0 rgba(220, 53, 69, 0.75);
            }

            70% {
                box-shadow: 0 0 0 8px rgba(220, 53, 69, 0);
            }

            100% {
                box-shadow: 0 0 0 0 rgba(220, 53, 69, 0);
            }
        }

        .pulse-warning {
            animation: pulse-red 2s infinite !important;
            font-weight: bold !important;
            border: 2px solid #dc3545 !important;
        }

        /* Gatekeeper Warning Animations */
        @keyframes pulse-red {
            0% {
                box-shadow: 0 0 0 0 rgba(220, 53, 69, 0.75);
            }

            70% {
                box-shadow: 0 0 0 8px rgba(220, 53, 69, 0);
            }

            100% {
                box-shadow: 0 0 0 0 rgba(220, 53, 69, 0);
            }
        }

        .pulse-warning {
            animation: pulse-red 2s infinite !important;
            font-weight: bold !important;
            border: 2px solid #dc3545 !important;
        }

        /* NEW: Blue Pulse for "Open" Folder */
        @keyframes pulse-blue {
            0% {
                box-shadow: 0 0 0 0 rgba(23, 162, 184, 0.75);
            }

            70% {
                box-shadow: 0 0 0 8px rgba(23, 162, 184, 0);
            }

            100% {
                box-shadow: 0 0 0 0 rgba(23, 162, 184, 0);
            }
        }

        .pulse-info {
            animation: pulse-blue 2s infinite !important;
            font-weight: bold !important;
            border: 1px solid #17a2b8 !important;
        }

        /* NEW: Green Pulse for "Approve" Checkmark */
        @keyframes pulse-green {
            0% {
                box-shadow: 0 0 0 0 rgba(40, 167, 69, 0.75);
            }

            70% {
                box-shadow: 0 0 0 8px rgba(40, 167, 69, 0);
            }

            100% {
                box-shadow: 0 0 0 0 rgba(40, 167, 69, 0);
            }
        }

        .pulse-success {
            animation: pulse-green 2s infinite !important;
            font-weight: bold !important;
            border: 1px solid #28a745 !important;
        }

        /* =========================================================
           MOBILE & UI BUG FIXES
        ========================================================= */

        /* 1. Prevent TextBoxes from squishing into multiple lines */
        #detailsModal .table-responsive input[type="text"] {
            min-width: 140px;
        }

        /* 2. Prevent Action Buttons from wrapping awkwardly */
        #detailsModal .table-responsive td:last-child,
        #detailsModal .table-responsive td:nth-last-child(2) {
            white-space: nowrap;
            min-width: 90px;
        }

        /* 3. Kill the "Ghost Checkbox" (Empty Footer Cell border artifact) */
        #gvDetails tfoot td:first-child {
            border: none !important;
            background: transparent !important;
        }

        /* 4. Make Dashboard Boxes flexible */
        .dashboard-container {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            width: 100%;
        }

        .dashboard-box {
            flex: 1 1 30%; /* Grow to fill space, but min 30% width */
            min-width: 115px;
            padding: 10px;
        }

        /* 5. Mobile Specific Media Query (< 768px) */
        @media (max-width: 768px) {
            /* Stack the modal footer vertically */
            #detailsModal .modal-footer {
                flex-direction: column;
                align-items: stretch !important;
            }

            /* Center the error messages */
            #lblModalMessage {
                text-align: center;
                display: block;
                margin-top: 15px;
                margin-bottom: 10px !important;
            }

            /* Make the action buttons wrap nicely and fill the screen */
            .modal-action-buttons {
                display: flex;
                flex-wrap: wrap;
                gap: 8px;
                justify-content: center;
            }

                .modal-action-buttons .btn {
                    margin: 0 !important;
                    flex: 1 1 45%; /* Buttons take half width */
                }

            /* Make the Inner Unified Modal buttons full width */
            #unifiedDayModal .modal-footer {
                flex-direction: column;
            }

                #unifiedDayModal .modal-footer .btn {
                    width: 100%;
                    margin-bottom: 8px;
                }
        }

        /* =========================================================
           INNER GRID MOBILE SCROLLING FIX
            ========================================================= */

        /* 1. Force the table to stretch and trigger horizontal scroll */
        #detailsModal .table-responsive .table th,
        #detailsModal .table-responsive .table td {
            white-space: nowrap;
            vertical-align: middle;
        }

        /* 2. Give the headers a little breathing room */
        #detailsModal .table-responsive .table th {
            padding-left: 12px;
            padding-right: 12px;
        }

        /* 3. Ensure the remarks box doesn't shrink to 0 */
        #detailsModal .table-responsive input[type="text"] {
            min-width: 160px; /* Forces enough width to read the text */
        }

        /* Status badge standardization */
        .claims-badge {
            display: inline-block;
            padding: 4px 10px;
            border-radius: 12px;
            font-size: 0.78rem;
            font-weight: 600;
            line-height: 1.2;
            white-space: nowrap;
        }
        .claims-badge-pending { background: #fff3cd; color: #856404; border: 1px solid #ffeeba; }
        .claims-badge-returned { background: #ffe8cc; color: #c45c00; border: 1px solid #ffc78a; }
        .claims-badge-approved { background: #d4edda; color: #155724; border: 1px solid #b9dfc0; }
        .claims-badge-completed { background: #cce5ff; color: #004085; border: 1px solid #9ec5fe; }
        .claims-badge-ready { background: #e8d9ff; color: #4a148c; border: 1px solid #c9a8ff; }
        .claims-badge-rejected { background: #f8d7da; color: #721c24; border: 1px solid #f1b0b7; }
        .claims-badge-default { background: #e9ecef; color: #495057; border: 1px solid #ced4da; }

        /* Sticky details modal action footer */
        #detailsModal .modal-content { display: flex; flex-direction: column; max-height: 92vh; }
        #detailsModal .modal-body { flex: 1 1 auto; overflow: hidden; }
        #detailsModal .modal-footer.sticky-modal-footer {
            position: sticky;
            bottom: 0;
            z-index: 1030;
            background: #f8f9fa;
            box-shadow: 0 -4px 12px rgba(0,0,0,0.08);
            flex-shrink: 0;
        }

        /* Processing overlay */
        .claims-ux-processing-overlay {
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(255,255,255,0.85);
            z-index: 10050;
            text-align: center;
            padding-top: 18vh;
        }
        .claims-ux-processing-card {
            display: inline-block;
            background: #fff;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 28px 36px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.12);
            max-width: 420px;
        }

        /* Summary counter chips */
        .request-summary-counters {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 12px;
        }
        .request-counter-chip {
            flex: 1 1 140px;
            min-width: 120px;
            padding: 10px 14px;
            border-radius: 6px;
            background: #fff;
            border: 1px solid #dee2e6;
            text-align: center;
        }
        .request-counter-chip .count { font-size: 1.4rem; font-weight: 700; display: block; }
        .request-counter-chip .label { font-size: 0.72rem; text-transform: uppercase; color: #6c757d; font-weight: 600; }

        /* Returned visibility */
        .returned-reason-block {
            font-size: 0.78rem;
            margin-top: 4px;
            color: #c45c00;
            max-width: 220px;
            white-space: normal;
        }
        .km-edited-badge {
            font-size: 0.68rem;
            vertical-align: middle;
            margin-left: 4px;
        }
        .receipt-count-hint {
            display: block;
            font-size: 0.68rem;
            color: #6c757d;
            margin-top: 2px;
            white-space: normal;
            line-height: 1.2;
        }

        /* Empty state */
        .claims-empty-state {
            text-align: center;
            padding: 48px 24px;
            color: #6c757d;
        }
        .claims-empty-state i { font-size: 2.5rem; margin-bottom: 12px; color: #adb5bd; }
    </style>
    <script src="<%= ResolveUrl("~/Scripts/claims-ux-stabilization.js") %>"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid mt-3">
            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel shadow-sm rounded-2">
                        <div class="x_title d-flex justify-content-between align-items-center bg-primary text-white p-2 rounded-top">
                            <h4 class="mb-0"><i class="fa fa-check-circle"></i>Monthly Claims Approval (V2)</h4>
                        </div>
                        <div class="x_content bg-light p-3 rounded-bottom">
                            <asp:UpdatePanel ID="upMainGrid" runat="server" UpdateMode="Conditional">
                                <ContentTemplate>
                                    <div class="card-box table-responsive">

                                        <div class="card-box border bg-white p-3 mb-3 shadow-sm rounded">
                                            <div class="text-center mb-3">
                                                <asp:Button ID="btnPrevMonth" runat="server" Text="Prev Month" CssClass="btn btn-info btn-sm px-4" OnClick="btnPrevMonth_Click" />
                                                <asp:Button ID="btnCurrentMonth" runat="server" Text="Current Month" CssClass="btn btn-primary btn-sm px-4 mx-2" OnClick="btnCurrentMonth_Click" />
                                                <asp:Button ID="btnNextMonth" runat="server" Text="Next Month" CssClass="btn btn-info btn-sm px-4" OnClick="btnNextMonth_Click" />
                                            </div>

                                            <div class="row align-items-end">
                                                <div class="col-md-3">
                                                    <label class="font-weight-bold text-muted small">Search By Year</label>
                                                    <asp:DropDownList ID="ddlYear" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
                                                </div>
                                                <div class="col-md-3">
                                                    <label class="font-weight-bold text-muted small">Search By Month</label>
                                                    <asp:DropDownList ID="ddlMonth" runat="server" CssClass="form-control form-control-sm">
                                                        <asp:ListItem Value="0">-- All Months --</asp:ListItem>
                                                        <asp:ListItem Value="1">January</asp:ListItem>
                                                        <asp:ListItem Value="2">February</asp:ListItem>
                                                        <asp:ListItem Value="3">March</asp:ListItem>
                                                        <asp:ListItem Value="4">April</asp:ListItem>
                                                        <asp:ListItem Value="5">May</asp:ListItem>
                                                        <asp:ListItem Value="6">June</asp:ListItem>
                                                        <asp:ListItem Value="7">July</asp:ListItem>
                                                        <asp:ListItem Value="8">August</asp:ListItem>
                                                        <asp:ListItem Value="9">September</asp:ListItem>
                                                        <asp:ListItem Value="10">October</asp:ListItem>
                                                        <asp:ListItem Value="11">November</asp:ListItem>
                                                        <asp:ListItem Value="12">December</asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>
                                                <div class="col-md-4">
                                                    <label class="font-weight-bold text-muted small">Search By Employee Code / Name</label>
                                                    <asp:TextBox ID="txtSearchEmp" runat="server" CssClass="form-control form-control-sm" placeholder="e.g. AHS1281 or Manish"></asp:TextBox>
                                                </div>
                                                <div class="col-md-2">
                                                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-success btn-sm w-100 mb-1" OnClick="btnSearch_Click" />
                                                    <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm w-100 m-0" OnClick="btnReset_Click" />
                                                </div>
                                            </div>
                                        </div>

                                        <div class="request-summary-counters" id="requestSummaryCounters">
                                            <div class="request-counter-chip border-warning">
                                                <span class="count"><asp:Label ID="lblCountPending" runat="server" Text="0" /></span>
                                                <span class="label">Pending</span>
                                            </div>
                                            <div class="request-counter-chip" style="border-color:#ffc78a;">
                                                <span class="count text-warning"><asp:Label ID="lblCountReturned" runat="server" Text="0" /></span>
                                                <span class="label">Returned</span>
                                            </div>
                                            <div class="request-counter-chip border-primary">
                                                <span class="count text-primary"><asp:Label ID="lblCountCompleted" runat="server" Text="0" /></span>
                                                <span class="label">Completed</span>
                                            </div>
                                            <div class="request-counter-chip" style="border-color:#c9a8ff;">
                                                <span class="count" style="color:#4a148c;"><asp:Label ID="lblCountReadyForPayment" runat="server" Text="0" /></span>
                                                <span class="label">Ready For Payment</span>
                                            </div>
                                        </div>

                                        <asp:Panel ID="pnlEmptyRequests" runat="server" Visible="false" CssClass="claims-empty-state border rounded bg-white mb-3">
                                            <i class="fa fa-inbox d-block"></i>
                                            <h5 class="mb-1">No approval requests found</h5>
                                            <p class="mb-0">Try selecting another month or employee.</p>
                                        </asp:Panel>

                                        <asp:Panel ID="pnlRequests" runat="server">
                                            <asp:GridView ID="gvRequests" runat="server" ClientIDMode="Static"
                                                CssClass="table table-striped jambo_table bulk_action dt-responsive nowrap"
                                                ShowHeaderWhenEmpty="true" AutoGenerateColumns="false"
                                                OnRowCommand="gvRequests_RowCommand" OnRowDataBound="gvRequests_RowDataBound"
                                                DataKeyNames="ApprovalRequestId">
                                                <HeaderStyle CssClass="gentilia-table-header" />
                                                <RowStyle CssClass="gentilia-row" />
                                                <AlternatingRowStyle CssClass="gentilia-row-alt" />
                                                <Columns>
                                                    <asp:BoundField DataField="ApprovalRequestId" HeaderText="ID" HeaderStyle-Width="50px" />
                                                    <asp:TemplateField HeaderText="Employee Name" HeaderStyle-Width="220px">
                                                        <ItemTemplate><%# Eval("EmployeeName") %> (<%# Eval("RequesterLoginId") %>)</ItemTemplate>
                                                    </asp:TemplateField>
                                                    <asp:BoundField DataField="RequestDate" HeaderText="Requested Date" DataFormatString="{0:dd-MMM-yyyy hh:mm:ss tt}" />
                                                    <asp:BoundField DataField="MonthName" HeaderText="Month" />
                                                    <asp:TemplateField HeaderText="Total Claim (₹)">
                                                        <ItemTemplate>
                                                            <span class="font-weight-bold">
                                                                <%# String.Format("{0:N2}", Convert.ToDecimal(Eval("TotalClaimAmount")) + Convert.ToDecimal(Eval("TotalExpenseAmount"))) %>
                                                            </span>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                    <asp:TemplateField HeaderText="Status">
                                                        <ItemTemplate>
                                                            <span class='<%# GetStatusBadgeCss(Eval("RequestStatus")) %>'><%# Eval("RequestStatus") %></span>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                    <asp:TemplateField HeaderText="Stage">
                                                        <ItemTemplate>
                                                            <small class="d-block"><%# Eval("ApprovalProgress") %></small>
                                                            <small class="text-muted"><i class="fa fa-clock-o"></i>Pending: <%# Eval("PendingWithRole") %></small>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                    <asp:TemplateField HeaderText="Actions">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="lnkView" runat="server" CommandName="View" CommandArgument='<%# Eval("ApprovalRequestId") %>' CssClass="btn btn-sm btn-primary">
                                                                <i class="fa fa-eye"></i> View
                                                            </asp:LinkButton>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>
                                                </Columns>
                                            </asp:GridView>
                                        </asp:Panel>
                                    </div>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="detailsModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true" runat="server" clientidmode="Static">
            <div class="modal-dialog modal-xl" role="document">
                <div class="modal-content">
                    <asp:UpdatePanel ID="upDetails" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:HiddenField ID="hfActionRequestId" runat="server" />
                            <asp:HiddenField ID="hfReturnReason" runat="server" ClientIDMode="Static" />
                            <asp:HiddenField ID="hfCurrentUserLevel" runat="server" ClientIDMode="Static" />
                            <asp:HiddenField ID="hfIsFinalApprover" runat="server" />
                            <asp:HiddenField ID="hfIsAuditor" runat="server" ClientIDMode="Static" Value="0" />
                            <div class="modal-header bg-light">
                                <h5 class="modal-title font-weight-bold">Request #<asp:Label ID="lblModalRequestId" runat="server" Text="0" /></h5>
                                <div class="ml-auto">
                                    <asp:Label ID="lblRequesterName" runat="server" Font-Bold="true" />
                                </div>
                                <button type="button" class="close ml-3" data-dismiss="modal"><span>×</span></button>
                            </div>

                            <div class="modal-body p-3">
                                <div class="table-responsive small">
                                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" CssClass="table table-hover table-bordered"
                                        DataKeyNames="ApprovalRequestDetailId" OnRowDataBound="gvDetails_RowDataBound" OnRowCommand="gvDetails_RowCommand" ShowFooter="true">
                                        <Columns>
                                            <asp:TemplateField HeaderText="Sel" ItemStyle-Width="30px">
                                                <HeaderTemplate>
                                                    <asp:CheckBox ID="chkSelectAll" runat="server" onclick="selectAllRows(this)" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkSelect" runat="server" CssClass="row-select-checkbox" />
                                                    <asp:HiddenField ID="hfActivityId" runat="server" Value='<%# Eval("ActivityId") %>' />
                                                    <asp:HiddenField ID="hfApprovalRequestDetailId" runat="server" Value='<%# Eval("ApprovalRequestDetailId") %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Activity Date">
                                                <ItemTemplate>
                                                    <%# Eval("ActivityDate", "{0:dd-MMM-yyyy}") %><br />
                                                    <small class="text-muted"><%# Eval("Day") %></small>
                                                    <asp:Label ID="lblDetailVersion" runat="server" CssClass="badge badge-secondary ml-1" />
                                                    <asp:Label ID="lblReturnedBadge" runat="server" Visible="false" CssClass="claims-badge claims-badge-returned d-block mt-1" Text="Returned" />
                                                    <asp:Label ID="lblReturnedReason" runat="server" Visible="false" CssClass="returned-reason-block d-block" />
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Dist. (KM)">
                                                <ItemTemplate>
                                                    <span class="km-value font-weight-bold font-md" data-activityid='<%# Eval("ActivityId") %>'>
                                                        <%# Eval("Snapshot_TotalDistanceKM", "{0:N2}") %>
                                                    </span>
                                                    <asp:Label ID="lblKmEdited" runat="server" Visible="false" CssClass="badge badge-info km-edited-badge" Text="Edited" ToolTip="KM has been modified — click folder to view history" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="Snapshot_CalculatedClaimAmount" HeaderText="Bike TA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_DAAmount" HeaderText="DA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_HotelAmount" HeaderText="Hotel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_TravelAmount" HeaderText="Travel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField
                                                DataField="Snapshot_StationaryAmount"
                                                HeaderText="Stat. (₹)"
                                                DataFormatString="{0:N2}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal"
                                                HeaderText="Day Total (₹)"
                                                DataFormatString="{0:N2}"
                                                ItemStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="Review">
                                                <ItemStyle HorizontalAlign="Center" />

                                                <ItemTemplate>
                                                    <asp:Label ID="lblClaimType" runat="server" CssClass="badge badge-pill badge-info" Text='<%# Eval("ClaimType") %>' />
                                                    <br />
                                                    <asp:Label ID="lblReviewMode"
                                                        runat="server"
                                                        CssClass='<%#
                                                            Convert.ToBoolean(Eval("ManualReview"))
                                                            ? "badge badge-danger"
                                                            : "badge badge-success" %>'
                                                        Text='<%#
                                                            Convert.ToBoolean(Eval("ManualReview"))
                                                            ? "Manual"
                                                            : "Bulk" %>' />
                                                    <asp:LinkButton
                                                        ID="lnkReview"
                                                        runat="server"
                                                        CssClass="btn btn-sm btn-primary btn-day-action"
                                                        OnClientClick="return false;"
                                                        data-activityid='<%# Eval("ActivityId") %>'
                                                        data-detailid='<%# Eval("ApprovalRequestDetailId") %>'
                                                        data-expenses='<%# Eval("ExpensesJson") %>'
                                                        data-activitydate='<%# Eval("ActivityDate","{0:dd-MMM-yyyy}") %>'
                                                        data-day='<%# Eval("Day") %>'
                                                        data-claimtype='<%# Eval("ClaimType") %>'
                                                        data-status='<%# Eval("DetailStatus") %>'
                                                        data-distance='<%# Eval("Snapshot_TotalDistanceKM") %>'
                                                        data-ta='<%# Eval("Snapshot_CalculatedClaimAmount") %>'
                                                        data-da='<%# Eval("Snapshot_DAAmount") %>'
                                                        data-hotel='<%# Eval("Snapshot_HotelAmount") %>'
                                                        data-travel='<%# Eval("Snapshot_TravelAmount") %>'
                                                        data-stationary='<%# Eval("Snapshot_StationaryAmount") %>'>
                                                        <i class="fa fa-search"></i>
                                                    </asp:LinkButton>

                                                </ItemTemplate>

                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:TextBox ID="txtRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' TextMode="MultiLine" Rows="2" CssClass="form-control form-control-sm claims-ux-track-dirty" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Approval" HeaderStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Button
                                                        ID="btnApproveRow"
                                                        runat="server"
                                                        CssClass="btn btn-success btn-sm claims-ux-postback-btn"
                                                        Text="Approve"
                                                        CommandName="ApproveRow"
                                                        CommandArgument='<%# Eval("ApprovalRequestDetailId") %>'
                                                        data-ux-loading="Approving..." />
                                                    <asp:Button
                                                        ID="btnReturnRow"
                                                        runat="server"
                                                        CssClass="btn btn-warning btn-sm mt-1 claims-ux-postback-btn"
                                                        Text="Return"
                                                        CommandName="ReturnRow"
                                                        CommandArgument='<%# Eval("ApprovalRequestDetailId") %>'
                                                        data-ux-loading="Returning..."
                                                        data-ux-confirm='{"title":"Return this Day?","message":"The employee will be allowed to edit only Returned items.","confirmText":"Return","confirmClass":"btn-warning"}' />
                                                    <asp:Button
                                                        ID="btnRejectDayRow"
                                                        runat="server"
                                                        CssClass="btn btn-danger btn-sm mt-1 claims-ux-postback-btn"
                                                        Text="Reject"
                                                        CommandName="RejectDayRow"
                                                        CommandArgument='<%# Eval("ApprovalRequestDetailId") %>'
                                                        data-ux-loading="Rejecting..."
                                                        data-ux-confirm='{"title":"Reject this Day?","message":"The employee cannot modify rejected claims.","confirmText":"Reject","confirmClass":"btn-danger"}' />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>

                                <div class="row mt-2 align-items-center">
                                    <div class="col-md-6">
                                        <asp:TextBox ID="txtBulkRemarks" runat="server" CssClass="form-control form-control-sm claims-ux-track-dirty" Placeholder="Bulk remarks for selected items..." />
                                    </div>
                                    <div class="col-md-6 text-right">
                                        <asp:Button ID="btnApproveSelected" runat="server" Text="Approve Selected" CssClass="btn btn-success btn-sm claims-ux-postback-btn" OnClick="btnApproveSelected_Click" data-ux-processing="Processing bulk approval..." data-ux-loading="Approving..." />
                                        <asp:Button ID="btnRejectSelected" runat="server" Text="Return Selected" CssClass="btn btn-warning btn-sm claims-ux-postback-btn" OnClick="btnReturnSelected_Click" data-ux-processing="Processing bulk return..." data-ux-loading="Returning..." data-ux-confirm='{"title":"Return selected days?","message":"The employee will be allowed to edit only Returned items.","confirmText":"Return","confirmClass":"btn-warning"}' />
                                        <asp:Button ID="btnRejectDaysSelected" runat="server" Text="Reject Selected" CssClass="btn btn-danger btn-sm claims-ux-postback-btn" OnClick="btnRejectDaysSelected_Click" data-ux-processing="Processing bulk reject..." data-ux-loading="Rejecting..." data-ux-confirm='{"title":"Reject selected days?","message":"The employee cannot modify rejected claims.","confirmText":"Reject","confirmClass":"btn-danger"}' />
                                    </div>
                                </div>

                                <asp:Button ID="btnRefreshCalculations" runat="server" OnClick="btnRefreshCalculations_Click" ClientIDMode="Static" Style="" />
                            </div>

                            <div class="modal-footer bg-light d-flex justify-content-between align-items-end sticky-modal-footer" style="border-top: 2px solid #dee2e6;">

                                <div class="dashboard-container">
                                    <div class="dashboard-box bg-white border">
                                        <span class="dashboard-label text-muted">Original Claimed</span>
                                        <h5 class="dashboard-value text-dark mb-0">₹
                                            <asp:Label ID="lblSummaryOriginal" runat="server" Text="0.00" /></h5>
                                        <small class="font-weight-bold text-muted d-block mt-1">
                                            <i class="fa fa-road"></i>
                                            <asp:Label ID="lblSummaryOriginalKM" runat="server" Text="0.00" />
                                            KM
                                        </small>
                                    </div>

                                    <div class="dashboard-box bg-white border border-danger" style="cursor: pointer; transition: 0.2s;" onclick="showDeductionsBreakdown()" title="Click to view deduction details">
                                        <span class="dashboard-label text-danger">Total Deductions <i class="fa fa-info-circle ml-1"></i></span>
                                        <h5 class="dashboard-value text-danger mb-0">(-) ₹
                                            <asp:Label ID="lblSummaryDeduction" runat="server" Text="0.00" /></h5>
                                        <small class="font-weight-bold text-danger d-block mt-1">
                                            <i class="fa fa-road"></i>(-)
                                            <asp:Label ID="lblSummaryDeductedKM" runat="server" Text="0.00" />
                                            KM
                                        </small>
                                    </div>

                                    <div class="dashboard-box bg-success text-white border border-success">
                                        <span class="dashboard-label text-white" style="opacity: 0.9">Net Approved</span>
                                        <h5 class="dashboard-value text-white mb-0">₹
                                            <asp:Label ID="lblSummaryApproved" runat="server" Text="0.00" /></h5>
                                        <small class="font-weight-bold text-white d-block mt-1">
                                            <i class="fa fa-road"></i>
                                            <asp:Label ID="lblSummaryApprovedKM" runat="server" Text="0.00" />
                                            KM
                                        </small>
                                    </div>
                                </div>

                                <div style="width: 100%;">
                                    <asp:Label ID="lblModalMessage" runat="server" CssClass="d-block mb-2 font-weight-bold text-right" />

                                    <div class="modal-action-buttons">
                                        <asp:LinkButton ID="lnkRecalculate" runat="server" CssClass="btn btn-info btn-sm m-0 claims-ux-postback-btn" OnClick="btnRecalculate_Click" OnClientClick="return armRecalculateProcessing(this);" ToolTip="Recalculate approved amounts" data-ux-processing="Recalculating totals..." data-ux-loading="Calculating...">
                                            <i class="fa fa-refresh"></i> Re-Calculate
                                        </asp:LinkButton>
                                        <asp:Button ID="btnReturnToEmployee" runat="server" Text="✖ Return" CssClass="btn btn-danger btn-sm m-0 ml-2" OnClick="btnReturnToEmployee_Click" OnClientClick="return promptForReturn();" Visible="true" />
                                        <asp:Button ID="btnForwardLevel" runat="server" Text="✔ Forward" CssClass="btn btn-success btn-sm m-0 ml-2" OnClick="btnForwardLevel_Click" OnClientClick="return showApprovalSummaryConfirm('forward');" Visible="false" />
                                        <asp:Button ID="btnMarkReadyForPayment" runat="server" Text="✔ Ready for Payment" CssClass="btn btn-warning btn-sm m-0 ml-2" OnClick="btnMarkReadyForPayment_Click" OnClientClick="return showApprovalSummaryConfirm('payment');" Visible="false" />
                                        <button type="button" class="btn btn-secondary btn-sm m-0 ml-2" data-dismiss="modal">Close</button>
                                    </div>
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>
        </div>

        <input type="hidden" id="km_modal_activityid_outer" />

        <div id="unifiedDayModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true" data-backdrop="static">
            <div class="modal-dialog modal-xl">
                <div class="modal-content border-0 shadow-lg">
                    <div class="modal-header bg-dark text-white p-3 align-items-center">
                        <h6 class="modal-title m-0 font-weight-bold text-uppercase" style="letter-spacing: 0.5px;">
                            <i class="fa fa-calendar-check mr-2"></i>Day Details & Actions
                        </h6>
                        <button type="button" class="close text-white" data-dismiss="modal" style="opacity: 1;">×</button>
                    </div>

                    <div class="modal-body p-0 bg-light">
                        <ul class="nav nav-tabs nav-justified m-0 bg-white shadow-sm" id="dayModalTabs" role="tablist">
                            <li class="nav-item">
                                <a class="nav-link active font-weight-bold text-dark py-3 border-0" id="receipts-tab" data-toggle="tab" href="#tab-receipts" role="tab">
                                    <i class="fa fa-receipt text-info mr-2"> </i>Expense Receipts
                                </a>
                            </li>
                            <li class="nav-item border-left">
                                <a class="nav-link font-weight-bold text-dark py-3 border-0" id="distance-tab" data-toggle="tab" href="#tab-distance" role="tab">
                                    <i class="fa fa-map-marker-alt text-danger mr-2"></i>Distance (KM) & Map
                                </a>
                            </li>
                        </ul>

                        <div class="tab-content p-4">

                            <div class="tab-pane active" id="tab-receipts" role="tabpanel">
                                <div id="unifiedExpensesBody">
                                    <div class="text-center py-5 text-muted">
                                        <i class="fa fa-spinner fa-spin fa-2x mb-3 text-primary"></i>
                                        <h5>Loading day details...</h5>
                                    </div>
                                </div>
                            </div>

                            <div class="tab-pane" id="tab-distance" role="tabpanel">
                                <input type="hidden" id="km_modal_activityid" />
                                <input type="hidden" id="km_modal_detailid" />

                                <div class="alert alert-info border-info mb-4 shadow-sm">
                                    <div class="row align-items-center">
                                        <div class="col-md-5"><strong><i class="fa fa-play-circle text-success mr-1"></i>From:</strong> <span id="km_modal_fromplace" class="text-dark ml-1"></span></div>
                                        <div class="col-md-5"><strong><i class="fa fa-stop-circle text-danger mr-1"></i>To:</strong> <span id="km_modal_toplace" class="text-dark ml-1"></span></div>
                                        <div class="col-md-2 text-right">
                                            <a href="#" id="km_modal_maplink" target="_blank" class="btn btn-sm btn-primary shadow-sm w-100" style="display: none;">
                                                <i class="fa fa-map-marked-alt mr-1"></i>Map
                                            </a>
                                        </div>
                                    </div>
                                </div>

                                <div class="row align-items-end mb-4" id="divKmControls">
                                    <div class="col-md-3">
                                        <div class="form-group mb-0">
                                            <label class="font-weight-bold text-muted small mb-1 text-uppercase">Current KM</label>
                                            <input type="text" id="km_modal_current" class="form-control bg-white font-weight-bold text-center" readonly />
                                        </div>
                                    </div>
                                    <div class="col-md-3">
                                        <div class="form-group mb-0">
                                            <label class="font-weight-bold text-primary small mb-1 text-uppercase">Modify KM To</label>
                                            <input type="number" step="0.01" id="km_modal_new" class="form-control border-primary text-center font-weight-bold claims-ux-track-dirty" />
                                        </div>
                                    </div>
                                    <div class="col-md-6">
                                        <div class="form-group mb-0">
                                            <label class="font-weight-bold text-muted small mb-1 text-uppercase">Reason for change</label>
                                            <div class="input-group">
                                                <input type="text" id="km_modal_reason" class="form-control claims-ux-track-dirty" placeholder="Required if changing KM..." />
                                                <div class="input-group-append">
                                                    <button type="button" id="km_modal_save" class="btn btn-primary shadow-sm" onclick="saveKM()"><i class="fa fa-save mr-1"></i>Save</button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div id="km_modal_message" class="mb-3"></div>

                                <h6 class="text-secondary border-bottom pb-2 mt-4 font-weight-bold text-uppercase" style="font-size: 0.85rem;"><i class="fa fa-history mr-2"></i>Modification History</h6>
                                <div class="table-responsive bg-white shadow-sm border rounded" style="max-height: 250px; overflow-y: auto;">
                                    <table class="table table-striped table-hover table-sm mb-0" id="km_history_tbl">
                                        <thead class="thead-light" style="position: sticky; top: 0; z-index: 10;">
                                            <tr>
                                                <th class="border-top-0">Date (IST)</th>
                                                <th class="border-top-0 text-right">Old KM</th>
                                                <th class="border-top-0 text-right">New KM</th>
                                                <th class="border-top-0">Changed By</th>
                                                <th class="border-top-0">Reason</th>
                                            </tr>
                                        </thead>
                                        <tbody></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>

                    <%--<div class="modal-footer bg-white border-top p-3 d-flex justify-content-between align-items-center">
                        <div id="unified_modal_message" class="font-weight-bold small text-uppercase"></div>
                        <div class="btn-group shadow-sm">
                            <button type="button"
                                id="btnCloseUnifiedModal"
                                class="btn btn-light border text-dark"
                                data-dismiss="modal">
                                Close
                            </button>
                            <button type="button" id="btnUnifiedRejectDay" class="btn btn-outline-danger" onclick="actionDayFromModal('REJECT')"><i class="fa fa-times mr-1"></i>Return Day</button>
                            <button type="button" id="btnUnifiedApproveDay" class="btn btn-success" onclick="actionDayFromModal('APPROVE')"><i class="fa fa-check mr-1"></i>Approve Remaining</button>
                        </div>
                    </div>--%>

                    <div class="modal-footer justify-content-between">

                        <div id="unified_modal_message"
                            class="text-muted small font-italic">
                        </div>

                        <div>

                            <button
                                type="button"
                                class="btn btn-success"
                                id="btnUnifiedApproveDay"
                                onclick="actionDayFromModal('APPROVE')">

                                <i class="fa fa-check mr-1"></i>

                                Approve Day

                            </button>

                            <button
                                type="button"
                                class="btn btn-warning"
                                id="btnUnifiedReturnDay"
                                onclick="actionDayFromModal('RETURN')">

                                <i class="fa fa-undo mr-1"></i>

                                Return Day

                            </button>

                            <button
                                type="button"
                                class="btn btn-danger"
                                id="btnUnifiedRejectDay"
                                onclick="actionDayFromModal('REJECT')"
                                style="display: none;">

                                <i class="fa fa-ban mr-1"></i>

                                Reject Day

                            </button>

                            <button
                                type="button"
                                class="btn btn-secondary"
                                data-dismiss="modal">
                                Close

                            </button>

                        </div>

                    </div>

                </div>
            </div>
        </div>

        <div class="modal fade" id="deductionsModal" tabindex="-1" role="dialog" style="z-index: 1060;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content border-danger">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title"><i class="fa fa-scissors"></i>Deduction Breakdown</h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>
                    <div class="modal-body p-0">
                        <div class="table-responsive" style="max-height: 400px; overflow-y: auto;">
                            <table class="table table-striped table-hover mb-0" id="tblDeductions">
                                <thead class="bg-light sticky-top">
                                    <tr>
                                        <th>Date</th>
                                        <th>Deduction Source</th>
                                        <th>Reason / Remarks</th>
                                        <th class="text-right text-danger">Amount (₹)</th>
                                    </tr>
                                </thead>
                                <tbody></tbody>
                                <tfoot class="bg-light font-weight-bold">
                                    <tr>
                                        <td colspan="3" class="text-right">Total Deductions:</td>
                                        <td class="text-right text-danger" id="lblDeductionsModalTotal">0.00</td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>

    <script src="https://cdn.datatables.net/1.10.20/js/jquery.dataTables.min.js"></script>
    <script src="https://cdn.datatables.net/1.10.20/js/dataTables.bootstrap4.min.js"></script>
    <script src="https://cdn.datatables.net/responsive/2.2.3/js/dataTables.responsive.min.js"></script>
    <script src="https://cdn.datatables.net/responsive/2.2.3/js/responsive.bootstrap4.min.js"></script>
    <script type="text/javascript">
        // Global variables
        var _refreshNeeded = false;
        var _currentUserLevel = <%= ViewState["CurrentUserLevel"] ?? 0 %>;

        function selectAllRows(chk) {
            var table = document.getElementById('gvDetails');
            if (!table) return;
            var boxes = table.querySelectorAll('tbody input[type="checkbox"]');
            for (var i = 0; i < boxes.length; i++) {
                boxes[i].checked = chk.checked;
            }
        }

        // =========================================================
        // 1. UNIFIED DAY MODAL TRIGGER & LOGIC
        // =========================================================
        $(document).on('click', '.btn-day-action', function(e) {
            e.preventDefault();
            var $row = $(this).closest('tr');

            // 1. Extract base data from grid
            var actId = $(this).data('activityid');
            var detailId = $(this).data('detailid'); 
            var currentKm = $row.find('.km-value').text().trim();
            var expensesJson = $(this).attr('data-expenses');

            // 2. Extract Data for Phase 6 Summary Header
            var details = {
                ActivityDate: $row.find('td:nth-child(2)').text().trim(),
                Day: $row.find('td:nth-child(2) small').text().trim(),
                PolicyMode: $(this).data('policymode'),
                Hotel: $(this).data('hotel'),
                Travel: $(this).data('travel'),
                Stationary: $(this).data('stationary'),
                ClaimType: $(this).data('claimtype'),
                ClaimStatus: $(this).data('status'),
                StatusClass:
                $row.find('.status-badge').hasClass('badge-success')
                    ? 'success'
                    : ($row.find('.status-badge').hasClass('badge-danger')
                        ? 'danger'
                        : 'warning'),
                TotalDistanceKM: $(this).data('distance'),
                TotalTA: $(this).data('ta'),
                TotalDA: $(this).data('da'),
            };

            // CHECK LOCK STATE: Is this parent day already "✔ Done"?
            var $btnApproveRow = $row.find('input[id*="btnApproveRow"]');
            var isRowLocked = $btnApproveRow.length > 0 && $btnApproveRow.val().indexOf('Done') !== -1;

            // Set UI Controls based on user level AND row lock state
            var activeLevel = parseInt($('#<%=hfCurrentUserLevel.ClientID%>').val(),10) || 0;
            var canAction = activeLevel > 0 && !isRowLocked;
        
            var currentLevel = parseInt($('#<%=hfCurrentUserLevel.ClientID%>').val(), 10);
            var finalLevel = parseInt($('#<%=hfIsFinalApprover.ClientID%>').val(), 10);
            var isAuditor = $('#hfIsAuditor').val() === "1";

            if (canAction) {
                $('#divKmControls').show();
                $('#btnUnifiedApproveDay').show();
                if (isAuditor) {
                    $('#btnUnifiedReturnDay').hide();
                    $('#btnUnifiedRejectDay').show();
                } else {
                    $('#btnUnifiedReturnDay').show();
                    $('#btnUnifiedRejectDay').hide();
                }
            } else {
                $('#divKmControls').hide();
                $('#btnUnifiedApproveDay').hide();
                $('#btnUnifiedReturnDay').hide();
                $('#btnUnifiedRejectDay').hide();
            }

            // Setup Distance Tab & Hidden Fields
            $('#km_modal_activityid').val(actId);
            $('#km_modal_detailid').val(detailId); 
            $('#km_modal_current').val(currentKm);
            $('#km_modal_new').val(currentKm);
            $('#km_modal_reason').val('');
            $('#km_modal_message').empty();
            $('#unified_modal_message').empty();

            loadKmHistory(actId);

            renderUnifiedExpensesModal(
                details,
                expensesJson,
                isRowLocked,
                canAction,
                isAuditor
            );

            // Show Modal
            $('#dayModalTabs a[href="#tab-receipts"]').tab('show');
            setTimeout(function () {
                document.getElementById('divKmControls')
                    .scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 200);
            $('#unifiedDayModal').modal('show');
        });


        function buildActivitySummary(details) {
            return `
            <div class="card shadow-sm border-0 mb-3">
                <div class="card-header bg-primary text-white py-2">
                    <strong>
                        <i class="fa fa-info-circle mr-2"></i>
                        Activity Summary
                    </strong>
                </div>

                <div class="card-body py-3">
                    <div class="row">
                        <div class="col-md-3 mb-2">
                            <small class="text-muted d-block">Activity Date</small>
                            <strong>${details.ActivityDate}</strong>
                        </div>
                        <div class="col-md-2 mb-2">
                            <small class="text-muted d-block">Day</small>
                            <strong>${details.Day}</strong>
                        </div>
                        <div class="col-md-3 mb-2">
                            <small class="text-muted d-block">Claim Type</small>

                            <span class="badge badge-info">
                                ${details.ClaimType}
                            </span>
                        </div>
                        <div class="col-md-2 mb-2">
                            <small class="text-muted d-block">Status</small>

                            <span class="badge badge-${details.StatusClass}">
                                ${details.ClaimStatus}
                            </span>
                        </div>
                        <div class="col-md-2 mb-2">
                            <small class="text-muted d-block">Policy</small>
                            <strong>${details.PolicyMode || '-'}</strong>
                        </div>
                    </div>
                </div>
            </div>
          `;
        }

        // =========================================================
        // 2. MODULAR RENDERING LOGIC (PHASE 6 & 7)
        // =========================================================
        function renderUnifiedExpensesModal(details, expensesJson, isRowLocked, canAction, isAuditor) {
            let html = '';

            // Render Top Summaries
            //html += buildActivitySummary(details);
            //html += buildTADASummary(details);
            html += renderActivityAndTADASummary(details);

            // Parse Line Items
            let expenses = [];
            try {
                if (expensesJson && expensesJson.trim() !== '' && expensesJson.trim() !== '[]') {
                    expenses = JSON.parse(expensesJson);
                }
            } catch (e) {
                console.error("Error parsing expenses JSON:", e);
            }

            var expenseCounts = (window.ClaimsUx && ClaimsUx.parseExpenseCounts)
                ? ClaimsUx.parseExpenseCounts(expenses)
                : { total: 0, approved: 0, pending: 0 };
            html += '<div class="alert alert-light border small py-2 mb-3">' +
                '<strong>Expenses (' + expenseCounts.total + ')</strong> &mdash; ' +
                'Approved: <span class="text-success font-weight-bold">' + expenseCounts.approved + '</span>, ' +
                'Pending: <span class="text-warning font-weight-bold">' + expenseCounts.pending + '</span>' +
                '</div>';

            // Render Individual Expense Cards
            if (expenses.length > 0) {
                html += '<h6 class="mt-4 mb-3 text-secondary font-weight-bold text-uppercase" style="font-size: 0.85rem;"><i class="fa fa-receipt mr-2"></i>Claimed Line Items</h6>';
                html += '<div class="row">';
            
                let hasPending = false;

                expenses.forEach(function (exp) {

                    var type = (exp.ExpenseType || '').toUpperCase();

                    if (exp.ClaimStatus !== 'Approved' &&
                        exp.ClaimStatus !== 'Rejected') {

                        hasPending = true;
                    }

                    switch (type) {

                        case 'HOTEL':

                            html += renderExpenseCard(
                                'Hotel',
                                'fa-bed',
                                'bg-info',
                                exp,
                                canAction,isAuditor);

                            break;

                        case 'TRAVEL':

                            html += renderExpenseCard(
                                'Travel',
                                'fa-bus',
                                'bg-primary',
                                exp,
                                canAction,isAuditor);

                            break;

                        case 'STATIONARY':

                            html += renderExpenseCard(
                                'Stationary',
                                'fa-paperclip',
                                'bg-warning text-dark',
                                exp,
                                canAction,isAuditor);

                            break;

                        case 'DA':

                            html += renderExpenseCard(
                                'Daily Allowance',
                                'fa-utensils',
                                'bg-secondary',
                                exp,
                                canAction,isAuditor);

                            break;

                    }

                });
            
                html += '</div>';

                //// THE GATEKEEPER: Lock/Unlock the "Approve Remaining" button
                //if (hasPending && !isRowLocked) {
                //    $('#btnUnifiedApproveDay').prop('disabled', true).attr('title', 'You must approve or reject all individual line items above first.');
                //} else {
                //    $('#btnUnifiedApproveDay').prop('disabled', false).removeAttr('title');
                //}

                // Footer action is always available.
                // Server will process ONLY the remaining pending line items.
                $('#btnUnifiedApproveDay')
                    .prop('disabled', !canAction)
                    .attr('title', canAction ? '' : 'You are not authorized to perform this action.');

                $('#btnUnifiedReturnDay')
                    .prop('disabled', !canAction)
                    .attr('title', canAction ? '' : 'You are not authorized to perform this action.');

                $('#btnUnifiedRejectDay')
                    .prop('disabled', !canAction)
                    .attr('title', canAction ? '' : 'You are not authorized to perform this action.');

            } else {
                html += `
            <div class="alert alert-light border mt-4 text-center text-muted shadow-sm py-4">
                <i class="fa fa-folder-open fa-3x mb-3 d-block text-secondary opacity-50"></i>
                <h6 class="mb-0">No additional line items (Hotel, Travel, Stationary) claimed for this day.</h6>
            </div>`;
                $('#btnUnifiedApproveDay').prop('disabled', !canAction).attr('title', canAction ? '' : 'You are not authorized to perform this action.');
            }

            // Inject into the DOM
            $('#unifiedExpensesBody').html(html);
        }

        function renderActivityAndTADASummary(details) {

            function amount(val) {
                return parseFloat(val || 0).toFixed(2);
            }

            return `
            <div class="row">

                <!-- Activity Summary -->
                <div class="col-md-6 mb-3">

                    <div class="card summary-card border-left-primary shadow-sm border-0 h-100">

                        <div class="card-body">

                            <h6 class="text-primary text-uppercase font-weight-bold mb-3 border-bottom pb-2"
                                style="font-size:0.75rem;letter-spacing:.5px;">

                                <i class="fa fa-calendar mr-1"></i>

                                Activity Summary

                            </h6>

                            <div class="d-flex justify-content-between mb-2">

                                <span class="text-muted small font-weight-bold">
                                    Activity Date
                                </span>

                                <strong>

                                    ${details.ActivityDate}

                                    <span class="text-muted">
                                        (${details.Day})
                                    </span>

                                </strong>

                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">
                                    Claim Type
                                </span>

                                <span class="badge badge-info">
                                    ${details.ClaimType || 'Standard'}
                                </span>
                            </div>

                            <div class="d-flex justify-content-between">
                                <span class="text-muted small font-weight-bold">
                                    Status
                                </span>

                                <span class="badge badge-${details.StatusClass}">

                                    ${details.ClaimStatus}

                                </span>
                            </div>
                        </div>
                    </div>
                </div>


                <!-- TA / DA Summary -->
                <div class="col-md-6 mb-3">

                    <div class="card summary-card border-left-success shadow-sm border-0 h-100">

                        <div class="card-body">

                            <h6 class="text-success text-uppercase font-weight-bold mb-3 border-bottom pb-2"
                                style="font-size:0.75rem;letter-spacing:.5px;">

                                <i class="fa fa-road mr-1"></i>

                                TA / DA Summary

                            </h6>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Verified Distance</span>
                                <strong>${details.TotalDistanceKM} KM</strong>
                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Bike TA</span>
                                <strong>₹ ${amount(details.TotalTA)}</strong>
                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Daily Allowance</span>
                                <strong>₹ ${amount(details.TotalDA)}</strong>
                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Hotel</span>
                                <strong>₹ ${amount(details.Hotel)}</strong>
                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Travel</span>
                                <strong>₹ ${amount(details.Travel)}</strong>
                            </div>

                            <div class="d-flex justify-content-between mb-2">
                                <span class="text-muted small font-weight-bold">Stationary</span>
                                <strong>₹ ${amount(details.Stationary)}</strong>
                            </div>

                            <hr class="my-2">

                            <div class="d-flex justify-content-between align-items-center">

                                <span class="font-weight-bold text-uppercase">
                                    Grand Total
                                </span>

                                <span class="h5 text-success mb-0">

                                    ₹ ${
                                    amount(
                                        parseFloat(details.TotalTA || 0) +
                                        parseFloat(details.TotalDA || 0) +
                                        parseFloat(details.Hotel || 0) +
                                        parseFloat(details.Travel || 0) +
                                        parseFloat(details.Stationary || 0)
                                        )
            }

                                </span>

                            </div>

                            <div class="text-right mt-3">
                                <button type="button"
                                        class="btn btn-outline-primary btn-sm"
                                        onclick="$('#dayModalTabs a[href=\\'#tab-distance\\']').tab('show');">
                                    <i class="fa fa-map-marker mr-1"></i>
                                    View Route
                                </button>
                            </div>
                       </div>
                    </div>
                </div>
            </div>`;
        }

        function renderExpenseCard(title, iconClass, headerColorClass, exp, canAction, isAuditor) {
            let amount = parseFloat(exp.Amount || 0).toFixed(2);
            let billNo = exp.BillNo || 'N/A';
            let remarks = exp.Purpose || '<em class="text-muted">No remarks provided.</em>';
            let status = (exp.ClaimStatus || 'Pending').trim();
       
            let attachmentHtml = '';
            if (exp.AttachmentPath && exp.AttachmentPath.trim() !== '') {
                let safePath = exp.AttachmentPath.replace(/\\/g, '/');
                attachmentHtml = `
                    <div class="text-center">
                        <span class="badge badge-success mb-2 px-3 py-2">
                            <i class="fa fa-check-circle mr-1"></i>
                            Receipt Available
                        </span>
                        <br>
                        <a href="${safePath}"
                           target="_blank"
                           class="btn btn-outline-primary btn-sm">
                            <i class="fa fa-eye mr-1"></i>
                            Preview Receipt
                        </a>
                    </div>

                `;

            }
            else {

                attachmentHtml = `
                    <div class="text-center">
                        <span class="badge badge-warning text-dark px-3 py-2">
                            <i class="fa fa-exclamation-triangle mr-1"></i>
                            No Receipt Uploaded
                        </span>
                    </div>
                `;

            }

            let actionButtons = '';

            if (canAction) {

                switch (status) {

                    //-------------------------------------------------
                    // Pending
                    //-------------------------------------------------

                    case "Pending":

                        actionButtons = `
                <small class="text-muted text-uppercase font-weight-bold d-block text-center mb-2">
                    Review Decision
                </small>

                <div class="btn-group d-flex">

                    <button
                        type="button"
                        class="btn btn-success btn-sm flex-fill"
                        onclick="updateExpense(${exp.ExpenseId},'APPROVE',this)">
                        <i class="fa fa-check mr-1"></i>
                        Approve
                    </button>

                    ${
                                    isAuditor
                                    ?
                                    `
                        <button
                            type="button"
                            class="btn btn-danger btn-sm flex-fill"
                            onclick="updateExpense(${exp.ExpenseId},'REJECT',this)">
                            <i class="fa fa-ban mr-1"></i>
                            Reject
                        </button>
                        `
                                    :
                                    ''
                        }

                </div>
            `;
                        break;

                        //-------------------------------------------------
                        // Returned (awaiting employee correction)
                        //-------------------------------------------------

                    case "Returned":

                        actionButtons = `
                <div class="alert alert-warning py-2 mb-2 text-center">
                    <i class="fa fa-undo mr-1"></i>
                    Returned for Employee Correction
                </div>
            `;
                        break;

                        //-------------------------------------------------
                        // Approved
                        //-------------------------------------------------

                    case "Approved":

                        actionButtons = `
                <div class="alert alert-success py-2 mb-2 text-center">
                    <i class="fa fa-check-circle mr-1"></i>
                    Expense Approved
                </div>

                ${
                                    isAuditor
                                    ?
                                    `
                <div class="btn-group d-flex">
                        <button
                            type="button"
                            class="btn btn-danger btn-sm flex-fill"
                            onclick="updateExpense(${exp.ExpenseId},'REJECT',this)">
                            <i class="fa fa-ban mr-1"></i>
                            Reject
                        </button>
                </div>
                        `
                                    :
                                    ''
                        }
            `;
                        break;

                        //-------------------------------------------------
                        // Permanently Rejected
                        //-------------------------------------------------

                    case "Rejected":

                        actionButtons = `
                            <div class="alert alert-danger text-center mb-0">
                                <i class="fa fa-times-circle mr-1"></i>
                                Permanently Rejected
                            </div>
                        `;
                        break;

                    default:

                        actionButtons = `
                        <div class="text-center">
                            <span class="badge badge-secondary p-2">
                                ${status}
                            </span>
                        </div>
                    `;
                }

            }
            else {

                let badgeClass = "secondary";

                switch(status){

                    case "Approved":
                        badgeClass="success";
                        break;

                    case "Returned":
                        badgeClass="warning";
                        break;

                    case "Rejected":
                        badgeClass="danger";
                        break;

                    default:
                        badgeClass="secondary";
                }

                actionButtons = `
                    <div class="text-center">
                        <span class="badge badge-${badgeClass} p-2 px-3">
                            ${status}
                        </span>
                    </div>
                `;
            }

            return `
            <div class="col-md-6 col-xl-4 mb-3">
                <div class="card expense-card h-100 border-0 shadow-sm" style="border-radius: 8px;">
                    <div class="card-header ${headerColorClass} border-0" style="border-top-left-radius:8px;border-top-right-radius:8px;">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <div class="font-weight-bold text-uppercase">
                                    <i class="fa ${iconClass} mr-2"></i>
                                    ${title}
                                </div>

                                <small class="d-block mt-1">
                                    Status :
                                    <span class="badge badge-light text-dark">
                                        ${status}
                                    </span>
                                </small>
                            </div>

                            <div class="text-right">
                                <div class="h5 mb-0 font-weight-bold">
                                    ₹ ${amount}
                                </div>
                            </div>
                        </div>

                    </div>
                    <div class="card-body p-3 bg-white d-flex flex-column" style="font-size: 0.85rem;">
                        <div class="mb-2 d-flex justify-content-between align-items-center">
                            <span class="text-muted font-weight-bold text-uppercase" style="font-size: 0.7rem;">Bill No:</span>
                            <span class="badge badge-light border text-dark">${billNo}</span>
                        </div>
                        <div class="mb-2 d-flex justify-content-between align-items-center">
                            <span class="text-muted font-weight-bold text-uppercase"
                                  style="font-size:0.7rem;">
                                Expense Date
                            </span>

                            <strong class="text-dark">
                                ${exp.ExpenseDate || '-'}
                            </strong>
                        </div>
                        <div class="mb-3 flex-grow-1">
                            <span class="text-muted font-weight-bold text-uppercase d-block mb-1"
                                  style="font-size:0.7rem;">
                                Purpose
                            </span>
                            <div class="border rounded bg-light p-2" style="min-height:60px;">
                                <span class="text-dark">${remarks}</span>
                            </div>
                        </div>
                        <hr class="my-3">
                        <div class="border-top pt-3 mt-auto">
                            <div class="text-muted text-uppercase font-weight-bold mb-2"
                                 style="font-size:0.70rem;">
                                <i class="fa fa-paperclip mr-1"></i>
                                Receipt Verification
                            </div>${attachmentHtml}</div>
                    </div>
                    <div class="card-footer bg-white border-top p-3" style="border-bottom-left-radius: 8px; border-bottom-right-radius: 8px; min-height: 48px;">
                        ${actionButtons}
                    </div>
                </div>
            </div>`;
        }

        // =========================================================
        // 3. EXPENSE / KM AJAX ACTIONS
        // =========================================================
        function updateExpense(expenseId, action, btn) {

            var remark = "";

            switch (action) {

                case "APPROVE":

                    if (!confirm("Approve this expense?"))
                        return;

                    break;

                case "RETURN":

                    remark = prompt("Reason for returning this expense:");

                    if (remark === null)
                        return;

                    if ($.trim(remark) === "") {
                        alert("Return reason is mandatory.");
                        return;
                    }

                    break;

                case "REJECT":

                    remark = prompt("Reason for rejecting this expense:");

                    if (remark === null)
                        return;

                    if ($.trim(remark) === "") {
                        alert("Reject reason is mandatory.");
                        return;
                    }

                    break;
            }

            $(btn).prop("disabled", true);

            $.ajax({
                type: "POST",
                url: "ClaimsApproval_v2.aspx/UpdateIndividualExpense",
                data: JSON.stringify({
                    expenseId: expenseId,
                    action: action,
                    remark: remark
                }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",

                success: function (r) {
                    if (r.d && r.d.success) {
                        document.getElementById("btnRefreshCalculations").click();
                    }
                    else {
                        alert(r.d.message || "Unable to update expense.");
                        $(btn).prop("disabled", false);
                    }
                },
                error: function () {
                    alert("Server error.");
                    $(btn).prop("disabled", false);
                }
            });

        }

        function saveKM() {
            var $saveBtn = $('#km_modal_save');
            $saveBtn.prop('disabled', true);
            $('#km_modal_message').empty();

            var id = $('#km_modal_activityid').val();
            var km = $('#km_modal_new').val();
            var reason = $('#km_modal_reason').val();
            var curKm = $('#km_modal_current').val();

            if(!km || !reason) { 
                $('#km_modal_message').html('<div class="text-danger font-weight-bold">⚠️ Both New KM and Reason are strictly required.</div>');
                $saveBtn.prop('disabled', false);
                return; 
            }
            if(parseFloat(km) === parseFloat(curKm)) {
                $('#km_modal_message').html('<div class="text-warning font-weight-bold">⚠️ New KM must be different from Current KM.</div>');
                $saveBtn.prop('disabled', false);
                return;
            }

            $.ajax({
                type: "POST",
                url: "ClaimsApproval_v2.aspx/UpdateActivityKM",
                data: JSON.stringify({ activityId: parseInt(id), newKm: parseFloat(km), reason: reason }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(r) {
                    if(r.d && r.d.success) {
                        $('#km_modal_message').html('<div class="alert alert-success py-2 mt-2 mb-0 font-weight-bold"><i class="fa fa-check-circle"></i> KM modified successfully!</div>');
                        $('#km_modal_current').val(km);
                        $('#km_modal_reason').val('');
                        loadKmHistory(id);
                        document.getElementById('btnRefreshCalculations').click();
                    
                        setTimeout(function() { 
                            $('#km_modal_message').fadeOut('fast', function() { $(this).empty().show(); }); 
                        }, 4000);
                    } else {
                        $('#km_modal_message').html('<div class="text-danger font-weight-bold">Error: ' + r.d.message + '</div>');
                    }
                },
                complete: function() {
                    $saveBtn.prop('disabled', false);
                }
            });
        }

        function loadKmHistory(activityId) {
            $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Loading...</td></tr>');
 
            $.ajax({
                type: 'POST',
                url: 'ClaimsApproval_v2.aspx/GetKMHistory', 
                data: JSON.stringify({ activityId: activityId }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json'
            }).done(function (resp) {
                var res = resp.d;
                if (!res || !res.success) {
                    $('#km_history_tbl tbody').empty().append('<tr><td colspan="5" class="text-danger">Error: ' + (res ? res.message : 'Unknown error') + '</td></tr>');
                    return;
                }

                var fPlace = res.fromPlace || '';
                var tPlace = res.toPlace || '';
 
                $('#km_modal_fromplace').text(fPlace || 'No data available');
                $('#km_modal_toplace').text(tPlace || 'No data available');

                if (fPlace && tPlace) {
                    var mapsUrl = 'https://www.google.com/maps/dir/?api=1&origin=' + encodeURIComponent(fPlace) + '&destination=' + encodeURIComponent(tPlace) + '&travelmode=driving';
                    $('#km_modal_maplink').attr('href', mapsUrl).show();
                } else {
                    $('#km_modal_maplink').attr('href', '#').hide();
                }

                var $tbody = $('#km_history_tbl tbody').empty();
                if (!res.history || res.history.length === 0) {
                    $tbody.append('<tr><td colspan="5" class="text-muted text-center font-italic">No previous KM modifications found.</td></tr>');
                    return;
                }

                res.history.forEach(function (r) {
                    var oldKm = r.OldKM != null ? parseFloat(r.OldKM).toFixed(2) : '';
                    var newKm = r.NewKM != null ? parseFloat(r.NewKM).toFixed(2) : '';
                    $tbody.append(
                        '<tr>' +
                        '<td>' + (r.ModifiedDateTimeIST || '') + '</td>' +
                        '<td class="text-right">' + oldKm + '</td>' +
                        '<td class="text-right">' + newKm + '</td>' +
                        '<td>' + (r.ModifiedBy || '') + '</td>' +
                        '<td>' + (r.Reason || '') + '</td>' +
                        '</tr>'
                    );
                });
            }).fail(function (xhr, status, err) {
                $('#km_history_tbl tbody').empty().append('<tr><td colspan="5" class="text-danger">Error communicating with server.</td></tr>');
            });
        }

        // =========================================================
        // 4. ACTION ENTIRE DAY FROM FOOTER
        // =========================================================
        function actionDayFromModal(action) {

            var detailId = $('#km_modal_detailid').val();
            var reqId = $('#<%= hfActionRequestId.ClientID %>').val();

            var actionText = "";

            switch (action) {

                case "APPROVE":
                    actionText = "approve this day";
                    break;

                case "RETURN":
                    actionText = "return this day to the employee for correction";
                    break;

                case "REJECT":
                    actionText = "permanently reject this day";
                    break;

                default:
                    alert("Invalid action.");
                    return;
            }

            if (action === 'REJECT') {
                if (window.ClaimsUx && ClaimsUx.showConfirm) {
                    ClaimsUx.showConfirm({
                        title: 'Reject this Day?',
                        message: 'The employee cannot modify rejected claims.',
                        confirmText: 'Reject',
                        confirmClass: 'btn-danger',
                        onConfirm: function () { executeDayAction(action, detailId, reqId); }
                    });
                } else if (confirm('Reject this Day?\n\nThe employee cannot modify rejected claims.')) {
                    executeDayAction(action, detailId, reqId);
                }
                return;
            }
            if (action === 'RETURN') {
                if (window.ClaimsUx && ClaimsUx.showConfirm) {
                    ClaimsUx.showConfirm({
                        title: 'Return this Day?',
                        message: 'The employee will be allowed to edit only Returned items.',
                        confirmText: 'Return',
                        confirmClass: 'btn-warning',
                        onConfirm: function () { executeDayAction(action, detailId, reqId); }
                    });
                } else if (confirm('Return this Day?\n\nThe employee will be allowed to edit only Returned items.')) {
                    executeDayAction(action, detailId, reqId);
                }
                return;
            }

            executeDayAction(action, detailId, reqId);
        }

        function executeDayAction(action, detailId, reqId) {
            $('#btnUnifiedApproveDay,#btnUnifiedReturnDay,#btnUnifiedRejectDay')
                .prop('disabled', true);

            if (window.ClaimsUx) {
                ClaimsUx.clearDirty();
                ClaimsUx.showProcessing('Processing day action...');
            }

            $('#unified_modal_message').html(
                '<span class="text-info">' +
                '<i class="fa fa-spinner fa-spin mr-1"></i>' +
                'Processing...' +
                '</span>'
            );

            $.ajax({

                type: "POST",

                url: "ClaimsApproval_v2.aspx/ActionDayRowSilent",

                data: JSON.stringify({

                    detailId: parseInt(detailId),

                    reqId: parseInt(reqId),

                    action: action

                }),

                contentType: "application/json; charset=utf-8",

                dataType: "json",

                success: function (r) {
                    if (window.ClaimsUx) ClaimsUx.hideProcessing();

                    if (r.d && r.d.success) {

                        $('#unifiedDayModal').modal('hide');

                        document.getElementById('btnRefreshCalculations').click();

                        return;
                    }

                    $('#unified_modal_message').html(

                        '<span class="text-danger font-weight-bold">' +

                        (r.d ? r.d.message : 'Unable to process request.') +

                        '</span>'
                    );

                    $('#btnUnifiedApproveDay,#btnUnifiedReturnDay,#btnUnifiedRejectDay')
                        .prop('disabled', false);

                },

                error: function () {
                    if (window.ClaimsUx) ClaimsUx.hideProcessing();

                    $('#unified_modal_message').html(

                        '<span class="text-danger font-weight-bold">' +

                        'Server error occurred.' +

                        '</span>'
                    );

                    $('#btnUnifiedApproveDay,#btnUnifiedReturnDay,#btnUnifiedRejectDay')
                        .prop('disabled', false);

                }

            });

        }

        // =========================================================
        // 5. UI HELPERS & DASHBOARD ACTIONS
        // =========================================================
        function openDetailsModal() { $('#detailsModal').modal('show'); }

        function showDeductionsBreakdown() {
            var reqId = document.getElementById('<%= hfActionRequestId.ClientID %>').value;
            if (!reqId) return;

            $('#tblDeductions tbody').html('<tr><td colspan="4" class="text-center">Loading details...</td></tr>');
            $('#lblDeductionsModalTotal').text('0.00');
            $('#deductionsModal').modal('show');

            $.ajax({
                type: 'POST',
                url: 'ClaimsApproval_v2.aspx/GetDeductionBreakdown',
                data: JSON.stringify({ approvalRequestId: parseInt(reqId) }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (resp) {
                    var responseObj = resp.d; 
                    var tbody = $('#tblDeductions tbody').empty();
                    var total = 0;

                    if (!responseObj || responseObj.success === false) {
                        var errMsg = responseObj ? responseObj.message : "Unknown error";
                        tbody.html('<tr><td colspan="4" class="text-center text-danger">Error: ' + errMsg + '</td></tr>');
                        return;
                    }

                    var data = responseObj.data;
                    if (!data || data.length === 0) {
                        tbody.html('<tr><td colspan="4" class="text-center text-muted">No deductions found.</td></tr>');
                        return;
                    }

                    data.forEach(function (item) {
                        total += item.DeductedAmount;
                        tbody.append(
                            '<tr>' +
                            '<td>' + item.ActivityDate + '</td>' +
                            '<td><strong>' + item.DeductionType + '</strong></td>' +
                            '<td>' + (item.Reason || '-') + '</td>' +
                            '<td class="text-right text-danger font-weight-bold">' + item.DeductedAmount.toFixed(2) + '</td>' +
                            '</tr>'
                        );
                    });

                    $('#lblDeductionsModalTotal').text(total.toFixed(2));
                },
                error: function () {
                    $('#tblDeductions tbody').html('<tr><td colspan="4" class="text-center text-danger">Error loading data.</td></tr>');
                }
            });
        }

        function promptForReturn() {
            var doReturn = function () {
                var reason = prompt('Please enter the reason for returning this ENTIRE claim to the employee:');
                if (reason != null && reason.trim() !== '') {
                    document.getElementById('hfReturnReason').value = reason;
                    if (window.ClaimsUx) {
                        ClaimsUx.armProcessingForPostback('Returning claim to employee...');
                        ClaimsUx.clearDirty();
                    }
                    var btn = document.getElementById('<%= btnReturnToEmployee.ClientID %>');
                    if (btn) {
                        if (window.ClaimsUx) {
                            $(btn).data('ux-skip-confirm', true);
                            $(btn).data('ux-locked', true);
                        }
                        setTimeout(function () { __doPostBack(btn.name, ''); }, 0);
                    }
                } else if (reason != null) {
                    alert('A reason is required to return the claim.');
                }
            };

            if (window.ClaimsUx && ClaimsUx.showConfirm) {
                ClaimsUx.showConfirm({
                    title: 'Return this claim to the employee?',
                    message: 'The employee will be allowed to edit only Returned items.<br><br>After confirming, you will be asked for a return reason.',
                    confirmText: 'Return',
                    confirmClass: 'btn-danger',
                    onConfirm: doReturn
                });
            } else if (confirm('Return this claim to the employee?\n\nThe employee will be allowed to edit only Returned items.')) {
                doReturn();
            }
            return false;
        }

        function showApprovalSummaryConfirm(actionType) {
            var daysApproved = 0, daysReturned = 0, daysRejected = 0, daysPending = 0;
            $('#gvDetails tbody tr').each(function () {
                var $row = $(this);
                if ($row.find('td').length < 3) return;
                if ($row.find('.claims-badge-returned').length) { daysReturned++; return; }
                var cls = $row.attr('class') || '';
                if (cls.indexOf('table-success') >= 0) daysApproved++;
                else if (cls.indexOf('table-danger') >= 0) daysRejected++;
                else daysPending++;
            });

            var deductions = $('#<%= lblSummaryDeduction.ClientID %>').text() || '0.00';
            var deductedKm = $('#<%= lblSummaryDeductedKM.ClientID %>').text() || '0.00';
            var netPayable = $('#<%= lblSummaryApproved.ClientID %>').text() || '0.00';
            var netKm = $('#<%= lblSummaryApprovedKM.ClientID %>').text() || '0.00';

            var title = actionType === 'payment' ? 'Mark this request Ready For Payment?' : 'Forward this request?';
            var subtitle = actionType === 'payment'
                ? 'This completes the approval workflow.'
                : 'Proceed to the next approval level?';
            var confirmText = actionType === 'payment' ? 'Confirm' : 'Forward';

            var html = '<div class="text-left"><p class="mb-2">' + subtitle + '</p>' +
                '<table class="table table-sm table-bordered mb-0">' +
                '<tr><td>Days Approved</td><td class="text-right font-weight-bold">' + daysApproved + '</td></tr>' +
                '<tr><td>Days Returned</td><td class="text-right font-weight-bold">' + daysReturned + '</td></tr>' +
                '<tr><td>Days Rejected</td><td class="text-right font-weight-bold">' + daysRejected + '</td></tr>' +
                '<tr><td>Expense Deductions</td><td class="text-right text-danger">(-) ₹ ' + deductions + '</td></tr>' +
                '<tr><td>KM Reduced</td><td class="text-right text-danger">(-) ' + deductedKm + ' KM</td></tr>' +
                '<tr class="table-success"><td><strong>Final Payable</strong></td><td class="text-right"><strong>₹ ' + netPayable + '</strong><br><small>' + netKm + ' KM</small></td></tr>' +
                '</table></div>';

            if (window.ClaimsUx && ClaimsUx.showConfirm) {
                ClaimsUx.showConfirm({
                    title: title,
                    message: html,
                    confirmText: confirmText,
                    confirmClass: actionType === 'payment' ? 'btn-warning' : 'btn-success',
                    onConfirm: function () {
                        if (window.ClaimsUx) {
                            ClaimsUx.armProcessingForPostback(actionType === 'payment' ? 'Finalizing request...' : 'Forwarding request...');
                            ClaimsUx.clearDirty();
                        }
                        var btn = document.getElementById(actionType === 'payment'
                            ? '<%= btnMarkReadyForPayment.ClientID %>'
                            : '<%= btnForwardLevel.ClientID %>');
                        if (btn) {
                            $(btn).data('ux-skip-confirm', true);
                            $(btn).data('ux-locked', true);
                            setTimeout(function () { __doPostBack(btn.name, ''); }, 0);
                        }
                    }
                });
            } else if (confirm(title + '\n\nProceed?')) {
                var btn = document.getElementById(actionType === 'payment'
                    ? '<%= btnMarkReadyForPayment.ClientID %>'
                    : '<%= btnForwardLevel.ClientID %>');
                if (btn) __doPostBack(btn.name, '');
            }
            return false;
        }

        function armRecalculateProcessing(btn) {
            if (window.ClaimsUx) {
                ClaimsUx.armProcessingForPostback('Recalculating totals...');
                $(btn).data('ux-locked', true);
            }
            return true;
        }

        function showRecalculateSpinner(btn) {
            return armRecalculateProcessing(btn);
        }

        // =========================================================
        // 6. ASP.NET AJAX UPDATEPANEL HOOKS & DATATABLES
        // =========================================================
        var _gridScrollTop = 0;
    
        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            var prm = Sys.WebForms.PageRequestManager.getInstance();
        
            prm.add_beginRequest(function (sender, args) {
                var gridContainer = document.querySelector('#detailsModal .table-responsive');
                if (gridContainer) { _gridScrollTop = gridContainer.scrollTop; }
            });

            prm.add_endRequest(function (sender, args) {
                var gridContainer = document.querySelector('#detailsModal .table-responsive');
                if (gridContainer) { gridContainer.scrollTop = _gridScrollTop; }
                initDataTablesSafe();
                decorateReceiptCounts();
                decorateKmEditedBadges();
            });
        }

        function decorateReceiptCounts() {
            if (!window.ClaimsUx || !ClaimsUx.parseExpenseCounts) return;
            $('.btn-day-action').each(function () {
                var json = $(this).attr('data-expenses');
                var counts = ClaimsUx.parseExpenseCounts(json);
                var $hint = $(this).siblings('.receipt-count-hint');
                if (!$hint.length) {
                    $hint = $('<small class="receipt-count-hint"></small>');
                    $(this).after($hint);
                }
                if (counts.total > 0) {
                    $hint.html('Expenses (' + counts.total + ')<br>Approved: ' + counts.approved + ' | Pending: ' + counts.pending);
                } else {
                    $hint.html('No expenses');
                }
            });
        }

        function decorateKmEditedBadges() {
            $('.km-value[data-activityid]').each(function () {
                var actId = $(this).data('activityid');
                var $badge = $(this).siblings('.km-edited-badge');
                if (!$badge.length) return;
                $.ajax({
                    type: 'POST',
                    url: 'ClaimsApproval_v2.aspx/GetKMHistory',
                    data: JSON.stringify({ activityId: parseInt(actId, 10) }),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json'
                }).done(function (resp) {
                    if (resp.d && resp.d.success && resp.d.history && resp.d.history.length > 0) {
                        $badge.show();
                    }
                });
            });
        }

        function applyDataTables() {
            var $grid = $('#gvRequests');
            if ($grid.length > 0 && $grid.find('tbody tr').length > 0 && typeof $.fn.dataTable !== 'undefined') {
                if ($.fn.dataTable.isDataTable('#gvRequests')) { $grid.DataTable().destroy(); }
                $grid.DataTable({
                    "pageLength": 15,                
                    "lengthMenu": [10, 15, 25, 50],  
                    "ordering": true,                
                    "searching": true,               
                    "responsive": true,              
                    "order": [[ 3, "desc" ]],        
                    "language": { "search": "Quick Filter:", "lengthMenu": "Show _MENU_ requests" },
                    "columnDefs": [
                        { "orderable": false, "targets": [7] }, 
                        { "responsivePriority": 1, "targets": [1, 4, 7] },
                        { "responsivePriority": 2, "targets": [0, 6] },
                        { "responsivePriority": 3, "targets": [2, 3, 5] }
                    ]
                });
            } else if (typeof $.fn.dataTable === 'undefined') {
                console.warn("DataTables library is not loaded yet.");
            }
        }

        function initDataTablesSafe() {
            if (typeof $.fn.dataTable !== 'undefined') {
                applyDataTables();
            } else {
                setTimeout(initDataTablesSafe, 100);
            }
        }

        function initClaimsApprovalPageUx() {
            if (window.ClaimsUx && ClaimsUx.initOnce) {
                ClaimsUx.initOnce({
                    dirtySelector: '.claims-ux-track-dirty, #km_modal_new, #km_modal_reason, #txtBulkRemarks'
                });
            }
            initDataTablesSafe();
            decorateReceiptCounts();
            decorateKmEditedBadges();
        }

        if (typeof Sys !== 'undefined' && Sys.Application) {
            Sys.Application.add_load(function () {
                initClaimsApprovalPageUx();
            });
        } else {
            $(document).ready(function () {
                initClaimsApprovalPageUx();
            });
        }

        var reopenDetailsModal = false;
        $('#unifiedDayModal').on('hidden.bs.modal', function () {

            if (reopenDetailsModal) {
                reopenDetailsModal = false;
                $('#detailsModal').modal('show');
            }

        });
    </script>
</asp:Content>
