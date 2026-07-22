<%@ Page Title="AIL | Approved Claims View" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="ApprovedClaimsView.aspx.cs" Inherits="AnmolDristi.ApprovedClaimsView" %>

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

        /* Row Highlighting */
        .table-success {
            background-color: #d4edda !important;
        }

        .table-danger {
            background-color: #f8d7da !important;
        }

        /* Sticky Header & Footer */
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
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid mt-3">
            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel shadow-sm rounded-2">

                        <div class="x_title d-flex justify-content-between align-items-center bg-primary text-white p-2 rounded-top">
                            <h4 class="mb-0"><i class="fa fa-list-alt"></i>Approved Claims View -
                                <asp:Label ID="lblCurrentHeader" runat="server" Font-Bold="true"></asp:Label></h4>
                        </div>

                        <div class="x_content bg-light p-3 rounded-bottom">

                            <div class="row mb-3 pb-3 border-bottom align-items-end">
                                <div class="col-md-12 text-center mb-3">
                                    <div class="btn-group" role="group">
                                        <asp:Button ID="btnPrevMonth" runat="server" Text="Prev Month" CssClass="btn btn-info" OnClick="btnPrevMonth_Click" />
                                        <asp:Button ID="btnCurrentMonth" runat="server" Text="Current Month" CssClass="btn btn-primary font-weight-bold" OnClick="btnCurrentMonth_Click" />
                                        <asp:Button ID="btnNextMonth" runat="server" Text="Next Month" CssClass="btn btn-info" OnClick="btnNextMonth_Click" />
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <label class="font-weight-bold text-muted small">Search By Year</label>
                                    <asp:DropDownList ID="ddlSearchYear" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>
                                </div>
                                <div class="col-md-3">
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
                                <div class="col-md-4">
                                    <label class="font-weight-bold text-muted small">Search By Employee Code</label>
                                    <asp:TextBox ID="txtSearchEmpCode" runat="server" CssClass="form-control form-control-sm" Placeholder="e.g. AHS1281"></asp:TextBox>
                                </div>
                                <div class="col-md-2 text-right">
                                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-success btn-sm w-100 mb-1" OnClick="btnSearch_Click" />
                                    <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm w-100" OnClick="btnReset_Click" />
                                </div>
                            </div>

                            <div class="row mb-2">
                                <div class="col-md-12 text-right">
                                    <asp:Button ID="btnExportSelected" runat="server" Text="⬇ Export Selected to Excel" CssClass="btn btn-success btn-sm font-weight-bold" OnClick="btnExportSelected_Click" />
                                </div>
                            </div>

                            <div class="card-box table-responsive">
                                <asp:Panel ID="pnlRequests" runat="server">
                                    <asp:GridView ID="gvRequests" runat="server" CssClass="table table-sm table-striped table-hover table-bordered gentilia-table"
                                        ShowHeaderWhenEmpty="true" AutoGenerateColumns="false" AllowPaging="true" PageSize="20"
                                        OnRowCommand="gvRequests_RowCommand" OnPageIndexChanging="gvRequests_PageIndexChanging" DataKeyNames="ApprovalRequestId" GridLines="None">
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

                                            <asp:BoundField DataField="ApprovalRequestId" HeaderText="ID" HeaderStyle-Width="50px" />
                                            <asp:TemplateField HeaderText="Employee Name" HeaderStyle-Width="220px">
                                                <ItemTemplate><%# Eval("EmployeeName") %> (<%# Eval("RequesterLoginId") %>)</ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="RequestDate" HeaderText="Requested Date" DataFormatString="{0:dd-MMM-yyyy hh:mm:ss tt}" />
                                            <asp:BoundField DataField="MonthName" HeaderText="Month" />
                                            <asp:TemplateField HeaderText="Approved Amount (₹)">
                                                <ItemTemplate>
                                                    <span class="font-weight-bold text-success">
                                                        <%# String.Format("{0:N2}", Eval("TotalApprovedAmount")) %>
                                                    </span>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Status">
                                                <ItemTemplate>
                                                    <span class="badge badge-success"><i class="fa fa-check"></i><%# Eval("RequestStatus") %></span>
                                                </ItemTemplate>
                                            </asp:TemplateField>
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
                                            <asp:BoundField DataField="Snapshot_StationaryAmount" HeaderText="Stat. (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal" HeaderText="Total (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Day Details" ItemStyle-HorizontalAlign="Center">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lnkViewDay" runat="server" CssClass="btn btn-sm btn-info btn-day-action"
                                                        OnClientClick="return false;"
                                                        data-activityid='<%# Eval("ActivityId") %>'
                                                        data-detailid='<%# Eval("ApprovalRequestDetailId") %>'
                                                        data-expenses='<%# Eval("ExpensesJson") %>'
                                                        data-activitydate='<%# Eval("ActivityDate","{0:dd-MMM-yyyy}") %>'
                                                        data-day='<%# Eval("Day") %>'
                                                        data-distance='<%# Eval("Snapshot_TotalDistanceKM") %>'
                                                        data-ta='<%# Eval("Snapshot_CalculatedClaimAmount") %>'
                                                        data-da='<%# Eval("Snapshot_DAAmount") %>'
                                                        data-hotel='<%# Eval("Snapshot_HotelAmount") %>'
                                                        data-travel='<%# Eval("Snapshot_TravelAmount") %>'
                                                        data-stationary='<%# Eval("Snapshot_StationaryAmount") %>'>
                                                        <i class="fa fa-search"></i> View
                                                    </asp:LinkButton>
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
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div id="unifiedDayModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-xl">
                <div class="modal-content border-0 shadow-lg">
                    <div class="modal-header bg-dark text-white p-3 align-items-center">
                        <h6 class="modal-title m-0 font-weight-bold text-uppercase"><i class="fa fa-calendar-check mr-2"></i>Day Details (Read-Only)</h6>
                        <button type="button" class="close text-white" data-dismiss="modal">×</button>
                    </div>
                    <div class="modal-body p-0 bg-light">
                        <ul class="nav nav-tabs nav-justified m-0 bg-white shadow-sm" id="dayModalTabs" role="tablist">
                            <li class="nav-item">
                                <a class="nav-link active font-weight-bold text-dark py-3 border-0" data-toggle="tab" href="#tab-receipts">
                                    <i class="fa fa-receipt text-info mr-2"></i>Expense Receipts
                                </a>
                            </li>
                            <li class="nav-item border-left">
                                <a class="nav-link font-weight-bold text-dark py-3 border-0" data-toggle="tab" href="#tab-distance">
                                    <i class="fa fa-map-marker-alt text-danger mr-2"></i>Distance & Map
                                </a>
                            </li>
                        </ul>
                        <div class="tab-content p-4">
                            <div class="tab-pane active" id="tab-receipts">
                                <div id="unifiedExpensesBody"></div>
                            </div>
                            <div class="tab-pane" id="tab-distance">
                                <div class="alert alert-info border-info mb-4 shadow-sm">
                                    <div class="row align-items-center">
                                        <div class="col-md-5"><strong><i class="fa fa-play-circle text-success mr-1"></i>From:</strong> <span id="km_modal_fromplace" class="text-dark ml-1"></span></div>
                                        <div class="col-md-5"><strong><i class="fa fa-stop-circle text-danger mr-1"></i>To:</strong> <span id="km_modal_toplace" class="text-dark ml-1"></span></div>
                                        <div class="col-md-2 text-right">
                                            <a href="#" id="km_modal_maplink" target="_blank" class="btn btn-sm btn-primary shadow-sm w-100" style="display: none;"><i class="fa fa-map-marked-alt mr-1"></i>Map</a>
                                        </div>
                                    </div>
                                </div>
                                <h6 class="text-secondary border-bottom pb-2 mt-4 font-weight-bold text-uppercase"><i class="fa fa-history mr-2"></i>Modification History</h6>
                                <div class="table-responsive bg-white shadow-sm border rounded" style="max-height: 250px; overflow-y: auto;">
                                    <table class="table table-striped table-hover table-sm mb-0" id="km_history_tbl">
                                        <thead class="thead-light" style="position: sticky; top: 0;">
                                            <tr>
                                                <th>Date (IST)</th>
                                                <th class="text-right">Old KM</th>
                                                <th class="text-right">New KM</th>
                                                <th>Changed By</th>
                                                <th>Reason</th>
                                            </tr>
                                        </thead>
                                        <tbody></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
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

            // Load history and open modal
            loadKmHistory(actId);
            $('#kmHistoryModal').modal('show');
        });

        function loadKmHistory(activityId) {
            $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Loading...</td></tr>');

            $.ajax({
                type: 'POST',
                url: 'ApprovedClaimsView.aspx/GetKMHistory',
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

                // Generate Maps Link
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
                url: 'ApprovedClaimsView.aspx/GetDeductionBreakdown',
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

        $(document).on('click', '.btn-day-action', function (e) {
            e.preventDefault();
            var $btn = $(this);

            var actId = $btn.data('activityid');
            var expensesJson = $btn.attr('data-expenses');

            var details = {
                ActivityDate: $btn.data('activitydate'),
                Day: $btn.data('day'),
                TotalDistanceKM: $btn.data('distance'),
                TotalTA: $btn.data('ta'),
                TotalDA: $btn.data('da'),
                Hotel: $btn.data('hotel'),
                Travel: $btn.data('travel'),
                Stationary: $btn.data('stationary'),
                ClaimStatus: 'Finalized',
                StatusClass: 'success'
            };

            // Load KM History automatically
            loadKmHistory(actId);

            // Call the exact same renderer from the Approval page, but force canAction = false!
            renderUnifiedExpensesModal(details, expensesJson, true, false, false);

            $('#dayModalTabs a[href="#tab-receipts"]').tab('show');
            $('#unifiedDayModal').modal('show');
        });

        // Copy/Paste the `renderUnifiedExpensesModal`, `renderActivityAndTADASummary`, 
        // and `renderExpenseCard` JavaScript functions directly from ClaimsApproval_v2.aspx here.

        function renderUnifiedExpensesModal(details, expensesJson, isRowLocked, canAction, isFinalApprover) {
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
                                canAction, isFinalApprover);

                            break;

                        case 'TRAVEL':

                            html += renderExpenseCard(
                                'Travel',
                                'fa-bus',
                                'bg-primary',
                                exp,
                                canAction, isFinalApprover);

                            break;

                        case 'STATIONARY':

                            html += renderExpenseCard(
                                'Stationary',
                                'fa-paperclip',
                                'bg-warning text-dark',
                                exp,
                                canAction, isFinalApprover);

                            break;

                        case 'DA':

                            html += renderExpenseCard(
                                'Daily Allowance',
                                'fa-utensils',
                                'bg-secondary',
                                exp,
                                canAction, isFinalApprover);

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

                $('#btnUnifiedRejectDay')
                    .prop('disabled', !canAction)
                    .attr('title', canAction ? '' : 'You are not authorized to perform this action.');

                $('#btnUnifiedApproveDay')
                .prop('disabled', false)
                .removeAttr('title');

            } else {
                html += `
            <div class="alert alert-light border mt-4 text-center text-muted shadow-sm py-4">
                <i class="fa fa-folder-open fa-3x mb-3 d-block text-secondary opacity-50"></i>
                <h6 class="mb-0">No additional line items (Hotel, Travel, Stationary) claimed for this day.</h6>
            </div>`;
                $('#btnUnifiedApproveDay').prop('disabled', false).removeAttr('title');
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

        function renderExpenseCard(title, iconClass, headerColorClass, exp, canAction, isFinalApprover) {
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

                    <button
                        type="button"
                        class="btn btn-warning btn-sm flex-fill"
                        onclick="updateExpense(${exp.ExpenseId},'RETURN',this)">
                        <i class="fa fa-undo mr-1"></i>
                        Return
                    </button>

                    ${
                                    isFinalApprover
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
                        // Returned
                        //-------------------------------------------------

                    case "Returned":

                        actionButtons = `
                <div class="alert alert-warning py-2 mb-2 text-center">
                    <i class="fa fa-undo mr-1"></i>
                    Returned for Correction
                </div>

                <div class="btn-group d-flex">

                    <button
                        type="button"
                        class="btn btn-success btn-sm flex-fill"
                        onclick="updateExpense(${exp.ExpenseId},'APPROVE',this)">
                        <i class="fa fa-check mr-1"></i>
                        Approve
                    </button>

                    ${
                                    isFinalApprover
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
                        // Approved
                        //-------------------------------------------------

                    case "Approved":

                        actionButtons = `
                <div class="alert alert-success py-2 mb-2 text-center">
                    <i class="fa fa-check-circle mr-1"></i>
                    Expense Approved
                </div>

                <div class="btn-group d-flex">

                    <button
                        type="button"
                        class="btn btn-outline-warning btn-sm flex-fill"
                        onclick="updateExpense(${exp.ExpenseId},'RETURN',this)">
                        <i class="fa fa-undo mr-1"></i>
                        Return
                    </button>

                    ${
                                    isFinalApprover
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

                switch (status) {

                    case "Approved":
                        badgeClass = "success";
                        break;

                    case "Returned":
                        badgeClass = "warning";
                        break;

                    case "Rejected":
                        badgeClass = "danger";
                        break;

                    default:
                        badgeClass = "secondary";
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

    </script>
</asp:Content>
