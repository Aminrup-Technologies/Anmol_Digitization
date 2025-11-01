<%@ Page Title="AIL | SO Cliams Approval" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ClaimsApproval.aspx.cs" Inherits="AnmolDristi.ClaimsApproval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
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
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hfActionRequestId" runat="server" />
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>SO Claims Approval</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <!-- Header grid: Pending Requests -->
                                        <asp:Panel ID="pnlRequests" runat="server">
                                            <asp:GridView ID="gvRequests" runat="server"
                                                CssClass="table table-sm table-striped" AutoGenerateColumns="false"
                                                AllowPaging="true" PageSize="20" OnPageIndexChanging="gvRequests_PageIndexChanging"
                                                OnRowCommand="gvRequests_RowCommand"
                                                DataKeyNames="ApprovalRequestId">
                                                <Columns>
                                                    <asp:BoundField DataField="ApprovalRequestId" HeaderText="ID" HeaderStyle-Width="60px" />
                                                    <asp:BoundField DataField="RequesterLoginId" HeaderText="Requester" />
                                                    <asp:BoundField DataField="RequestDate" HeaderText="Request Date" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                                                    <asp:BoundField DataField="YearNo" HeaderText="Year" HeaderStyle-Width="60px" />
                                                    <asp:BoundField DataField="MonthNo" HeaderText="Month" HeaderStyle-Width="60px" />
                                                    <asp:BoundField DataField="TotalClaimAmount" HeaderText="Total Claim" DataFormatString="{0:N2}" />
                                                    <asp:BoundField DataField="Status" HeaderText="Status" />
                                                    <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="160px">
                                                        <ItemTemplate>
                                                            <asp:LinkButton ID="lnkView" runat="server" CommandName="View" CommandArgument='<%# Eval("ApprovalRequestId") %>'
                                                                CssClass="btn btn-sm btn-primary">View & Approve</asp:LinkButton>
                                                            &nbsp;
                                                              <asp:LinkButton ID="lnkApproveQuick" runat="server" Visible="false" CommandName="ApproveQuick" CommandArgument='<%# Eval("ApprovalRequestId") %>'
                                                                  CssClass="btn btn-sm btn-success">Approve</asp:LinkButton>
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
            </div>
        </div>

        <div id="detailsModal" class="modal fade" tabindex="-1" role="dialog" aria-hidden="true" runat="server" clientidmode="Static">
            <div class="modal-dialog modal-xl" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Approval Request Details -
                            <asp:Label ID="lblModalRequestId" runat="server" Text="0" /></h5>
                        <div class="ml-3">
                            <small class="text-muted">Status:<asp:Label ID="lblHeaderStatus" runat="server" Text="Pending" />
                                &nbsp; | &nbsp;
                                Total Claim:<asp:Label ID="lblHeaderTotalClaim" runat="server" Text="0.00" />
                            </small>
                        </div>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close" onclick="$('#detailsModal').modal('hide');">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:UpdatePanel ID="upDetails" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>
                                <div class="table-responsive">
                                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" CssClass="table table-sm table-hover table-bordered"
                                        DataKeyNames="ApprovalRequestDetailId" OnRowDataBound="gvDetails_RowDataBound" OnRowCommand="gvDetails_RowCommand" ShowFooter="true" GridLines="None">
                                        <Columns>
                                            <asp:TemplateField HeaderText="" ItemStyle-Width="36px" HeaderStyle-HorizontalAlign="Center">
                                                <HeaderTemplate>
                                                    All<asp:CheckBox ID="chkSelect1" runat="server" CssClass="row-header-select" onclick="toggleHeaderCheckbox(this);"/>
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

                                            <asp:BoundField DataField="User_Name" HeaderText="Salesperson" ItemStyle-Width="16%" />
                                            <asp:BoundField DataField="Snapshot_TotalDistanceKM" HeaderText="Distance (KM)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="9%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_CalculatedClaimAmount" HeaderText="TA Claim (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="9%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_DAAmount" HeaderText="DA (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_HotelAmount" HeaderText="Hotel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_TravelAmount" HeaderText="Travel (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="8%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:BoundField DataField="Snapshot_ExpenseTotal" HeaderText="Snapshot Total (₹)" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" ItemStyle-Width="10%" FooterStyle-HorizontalAlign="Right" />
                                            <asp:TemplateField HeaderText="Claim Status" ItemStyle-Width="5%">
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
                                                            Eval("ClaimStatus").ToString() == "Submitted" ? "badge badge-info" :
                                                            Eval("ClaimStatus").ToString() == "Approved" ? "badge badge-success" :
                                                            Eval("ClaimStatus").ToString() == "Rejected" ? "badge badge-danger" :
                                                            Eval("ClaimStatus").ToString() == "Processed" ? "badge badge-primary" :
                                                            "badge badge-secondary" %>'>

                                                        <i class='<%# 
                                                            Eval("ClaimStatus").ToString() == "Draft" ? "fa fa-pencil-square-o" :
                                                            Eval("ClaimStatus").ToString() == "Submitted" ? "fa fa-paper-plane" :
                                                            Eval("ClaimStatus").ToString() == "Approved" ? "fa fa-check-circle" :
                                                            Eval("ClaimStatus").ToString() == "Rejected" ? "fa fa-times-circle" :
                                                            Eval("ClaimStatus").ToString() == "Processed" ? "fa fa-cogs" :
                                                            "fa fa-clock-o" %>'></i>
                                                    </span>
                                                    <%# Eval("ClaimStatus") %>
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:BoundField DataField="DetailStatus" Visible="true" HeaderText="Detail Status" ItemStyle-Width="8%" />

                                            <asp:TemplateField HeaderText="Manager Remarks" HeaderStyle-Width="15%">
                                                <ItemTemplate>
                                                    <asp:TextBox ID="txtRowRemark" runat="server" Text='<%# Eval("DetailRemarks") %>' CssClass="form-control form-control-sm row-remark" />
                                                </ItemTemplate>
                                                <FooterTemplate>
                                                    <asp:TextBox ID="txtFooterRemarks" Visible="false" runat="server" CssClass="form-control form-control-sm" Placeholder="Bulk remarks (fallback)"></asp:TextBox>
                                                </FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Activity Remarks" Visible="true">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblActivityRemarks" runat="server" Text='<%# Eval("ActivityRemarks") %>' CssClass="text-muted small" />
                                                </ItemTemplate>
                                                <FooterTemplate></FooterTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Action" ItemStyle-Width="110px">
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
                                </div>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnApproveSelected" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="btnRejectSelected" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="gvDetails" EventName="RowCommand" />
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>

                    <div class="modal-footer">
                        <asp:Label ID="lblModalMessage" runat="server" CssClass="text-danger mr-auto" />
                        <button type="button" class="btn btn-secondary" data-dismiss="modal" onclick="$('#detailsModal').modal('hide');">Close</button>
                    </div>
                </div>
            </div>
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

        // Called from header checkbox: pass `this`
        function toggleHeaderCheckbox(chk) {
            var container = getDetailsContainer();
            if (!container) return;
            Array.prototype.forEach.call(container.querySelectorAll('.row-select-checkbox'), function (cb) {
                cb.checked = chk.checked;
            });
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

        //-------

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

        function toggleHeaderCheckbox(chk) {
            var container = getDetailsContainer();
            if (!container) return;

            enforceRowSelectability();

            var boxes = container.querySelectorAll('.row-select-checkbox:not(:disabled)');
            boxes.forEach(function (cb) { setCheckboxChecked(cb, chk.checked); });

            syncHeaderCheckbox();
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


    </script>

</asp:Content>
