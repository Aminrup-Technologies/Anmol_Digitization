<%@ Page Title="AIL | SO Cliams Approval" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ClaimsApproval.aspx.cs" Inherits="AnmolDristi.ClaimsApproval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .btn-group .btn i {
            margin-right: 4px;
        }

        .btn-outline-success:hover i {
            color: #28a745 !important;
            text-shadow: 0 0 6px rgba(40, 167, 69, 0.5);
        }

        .btn-outline-danger:hover i {
            color: #dc3545 !important;
            text-shadow: 0 0 6px rgba(220, 53, 69, 0.5);
        }

        .invalid-remark {
            border: 2px solid #dc3545 !important;
            background-color: #ffe6e6;
            animation: shake 0.25s linear 2;
        }

        @keyframes shake {
            0% {
                transform: translateX(0);
            }

            25% {
                transform: translateX(-4px);
            }

            50% {
                transform: translateX(4px);
            }

            75% {
                transform: translateX(-4px);
            }

            100% {
                transform: translateX(0);
            }
        }

        .row-disabled {
            opacity: 0.6;
        }

        .row-disabled input[type="checkbox"] {
                cursor: not-allowed;
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
            vertical-align: middle;
            text-transform: capitalize;
            box-shadow: 0 1px 0 rgba(0,0,0,0.05);
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

        .status-draft {
            background: #EEF2FF;
            color: #274093;
            border: 1px solid #D0D8FF;
        }

        .status-other {
            background: #F3F6F8;
            color: #4B5563;
            border: 1px solid #E2E8F0;
        }

        .total-claim-cell {
            font-weight: 700;
        }

        .action-buttons .btn i {
            margin-right: 6px;
        }

        .gentilia-table tbody tr:hover {
            background: #f1f6f9;
        }

        .gentilia-table tbody tr:hover {
            background: #f1f6f9;
        }

        @media (max-width: 767px) {
            .gentilia-table thead {
                display: none;
            }

            .gentilia-table tr {
                display: block;
                margin-bottom: 10px;
            }

            .gentilia-table td {
                display: flex;
                justify-content: space-between;
                padding: 6px 8px;
            }
        }

        /* Container & modal sizing */
        #detailsModal .modal-dialog {
            max-width: 1200px;
        }

        #detailsModal .modal-content {
            border-radius: 8px;
            box-shadow: 0 6px 24px rgba(45,55,72,0.25);
            border: 0;
        }

        /* Header */
        #detailsModal .modal-header {
            background: linear-gradient(90deg, rgba(55,75,85,0.06), rgba(255,255,255,0));
            padding: 14px 18px;
            border-bottom: 1px solid rgba(0,0,0,0.05);
            align-items: center;
        }

        #detailsModal .modal-title {
            font-size: 1.05rem;
            font-weight: 600;
            color: #2f4050; /* neutral Gentilia tone */
            margin-right: 12px;
        }

        #detailsModal .modal-header small.text-muted {
            font-size: 0.86rem;
            color: #6b7280;
        }

        /* Close button */
        #detailsModal .modal-header .close {
            background: transparent;
            border: 0;
            font-size: 1.25rem;
            opacity: .85;
        }

        /* Make modal body scrollable (keeps header/footer visible) */
        #detailsModal .modal-body {
            max-height: calc(100vh - 240px);
            overflow: auto;
            padding: 16px 18px;
            background: #ffffff;
        }

        /* Table - compact & neat */
        #detailsModal .table {
            margin-bottom: 0;
            font-size: 0.92rem;
            white-space: nowrap;
        }

        #detailsModal .table-sm th,
        #detailsModal .table-sm td {
            padding: 10px 8px;
            vertical-align: middle;
        }

        /* Table header: muted and sticky */
        #detailsModal .table thead th {
            background: #f7fafc; /* light gentilia header */
            color: #475569;
            font-weight: 700;
            text-transform: none;
            font-size: 0.85rem;
            border-bottom: 1px solid rgba(0,0,0,0.06);
            position: sticky;
            top: 0;
            z-index: 5;
        }

        /* Disable hover effect on header (explicit) */
        #detailsModal .table thead tr:hover,
        #detailsModal .table thead th:hover {
            background-color: inherit !important;
            cursor: default !important;
        }

        /* Body rows: subtle alternating and selected state */
        #detailsModal .table tbody tr:nth-child(odd) {
            background: #ffffff;
        }

        #detailsModal .table tbody tr:nth-child(even) {
            background: #fbfdff;
        }

        #detailsModal .table tbody tr.selected-row {
            background: rgba(220,38,38,0.08) !important;
        }

        /* Small inputs inside grid */
        #detailsModal .row-remark {
            height: 32px;
            padding: 6px 8px;
            font-size: 0.9rem;
        }

        /* Checkbox column narrow */
        #detailsModal .table td:first-child,
        #detailsModal .table th:first-child {
            text-align: center;
            width: 48px;
        }

        /* Action buttons styling (keep icons visible) */
        #detailsModal .btn-outline-success,
        #detailsModal .btn-outline-danger {
            min-width: 40px;
            height: 34px;
            padding: 0 8px;
            border-radius: 6px;
            font-weight: 600;
            line-height: 1;
        }

        #detailsModal .btn-outline-success {
            color: #12703a;
            border-color: rgba(18,112,58,0.15);
        }

        #detailsModal .btn-outline-danger {
            color: #a11b1b;
            border-color: rgba(161,27,27,0.12);
        }

        /* Footer layout */
        #detailsModal .modal-footer {
            border-top: 1px solid rgba(0,0,0,0.04);
            padding: 12px 18px;
            display: flex;
            align-items: center;
            gap: 12px;
            background: #fbfcfd;
        }

        /* Footer label (error / status) push to left */
        #detailsModal .modal-footer .text-danger {
            margin-right: auto;
            font-size: 0.92rem;
        }

        /* Utility: make inline badges inside the table compact */
        #detailsModal .badge {
            font-size: 0.78rem;
            padding: 6px 8px;
            border-radius: 12px;
        }

        /* Sticky Header Logic */
        #detailsModal .table-responsive {
            /* Ensure the container handles the scroll */
            max-height: 60vh; /* Adjust height as needed to fit your screen */
            overflow-y: auto;
        }

        #detailsModal .table thead th {
            position: sticky;
            top: 0;
            z-index: 1020; /* High z-index keeps it above row content */
            background-color: #f8f9fa; /* Solid color (light gray) prevents transparency issues */
            border-bottom: 2px solid #dee2e6; /* Stronger border for separation */
            box-shadow: 0 2px 2px -1px rgba(0, 0, 0, 0.1); /* Subtle shadow for depth */
        }

        /* Responsive: ensure modal fits on small screens */
        @media (max-width: 991px) {
            #detailsModal .modal-dialog {
                max-width: 92%;
                margin: 12px auto;
            }

            #detailsModal .table thead th {
                font-size: 0.78rem;
            }

            #detailsModal .table td {
                font-size: 0.84rem;
                padding: 8px 6px;
            }
        }

        #kmEditModal .modal-header {
            border-bottom: 2px solid #004085;
        }

        #kmEditModal .modal-footer {
            border-top: 1px solid #ddd;
        }

        #km_history_tbl thead th {
            background: #343a40 !important;
            color: white;
            position: sticky;
            top: 0;
            z-index: 2;
        }

        #km_history_tbl tbody tr:hover {
            background-color: #f1f1f1;
        }

        #km_history_tbl td, #km_history_tbl th {
            white-space: nowrap;
        }

        .progress-cell[data-progress="Not Started"] {
            color: #6c757d;
        }

        .progress-cell[data-progress="Partially Approved"] {
            color: #fd7e14;
            font-weight: 600;
        }

        .progress-cell[data-progress="Fully Approved"] {
            color: #198754;
            font-weight: 600;
        }

        .text-muted-fallback {
            color: #6c757d;
            font-style: italic;
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
                            <h4 class="mb-0">
                                <i class="fa fa-check-circle">&nbsp;</i>Monthly Claims Approval
                            </h4>
                            <ul class="nav navbar-right panel_toolbox mb-0">
                                <li>
                                    <a class="text-white collapse-link">
                                        <i class="fa fa-chevron-up"></i>
                                    </a>
                                </li>
                            </ul>
                        </div>

                        <div class="x_content bg-light p-3 rounded-bottom">
                            <div class="card-box table-responsive">
                                <asp:Panel ID="pnlRequests" runat="server">
                                    <asp:GridView ID="gvRequests" runat="server" CssClass="table table-sm table-striped table-hover table-bordered gentilia-table" ShowHeaderWhenEmpty="true"
                                        AutoGenerateColumns="false" AllowPaging="true" PageSize="20" OnPageIndexChanging="gvRequests_PageIndexChanging" OnRowCommand="gvRequests_RowCommand" DataKeyNames="ApprovalRequestId" GridLines="None">
                                        <HeaderStyle CssClass="gentilia-table-header" />
                                        <RowStyle CssClass="gentilia-row" />
                                        <AlternatingRowStyle CssClass="gentilia-row-alt" />

                                        <Columns>
                                            <asp:BoundField DataField="ApprovalRequestId" HeaderText="SL" HeaderStyle-Width="60px" />
                                            <asp:TemplateField HeaderText="Employee Name " HeaderStyle-Width="220px">
                                                <ItemTemplate>
                                                    <%# Eval("EmployeeName") %> (<%# Eval("RequesterLoginId") %>)
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="RequestDate" HeaderText="Requested Date" DataFormatString="{0:dd-MMM-yyyy hh:mm tt}" />
                                            <asp:TemplateField HeaderText="Claimed for Month" HeaderStyle-Width="150px">
                                                <ItemTemplate>
                                                    <span class="month-cell" data-month='<%# Eval("MonthNo") %>'><%# Eval("MonthNo") %></span>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Grand Claim Amount (₹)" HeaderStyle-Width="170px">
                                                <ItemTemplate>
                                                    <span class="grand-total">
                                                        <%# 
                                                          String.Format(
                                                              "{0:N2}",
                                                              Convert.ToDecimal(Eval("TotalClaimAmount"))
                                                            + Convert.ToDecimal(Eval("TotalExpenseAmount"))
                                                          ) 
                                                        %>
                                                    </span>
                                                </ItemTemplate>
                                            </asp:TemplateField>


                                            <asp:TemplateField HeaderText="Status" HeaderStyle-Width="120px">
                                                <ItemTemplate>
                                                    <span class="status-cell" data-status='<%# Eval("RequestStatus") %>'>
                                                        <%# Eval("RequestStatus") %>
                                                    </span>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Current Stage" HeaderStyle-Width="180px">
                                                <ItemTemplate>
                                                    <div class="d-flex flex-column">
                                                        <span class="progress-cell font-weight-bold" data-progress='<%# Eval("ApprovalProgress") %>'>
                                                            <%# Eval("ApprovalProgress") %>
                                                        </span>
                                                        <small class="text-muted">
                                                            <i class="fa fa-clock-o"></i>Pending: <%# Eval("PendingWithRole") %>
                                                        </small>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:TemplateField>


                                            <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="100px">
                                                <ItemTemplate>
                                                    <div class="action-buttons btn-group" role="group" aria-label="Actions">
                                                        <asp:LinkButton ID="lnkView" runat="server" CommandName="View" CommandArgument='<%# Eval("ApprovalRequestId") %>' CssClass="btn btn-sm gentilia-btn-primary">
                                                            <i class="fa fa-eye"></i> View
                                                        </asp:LinkButton>

                                                        <asp:LinkButton ID="lnkApproveQuick" runat="server" Visible="false" CommandName="ApproveQuick" CommandArgument='<%# Eval("ApprovalRequestId") %>' CssClass="btn btn-sm gentilia-btn-success">
                                                            <i class="fa fa-check"></i> Approve
                                                        </asp:LinkButton>
                                                    </div>
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
                        <div class="d-flex flex-column justify-content-center">
                            <h5 class="modal-title text-dark font-weight-bold mb-0">Request #<asp:Label ID="lblModalRequestId" runat="server" Text="0" />
                            </h5>
                            <div class="text-muted mt-1" style="font-size: 0.9rem;">
                                <i class="fa fa-user-circle-o mr-1"></i>
                                <asp:Label ID="lblRequesterName" runat="server" Font-Bold="true" Text="-" />
                                <span class="text-secondary ml-1">(<asp:Label ID="lblRequesterCode" runat="server" Text="-" />)</span>
                            </div>
                        </div>

                        <div class="ml-auto text-right">
                            <h5 class="mb-0 text-primary">
                                <small class="text-muted mr-1" style="font-size: 0.8rem;">Grand Total:</small>
                                ₹
                                <asp:Label ID="lblHeaderTotalClaim" runat="server" Text="0.00" Font-Bold="true" />
                            </h5>
                            <div class="mt-1">
                                <span class="badge badge-warning border px-2 py-1">Status:
                                    <asp:Label ID="lblHeaderStatus" runat="server" Text="Pending" />
                                </span>
                            </div>
                        </div>

                        <button type="button" class="close ml-3 align-self-start" data-dismiss="modal" aria-label="Close" onclick="$('#detailsModal').modal('hide');">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:UpdatePanel ID="upDetails" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>
                                <div class="table-responsive small">
                                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" CssClass="table table-sm table-hover table-bordered small"
                                        DataKeyNames="ApprovalRequestDetailId" OnRowDataBound="gvDetails_RowDataBound" OnRowCommand="gvDetails_RowCommand" ShowFooter="true" GridLines="None">
                                        <Columns>
                                            <asp:TemplateField HeaderText="" ItemStyle-Width="36px" HeaderStyle-HorizontalAlign="Center">
                                                <HeaderTemplate>
                                                    All<asp:CheckBox ID="chkSelect1" runat="server" CssClass="row-header-select" onclick="toggleHeaderCheckbox(this);" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkSelect" runat="server" CssClass="row-select-checkbox" />
                                                    <input type="hidden" runat="server" class="row-status" value='<%# Eval("ClaimStatus") %>' />
                                                    <%-- or Eval("DetailStatus") --%>
                                                </ItemTemplate>

                                                <FooterTemplate><small class="text-muted">Sel.</small></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Date & Day" ItemStyle-Width="10%">
                                                <ItemTemplate>
                                                    <%# Eval("ActivityDate", "{0:dd-MM-yyyy}") %><br />
                                                    <small class="text-muted"><%# Eval("Day") %></small>
                                                </ItemTemplate>
                                                <FooterTemplate><strong>Total :</strong>&nbsp;</FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="User_Name" HeaderText="Salesperson" Visible="false" ItemStyle-Width="16%" />

                                            <asp:TemplateField HeaderText="Distance (KM)">
                                                <ItemTemplate>
                                                    <span class="km-value"
                                                        data-activityid='<%# Eval("ActivityId") %>'
                                                        data-currentkm='<%# Eval("Snapshot_TotalDistanceKM") %>'
                                                        data-claimstatus='<%# Eval("ClaimStatus") %>'
                                                        id="spn_km_<%# Eval("ActivityId") %>">
                                                        <%# Eval("Snapshot_TotalDistanceKM", "{0:N2}") %>
                                                    </span>
                                                    &nbsp;
                                                <asp:LinkButton ID="lnkEditKM" runat="server" CommandName="EditKM"
                                                    CommandArgument='<%# Eval("ActivityId") %>' CssClass="btn btn-sm btn-outline-primary lnk-edit-km">
                                                    Edit
                                                </asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="Snapshot_CalculatedClaimAmount" HeaderText="TA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="9%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_DAAmount" HeaderText="DA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_HotelAmount" HeaderText="Hotel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_TravelAmount" HeaderText="Travel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal" HeaderText="Total (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="View Details" ItemStyle-Width="7%" HeaderStyle-HorizontalAlign="Center">
                                                <ItemTemplate>
                                                    <asp:Button ID="btnExp" runat="server" CssClass="btn btn-sm btn-outline-info btn-expenses" Text="Receipts" />
                                                    <asp:HiddenField ID="hfExpensesJson" runat="server" Value='<%# Eval("ExpensesJson") %>' />
                                                    <a href="javascript:void(0);" class="open-expenses-modal small" title="Open expense details">
                                                        <i class="fa fa-eye"></i>
                                                    </a>
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Status" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <span data-toggle="tooltip"
                                                        title='<%# 
                                                            Eval("ClaimStatus").ToString() == "Draft" ? "Draft – not submitted yet" :
                                                            Eval("ClaimStatus").ToString() == "Submitted" ? "Submitted – awaiting approval" :
                                                            Eval("ClaimStatus").ToString() == "Approved" ? "Approved by manager" :
                                                            Eval("ClaimStatus").ToString() == "Rejected" ? "Rejected – please review" :
                                                            Eval("ClaimStatus").ToString() == "Processed" ? "Processed in accounts" :
                                                            "Unknown status" %>'
                                                        class='<%# 
                                                            Eval("ClaimStatus").ToString() == "Draft" ? "badge badge-warning" :
                                                            Eval("ClaimStatus").ToString() == "Submitted" ? "badge badge-warning" :
                                                            Eval("ClaimStatus").ToString() == "Approved" ? "badge badge-success" :
                                                            Eval("ClaimStatus").ToString() == "Rejected" ? "badge badge-danger" :
                                                            Eval("ClaimStatus").ToString() == "Processed" ? "badge badge-primary" :
                                                            "badge badge-secondary" %>'>

                                                        <i class='<%# 
                                                            Eval("ClaimStatus").ToString() == "Draft" ? "fa fa-pencil-square-o" :
                                                            Eval("ClaimStatus").ToString() == "Submitted" ? "fa fa-hourglass-half" :
                                                            Eval("ClaimStatus").ToString() == "Approved" ? "fa fa-check-circle" :
                                                            Eval("ClaimStatus").ToString() == "Rejected" ? "fa fa-times-circle" :
                                                            Eval("ClaimStatus").ToString() == "Processed" ? "fa fa-cogs" :
                                                            "fa fa-clock-o" %>'></i>
                                                    </span>
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="DetailStatus" Visible="false" HeaderText="Detail Status" ItemStyle-Width="8%" />

                                            <asp:TemplateField HeaderText="Manager Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:TextBox ID="txtRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' CssClass="form-control form-control-sm row-remark" />
                                                </ItemTemplate>
                                                <FooterTemplate>
                                                    <asp:TextBox ID="txtFooterRemarks" Visible="false" runat="server" CssClass="form-control form-control-sm" Placeholder="Bulk remarks (fallback)"></asp:TextBox>
                                                </FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Activity Remarks" Visible="false">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblActivityRemarks" runat="server" Text='<%# Eval("ActivityRemarks") %>' CssClass="text-muted small" />
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Aprrove/ Reject" ItemStyle-Width="8%">
                                                <ItemTemplate>
                                                    <div class="btn-group" role="group">
                                                        <asp:Button ID="btnApproveRow" runat="server" UseSubmitBehavior="false" CssClass="btn btn-sm btn-outline-success" Text="✔" CommandName="ApproveRow" CommandArgument='<%# Eval("ApprovalRequestDetailId") %>' />
                                                        <asp:Button ID="btnRejectRow" runat="server" UseSubmitBehavior="false" CssClass="btn btn-sm btn-outline-danger ml-1" CausesValidation="false" Text="✖" CommandName="RejectRow" CommandArgument='<%# Eval("ApprovalRequestDetailId") %>' OnClientClick="if (!validateSingleReject(this)) return false;" />
                                                    </div>
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>

                                <div class="form-row align-items-center mt-2">
                                    <div class="col-auto">
                                        <asp:Button ID="btnSelectAll" runat="server" CssClass="btn btn-info btn-sm" Text="Select All" OnClientClick="selectAllRows(); return false;" />
                                        <asp:Button ID="btnClearSelection" runat="server" CssClass="btn btn-primary btn-sm ml-1" Text="Clear" OnClientClick="clearAllRows(); return false;" />
                                    </div>

                                    <div class="col">
                                        <asp:TextBox ID="txtBulkRemarks" runat="server" CssClass="form-control form-control-sm" Placeholder="Additional Remarks......"></asp:TextBox>
                                    </div>

                                    <div class="col-auto">
                                        <asp:Button ID="btnApproveSelected" runat="server" CssClass="btn btn-success btn-sm" Text="Approve Selected" OnClick="btnApproveSelected_Click" />
                                        <asp:Button ID="btnRejectSelected" runat="server" Visible="false"
                                            CssClass="btn btn-danger btn-sm ml-2"
                                            Text="Reject Selected"
                                            OnClick="btnRejectSelected_Click"
                                            OnClientClick="return validateReject();" />
                                    </div>
                                    <div class="col-auto">
                                        <asp:Button ID="btnMarkReadyForPayment" runat="server" Text="✔ Mark Ready for Payment" CssClass="btn btn-warning btn-sm" OnClick="btnMarkReadyForPayment_Click" Visible="false" />
                                        &nbsp;
                                        <asp:Button ID="btnReopenRequest" runat="server" Text="Reopen Request" CssClass="btn btn-warning" OnClick="btnReopenRequest_Click" Visible="false" />
                                    </div>
                                </div>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnApproveSelected" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="btnRejectSelected" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="gvDetails" EventName="RowCommand" />
                            </Triggers>
                        </asp:UpdatePanel>
                        <script type="text/javascript">
                            window.__currentUserLevel = <%= ViewState["CurrentUserLevel"] ?? 0 %>;
                        </script>
                    </div>

                    <div class="modal-footer">
                        <asp:Label ID="lblModalMessage" runat="server" CssClass="text-danger mr-auto" />
                        <button type="button" class="btn btn-secondary" data-dismiss="modal" onclick="$('#detailsModal').modal('hide');">Close</button>
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
                        <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
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
                                    <a href="#" id="km_modal_maplink"
                                        target="_blank"
                                        class="btn btn-sm btn-outline-primary"
                                        style="display: none;">
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

                        <div class="table-responsive" style="max-height: 300px; overflow-y: auto; border: 1px solid #ddd; border-radius: 5px;">
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
                        <button type="button" id="km_modal_save" class="btn btn-success"><i class="fa fa-save"></i>Save</button>
                        <button type="button" class="btn btn-secondary" data-dismiss="modal"><i class="fa fa-times"></i>Cancel</button>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade" id="expensesModal" tabindex="-1" role="dialog" aria-labelledby="expensesModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title"><i class="fa fa-receipt"></i>Expense Details</h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        <div id="expensesModalBody">Loading…</div>
                    </div>
                    <div class="modal-footer">
                        <small class="text-muted mr-auto">Tip: Click on Bill No to view the attachment</small>
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="expenseTooltip"
            style="display: none; position: absolute; z-index: 99999; max-width: 420px; background: #fff; border: 1px solid #ccc; box-shadow: 0 2px 8px rgba(0,0,0,.2); padding: 8px; font-size: 12px; border-radius: 4px;">
        </div>

    </div>

    <script type="text/javascript">
        $(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        // Open and close helpers (used by codebehind script registration too)
        function openDetailsModal() {
            $('#detailsModal').modal('show');
        }
        function closeDetailsModal() {
            $('#detailsModal').modal('hide');
        }
        function validateSingleReject(btn) {
            var row = btn.closest('tr');
            if (!row) return true;

            var remark = row.querySelector('.row-remark');
            if (!remark || remark.value.trim().length === 0) {
                if (remark) {
                    remark.classList.add('invalid-remark');
                    remark.focus();
                    if (remark.scrollIntoView) remark.scrollIntoView({ block: 'center' });
                    setTimeout(function () { remark.classList.remove('invalid-remark'); }, 2500);
                }
                new PNotify({
                    title: 'Remark Required',
                    text: 'Please enter Manager Remark before rejecting this row.',
                    type: 'error',
                    styling: 'bootstrap3',
                    delay: 2000
                });
                return false; // block postback
            }
            return true; // allow postback
        }

        // Called before Reject postback to ensure at least one selected and remarks provided
        function validateReject() {
            var anyChecked = $('#detailsModal').find('input[type=checkbox][id$=chkSelect]:checked').length > 0;
            if (!anyChecked) {
                alert('Please select at least one row to reject.');
                return false;
            }
            // check for per-row remarks OR bulk remarks
            var anyRowRemark = false;
            $('#detailsModal').find('input[type=text][id$=txtRowRemark]').each(function () {
                if ($(this).val().trim().length > 0) { anyRowRemark = true; }
            });
            var bulkRemark = $('#<%= txtBulkRemarks.ClientID %>').val().trim();
            if (!anyRowRemark && bulkRemark.length === 0) {
                alert('Please provide per-row remarks or a bulk remark for rejection.');
                return false;
            }
            return true;
        }
        // find the best container for the details area (UpdatePanel -> gvDetails -> modal)
        function getDetailsContainer() {
            var up = document.getElementById('<%= upDetails.ClientID %>');
            if (up) return up;
            var gv = document.getElementById('<%= gvDetails.ClientID %>');
            if (gv) return gv;
            return document.getElementById('detailsModal') || document;
        }

        function selectAllRows() {
            var container = getDetailsContainer();
            if (!container) return;
            Array.prototype.forEach.call(container.querySelectorAll('.row-select-checkbox'), function (cb) {
                cb.checked = true;
            });
            syncHeaderCheckbox();
        }

        function clearAllRows() {
            var container = getDetailsContainer();
            if (!container) return;
            Array.prototype.forEach.call(container.querySelectorAll('.row-select-checkbox'), function (cb) {
                cb.checked = false;
            });
            syncHeaderCheckbox();
        }

        // Keep header checkbox in sync with rows (call after binding)
        function syncHeaderCheckbox() {
            var container = getDetailsContainer();
            if (!container) return;
            var header = container.querySelector('.row-header-select');
            if (!header) return;
            var all = container.querySelectorAll('.row-select-checkbox');
            if (!all || all.length === 0) { header.checked = false; return; }
            var checked = container.querySelectorAll('.row-select-checkbox:checked').length;
            header.checked = (checked === all.length);
        }

        // Called before Reject postback to ensure at least one selected and remarks provided
        function validateReject() {
            var container = getDetailsContainer();
            if (!container) { alert('Details not loaded'); return false; }

            var anyChecked = container.querySelectorAll('.row-select-checkbox:checked').length > 0;
            if (!anyChecked) {
                alert('Please select at least one row to reject.');
                return false;
            }

            var anyRowRemark = false;
            Array.prototype.forEach.call(container.querySelectorAll('input.row-remark, textarea.row-remark'), function (el) {
                if (el.value.trim().length > 0) anyRowRemark = true;
            });

            var bulk = document.getElementById('<%= txtBulkRemarks.ClientID %>');
            var bulkVal = bulk ? bulk.value.trim() : '';

            if (!anyRowRemark && bulkVal.length === 0) {
                alert('Please provide per-row remarks or a bulk remark for rejection.');
                return false;
            }
            return true;
        }

        // Initialize tooltips and sync header after each partial postback
        function initDetailsUI() {
            var container = getDetailsContainer();
            if (!container) return;

            // re-init Bootstrap tooltips (if available)
            if (typeof ($) !== 'undefined' && $.fn && $.fn.tooltip) {
                $(container).find('[data-toggle="tooltip"]').tooltip();
            }

            // wire up row checkbox click to sync header checkbox
            var rows = container.querySelectorAll('.row-select-checkbox');
            Array.prototype.forEach.call(rows, function (cb) {
                // avoid adding multiple handlers
                if (!cb._rowHandlerAttached) {
                    cb.addEventListener('change', syncHeaderCheckbox);
                    cb._rowHandlerAttached = true;
                }
            });

            // sync header checkbox initial state
            syncHeaderCheckbox();
        }

        // attach init on full load
        document.addEventListener('DOMContentLoaded', function () {
            initDetailsUI();
        });

        // handle UpdatePanel async postbacks
        if (typeof (Sys) !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initDetailsUI();
            });
        }

        // Allow only these statuses to be selectable
        var ALLOWED_STATUSES = new Set(["Submitted"]); // add more as needed

        function getDetailsContainer() {
            return document.getElementById('<%= upDetails.ClientID %>') ||
           document.getElementById('<%= gvDetails.ClientID %>') ||
         document.getElementById('detailsModal') || document;
        }

        function getRowStatus(tr) {
            var hf = tr.querySelector('.row-status');
            return hf ? (hf.value || hf.getAttribute('value') || '').trim() : '';
        }

        function isSelectableRow(tr) {
            var st = getRowStatus(tr);
            return ALLOWED_STATUSES.has(st);
        }

        // If you use iCheck (Gentelella often does), adapt check/uncheck helpers:
        function setCheckboxChecked(cb, val) {
            // native
            cb.checked = !!val;

            // fire change so any custom UI reflects it
            try { cb.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }

            // iCheck support (if present)
            if (window.jQuery && jQuery(cb).iCheck) {
                if (val) jQuery(cb).iCheck('check'); else jQuery(cb).iCheck('uncheck');
            }
        }

        function enforceRowSelectability() {
            var container = getDetailsContainer();
            if (!container) return;

            container.querySelectorAll('tr').forEach(function (tr) {
                var cb = tr.querySelector('.row-select-checkbox');
                if (!cb) return;

                if (isSelectableRow(tr)) {
                    cb.disabled = false;
                    tr.classList.remove('row-disabled');
                } else {
                    setCheckboxChecked(cb, false);
                    cb.disabled = true;
                    tr.classList.add('row-disabled');
                }
            });
        }

        function syncHeaderCheckbox() {
            var container = getDetailsContainer();
            if (!container) return;
            var header = container.querySelector('.row-header-select');
            if (!header) return;

            var all = container.querySelectorAll('.row-select-checkbox:not(:disabled)');
            if (!all.length) { header.checked = false; return; }

            var checked = container.querySelectorAll('.row-select-checkbox:checked:not(:disabled)').length;
            header.checked = (checked === all.length);
        }

        function selectAllRows() {
            var container = getDetailsContainer();
            if (!container) return;

            enforceRowSelectability();

            var boxes = container.querySelectorAll('.row-select-checkbox:not(:disabled)');
            var changed = 0;
            boxes.forEach(function (cb) {
                if (!cb.checked) { setCheckboxChecked(cb, true); changed++; }
            });

            syncHeaderCheckbox();

            if (typeof PNotify !== 'undefined') {
                new PNotify({ title: 'Selected', text: 'Checked ' + changed + ' row(s).', type: 'info', styling: 'bootstrap3', delay: 1200 });
            }
        }

        function clearAllRows() {
            var container = getDetailsContainer();
            if (!container) return;

            var boxes = container.querySelectorAll('.row-select-checkbox:not(:disabled)');
            var changed = 0;
            boxes.forEach(function (cb) {
                if (cb.checked) { setCheckboxChecked(cb, false); changed++; }
            });

            syncHeaderCheckbox();

            if (typeof PNotify !== 'undefined') {
                new PNotify({ title: 'Cleared', text: 'Unchecked ' + changed + ' row(s).', type: 'notice', styling: 'bootstrap3', delay: 1200 });
            }
        }

        // Ensure this is the ONLY definition of this function
        function toggleHeaderCheckbox(headerChk) {
            // 1. Get the container (table)
            var $tbl = $(headerChk).closest('table');
            var isChecked = headerChk.checked;

            // 2. Select ONLY rows that are NOT disabled
            // This prevents "Already Approved" rows from being checked visually
            $tbl.find('.row-select-checkbox:not(:disabled)').each(function () {
                this.checked = isChecked;
        
                // Trigger change event if you have listeners for row highlighting
                $(this).trigger('change'); 
            });
        }

        function initDetailsUI() {
            var container = getDetailsContainer();
            if (!container) return;

            if (typeof ($) !== 'undefined' && $.fn && $.fn.tooltip) {
                $(container).find('[data-toggle="tooltip"]').tooltip();
            }

            enforceRowSelectability();

            container.querySelectorAll('.row-select-checkbox').forEach(function (cb) {
                if (!cb._rowHandlerAttached) {
                    cb.addEventListener('change', syncHeaderCheckbox);
                    cb._rowHandlerAttached = true;
                }
            });

            syncHeaderCheckbox();
        }

        document.addEventListener('DOMContentLoaded', initDetailsUI);
        if (typeof (Sys) !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initDetailsUI);
        }

        // Optional: change allowed statuses dynamically
        function setAllowedStatuses(arr) {
            ALLOWED_STATUSES = new Set((arr || []).map(function (s) { return (s || '').trim(); }));
            initDetailsUI();
        }

        $(function () {
            var monthNames = ["", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

            // Map status -> css class + optional icon
            var statusMap = {
                "Pending": { cls: "status-pending", icon: "fa-hourglass-half" },
                "Approved": { cls: "status-approved", icon: "fa-check-circle" },
                "Rejected": { cls: "status-rejected", icon: "fa-times-circle" },
                "Draft": { cls: "status-draft", icon: "fa-pencil-alt" }
                // any other statuses will use status-other
            };

            // Utility: format number to Indian currency style with ₹ (basic approach)
            function formatCurrencyINR(value) {
                if (value === null || value === undefined || value === "") return "";
                var num = parseFloat(value);
                if (isNaN(num)) return value;
                // Use toLocaleString for grouping — this uses user's locale; for forced 'en-IN' you can pass that but older browsers may not support
                try {
                    return "₹ " + num.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                } catch (e) {
                    return "₹ " + num.toFixed(2);
                }
            }

            // Transform each row: month, status, amount
            $("#<%= gvRequests.ClientID %> tbody tr").each(function () {
                var $tr = $(this);

                // Month: element with class month-cell
                var $monthCell = $tr.find(".month-cell");
                if ($monthCell.length) {
                    var monthNo = parseInt($monthCell.data("month"), 10);
                    if (!isNaN(monthNo) && monthNo >= 1 && monthNo <= 12) {
                        $monthCell.text(monthNames[monthNo]);
                    } else {
                        $monthCell.text(""); // blank invalid
                    }
                }

                // Status -> badge
                var $statusCell = $tr.find(".status-cell");
                if ($statusCell.length) {
                    var rawStatus = ($statusCell.data("status") || "").toString().trim();
                    var mapping = statusMap[rawStatus] || { cls: "status-other", icon: "fa-info-circle" };
                    var iconHtml = '<i class="fa ' + mapping.icon + '" aria-hidden="true" style="margin-right:6px"></i>';
                    var badge = $('<span class="status-badge ' + mapping.cls + '" title="' + rawStatus + '"></span>');
                    badge.html(iconHtml + rawStatus);
                    $statusCell.empty().append(badge);
                }

                // Total Claim -> formatted currency
                var $amtCell = $tr.find(".total-claim-cell");
                if ($amtCell.length) {
                    var amt = $amtCell.data("amount");
                    var formatted = formatCurrencyINR(amt);
                    $amtCell.text(formatted);
                }
            });

            // Optional: add tooltip for headers or badges (if theme includes bootstrap tooltip)
            if ($.fn.tooltip) {
                $("#<%= gvRequests.ClientID %> .status-badge").tooltip({ container: 'body' });
            }
        });
    </script>

    <script type="text/javascript">
        /* small enhancements: tooltips, row selection highlight, sticky header support (requires jQuery + bootstrap tooltip) */
        (function ($) {
            $(function () {

                // init bootstrap tooltips if present
                if ($.fn.tooltip) {
                    $('#detailsModal [data-toggle="tooltip"]').tooltip({ container: 'body' });
                }

                // highlight selected row when its checkbox checked (keeps server behavior unchanged)
                $('#detailsModal').on('change', '.row-select-checkbox', function () {
                    var $tr = $(this).closest('tr');
                    if (this.checked) $tr.addClass('selected-row'); else $tr.removeClass('selected-row');
                });

                // if header checkbox toggles all rows, respect selected styling
                window.toggleHeaderCheckbox = window.toggleHeaderCheckbox || function (headerChk) {
                    var $tbl = $(headerChk).closest('table');
                    var isChecked = headerChk.checked;
                    $tbl.find('.row-select-checkbox').each(function () {
                        this.checked = isChecked;
                        $(this).trigger('change');
                    });
                };

                // optional helpers used by existing buttons (keeps names used in markup)
                window.selectAllRows = window.selectAllRows || function () {
                    $('#detailsModal .row-select-checkbox').each(function () { this.checked = true; $(this).trigger('change'); });
                };
                window.clearAllRows = window.clearAllRows || function () {
                    $('#detailsModal .row-select-checkbox').each(function () { this.checked = false; $(this).trigger('change'); });
                };

                // On modal show, ensure any pre-checked boxes are highlighted
                $('#detailsModal').on('shown.bs.modal', function () {
                    $(this).find('.row-select-checkbox').each(function () {
                        if (this.checked) $(this).closest('tr').addClass('selected-row');
                    });
                });
            });
        })(jQuery);

        /* --- helpers (kept, lightly cleaned) ------------------------------------- */
        function htmlDecode(encoded) {
            if (!encoded) return encoded;
            var e = document.createElement('textarea');
            e.innerHTML = encoded;
            return e.value;
        }

        function htmlEncode(s) {
            if (s === null || s === undefined) return '';
            return String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function parseExpensesJson(jsonStr) {
            if (!jsonStr) return null;
            var trimmed = String(jsonStr).trim();
            try {
                if (trimmed.charAt(0) === '[' || trimmed.charAt(0) === '{') {
                    return JSON.parse(trimmed);
                }
            } catch (e) { /* fallthrough */ }

            try {
                var decoded = htmlDecode(jsonStr).trim();

                // Wrap if SQL missed [] or objects are concatenated
                if (decoded.indexOf('},{') !== -1 && decoded.charAt(0) === '{') {
                    decoded = '[' + decoded + ']';
                }
                if (decoded.indexOf('}{') !== -1 && decoded.indexOf('},{') === -1) {
                    decoded = decoded.replace(/}\s*{/g, '},{');
                    decoded = '[' + decoded + ']';
                }

                return JSON.parse(decoded);
            } catch (ex) {
                console.error("Failed to parse expenses JSON:", ex, jsonStr);
                return null;
            }
        }

        function maybeParsePolicySnapshot(policySnapshot) {
            if (!policySnapshot) return null;
            if (typeof policySnapshot === 'object') return policySnapshot;

            try {
                var text = htmlDecode(policySnapshot).trim();
                if (text.charAt(0) === '{' || text.charAt(0) === '[')
                    return JSON.parse(text);

                var obj = {};
                if (text.indexOf('=') !== -1 && text.indexOf(';') !== -1) {
                    var parts = text.split(';');
                    for (var i = 0; i < parts.length; i++) {
                        var kv = parts[i].split('=');
                        if (kv.length >= 2) obj[kv[0]] = kv.slice(1).join('=');
                    }
                    return obj;
                }
            } catch (e) {
                console.warn("PolicySnapshot parse failed:", e);
            }
            return null;
        }

        /* --- modal renderer ----------------------------------------------------- */
        function openExpensesModal(jsonStr) {
            var arr = parseExpensesJson(jsonStr);
            var $body = $('#expensesModalBody').empty();

            if (!arr || arr.length === 0) {
                $body.html('<p class="text-muted">No expenses for this activity.</p>');
                $('#expensesModal').modal('show');
                return;
            }

            var html = '<div class="table-responsive"><table class="table table-sm table-striped table-bordered mb-0">' +
                '<thead><tr>' +
                '<th style="width:40px">#</th>' +
                '<th>Type</th>' +
                '<th>Date</th>' +
                '<th>Purpose / Policy</th>' +
                '<th class="text-right">Amount</th>' +
                '<th>Bill No</th>' +
                '</tr></thead><tbody>';

            // --- build rows and accumulate total (replace your old for/total code) ---
            var totalAmt = 0;
            for (var idx = 0; idx < arr.length; idx++) {
                var ex = arr[idx] || {};

                // date formatting
                var dateStr = ex.ExpenseDate ? ex.ExpenseDate : '';
                try {
                    var d = new Date(ex.ExpenseDate);
                    if (!isNaN(d.getTime())) dateStr = d.toLocaleDateString();
                } catch (e) { /* ignore */ }

                // amount display and numeric value for totaling
                var amtVal = parseFloat(ex.Amount);
                var amtDisplay = '';
                if (!isNaN(amtVal)) {
                    totalAmt += amtVal;
                    amtDisplay = amtVal.toFixed(2);
                } else {
                    amtDisplay = (ex.Amount != null ? htmlEncode(ex.Amount) : '');
                }

                // bill column behavior (clickable when attachment exists)
                var billHtml = '';
                if (ex.BillNo) {
                    if (ex.AttachmentPath) {
                        var href = (ex.AttachmentPath || '#').replace(/"/g, '&quot;');
                        billHtml = '<a href="' + href + '" target="_blank" rel="noopener noreferrer">' + htmlEncode(ex.BillNo) + '</a>';
                    } else {
                        billHtml = '<span class="text-muted">' + htmlEncode(ex.BillNo) + '</span>';
                    }
                } else {
                    billHtml = '<span class="text-muted">—</span>';
                }

                // policy snapshot snippet (unchanged)
                var ps = maybeParsePolicySnapshot(ex.PolicySnapshot);
                var psHtml = '';
                if (ps) {
                    if (ps.PolicyMode) {
                        psHtml = '<small class="text-muted">Mode: <strong>' + htmlEncode(ps.PolicyMode) + '</strong></small>';
                    } else if (typeof ps === 'object') {
                        var keys = Object.keys(ps).slice(0, 3);
                        var parts = [];
                        for (var k = 0; k < keys.length; k++) parts.push(keys[k] + '=' + htmlEncode(ps[keys[k]]));
                        psHtml = '<small class="text-muted">' + parts.join('; ') + '</small>';
                    }
                }

                html += '<tr>' +
                    '<td>' + (idx + 1) + '</td>' +
                    '<td>' + htmlEncode(ex.ExpenseType || '') + '</td>' +
                    '<td>' + htmlEncode(dateStr) + '</td>' +
                    '<td>' + htmlEncode(ex.Purpose || '') + (psHtml ? '<br/>' + psHtml : '') + '</td>' +
                    '<td class="text-right">' + htmlEncode(amtDisplay) + (ex.Currency ? ' ' + htmlEncode(ex.Currency) : '') + '</td>' +
                    '<td>' + billHtml + '</td>' +
                    '</tr>';
            }

            // --- append total row (after loop) ---
            html += '<tr class="font-weight-bold bg-light">' +
                    '<td colspan="4" class="text-right">Total</td>' +
                    '<td class="text-right">' + totalAmt.toFixed(2) + '</td>' +
                    '<td></td>' +
                    '</tr>';

            html += '</tbody></table></div>';
            $body.html(html);
            $('#expensesModal').modal('show');
        }

        /* --- popover summary renderer ------------------------------------------ */
        function makePopoverContentFromJson(jsonStr) {
            var arr = parseExpensesJson(jsonStr);
            if (!arr) return '<small class="text-danger">Invalid data</small>';
            if (!arr.length) return '<small class="text-muted">No expenses</small>';

            var html = '<div class="small"><table class="table table-sm mb-0"><tbody>';
            for (var i = 0; i < Math.min(arr.length, 5) ; i++) {
                var ex = arr[i] || {};
                html += '<tr><td style="white-space:nowrap;">' + htmlEncode(ex.ExpenseType || '') +
                    '</td><td class="text-right" style="white-space:nowrap;">' +
                    (ex.Amount != null && !isNaN(ex.Amount) ? Number(ex.Amount).toFixed(2) : '') + ' ' +
                    (ex.Currency || '') + '</td></tr>';
            }
            if (arr.length > 5)
                html += '<tr><td colspan="2" class="text-muted">+' + (arr.length - 5) + ' more</td></tr>';
            html += '</tbody></table></div>';
            return html;
        }

        /* --- Event wiring and robust manual popover handling -------------------- */
        (function ($) {
            if (typeof $ === 'undefined') {
                console.error('jQuery not found — event wiring aborted.');
                return;
            }

            // utility: read payload trying multiple places
            function readExpensesPayloadInternal($btn) {
                if (!$btn || !$btn.length) return null;
                // 1) data-expenses attribute
                var attr = $btn.attr('data-expenses');
                if (attr !== undefined && attr !== null && $.trim(attr) !== '') {
                    return attr;
                }
                // 2) dataset (older browsers may not have this)
                try {
                    if ($btn[0] && $btn[0].dataset && $btn[0].dataset.expenses) {
                        return $btn[0].dataset.expenses;
                    }
                } catch (ignore) { }

                // 3) hidden field in same row/cell
                var $row = $btn.closest('tr');
                if ($row && $row.length) {
                    var $hf = $row.find('input[type=hidden][id$=hfExpensesJson]').first();
                    if ($hf && $hf.length && $.trim($hf.val()) !== '') {
                        return $hf.val();
                    }
                    $hf = $row.find('input[type=hidden].hfExpensesJson').first();
                    if ($hf && $hf.length && $.trim($hf.val()) !== '') {
                        return $hf.val();
                    }
                }

                // 4) jQuery data
                var jdata = $btn.data('expenses');
                if (jdata) {
                    return jdata;
                }

                return null;
            }

            // expose a safe global wrapper in case other pages call this
            window.readExpensesPayloadFromBtn = function (elOrJq) {
                try {
                    return readExpensesPayloadInternal($(elOrJq));
                } catch (ex) {
                    return null;
                }
            };

            // delegate click to open modal (keeps same behaviour)
            $(document).on('click', '.open-expenses-modal, .btn-expenses', function (e) {
                e.preventDefault();
                var $t = $(this);
                var payload = null;

                if ($t.hasClass('btn-expenses')) {
                    payload = readExpensesPayloadInternal($t);
                } else {
                    var $row = $t.closest('tr');
                    var $btnInRow = $row.find('.btn-expenses').first();
                    if ($btnInRow && $btnInRow.length) payload = readExpensesPayloadInternal($btnInRow);
                    else {
                        var hf = $row.find('input[type=hidden][id$=hfExpensesJson], input[type=hidden].hfExpensesJson').first();
                        if (hf && hf.length) payload = hf.val();
                    }
                }

                openExpensesModal(payload);
            });

            var hideTimer = null;

            function clearHideTimer() {
                if (hideTimer) {
                    clearTimeout(hideTimer);
                    hideTimer = null;
                }
            }

            $(document).on('mouseenter', '.btn-expenses', function (e) {
                var $btn = $(this);
                clearHideTimer();

                var payload = readExpensesPayloadInternal($btn);
                var contentHtml = makePopoverContentFromJson(payload);

                if (!contentHtml) return;

                var $tip = $('#expenseTooltip');
                $tip.html(contentHtml);

                // Position tooltip near button
                var offset = $btn.offset();
                var left = offset.left + $btn.outerWidth() + 8;
                var top = offset.top;

                $tip.css({
                    left: left + 'px',
                    top: top + 'px'
                }).fadeIn(100);
            });

            $(document).on('mouseleave', '.btn-expenses', function () {
                clearHideTimer();
                hideTimer = setTimeout(function () {
                    $('#expenseTooltip').fadeOut(100);
                }, 150);
            });

            // Keep tooltip visible when hovering it
            $(document).on('mouseenter', '#expenseTooltip', function () {
                clearHideTimer();
            }).on('mouseleave', '#expenseTooltip', function () {
                hideTimer = setTimeout(function () {
                    $('#expenseTooltip').fadeOut(100);
                }, 150);
            });

            $(window).on('beforeunload', function () {
                $('#expenseTooltip').hide();
            });


            $(document).on('contextmenu', '.btn-expenses, .open-expenses-modal', function (e) {
                var $el = $(this);
                console.log('DEBUG element:', $el.get(0), 'attrs:', $el.prop('attributes'));
            });

            console.info('Expenses event wiring active — hover .btn-expenses or click .open-expenses-modal');
        })(jQuery);
    </script>

    <script type="text/javascript">
        (function () {
            // ---------- persistent JS fallback ----------
            window.__kmCurrentActivityId = null;

            // ---------- helpers ----------
            function nowTag() { return (new Date()).toISOString(); }
            function parseNum(v) {
                if (v === null || v === undefined) return NaN;
                var s = (typeof v === 'string') ? v.trim() : v;
                if (s === '') return NaN;
                var n = parseFloat(s);
                return isNaN(n) ? NaN : n;
            }

            function log() { try { console.log.apply(console, arguments); } catch (e) { } }
            function logWarn() { try { console.warn.apply(console, arguments); } catch (e) { } }
            function logErr() { try { console.error.apply(console, arguments); } catch (e) { } }

            // ---------- history loader ----------
            function loadKmHistory(activityId) {
                log(nowTag(), "loadKmHistory called", activityId);
                $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Loading...</td></tr>');
                var start = Date.now();
                $.ajax({
                    type: 'POST',
                    url: 'ClaimsApproval.aspx/GetKMHistory',
                    data: JSON.stringify({ activityId: activityId }),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json'
                }).done(function (resp) {
                    log(nowTag(), "GetKMHistory resp:", resp);
                    var data = (resp && resp.d) ? resp.d : [];
                    var $tbody = $('#km_history_tbl tbody').empty();
                    if (!data || data.length === 0) {
                        $tbody.append('<tr><td colspan="5">No history</td></tr>');
                        return;
                    }
                    // Set From / To reference (top of modal)
                    var fromPlace = '';
                    var toPlace = '';

                    if (data && data.length > 0) {
                        var first = data[0];
                        fromPlace = first.TravelFromPlace || '';
                        toPlace = first.TravelToPlace || '';
                    }

                    $('#km_modal_fromplace').text(fromPlace || 'No data available');
                    $('#km_modal_toplace').text(toPlace || 'No data available');

                    // Google Maps link
                    if (fromPlace && toPlace) {
                        var mapsUrl =
                            'https://www.google.com/maps/dir/?api=1' +
                            '&origin=' + encodeURIComponent(fromPlace) +
                            '&destination=' + encodeURIComponent(toPlace) +
                            '&travelmode=driving';

                        $('#km_modal_maplink')
                            .attr('href', mapsUrl)
                            .show();
                    } else {
                        $('#km_modal_maplink')
                            .attr('href', '#')
                            .hide();
                    }

                    $('#km_modal_travel_ref').show();



                    data.forEach(function (r) {
                        var whenLocal = r.ModifiedDateTimeIST || r.ModifiedDateTime || '';
                        var oldKm = (r.OldKM === null || r.OldKM === undefined) ? '' : parseFloat(r.OldKM).toFixed(2);
                        var newKm = (r.NewKM === null || r.NewKM === undefined) ? '' : parseFloat(r.NewKM).toFixed(2);
                        $tbody.append(
                          '<tr>' +
                            '<td>' + $('<div/>').text(whenLocal).html() + '</td>' +
                            '<td>' + $('<div/>').text(oldKm).html() + '</td>' +
                            '<td>' + $('<div/>').text(newKm).html() + '</td>' +
                            '<td>' + $('<div/>').text(r.ModifiedBy || '').html() + '</td>' +
                            '<td>' + $('<div/>').text(r.Reason || '').html() + '</td>' +
                          '</tr>'
                        );
                    });
                }).fail(function (xhr, status, err) {
                    $('#km_history_tbl tbody').empty().append('<tr><td colspan="5">Error loading history</td></tr>');
                    logErr(nowTag(), "GetKMHistory failed", status, err, xhr && xhr.responseText);
                });
            }

            // ---------- open modal handler ----------
            function onEditKmClickHandler(e) {
                e.preventDefault();
                var $btn = $(this);
                var $row = $btn.closest('tr');
                var $span = $row.find('.km-value').first();

                // resolve claimStatus
                var claimStatus = ($span.data('claimstatus') || '').toString().trim();
                log(nowTag(), "Edit click: claimStatus=", claimStatus);

                //var allowed = ["Draft", "Submitted"];
                //if (allowed.indexOf(claimStatus) === -1) {
                //    $('#km_modal_message').html('<div class="text-warning">KM cannot be edited for status: ' + (claimStatus || 'Unknown') + '.</div>');
                //    return false;
                //}

                var allowed = ["Draft", "Submitted"];
                var isAuditor = (window.__currentUserLevel === 2);

                if (!isAuditor && allowed.indexOf(claimStatus) === -1) {
                    $('#km_modal_message').html(
                      '<div class="text-warning">KM cannot be edited for status: ' + (claimStatus || 'Unknown') + '.</div>'
                    );
                    return false;
                }


                // resolve activityId robustly (prefer span data attr)
                var activityId = $span.data('activityid') || $btn.data('activityid') || $btn.attr('data-activityid') || null;
                activityId = activityId ? parseInt(activityId, 10) : null;

                if (!activityId || isNaN(activityId) || activityId <= 0) {
                    // last-resort: try scanning tr for activity id tokens (unlikely)
                    var txt = $row.text();
                    var m = txt.match(/ActivityId\s*[:=]\s*(\d+)/i);
                    if (m && m[1]) activityId = parseInt(m[1], 10);
                }

                if (!activityId || isNaN(activityId) || activityId <= 0) {
                    $('#km_modal_message').html('<div class="text-danger">Invalid activity selected. See console.</div>');
                    logErr(nowTag(), "Invalid activityId. Row HTML:", $row.html());
                    return;
                }

                // Persist in several places: JS var, modal hidden input, outer hidden input
                window.__kmCurrentActivityId = activityId;
                $('#km_modal_activityid').val(activityId);
                $('#km_modal_activityid_outer').val(activityId); // safe outer fallback if present

                var currentKm = $span.data('currentkm');
                $('#km_modal_current').val(isNaN(parseNum(currentKm)) ? '' : parseNum(currentKm).toFixed(2));
                $('#km_modal_new').val(isNaN(parseNum(currentKm)) ? '' : parseNum(currentKm).toFixed(2));
                $('#km_modal_reason').val('');
                $('#km_modal_message').empty();

                loadKmHistory(activityId);
                $('#kmEditModal').modal('show');
                log(nowTag(), "Modal shown for activityId:", activityId);
            }

            // ---------- save handler ----------
            function onSaveKmClickHandler() {
                var $saveBtn = $(this);
                $saveBtn.prop('disabled', true);
                $('#km_modal_message').empty();

                // read activityId with fallbacks: inner hidden → outer hidden → JS var → span fallback
                var activityId = parseInt($('#km_modal_activityid').val(), 10);
                if (!activityId || isNaN(activityId) || activityId <= 0) {
                    activityId = parseInt($('#km_modal_activityid_outer').val(), 10) || window.__kmCurrentActivityId || null;
                }
                if (!activityId || isNaN(activityId) || activityId <= 0) {
                    // last resort: find first visible .km-value with same modal data (not ideal)
                    var $span = $('.km-value[data-activityid]').first();
                    activityId = $span && $span.length ? parseInt($span.data('activityid'), 10) : null;
                }

                var newKmRaw = $('#km_modal_new').val();
                var newKm = parseNum(newKmRaw);
                var curKm = parseNum($('#km_modal_current').val());
                var reason = ($('#km_modal_reason').val() || '').trim();

                log(nowTag(), "Save clicked inputs:", { activityId: activityId, newKmRaw: newKmRaw, newKm: newKm, curKm: curKm, reason: reason });

                if (!activityId || isNaN(activityId) || activityId <= 0) {
                    $('#km_modal_message').html('<div class="text-danger">Invalid activity selected.</div>');
                    logErr(nowTag(), "Invalid activityId at save:", activityId);
                    $saveBtn.prop('disabled', false);
                    return;
                }

                if (isNaN(newKm)) {
                    $('#km_modal_message').html('<div class="text-danger">Enter a valid New KM value.</div>');
                    logWarn(nowTag(), "newKm invalid:", newKmRaw);
                    $saveBtn.prop('disabled', false);
                    return;
                }

                if (Math.abs(newKm - curKm) > 0.001 && reason.length === 0) {
                    $('#km_modal_message').html('<div class="text-danger">Reason is required when KM is changed.</div>');
                    logWarn(nowTag(), "Reason required but missing for changed KM");
                    $saveBtn.prop('disabled', false);
                    return;
                }

                // keep JS var in sync
                window.__kmCurrentActivityId = activityId;
                $('#km_modal_activityid').val(activityId);
                $('#km_modal_activityid_outer').val(activityId);

                // call server
                var payload = { activityId: activityId, newKm: newKm, reason: reason };
                log(nowTag(), "Calling UpdateActivityKM payload:", payload);
                var start = Date.now();

                $.ajax({
                    type: 'POST',
                    url: 'ClaimsApproval.aspx/UpdateActivityKM',
                    data: JSON.stringify(payload),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json'
                }).done(function (resp) {
                    var dur = Date.now() - start;
                    log(nowTag(), "UpdateActivityKM done (ms):", dur, resp);
                    var r = resp && resp.d ? resp.d : resp;
                    if (r && r.success) {
                        log(nowTag(), "KM updated successfully for activityId:", activityId);
                        // update UI cell
                        var $span = $('.km-value[data-activityid="' + activityId + '"]').first();
                        if ($span && $span.length) {
                            $span.text(parseFloat(newKm).toFixed(2));
                            $span.data('currentkm', newKm);
                        }
                        $('#km_modal_message').html('<div class="text-success">Saved.</div>');
                        loadKmHistory(activityId);
                    } else {
                        var msg = (r && r.message) ? r.message : 'Error saving.';
                        $('#km_modal_message').html('<div class="text-danger">' + $('<div/>').text(msg).html() + '</div>');
                        logErr(nowTag(), "UpdateActivityKM returned success=false:", r);
                    }
                }).fail(function (xhr, status, error) {
                    var dur = Date.now() - start;
                    var serverMsg = xhr && xhr.responseText ? xhr.responseText : '';
                    $('#km_modal_message').html('<div class="text-danger">Error saving KM. See console for details.</div>');
                    logErr(nowTag(), "UpdateActivityKM failed (ms):", dur, "status:", status, "error:", error, "responseText:", serverMsg);
                }).always(function () {
                    $saveBtn.prop('disabled', false);
                    log(nowTag(), "UpdateActivityKM ajax complete (always)");
                });

            }

            // ---------- init bindings (idempotent) ----------
            function initKmHandlers() {
                // use namespaced events so we can .off safely
                $(document).off('click.km', '.btn-edit-km').on('click.km', '.btn-edit-km', onEditKmClickHandler);
                $('#km_modal_save').off('click.km').on('click.km', onSaveKmClickHandler);
                log(nowTag(), "KM handlers initialized/reinitialized");
            }

            // initial init
            $(function () {
                initKmHandlers();

                // rebind after partial postbacks (if UpdatePanels used)
                if (typeof (Sys) !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                        log(nowTag(), "endRequest: re-initializing KM handlers");
                        initKmHandlers();
                    });
                }
            });

        })();
    </script>

</asp:Content>
