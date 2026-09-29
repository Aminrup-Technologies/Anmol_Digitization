<%@ Page Title="AIL | My Claims View" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="MyClaimsView.aspx.cs" Inherits="AnmolDristi.MyClaimsView" %>

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

        .table-success {
            background-color: #d4edda !important;
        }

        .table-danger {
            background-color: #f8d7da !important;
        }

        #detailsModal .modal-body {
            padding: 16px 18px;
            background: #ffffff;
        }

        #detailsModal .table-responsive {
            max-height: 55vh;
            overflow-y: auto;
            overflow-x: auto;
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

        /* =========================================
           📱 MOBILE RESPONSIVE FIXES
           ========================================= */
        @media (max-width: 767px) {
            /* Fix overflowing button group by stacking them */
            .mobile-btn-group {
                display: flex;
                flex-direction: column;
                width: 100%;
            }

                .mobile-btn-group .btn {
                    width: 100%;
                    border-radius: 4px !important; /* Removes the flat edges of grouped buttons */
                    margin-bottom: 8px;
                }

            /* Adjust header text size */
            .x_title h4 {
                font-size: 1.1rem !important;
            }

            /* Shrink table font slightly for better fit */
            .gentilia-table {
                font-size: 0.82rem;
            }

            .btn-sm {
                padding: 0.25rem 0.4rem;
                font-size: 0.75rem;
            }

            .dashboard-box {
                margin-bottom: 10px;
                width: 100%;
            }

            .modal-footer {
                flex-direction: column;
                align-items: stretch !important;
            }

                .modal-footer .text-right {
                    text-align: center !important;
                    margin-left: 0 !important;
                    margin-top: 10px;
                }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid mt-3">
            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel shadow-sm rounded-2">

                        <div class="x_title d-flex justify-content-between align-items-center bg-primary text-white p-2 rounded-top">
                            <h4 class="mb-0"><i class="fa fa-user"></i>My Claims -
                                <asp:Label ID="lblCurrentHeader" runat="server" Font-Bold="true"></asp:Label></h4>
                        </div>

                        <div class="x_content bg-light p-3 rounded-bottom">
                            <div class="row mb-3 pb-3 border-bottom align-items-end">

                                <div class="col-md-12 text-center mb-3">
                                    <div class="btn-group mobile-btn-group" role="group">
                                        <asp:Button ID="btnPrevMonth" runat="server" Text="Prev Month" CssClass="btn btn-info" OnClick="btnPrevMonth_Click" />
                                        <asp:Button ID="btnCurrentMonth" runat="server" Text="Current Month" CssClass="btn btn-primary font-weight-bold" OnClick="btnCurrentMonth_Click" />
                                        <asp:Button ID="btnNextMonth" runat="server" Text="Next Month" CssClass="btn btn-info" OnClick="btnNextMonth_Click" />
                                    </div>
                                </div>

                                <div class="col-md-3 mb-2">
                                    <label class="font-weight-bold text-muted small">Search By Year</label>
                                    <asp:DropDownList ID="ddlSearchYear" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
                                </div>

                                <div class="col-md-3 mb-2">
                                    <label class="font-weight-bold text-muted small">Search By Month</label>
                                    <asp:DropDownList ID="ddlSearchMonth" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Text="-- Select Month --" Value="0"></asp:ListItem>
                                        <asp:ListItem Text="January" Value="1"></asp:ListItem>
                                        <asp:ListItem Text="February" Value="2"></asp:ListItem>
                                        <asp:ListItem Text="March" Value="3"></asp:ListItem>
                                        <asp:ListItem Text="April" Value="4"></asp:ListItem>
                                        <asp:ListItem Text="May" Value="5"></asp:ListItem>
                                        <asp:ListItem Text="June" Value="6"></asp:ListItem>
                                        <asp:ListItem Text="July" Value="7"></asp:ListItem>
                                        <asp:ListItem Text="August" Value="8"></asp:ListItem>
                                        <asp:ListItem Text="September" Value="9"></asp:ListItem>
                                        <asp:ListItem Text="October" Value="10"></asp:ListItem>
                                        <asp:ListItem Text="November" Value="11"></asp:ListItem>
                                        <asp:ListItem Text="December" Value="12"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <div class="col-md-4 mb-2">
                                    <label class="font-weight-bold text-muted small">Claim Status</label>
                                    <asp:DropDownList ID="ddlSearchStatus" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Text="-- All Statuses --" Value=""></asp:ListItem>
                                        <asp:ListItem Text="Pending" Value="Pending"></asp:ListItem>
                                        <asp:ListItem Text="Approved" Value="Approved"></asp:ListItem>
                                        <asp:ListItem Text="Rejected" Value="Rejected"></asp:ListItem>
                                        <asp:ListItem Text="ReadyForPayment" Value="ReadyForPayment"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <div class="col-md-2 mt-2 mt-md-0 text-right">
                                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-success btn-sm w-100 mb-2" OnClick="btnSearch_Click" />
                                    <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm w-100" OnClick="btnReset_Click" />
                                </div>
                            </div>

                            <div class="row mb-2">
                                <div class="col-md-12 text-md-right text-center">
                                    <asp:Button ID="btnExportSelected" runat="server" Text="⬇ Export Selected to Excel" CssClass="btn btn-success btn-sm font-weight-bold w-100 w-md-auto" OnClick="btnExportSelected_Click" />
                                </div>
                            </div>

                            <div class="card-box table-responsive">
                                <asp:Panel ID="pnlRequests" runat="server">
                                    <asp:GridView ID="gvRequests" runat="server" CssClass="table table-sm table-striped table-hover table-bordered gentilia-table"
                                        ShowHeaderWhenEmpty="true" AutoGenerateColumns="false" AllowPaging="true" PageSize="20"
                                        OnRowCommand="gvRequests_RowCommand" DataKeyNames="ApprovalRequestId" GridLines="None">
                                        <HeaderStyle CssClass="gentilia-table-header" />
                                        <RowStyle CssClass="gentilia-row" />
                                        <AlternatingRowStyle CssClass="gentilia-row-alt" />
                                        <Columns>
                                            <asp:TemplateField ItemStyle-Width="40px" HeaderStyle-CssClass="text-center" ItemStyle-CssClass="text-center">
                                                <HeaderTemplate>
                                                    <asp:CheckBox ID="chkSelectAllReq" runat="server" onclick="selectAllRequests(this)" ToolTip="Select All" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkSelectReq" runat="server" CssClass="req-select-checkbox" />
                                                    <asp:HiddenField ID="hfReqId" runat="server" Value='<%# Eval("ApprovalRequestId") %>' />
                                                    <asp:HiddenField ID="hfEmpCode" runat="server" Value='<%# Eval("RequesterLoginId") %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="ApprovalRequestId" HeaderText="Claim ID" HeaderStyle-Width="80px" />
                                            <asp:BoundField DataField="RequestDate" HeaderText="Submitted On" DataFormatString="{0:dd-MMM-yyyy}" />
                                            <asp:BoundField DataField="MonthName" HeaderText="Claim Month" />

                                            <asp:TemplateField HeaderText="Requested (₹)">
                                                <ItemTemplate><span class="font-weight-bold text-secondary"><%# String.Format("{0:N2}", Eval("TotalRequestedAmount")) %></span></ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Approved (₹)">
                                                <ItemTemplate><span class="font-weight-bold text-success"><%# String.Format("{0:N2}", Eval("TotalApprovedAmount")) %></span></ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Status">
                                                <ItemTemplate>
                                                    <span class="badge badge-info"><%# Eval("RequestStatus") %></span>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="PendingWithRole" HeaderText="Pending With" />

                                            <asp:TemplateField HeaderText="Actions">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lnkView" runat="server" CommandName="View" CommandArgument='<%# Eval("ApprovalRequestId") %>' CssClass="btn btn-sm btn-primary" ToolTip="View Details">
                                                        <i class="fa fa-eye"></i> View
                                                    </asp:LinkButton>
                                                    <asp:LinkButton ID="lnkExport" runat="server" CommandName="ExportExcel" CommandArgument='<%# Eval("ApprovalRequestId") %>' CssClass="btn btn-sm btn-outline-success" ToolTip="Export to Excel">
                                                        <i class="fa fa-file-excel-o"></i>
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
                        <button type="button" class="close ml-3" data-dismiss="modal"><span>×</span></button>
                    </div>

                    <div class="modal-body">
                        <asp:UpdatePanel ID="upDetails" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>
                                <div class="row mb-4 mt-2">
                                    <div class="col-md-12">
                                        <h6 class="font-weight-bold text-secondary border-bottom pb-2 mb-3">
                                            <i class="fa fa-sitemap"></i>Claim Progress Tracker
                                        </h6>
                                        <div class="d-flex justify-content-between text-center align-items-top" style="overflow-x: auto;">
                                            <asp:Repeater ID="rptApprovalSteps" runat="server">
                                                <ItemTemplate>
                                                    <div class="flex-fill px-2 position-relative" style="min-width: 150px;">

                                                        <div style='height: 3px; background-color: <%# Eval("LineColor") %>; position: absolute; top: 15px; left: 50%; right: -50%; z-index: 1; display: <%# (Container.ItemIndex == ((System.Data.DataTable)rptApprovalSteps.DataSource).Rows.Count - 1) ? "none" : "block" %>'></div>

                                                        <div style='width: 30px; height: 30px; border-radius: 50%; background-color: <%# Eval("BadgeColor") %>; color: white; line-height: 30px; margin: 0 auto; position: relative; z-index: 2; box-shadow: 0 0 0 3px #fff;' title='<%# Eval("Status") %>'>
                                                            <i class='<%# Eval("Icon") %>'></i>
                                                        </div>

                                                        <div class="mt-2 small">
                                                            <span class="text-uppercase font-weight-bold" style="font-size: 0.7rem; color: #6c757d;"><%# Eval("ApproverRole") %></span><br />
                                                            <strong class="text-dark"><%# Eval("ApproverName") %></strong><br />

                                                            <span class='badge badge-<%# Eval("BootstrapColor") %> mt-1 mb-1'><%# Eval("Status") %></span><br />

                                                            <span class="text-muted d-block" style="font-size: 0.7rem;"><%# Eval("ActionDateStr") %></span>
                                                            <span class="<%# Eval("DaysCssClass") %> font-weight-bold" style="font-size: 0.75rem;"><%# Eval("DaysText") %></span>
                                                        </div>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </div>
                                </div>
                                <div class="table-responsive small">
                                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" CssClass="table table-hover table-bordered"
                                        DataKeyNames="ApprovalRequestDetailId" OnRowDataBound="gvDetails_RowDataBound" ShowFooter="true">
                                        <Columns>
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
                                                    <button type="button" class="btn btn-sm btn-outline-info ml-2 btn-view-km-history" title="View KM Change History">
                                                        <i class="fa fa-history"></i>
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
                                                    <asp:Label ID="lblRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Receipts" ItemStyle-HorizontalAlign="Center">
                                                <ItemTemplate>
                                                    <button type="button" class="btn btn-sm btn-info text-white btn-expenses" data-expenses='<%# Eval("ExpensesJson") %>' title="View Receipts">
                                                        <i class="fa fa-list-alt"></i>View
                                                    </button>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>

                    <div class="modal-footer bg-light d-flex justify-content-between align-items-center">
                        <div class="dashboard-box bg-white border">
                            <span class="dashboard-label text-muted">Original Claimed</span>
                            <h5 class="dashboard-value text-dark mb-0">₹<asp:Label ID="lblSummaryOriginal" runat="server" Text="0.00" /></h5>
                            <small class="font-weight-bold text-muted d-block mt-1">
                                <i class="fa fa-road"></i>
                                <asp:Label ID="lblSummaryOriginalKM" runat="server" Text="0.00" />
                                KM
                            </small>
                        </div>

                        <div class="dashboard-box bg-white border border-danger" style="cursor: pointer; transition: 0.2s;" onclick="showDeductionsBreakdown()" title="Click to view deduction details">
                            <span class="dashboard-label text-danger">Total Deductions <i class="fa fa-info-circle ml-1"></i></span>
                            <h5 class="dashboard-value text-danger mb-0">(-) ₹<asp:Label ID="lblSummaryDeduction" runat="server" Text="0.00" /></h5>
                            <small class="font-weight-bold text-danger d-block mt-1">
                                <i class="fa fa-road"></i>(-)<asp:Label ID="lblSummaryDeductedKM" runat="server" Text="0.00" />
                                KM
                            </small>
                        </div>

                        <div class="dashboard-box bg-success text-white border border-success">
                            <span class="dashboard-label text-white" style="opacity: 0.9">Net Approved</span>
                            <h5 class="dashboard-value text-white mb-0">₹<asp:Label ID="lblSummaryApproved" runat="server" Text="0.00" /></h5>
                            <small class="font-weight-bold text-white d-block mt-1">
                                <i class="fa fa-road"></i>
                                <asp:Label ID="lblSummaryApprovedKM" runat="server" Text="0.00" />
                                KM
                            </small>
                        </div>

                        <div class="ml-4 text-right">
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="kmHistoryModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header bg-info text-white">
                        <h5 class="modal-title"><i class="fa fa-road"></i>KM Change History</h5>
                        <button type="button" class="close text-white" data-dismiss="modal">×</button>
                    </div>
                    <div class="modal-body">
                        <div class="alert alert-light border mb-3">
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

                        <h5 class="text-secondary mb-3"><i class="fa fa-history"></i>Modifications</h5>
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
                    </div>
                    <div class="modal-footer bg-light">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="expensesModal" class="modal fade" tabindex="-1" role="dialog">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Expense Receipts</h5>
                        <button type="button" class="close" data-dismiss="modal">×</button>
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

    <script type="text/javascript">
        // --- READ-ONLY EXPENSE HANDLING ---
        $(document).on('click', '.btn-expenses', function () {
            var json = $(this).attr('data-expenses');
            if (json) {
                var data = JSON.parse(json);
                renderExpenses(data);
            }
        });

        function selectAllRequests(chk) {
            $('.req-select-checkbox input').prop('checked', chk.checked);
        }

        function renderExpenses(data) {
            if (!data || data.length === 0) {
                $('#expensesModalBody').html('<div class="alert alert-warning">No receipts attached for this day.</div>');
                $('#expensesModal').modal('show');
                return;
            }

            var html = '<table class="table table-bordered table-striped table-hover"><thead><tr class="bg-light">' +
                       '<th>Date</th><th>Type</th><th>Purpose</th><th>Bill No</th><th class="text-right">Amount</th><th class="text-center">Status</th></tr></thead><tbody>';
            $.each(data, function (i, item) {
                var expDate = item.ExpenseDate ? new Date(item.ExpenseDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '-';

                var statusBadge = '';
                if (item.ClaimStatus === 'Approved') {
                    statusBadge = '<span class="badge badge-success px-2 py-1"><i class="fa fa-check"></i> Approved</span>';
                } else if (item.ClaimStatus === 'Rejected') {
                    statusBadge = '<span class="badge badge-danger px-2 py-1"><i class="fa fa-times"></i> Rejected</span>';
                } else {
                    statusBadge = '<span class="badge badge-warning text-dark px-2 py-1">Pending</span>';
                }

                var billLink = item.AttachmentPath ?
                    '<a href="' + item.AttachmentPath + '" target="_blank" class="text-primary font-weight-bold"><i class="fa fa-paperclip"></i> ' + (item.BillNo || 'View Bill') + '</a>' : (item.BillNo || '-');

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

        // --- KM HISTORY HANDLING ---
        $(document).on('click', '.btn-view-km-history', function (e) {
            e.preventDefault();
            var actId = $(this).closest('td').find('.km-value').data('activityid');
            loadKmHistory(actId);
            $('#kmHistoryModal').modal('show');
        });

        function loadKmHistory(activityId) {
            $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Loading...</td></tr>');
            $.ajax({
                type: 'POST',
                url: 'MyClaimsView.aspx/GetKMHistory', // Note: Updated to point to new page
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

        function openDetailsModal() {
            $('#detailsModal').modal('show');
        }

        function showDeductionsBreakdown() {
            var reqId = document.getElementById('<%= lblModalRequestId.ClientID %>').innerText;
            if (!reqId || reqId === "0") return;

            $('#tblDeductions tbody').html('<tr><td colspan="4" class="text-center">Loading details...</td></tr>');
            $('#lblDeductionsModalTotal').text('0.00');
            $('#deductionsModal').modal('show');

            $.ajax({
                type: 'POST',
                url: 'MyClaimsView.aspx/GetDeductionBreakdown', // Note: Updated to point to new page
                data: JSON.stringify({ approvalRequestId: parseInt(reqId) }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (resp) {
                    var responseObj = resp.d;
                    var tbody = $('#tblDeductions tbody').empty();
                    var total = 0;

                    if (!responseObj || responseObj.success === false) {
                        tbody.html('<tr><td colspan="4" class="text-center text-danger">Error loading data.</td></tr>');
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
    </script>
</asp:Content>
