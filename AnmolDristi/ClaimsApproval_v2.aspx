<%@ Page Title="AIL | Claims Approval V2" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="ClaimsApproval_v2.aspx.cs" Inherits="AnmolDristi.ClaimsApproval_v2" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://cdn.datatables.net/1.10.20/css/dataTables.bootstrap4.min.css" rel="stylesheet" />
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
    </style>
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

                                        <asp:Panel ID="pnlRequests" runat="server">
                                            <asp:GridView ID="gvRequests" runat="server" ClientIDMode="Static"
                                                CssClass="table table-striped jambo_table bulk_action dt-responsive nowrap"
                                                ShowHeaderWhenEmpty="true" AutoGenerateColumns="false"
                                                OnRowCommand="gvRequests_RowCommand" DataKeyNames="ApprovalRequestId">
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
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="Snapshot_CalculatedClaimAmount" HeaderText="TA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_DAAmount" HeaderText="DA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_HotelAmount" HeaderText="Hotel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_TravelAmount" HeaderText="Travel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal" HeaderText="Total (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="Day Action" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="8%">
                                                <ItemTemplate>
                                                    <button type="button" runat="server" id="btnExp" class="btn btn-sm btn-info text-white btn-day-action shadow-sm"
                                                        data-activityid='<%# Eval("ActivityId") %>'
                                                        data-expenses='<%# Eval("ExpensesJson") %>' title="View Receipts & Distance">
                                                        <i class="fa fa-folder-open"></i>Open
                                                    </button>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:TextBox ID="txtRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' CssClass="form-control form-control-sm" />
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

                                <asp:Button ID="btnRefreshCalculations" runat="server" OnClick="btnRefreshCalculations_Click" ClientIDMode="Static" Style="" />
                            </div>

                            <div class="modal-footer bg-light d-flex justify-content-between align-items-end" style="">
                                <div class="d-flex" style="">
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

                                    <div class="dashboard-box bg-white border border-danger" style="" onclick="showDeductionsBreakdown()" title="Click to view deduction details">
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
                                        <span class="dashboard-label text-white" style="">Net Approved</span>
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
                                        <asp:LinkButton ID="lnkRecalculate" runat="server" CssClass="btn btn-info btn-sm m-0" OnClick="btnRecalculate_Click" OnClientClick="showRecalculateSpinner(this);" ToolTip="Recalculate approved amounts">
                                            <i class="fa fa-refresh"></i> Re-Calculate
                                        </asp:LinkButton>
                                        <asp:Button ID="btnReturnToEmployee" runat="server" Text="✖ Return to Employee" CssClass="btn btn-danger btn-sm m-0 ml-2" OnClick="btnReturnToEmployee_Click" OnClientClick="return promptForReturn();" />
                                        <asp:Button ID="btnForwardLevel" runat="server" Text="✔ Forward to Next Level" CssClass="btn btn-success btn-sm m-0 ml-2" OnClick="btnForwardLevel_Click" Visible="false" />
                                        <asp:Button ID="btnMarkReadyForPayment" runat="server" Text="✔ Mark Ready for Payment" CssClass="btn btn-warning btn-sm m-0 ml-2" OnClick="btnMarkReadyForPayment_Click" Visible="false" />
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
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header bg-dark text-white p-2">
                        <h6 class="modal-title m-0"><i class="fa fa-calendar-check-o mr-2"></i>Day Details & Actions</h6>
                        <button type="button" class="close text-white" data-dismiss="modal">×</button>
                    </div>

                    <div class="modal-body p-0">
                        <ul class="nav nav-tabs nav-justified bg-light m-0" id="dayModalTabs" role="tablist">
                            <li class="nav-item">
                                <a class="nav-link active font-weight-bold text-dark py-3" id="receipts-tab" data-toggle="tab" href="#tab-receipts" role="tab">
                                    <i class="fa fa-receipt text-info"></i>Expense Receipts
                                </a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link font-weight-bold text-dark py-3" id="distance-tab" data-toggle="tab" href="#tab-distance" role="tab">
                                    <i class="fa fa-map-marker text-danger"></i>Distance (KM) & Map
                                </a>
                            </li>
                        </ul>

                        <div class="tab-content p-3">
                            <div class="tab-pane active" id="tab-receipts" role="tabpanel">
                                <div id="unifiedExpensesBody">
                                    <div class="text-center py-4 text-muted">
                                        <i class="fa fa-spinner fa-spin fa-2x"></i>
                                        <br />
                                        Loading receipts...
                                    </div>
                                </div>
                            </div>

                            <div class="tab-pane" id="tab-distance" role="tabpanel">
                                <input type="hidden" id="km_modal_activityid" />

                                <div class="alert alert-info border mb-3 py-2">
                                    <div class="row align-items-center">
                                        <div class="col-md-4"><strong>From:</strong> <span id="km_modal_fromplace" class="text-dark"></span></div>
                                        <div class="col-md-4"><strong>To:</strong> <span id="km_modal_toplace" class="text-dark"></span></div>
                                        <div class="col-md-4 text-right">
                                            <a href="#" id="km_modal_maplink" target="_blank" class="btn btn-sm btn-primary shadow-sm" style="display: none;">
                                                <i class="fa fa-map"></i>View on Map
                                            </a>
                                        </div>
                                    </div>
                                </div>

                                <div class="row" id="divKmControls">
                                    <div class="col-md-3">
                                        <div class="form-group">
                                            <label class="font-weight-bold text-muted small mb-1">Current KM</label>
                                            <input type="text" id="km_modal_current" class="form-control form-control-sm bg-light font-weight-bold" readonly />
                                        </div>
                                    </div>
                                    <div class="col-md-3">
                                        <div class="form-group">
                                            <label class="font-weight-bold text-primary small mb-1">New KM</label>
                                            <input type="number" step="0.01" id="km_modal_new" class="form-control form-control-sm border-primary" />
                                        </div>
                                    </div>
                                    <div class="col-md-6">
                                        <div class="form-group">
                                            <label class="font-weight-bold text-muted small mb-1">Reason for change</label>
                                            <div class="input-group input-group-sm">
                                                <input type="text" id="km_modal_reason" class="form-control" placeholder="Required if changing KM..." />
                                                <div class="input-group-append">
                                                    <button type="button" id="km_modal_save" class="btn btn-success" onclick="saveKM()"><i class="fa fa-save"></i>Save</button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div id="km_modal_message" class="mb-2"></div>

                                <h6 class="text-secondary border-bottom pb-1 mt-3"><i class="fa fa-history"></i>Modification History</h6>
                                <div class="table-responsive" style="max-height: 200px; overflow-y: auto;">
                                    <table class="table table-striped table-sm border" id="km_history_tbl">
                                        <thead class="thead-light">
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
    <script type="text/javascript">
        // Global refresh flag
        var _refreshNeeded = false;
        var _currentUserLevel = <%= ViewState["CurrentUserLevel"] ?? 0 %>;

        function selectAllRows(chk) {
            $('.row-select-checkbox input').prop('checked', chk.checked);
        }

        // =========================================================
        // EXPENSE / KM ACTIONS
        // =========================================================
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
                        // Trigger the background calculation refresh INSTANTLY!
                        document.getElementById('btnRefreshCalculations').click();
                    } else {
                        alert(r.d.message);
                    }
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

        // =========================================================
        // UNIFIED DAY MODAL LOGIC
        // =========================================================
        $(document).on('click', '.btn-day-action', function(e) {
            e.preventDefault();
            var $row = $(this).closest('tr');
            
            var actId = $(this).data('activityid');
            var currentKm = $row.find('.km-value').text().trim();
            var expensesJson = $(this).attr('data-expenses');
            
            var activeLevel = parseInt(document.getElementById('hfCurrentUserLevel').value) || 0;
            if (activeLevel === 1 || activeLevel === 2) {
                $('#divKmControls').show();
            } else {
                $('#divKmControls').hide();
            }

            // Setup Distance Tab
            $('#km_modal_activityid').val(actId);
            $('#km_modal_current').val(currentKm);
            $('#km_modal_new').val(currentKm);
            $('#km_modal_reason').val('');
            $('#km_modal_message').empty();
            loadKmHistory(actId);
            
            // Setup Receipts Tab
            if (expensesJson && expensesJson.trim() !== "[]") {
                var data = JSON.parse(expensesJson);
                renderExpenses(data);
            } else {
                $('#unifiedExpensesBody').html('<div class="alert alert-warning text-center mt-3"><i class="fa fa-folder-open-o fa-2x d-block mb-2"></i> No receipts attached for this day.</div>');
            }
            
            $('#dayModalTabs a[href="#tab-receipts"]').tab('show');
            $('#unifiedDayModal').modal('show');
        });

        function renderExpenses(data) {
            var html = '<table class="table table-sm table-bordered table-striped table-hover mt-2"><thead class="bg-light">' +
                       '<tr><th>Date</th><th>Type</th><th>Purpose</th><th>Bill No</th><th class="text-right">Amount</th><th class="text-center">Action</th></tr></thead><tbody>';
            
            var activeLevel = parseInt(document.getElementById('hfCurrentUserLevel').value) || 0;
            var canAction = (activeLevel === 1 || activeLevel === 2);

            $.each(data, function(i, item) {
                var expDate = item.ExpenseDate ? new Date(item.ExpenseDate).toLocaleDateString('en-GB', {day: '2-digit', month: 'short', year: 'numeric'}) : '-';
                var statusBadge = '';
                
                if(item.ClaimStatus === 'Approved') {
                    statusBadge = '<span class="badge badge-success px-2 py-1"><i class="fa fa-check"></i> Approved</span>';
                } else if(item.ClaimStatus === 'Rejected') {
                    statusBadge = '<span class="badge badge-danger px-2 py-1"><i class="fa fa-times"></i> Rejected</span>';
                } else {
                    if(canAction) {
                        statusBadge = '<button type="button" class="btn btn-sm btn-success mr-1 py-0 px-2" onclick="updateExpense(' + item.ExpenseId + ',\'Approved\', this)" title="Approve"><i class="fa fa-check"></i></button>' +
                                      '<button type="button" class="btn btn-sm btn-danger py-0 px-2" onclick="updateExpense(' + item.ExpenseId + ',\'Rejected\', this)" title="Reject"><i class="fa fa-times"></i></button>';
                    } else {
                        statusBadge = '<span class="badge badge-warning text-dark px-2 py-1">Pending</span>';
                    }
                }
        
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
            $('#unifiedExpensesBody').html(html);
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
                        $('#unifiedDayModal').modal('hide');
                        document.getElementById('btnRefreshCalculations').click();
                    } else {
                        $('#km_modal_message').html('<div class="text-danger">Error: ' + r.d.message + '</div>');
                    }
                },
                complete: function() {
                    $saveBtn.prop('disabled', false);
                }
            });
        }

        // =========================================================
        // UI HELPERS & DASHBOARD ACTIONS
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
            var reason = prompt("Please enter the reason for returning this ENTIRE claim to the employee:");
            if (reason != null && reason.trim() !== "") {
                document.getElementById('hfReturnReason').value = reason;
                return true;
            } else if (reason != null) {
                alert("A reason is required to return the claim.");
                return false;
            }
            return false;
        }

        function showRecalculateSpinner(btn) {
            btn.innerHTML = '<i class="fa fa-spinner fa-spin"></i> Calculating...';
            btn.classList.add('disabled');
            return true;
        }

        // =========================================================
        // ASP.NET AJAX UPDATEPANEL HOOKS
        // =========================================================
        var _gridScrollTop = 0;
        
        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            
            prm.add_beginRequest(function (sender, args) {
                // Scroll Memory Check
                var gridContainer = document.querySelector('#detailsModal .table-responsive');
                if (gridContainer) {
                    _gridScrollTop = gridContainer.scrollTop;
                }
            });

            prm.add_endRequest(function (sender, args) {
                // Restore Scroll Memory
                var gridContainer = document.querySelector('#detailsModal .table-responsive');
                if (gridContainer) {
                    gridContainer.scrollTop = _gridScrollTop;
                }
                
                // Re-initialize Datatables
                initDataTablesSafe();
            });
        }

        // =========================================================
        // GENTELLELA DATATABLES INITIALIZATION
        // =========================================================
        function applyDataTables() {
            var $grid = $('#gvRequests');
            
            if ($grid.length > 0 && $grid.find('tbody tr').length > 0 && typeof $.fn.dataTable !== 'undefined') {
                if ($.fn.dataTable.isDataTable('#gvRequests')) {
                    $grid.DataTable().destroy();
                }

                $grid.DataTable({
                    "pageLength": 15,                
                    "lengthMenu": [10, 15, 25, 50],  
                    "ordering": true,                
                    "searching": true,               
                    "responsive": true,
                    "order": [[ 3, "desc" ]],        
                    "language": {
                        "search": "Quick Filter:",
                        "lengthMenu": "Show _MENU_ requests"
                    },
                    "columnDefs": [
                        { "orderable": false, "targets": [7] } 
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

        $(document).ready(function () {
            initDataTablesSafe();
        });
    </script>
</asp:Content>
