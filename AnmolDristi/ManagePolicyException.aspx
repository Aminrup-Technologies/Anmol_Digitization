<%@ Page Title="Manage Policy Exceptions" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ManagePolicyException.aspx.cs" Inherits="AnmolDristi.ManagePolicyException" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid">
            <div class="page-title">
                <div class="title_left">
                    <h5>Manage Policy Exceptions</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Search</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Employee Code</label>
                                    <asp:TextBox ID="txtSearchEmployeeCode" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Employee Name</label>
                                    <asp:TextBox ID="txtSearchEmployeeName" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Region</label>
                                    <asp:DropDownList ID="ddlSearchRegion" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Branch</label>
                                    <asp:DropDownList ID="ddlSearchBranch" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Grade</label>
                                    <asp:DropDownList ID="ddlSearchGrade" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Designation</label>
                                    <asp:DropDownList ID="ddlSearchDesignation" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Active Status</label>
                                    <asp:DropDownList ID="ddlSearchActive" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Text="All" Value="" />
                                        <asp:ListItem Text="Active" Value="1" />
                                        <asp:ListItem Text="Inactive" Value="0" />
                                    </asp:DropDownList>
                                </div>
                                <div class="col-md-2 col-sm-6 form-group">
                                    <label>Effective Date</label>
                                    <asp:TextBox ID="txtSearchEffectiveDate" runat="server" CssClass="form-control form-control-sm" TextMode="Date" />
                                </div>
                                <div class="col-md-4 col-sm-12 form-group" style="padding-top: 24px;">
                                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary btn-sm" OnClick="btnSearch_Click" />
                                    <asp:Button ID="btnClearSearch" runat="server" Text="Clear" CssClass="btn btn-default btn-sm" OnClick="btnClearSearch_Click" CausesValidation="false" />
                                    <asp:Button ID="btnAddNew" runat="server" Text="Add Exception" CssClass="btn btn-success btn-sm" OnClick="btnAddNew_Click" CausesValidation="false" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Policy Exceptions</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content table-responsive">
                            <asp:Label ID="lblPageMessage" runat="server" CssClass="text-danger" />
                            <asp:GridView ID="gvExceptions" runat="server" AutoGenerateColumns="False"
                                CssClass="table table-striped table-hover table-bordered table-sm"
                                DataKeyNames="ExceptionId"
                                OnRowCommand="gvExceptions_RowCommand"
                                EmptyDataText="No policy exceptions found.">
                                <Columns>
                                    <asp:BoundField DataField="ExceptionId" HeaderText="Id" Visible="false" />
                                    <asp:BoundField DataField="EmployeeCode" HeaderText="Employee Code" />
                                    <asp:BoundField DataField="EmployeeName" HeaderText="Employee Name" />
                                    <asp:BoundField DataField="RegionName" HeaderText="Region" />
                                    <asp:BoundField DataField="BranchName" HeaderText="Branch" />
                                    <asp:BoundField DataField="GradeName" HeaderText="Grade" />
                                    <asp:BoundField DataField="DesignationName" HeaderText="Designation" />
                                    <asp:BoundField DataField="Priority" HeaderText="Priority" ItemStyle-HorizontalAlign="Center" />
                                    <asp:BoundField DataField="EffectiveFrom" HeaderText="Effective From" DataFormatString="{0:yyyy-MM-dd}" />
                                    <asp:BoundField DataField="EffectiveTo" HeaderText="Effective To" DataFormatString="{0:yyyy-MM-dd}" />
                                    <asp:TemplateField HeaderText="Status">
                                        <ItemTemplate>
                                            <span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "label label-success" : "label label-default" %>'>
                                                <%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="ModifiedOn" HeaderText="Last Modified" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                                    <asp:TemplateField HeaderText="Actions">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRow" CommandArgument='<%# Eval("ExceptionId") %>' CssClass="btn btn-xs btn-default" Text="Edit" CausesValidation="false" />
                                            <asp:LinkButton ID="lnkView" runat="server" CommandName="ViewRow" CommandArgument='<%# Eval("ExceptionId") %>' CssClass="btn btn-xs btn-info" Text="View" CausesValidation="false" />
                                            <asp:LinkButton ID="lnkToggle" runat="server"
                                                CommandName='<%# Convert.ToBoolean(Eval("IsActive")) ? "DeactivateRow" : "ActivateRow" %>'
                                                CommandArgument='<%# Eval("ExceptionId") %>'
                                                CssClass='<%# Convert.ToBoolean(Eval("IsActive")) ? "btn btn-xs btn-warning" : "btn btn-xs btn-success" %>'
                                                Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Deactivate" : "Activate" %>'
                                                CausesValidation="false"
                                                OnClientClick="return confirm('Change active status for this policy exception?');" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="modalPolicyException" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><asp:Label ID="lblModalTitle" runat="server" Text="Policy Exception" /></h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:Panel ID="pnlForm" runat="server">
                        <asp:HiddenField ID="hfExceptionId" runat="server" Value="0" />
                        <asp:HiddenField ID="hfRegionId" runat="server" />
                        <asp:HiddenField ID="hfBranchId" runat="server" />
                        <asp:HiddenField ID="hfGradeId" runat="server" />
                        <asp:HiddenField ID="hfDesignationId" runat="server" />

                        <div class="card mb-3">
                            <div class="card-header"><strong>Employee Information</strong></div>
                            <div class="card-body">
                                <div class="row">
                                    <div class="col-md-4 form-group">
                                        <label>Employee Code <span class="text-danger">*</span></label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtEmployeeCode" runat="server" CssClass="form-control" />
                                            <span class="input-group-btn">
                                                <asp:Button ID="btnLoadEmployee" runat="server" Text="Load" CssClass="btn btn-info" OnClick="btnLoadEmployee_Click" CausesValidation="false" />
                                            </span>
                                        </div>
                                        <asp:RequiredFieldValidator ID="rfvEmployeeCode" runat="server" ControlToValidate="txtEmployeeCode" ErrorMessage="Employee code is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="SaveException" />
                                    </div>
                                    <div class="col-md-8 form-group">
                                        <label>Employee Name</label>
                                        <asp:TextBox ID="txtEmployeeName" runat="server" CssClass="form-control" ReadOnly="true" BackColor="#f8f9fa" />
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="col-md-3 col-sm-6 form-group">
                                        <label class="font-weight-bold">Region</label>
                                        <asp:TextBox ID="txtRegionName" runat="server" CssClass="form-control" ReadOnly="true" BackColor="#f8f9fa" placeholder="Load employee to populate" />
                                    </div>
                                    <div class="col-md-3 col-sm-6 form-group">
                                        <label class="font-weight-bold">Branch</label>
                                        <asp:TextBox ID="txtBranchName" runat="server" CssClass="form-control" ReadOnly="true" BackColor="#f8f9fa" placeholder="Load employee to populate" />
                                    </div>
                                    <div class="col-md-3 col-sm-6 form-group">
                                        <label class="font-weight-bold">Grade</label>
                                        <asp:TextBox ID="txtGradeName" runat="server" CssClass="form-control" ReadOnly="true" BackColor="#f8f9fa" placeholder="Load employee to populate" />
                                    </div>
                                    <div class="col-md-3 col-sm-6 form-group">
                                        <label class="font-weight-bold">Designation</label>
                                        <asp:TextBox ID="txtDesignationName" runat="server" CssClass="form-control" ReadOnly="true" BackColor="#f8f9fa" placeholder="Load employee to populate" />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="card mb-3">
                            <div class="card-header"><strong>Policy Information</strong></div>
                            <div class="card-body">
                                <div class="row">
                                    <div class="col-md-3 form-group">
                                        <label>Priority <span class="text-danger">*</span></label>
                                        <asp:TextBox ID="txtPriority" runat="server" CssClass="form-control" Text="1" />
                                        <asp:RequiredFieldValidator ID="rfvPriority" runat="server" ControlToValidate="txtPriority" ErrorMessage="Priority is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="SaveException" />
                                    </div>
                                    <div class="col-md-3 form-group">
                                        <label>Effective From <span class="text-danger">*</span></label>
                                        <asp:TextBox ID="txtEffectiveFrom" runat="server" CssClass="form-control" TextMode="Date" />
                                        <asp:RequiredFieldValidator ID="rfvEffectiveFrom" runat="server" ControlToValidate="txtEffectiveFrom" ErrorMessage="Effective From is required." CssClass="text-danger" Display="Dynamic" ValidationGroup="SaveException" />
                                    </div>
                                    <div class="col-md-3 form-group">
                                        <label>Effective To</label>
                                        <asp:TextBox ID="txtEffectiveTo" runat="server" CssClass="form-control" TextMode="Date" />
                                    </div>
                                    <div class="col-md-3 form-group" style="padding-top: 28px;">
                                        <label><asp:CheckBox ID="chkIsActive" runat="server" Checked="true" /> Active</label>
                                        <asp:Label ID="lblIsActiveNote" runat="server" CssClass="text-muted small d-block" Visible="false" Text="New exceptions are always created as active." />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <asp:Panel ID="pnlChangeReason" runat="server" CssClass="card mb-3" Visible="false">
                            <div class="card-header"><strong>Reason for Change</strong></div>
                            <div class="card-body">
                                <asp:TextBox ID="txtChangeReason" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2"
                                    placeholder="Describe why this policy exception is being created or changed" />
                                <asp:Label ID="lblChangeReasonHint" runat="server" CssClass="text-muted small d-block mt-1" />
                                <asp:RequiredFieldValidator ID="rfvChangeReason" runat="server" ControlToValidate="txtChangeReason"
                                    ErrorMessage="Reason for Change is required for updates." CssClass="text-danger" Display="Dynamic"
                                    ValidationGroup="SaveException" Enabled="false" />
                            </div>
                        </asp:Panel>

                        <div class="card mb-2">
                            <div class="card-header"><strong>Policy Override</strong></div>
                            <div class="card-body">
                                <p class="text-muted small">Enter at least one override value. Leave blank fields unchanged in the resolved policy.</p>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">HQ/Ex HQ DA</label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtDA" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Outstation DA</label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtOutstationDA" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Own Arrangement</label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtOwnAllowance" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Hotel Cap</label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtHotelCap" runat="server" CssClass="form-control" placeholder="Amount" />
                                            <span class="input-group-addon">per night</span>
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Bike Travel Allowance</label>
                                    <div class="col-md-8">
                                        <div class="row">
                                            <div class="col-sm-6" style="margin-bottom: 6px;">
                                                <div class="input-group input-group-sm">
                                                    <span class="input-group-addon">@ Rs.</span>
                                                    <asp:TextBox ID="txtRatePerKm" runat="server" CssClass="form-control" placeholder="Rate" />
                                                    <span class="input-group-addon">/km</span>
                                                </div>
                                            </div>
                                            <div class="col-sm-6" style="margin-bottom: 6px;">
                                                <div class="input-group input-group-sm">
                                                    <span class="input-group-addon">up to</span>
                                                    <asp:TextBox ID="txtMaxKM" runat="server" CssClass="form-control" placeholder="KM" />
                                                    <span class="input-group-addon">kms per month</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row">
                                    <label class="col-md-4 col-form-label">Remarks</label>
                                    <div class="col-md-8">
                                        <asp:TextBox ID="txtRemarks" runat="server" CssClass="form-control form-control-sm" TextMode="MultiLine" Rows="2" />
                                    </div>
                                </div>
                            </div>
                        </div>

                        <asp:Panel ID="pnlAuditInfo" runat="server" Visible="false" CssClass="small text-muted">
                            <hr />
                            <div>Created By: <asp:Label ID="lblCreatedBy" runat="server" /> on <asp:Label ID="lblCreatedOn" runat="server" /></div>
                            <div>Modified By: <asp:Label ID="lblModifiedBy" runat="server" /> on <asp:Label ID="lblModifiedOn" runat="server" /></div>
                        </asp:Panel>

                        <asp:Panel ID="pnlHistory" runat="server" Visible="false" CssClass="mt-3">
                            <div class="card mb-2">
                                <div class="card-header clearfix">
                                    <strong>History</strong>
                                    <asp:Button ID="btnExportHistory" runat="server" Text="Export Excel" CssClass="btn btn-xs btn-success pull-right"
                                        OnClick="btnExportHistory_Click" CausesValidation="false" />
                                </div>
                                <div class="card-body">
                                    <asp:Label ID="lblHistoryVersionNote" runat="server" CssClass="text-info small d-block mb-2" />

                                    <h6 class="text-muted">Timeline</h6>
                                    <asp:Repeater ID="rptHistoryTimeline" runat="server" OnItemCommand="rptHistoryTimeline_ItemCommand">
                                        <HeaderTemplate>
                                            <div class="policy-history-timeline">
                                        </HeaderTemplate>
                                        <ItemTemplate>
                                            <div class="policy-history-timeline-item">
                                                <div class="policy-history-timeline-version">Version <%# Eval("VersionNo") %></div>
                                                <div><strong><%# Eval("ActionType") %></strong></div>
                                                <div class="text-muted small"><%# Eval("ChangedBy") %></div>
                                                <div class="text-muted small"><%# string.Format("{0:dd-MMM-yyyy HH:mm}", Eval("ChangedOn")) %></div>
                                                <div class="text-muted small"><%# Eval("ChangeReason") %></div>
                                                <asp:LinkButton ID="lnkTimelineView" runat="server"
                                                    CommandName="ViewHistoryVersion"
                                                    CommandArgument='<%# Eval("VersionNo") %>'
                                                    CssClass="btn btn-xs btn-link"
                                                    Text="View snapshot"
                                                    CausesValidation="false" />
                                            </div>
                                        </ItemTemplate>
                                        <SeparatorTemplate>
                                            <div class="policy-history-timeline-arrow">↓</div>
                                        </SeparatorTemplate>
                                        <FooterTemplate>
                                            </div>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                    <asp:Label ID="lblTimelineEmpty" runat="server" CssClass="text-muted small" Visible="false" Text="No history recorded yet." />

                                    <h6 class="text-muted mt-3">History Grid</h6>
                                    <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="False"
                                        CssClass="table table-striped table-bordered table-sm"
                                        DataKeyNames="VersionNo"
                                        OnRowCommand="gvHistory_RowCommand"
                                        EmptyDataText="No history recorded yet.">
                                        <Columns>
                                            <asp:BoundField DataField="VersionNo" HeaderText="Version" ItemStyle-HorizontalAlign="Center" />
                                            <asp:BoundField DataField="ActionType" HeaderText="Action" />
                                            <asp:BoundField DataField="ChangedBy" HeaderText="Changed By" />
                                            <asp:BoundField DataField="ChangedOn" HeaderText="Changed On" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                                            <asp:BoundField DataField="ChangeReason" HeaderText="Reason" />
                                            <asp:TemplateField HeaderText="">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lnkViewVersion" runat="server"
                                                        CommandName="ViewHistoryVersion"
                                                        CommandArgument='<%# Eval("VersionNo") %>'
                                                        CssClass="btn btn-xs btn-default"
                                                        Text="View"
                                                        CausesValidation="false" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>

                                    <h6 class="text-muted mt-3">Compare Versions</h6>
                                    <div class="row">
                                        <div class="col-md-4 form-group">
                                            <label>Version A</label>
                                            <asp:DropDownList ID="ddlCompareVersionA" runat="server" CssClass="form-control form-control-sm" />
                                        </div>
                                        <div class="col-md-4 form-group">
                                            <label>Version B</label>
                                            <asp:DropDownList ID="ddlCompareVersionB" runat="server" CssClass="form-control form-control-sm" />
                                        </div>
                                        <div class="col-md-4 form-group" style="padding-top: 24px;">
                                            <asp:Button ID="btnCompareHistory" runat="server" Text="Compare" CssClass="btn btn-info btn-sm"
                                                OnClick="btnCompareHistory_Click" CausesValidation="false" />
                                        </div>
                                    </div>
                                    <asp:Label ID="lblCompareMessage" runat="server" CssClass="text-danger small d-block" />
                                    <asp:Panel ID="pnlCompareResults" runat="server" Visible="false">
                                        <asp:GridView ID="gvHistoryCompare" runat="server" AutoGenerateColumns="False"
                                            CssClass="table table-bordered table-sm history-compare-table"
                                            OnRowDataBound="gvHistoryCompare_RowDataBound">
                                            <Columns>
                                                <asp:BoundField DataField="Field" HeaderText="Field" />
                                                <asp:BoundField DataField="VersionAValue" HeaderText="Version A" />
                                                <asp:BoundField DataField="VersionBValue" HeaderText="Version B" />
                                            </Columns>
                                        </asp:GridView>
                                    </asp:Panel>
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Label ID="lblFormMessage" runat="server" CssClass="text-danger" />
                    </asp:Panel>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" ValidationGroup="SaveException" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <style type="text/css">
        .policy-history-timeline { margin-bottom: 12px; }
        .policy-history-timeline-item {
            border-left: 3px solid #5bc0de;
            padding: 6px 10px;
            margin-left: 8px;
            background: #f9fbfd;
        }
        .policy-history-timeline-arrow {
            margin: 2px 0 2px 18px;
            color: #999;
            font-size: 12px;
        }
        .policy-history-timeline .policy-history-timeline-item:last-of-type + .policy-history-timeline-arrow { display: none; }
        .history-compare-table tr.history-changed td { background: #fff3cd; font-weight: 600; }
        .history-compare-table tr.history-unchanged td { color: #999; }
    </style>
    <script type="text/javascript">
        function showPolicyExceptionModal() {
            $('#modalPolicyException').modal('show');
        }
        function setPolicyExceptionReadOnly(isReadOnly) {
            var $form = $('#<%= pnlForm.ClientID %>');
            var $history = $('#<%= pnlHistory.ClientID %>');
            $form.find('input,textarea,select,button')
                .not('[data-dismiss="modal"]')
                .not($history.find('input,textarea,select,button,a'))
                .prop('disabled', isReadOnly);
            // Keep history controls clickable in read-only View mode.
            $history.find('input,textarea,select,button,a').prop('disabled', false);
            $('#<%= btnLoadEmployee.ClientID %>').prop('disabled', isReadOnly);
            $('#<%= btnSave.ClientID %>').toggle(!isReadOnly);
        }
    </script>
</asp:Content>
