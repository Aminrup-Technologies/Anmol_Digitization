<%@ Page Title="AIL | Claims Approval V2" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="ClaimsApproval_v2.aspx.cs" Inherits="AnmolDristi.ClaimsApproval_v2" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
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

        /* Status Badges */
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

        /* Modal Dashboard Styles */
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

        /* Row Highlighting for Handover */
        .table-warning {
            background-color: #fff3cd !important;
        }

        .table-success {
            background-color: #d4edda !important;
        }

        .table-danger {
            background-color: #f8d7da !important;
        }

        /* Sticky Header */
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

        /* =========================================================
           MODAL & STICKY GRID LOGIC (PERFECT SCROLLING)
        ========================================================= */

        /* 1. Remove scroll from the outer modal body to prevent double scrollbars */
        #detailsModal .modal-body {
            padding: 16px 18px;
            background: #ffffff;
            /* Note: removed overflow:auto and max-height from here */
        }

        /* 2. The Table Container becomes the ONLY scrolling element */
        #detailsModal .table-responsive {
            max-height: 55vh; /* Adjusts dynamically to screen */
            overflow-y: auto;
            overflow-x: auto;
            margin-bottom: 15px; /* Adds breathing room above bulk actions */
            border-bottom: 1px solid #dee2e6; /* Clean edge */
        }

        /* 3. Sticky Header (Top) */
        #detailsModal .table thead th {
            position: sticky;
            top: 0;
            z-index: 1020;
            background-color: #f8f9fa !important;
            border-bottom: 2px solid #dee2e6;
            box-shadow: 0 2px 2px -1px rgba(0, 0, 0, 0.1);
        }

        /* 4. Sticky Footer (Bottom Totals) */
        #detailsModal .table tfoot td {
            position: sticky;
            bottom: 0;
            z-index: 1020;
            background-color: #f8f9fa !important;
            border-top: 2px solid #dee2e6 !important;
            box-shadow: 0 -2px 4px -1px rgba(0, 0, 0, 0.08);
        }

        /* Ensure hover effects don't mess up the sticky header/footer colors */
        #detailsModal .table tfoot tr:hover td {
            background-color: #f8f9fa !important;
        }

        /* Gatekeeper Warning Animation */
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
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hfActionRequestId" runat="server" />

    <div class="right_col" role="main">
        <div class="container-fluid mt-3">
            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel shadow-sm rounded-2">
                        <div class="x_title d-flex justify-content-between align-items-center bg-primary text-white p-2 rounded-top">
                            <h4 class="mb-0"><i class="fa fa-check-circle"></i>Monthly Claims Approval (V2)</h4>
                        </div>
                        <div class="x_content bg-light p-3 rounded-bottom">
                            <div class="card-box table-responsive">
                                <asp:Panel ID="pnlRequests" runat="server">
                                    <asp:GridView ID="gvRequests" runat="server" CssClass="table table-sm table-striped table-hover table-bordered gentilia-table"
                                        ShowHeaderWhenEmpty="true" AutoGenerateColumns="false" AllowPaging="true" PageSize="20"
                                        OnRowCommand="gvRequests_RowCommand"
                                        DataKeyNames="ApprovalRequestId" GridLines="None">
                                        <HeaderStyle CssClass="gentilia-table-header" />
                                        <RowStyle CssClass="gentilia-row" />
                                        <AlternatingRowStyle CssClass="gentilia-row-alt" />
                                        <Columns>
                                            <asp:BoundField DataField="ApprovalRequestId" HeaderText="ID" HeaderStyle-Width="50px" />
                                            <asp:TemplateField HeaderText="Employee Name" HeaderStyle-Width="220px">
                                                <ItemTemplate><%# Eval("EmployeeName") %> (<%# Eval("RequesterLoginId") %>)</ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="RequestDate" HeaderText="Requested Date" DataFormatString="{0:dd-MMM-yyyy}" />
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
                                                    <span class="badge badge-info"><%# Eval("RequestStatus") %></span>
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
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="detailsModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true" runat="server" clientidmode="Static">
            <div class="modal-dialog modal-xl" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-light">
                        <h5 class="modal-title font-weight-bold">Request #<asp:Label ID="lblModalRequestId" runat="server" Text="0" /></h5>
                        <div class="ml-auto">
                            <asp:Label ID="lblRequesterName" runat="server" Font-Bold="true" />
                        </div>
                        <button type="button" class="close ml-3" data-dismiss="modal"><span>&times;</span></button>
                    </div>

                    <div class="modal-body">
                        <asp:UpdatePanel ID="upDetails" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>
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
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Date">
                                                <ItemTemplate>
                                                    <%# Eval("ActivityDate", "{0:dd-MMM-yyyy}") %><br />
                                                    <small class="text-muted"><%# Eval("Day") %></small>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Distance (KM)">
                                                <ItemTemplate>
                                                    <span class="km-value font-weight-bold" data-activityid='<%# Eval("ActivityId") %>'>
                                                        <%# Eval("Snapshot_TotalDistanceKM", "{0:N2}") %>
                                                    </span>
                                                    <button type="button" runat="server" id="btnEditKM" class="btn btn-sm btn-outline-primary ml-2 btn-edit-km" title="Modify KM">
                                                        <i class="fa fa-pencil-square-o"></i>Edit
                                                    </button>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="Snapshot_CalculatedClaimAmount" HeaderText="TA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_DAAmount" HeaderText="DA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_HotelAmount" HeaderText="Hotel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_TravelAmount" HeaderText="Travel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal" HeaderText="Total (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />

                                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:TextBox ID="txtRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' CssClass="form-control form-control-sm" />
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Details" ItemStyle-HorizontalAlign="Center">
                                                <ItemTemplate>
                                                    <button type="button" runat="server" id="btnExp" class="btn btn-sm btn-info text-white btn-expenses" data-expenses='<%# Eval("ExpensesJson") %>' title="View Receipts">
                                                        <i class="fa fa-list-alt"></i>View
                                                    </button>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Action" HeaderStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Button ID="btnApproveRow" runat="server" CssClass="btn btn-sm btn-outline-success" Text="✔" CommandName="ApproveRow" CommandArgument='<%# Eval("ApprovalRequestDetailId") %>' />
                                                    <asp:Button ID="btnRejectRow" runat="server" CssClass="btn btn-sm btn-outline-danger" Text="✖" CommandName="RejectRow" CommandArgument='<%# Eval("ApprovalRequestDetailId") %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>

                                <div class="row mt-2 align-items-center">
                                    <div class="col-md-6">
                                        <asp:TextBox ID="txtBulkRemarks" runat="server" CssClass="form-control form-control-sm" Placeholder="Bulk remarks for selected items..." />
                                    </div>
                                    <div class="col-md-6 text-right">
                                        <asp:Button ID="btnApproveSelected" runat="server" Text="Approve Selected" CssClass="btn btn-success btn-sm" OnClick="btnApproveSelected_Click" />
                                        <asp:Button ID="btnRejectSelected" runat="server" Text="Reject Selected" CssClass="btn btn-danger btn-sm" OnClick="btnRejectSelected_Click" />
                                    </div>
                                </div>

                                <asp:Button ID="btnRefreshCalculations" runat="server" OnClick="btnRefreshCalculations_Click" ClientIDMode="Static" Style="display: none;" />
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>

                    <%--<div class="modal-footer bg-light d-flex justify-content-between align-items-center">
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

                        <div class="ml-4 text-right">
                            <asp:Label ID="lblModalMessage" runat="server" CssClass="text-danger font-weight-bold d-block mb-1" />
                            <asp:Button ID="btnRecalculate" runat="server" Text="↻ Re-Calculate" CssClass="btn btn-info btn-sm mr-2" OnClick="btnRecalculate_Click" ToolTip="Recalculate approved amounts for all categories" />
                            <asp:Button ID="btnMarkReadyForPayment" runat="server" Text="✔ Mark Ready for Payment" CssClass="btn btn-warning btn-sm mr-2" OnClick="btnMarkReadyForPayment_Click" Visible="false" />
                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                        </div>
                    </div>--%>

                    <div class="modal-footer bg-light d-flex justify-content-between align-items-end" style="border-top: 2px solid #dee2e6;">

                        <div class="d-flex" style="gap: 15px;">
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

                        <div class="text-right">
                            <asp:Label ID="lblModalMessage" runat="server" CssClass="d-block mb-2 font-weight-bold" />

                            <div>
                                <asp:Button ID="btnRecalculate" runat="server" Text="↻ Re-Calculate" CssClass="btn btn-info btn-sm m-0" OnClick="btnRecalculate_Click" ToolTip="Recalculate approved amounts" />

                                <asp:Button ID="btnForwardLevel" runat="server" Text="✔ Forward to Next Level" CssClass="btn btn-success btn-sm m-0 ml-2" OnClick="btnForwardLevel_Click" Visible="false" />

                                <asp:Button ID="btnMarkReadyForPayment" runat="server" Text="✔ Mark Ready for Payment" CssClass="btn btn-warning btn-sm m-0 ml-2" OnClick="btnMarkReadyForPayment_Click" Visible="false" />

                                <button type="button" class="btn btn-secondary btn-sm m-0 ml-2" data-dismiss="modal">Close</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <input type="hidden" id="km_modal_activityid_outer" />

        <div id="kmEditModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title">Edit Distance (KM)</h5>
                        <button type="button" class="close text-white" data-dismiss="modal">×</button>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" id="km_modal_activityid" />

                        <div class="alert alert-light border mb-3" id="km_modal_travel_ref">
                            <div class="row align-items-center">
                                <div class="col-md-4">
                                    <strong>From:</strong>
                                    <span id="km_modal_fromplace" class="text-primary"></span>
                                </div>
                                <div class="col-md-4">
                                    <strong>To:</strong>
                                    <span id="km_modal_toplace" class="text-primary"></span>
                                </div>
                                <div class="col-md-4 text-right">
                                    <a href="#" id="km_modal_maplink" target="_blank" class="btn btn-sm btn-outline-primary" style="display: none;">
                                        <i class="fa fa-map-marker"></i>View Distance
                                    </a>
                                </div>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4">
                                <div class="form-group">
                                    <label class="font-weight-bold">Current KM</label>
                                    <input type="text" id="km_modal_current" class="form-control bg-light" readonly />
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="form-group">
                                    <label class="font-weight-bold">New KM</label>
                                    <input type="number" step="0.01" id="km_modal_new" class="form-control" />
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="form-group">
                                    <label class="font-weight-bold">Reason (when KM changes)</label>
                                    <textarea id="km_modal_reason" class="form-control" rows="2" placeholder="Enter reason"></textarea>
                                </div>
                            </div>
                        </div>

                        <hr />

                        <h5 class="text-secondary mb-3"><i class="fa fa-history"></i>KM Change History</h5>
                        <div class="table-responsive" style="max-height: 250px; overflow-y: auto; border: 1px solid #ddd; border-radius: 5px;">
                            <table class="table table-striped table-hover table-sm" id="km_history_tbl">
                                <thead class="thead-dark">
                                    <tr>
                                        <th>Date (IST)</th>
                                        <th>Old KM</th>
                                        <th>New KM</th>
                                        <th>Changed By</th>
                                        <th>Reason</th>
                                    </tr>
                                </thead>
                                <tbody></tbody>
                            </table>
                        </div>

                        <div id="km_modal_message" class="mt-2"></div>
                    </div>

                    <div class="modal-footer bg-light">
                        <button type="button" id="km_modal_save" class="btn btn-success" onclick="saveKM()"><i class="fa fa-save"></i>Save</button>
                        <button type="button" class="btn btn-secondary" data-dismiss="modal"><i class="fa fa-times"></i>Cancel</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="expensesModal" class="modal fade" tabindex="-1" role="dialog">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Expense Receipts</h5>
                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                    </div>
                    <div class="modal-body" id="expensesModalBody">Loading...</div>
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
                                <tbody>
                                </tbody>
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

    <script type="text/javascript">
        // Global refresh flag
        var _refreshNeeded = false;
        var _currentUserLevel = <%= ViewState["CurrentUserLevel"] ?? 0 %>;

        function selectAllRows(chk) {
            $('.row-select-checkbox input').prop('checked', chk.checked);
        }

        // --- EXPENSE HANDLING ---
        $(document).on('click', '.btn-expenses', function() {
            var json = $(this).attr('data-expenses'); // Use attr to ensure raw JSON string is grabbed
            if (json) {
                var data = JSON.parse(json);
                renderExpenses(data);
            }
        });

        function renderExpenses(data) {
            if(!data || data.length === 0) {
                $('#expensesModalBody').html('<div class="alert alert-warning">No receipts attached for this day.</div>');
                $('#expensesModal').modal('show');
                return;
            }

            // Added Date and Purpose columns
            var html = '<table class="table table-bordered table-striped table-hover"><thead><tr class="bg-light">' +
                       '<th>Date</th><th>Type</th><th>Purpose</th><th>Bill No</th><th class="text-right">Amount</th><th class="text-center">Action</th></tr></thead><tbody>';
    
            var isAuditor = (_currentUserLevel === 2);

            $.each(data, function(i, item) {
                // Safely format the date
                var expDate = item.ExpenseDate ? new Date(item.ExpenseDate).toLocaleDateString('en-GB', {day: '2-digit', month: 'short', year: 'numeric'}) : '-';
        
                var statusBadge = '';
                if(item.ClaimStatus === 'Approved') {
                    statusBadge = '<span class="badge badge-success px-2 py-1"><i class="fa fa-check"></i> Approved</span>';
                }
                else if(item.ClaimStatus === 'Rejected') {
                    statusBadge = '<span class="badge badge-danger px-2 py-1"><i class="fa fa-times"></i> Rejected</span>';
                }
                else {
                    // Here are your Action buttons!
                    if(isAuditor) {
                        statusBadge = '<button type="button" class="btn btn-sm btn-success mr-1" onclick="updateExpense(' + item.ExpenseId + ',\'Approved\', this)" title="Approve"><i class="fa fa-check"></i></button>' +
                                      '<button type="button" class="btn btn-sm btn-danger" onclick="updateExpense(' + item.ExpenseId + ',\'Rejected\', this)" title="Reject"><i class="fa fa-times"></i></button>';
                    } else {
                        statusBadge = '<span class="badge badge-warning text-dark px-2 py-1">Pending</span>';
                    }
                }
        
                // Add paperclip icon to bill link
                var billLink = item.AttachmentPath ? '<a href="' + item.AttachmentPath + '" target="_blank" class="text-primary font-weight-bold"><i class="fa fa-paperclip"></i> ' + (item.BillNo || 'View Bill') + '</a>' : (item.BillNo || '-');

                html += '<tr>' +
                        '<td>' + expDate + '</td>' +
                        '<td><strong>' + (item.ExpenseType || '-') + '</strong></td>' +
                        '<td><small>' + (item.Purpose || '-') + '</small></td>' +
                        '<td>' + billLink + '</td>' +
                        '<td class="text-right font-weight-bold text-dark">₹ ' + parseFloat(item.Amount).toFixed(2) + '</td>' +
                        '<td class="text-center">' + statusBadge + '</td>' +
                        '</tr>';
            });
            html += '</tbody></table>';
            $('#expensesModalBody').html(html);
            $('#expensesModal').modal('show');
        }

        // --- KM EDITING ---
        // Updated delegation to ensure it finds the correct Activity ID
        $(document).on('click', '.btn-edit-km', function(e) {
            e.preventDefault();
            // Traverse back up to the TD, then find the span with the ID
            var actId = $(this).closest('td').find('.km-value').data('activityid');
            $('#km_activity_id').val(actId);
            $('#km_new_value').val('');
            $('#km_reason').val('');
            $('#kmEditModal').modal('show');
        });

        function updateExpense(id, status, btn) {
            if(!confirm('Mark this receipt as ' + status + '?')) return;
            $(btn).prop('disabled', true);

            $.ajax({
                type: "POST",
                url: "ClaimsApproval_v2.aspx/UpdateIndividualExpense",
                data: JSON.stringify({ expenseId: id, status: status }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function(r) {
                    if(r.d && r.d.success) {
                        var badge = (status === 'Approved') ? '<span class="badge badge-success">Approved</span>' : '<span class="badge badge-danger">Rejected</span>';
                        $(btn).parent().html(badge);
                        _refreshNeeded = true; // MARK DIRTY
                    } else {
                        alert(r.d.message);
                    }
                }
            });
        }

        $('#expensesModal').on('hidden.bs.modal', function () {
            if (_refreshNeeded) {
                _refreshNeeded = false;
                // Trigger Server Refresh
                document.getElementById('btnRefreshCalculations').click();
            }
        });

        function loadKmHistory(activityId) {
            $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Loading...</td></tr>');
    
            $.ajax({
                type: 'POST',
                url: 'ClaimsApproval_v2.aspx/GetKMHistory', // 🔑 FIXED URL to V2
                data: JSON.stringify({ activityId: activityId }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json'
            }).done(function (resp) {
                var res = resp.d;
        
                if (!res || !res.success) {
                    $('#km_history_tbl tbody').empty().append('<tr><td colspan="5" class="text-danger">Error: ' + (res ? res.message : 'Unknown error') + '</td></tr>');
                    return;
                }

                // 1. Populate 'From' and 'To' Reference safely
                var fPlace = res.fromPlace || '';
                var tPlace = res.toPlace || '';
        
                $('#km_modal_fromplace').text(fPlace || 'No data available');
                $('#km_modal_toplace').text(tPlace || 'No data available');

                // Generate Maps Link (✅ FIXED URL FORMAT)
                if (fPlace && tPlace) {
                    var mapsUrl = 'https://www.google.com/maps/dir/?api=1&origin=' + encodeURIComponent(fPlace) + '&destination=' + encodeURIComponent(tPlace) + '&travelmode=driving';
                    $('#km_modal_maplink').attr('href', mapsUrl).show();
                } else {
                    $('#km_modal_maplink').attr('href', '#').hide();
                }

                // 2. Populate History Table
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
                        '<td>' + oldKm + '</td>' +
                        '<td>' + newKm + '</td>' +
                        '<td>' + (r.ModifiedBy || '') + '</td>' +
                        '<td>' + (r.Reason || '') + '</td>' +
                        '</tr>'
                    );
                });

            }).fail(function (xhr, status, err) {
                $('#km_history_tbl tbody').empty().append('<tr><td colspan="5" class="text-danger">Error communicating with server.</td></tr>');
            });
        }

        // Open Modal Event
        $(document).on('click', '.btn-edit-km', function(e) {
            e.preventDefault();
            var actId = $(this).closest('td').find('.km-value').data('activityid');
            var currentKm = $(this).closest('td').find('.km-value').text().trim();
            
            $('#km_modal_activityid').val(actId);
            $('#km_modal_current').val(currentKm);
            $('#km_modal_new').val(currentKm); // Pre-fill with current
            $('#km_modal_reason').val('');
            $('#km_modal_message').empty();
            
            // Trigger history load
            loadKmHistory(actId);
            
            $('#kmEditModal').modal('show');
        });

        // Ensure your save function is also pointing to V2!
        function onSaveKmClickHandler() {
            var $saveBtn = $(this);
            $saveBtn.prop('disabled', true);
            $('#km_modal_message').empty();

            var activityId = parseInt($('#km_modal_activityid').val(), 10) || window.__kmCurrentActivityId;
            var newKmRaw = $('#km_modal_new').val();
            var newKm = parseFloat(newKmRaw);
            var curKm = parseFloat($('#km_modal_current').val());
            var reason = ($('#km_modal_reason').val() || '').trim();

            if (!activityId || isNaN(activityId)) {
                $('#km_modal_message').html('<div class="text-danger">Invalid activity selected.</div>');
                $saveBtn.prop('disabled', false); return;
            }
            if (isNaN(newKm)) {
                $('#km_modal_message').html('<div class="text-danger">Enter a valid New KM value.</div>');
                $saveBtn.prop('disabled', false); return;
            }
            if (Math.abs(newKm - curKm) > 0.001 && reason.length === 0) {
                $('#km_modal_message').html('<div class="text-danger">Reason is required when KM is changed.</div>');
                $saveBtn.prop('disabled', false); return;
            }

            $.ajax({
                type: 'POST',
                url: 'ClaimsApproval_v2.aspx/UpdateActivityKM', // 🔑 FIXED URL to V2
                data: JSON.stringify({ activityId: activityId, newKm: newKm, reason: reason }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json'
            }).done(function (resp) {
                var r = resp && resp.d ? resp.d : resp;
                if (r && r.success) {
                    // Update UI
                    var $span = $('.km-value[data-activityid="' + activityId + '"]').first();
                    if ($span && $span.length) {
                        $span.text(parseFloat(newKm).toFixed(2));
                        $span.data('currentkm', newKm);
                    }
                    $('#kmEditModal').modal('hide');
                    // Trigger background calculation refresh
                    document.getElementById('btnRefreshCalculations').click();
                } else {
                    $('#km_modal_message').html('<div class="text-danger">' + (r.message || 'Error saving.') + '</div>');
                }
            }).fail(function () {
                $('#km_modal_message').html('<div class="text-danger">Server Error while saving.</div>');
            }).always(function () {
                $saveBtn.prop('disabled', false);
            });
        }

        // Save Action
        function saveKM() {
            var $saveBtn = $('#km_modal_save');
            $saveBtn.prop('disabled', true);
            $('#km_modal_message').empty();

            var id = $('#km_modal_activityid').val();
            var km = $('#km_modal_new').val();
            var reason = $('#km_modal_reason').val();
            
            if(!km || !reason) { 
                $('#km_modal_message').html('<div class="text-danger">KM and Reason are required.</div>'); 
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
                        $('#kmEditModal').modal('hide');
                        // Trigger Server Refresh
                        document.getElementById('btnRefreshCalculations').click();
                    } else {
                        $('#km_modal_message').html('<div class="text-danger">' + r.d.message + '</div>');
                    }
                },
                error: function() {
                    $('#km_modal_message').html('<div class="text-danger">Server error occurred.</div>');
                },
                complete: function() {
                    $saveBtn.prop('disabled', false);
                }
            });
        }

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
                    // ASP.NET wraps the response in resp.d
                    var responseObj = resp.d; 
                    var tbody = $('#tblDeductions tbody').empty();
                    var total = 0;

                    // 1. Check if the server reported an error
                    if (!responseObj || responseObj.success === false) {
                        var errMsg = responseObj ? responseObj.message : "Unknown error";
                        tbody.html('<tr><td colspan="4" class="text-center text-danger">Error: ' + errMsg + '</td></tr>');
                        return;
                    }

                    // 2. Extract the actual array
                    var data = responseObj.data;

                    // 3. Handle empty data
                    if (!data || data.length === 0) {
                        tbody.html('<tr><td colspan="4" class="text-center text-muted">No deductions found.</td></tr>');
                        return;
                    }

                    // 4. Populate the table safely
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
    </script>
</asp:Content>
