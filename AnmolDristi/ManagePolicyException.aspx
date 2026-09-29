<%@ Page Title="Manage Policy Exceptions" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ManagePolicyException.aspx.cs" Inherits="AnmolDristi.ManagePolicyException" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container-fluid pe-ops">
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
                                <div class="col-md-2 col-sm-6 form-group pe-quick-search">
                                    <label>Employee Code</label>
                                    <asp:TextBox ID="txtSearchEmployeeCode" runat="server" CssClass="form-control form-control-sm pe-employee-autocomplete" autocomplete="off" />
                                    <div id="peRecentSearches" class="pe-recent-searches" aria-live="polite"></div>
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
                                    <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary btn-sm" OnClick="btnSearch_Click" OnClientClick="peSaveRecentSearch();" />
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
                                CssClass="table table-striped table-hover table-bordered table-sm pe-grid"
                                DataKeyNames="ExceptionId"
                                OnRowCommand="gvExceptions_RowCommand"
                                OnRowDataBound="gvExceptions_RowDataBound"
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
                                    <asp:TemplateField HeaderText="Effective To">
                                        <ItemTemplate>
                                            <div><%# Eval("EffectiveTo", "{0:yyyy-MM-dd}") %></div>
                                            <asp:Literal ID="litExpiryAlert" runat="server" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Status">
                                        <ItemTemplate>
                                            <%# GetStatusBadgeHtml(Eval("IsActive"), Eval("EffectiveFrom"), Eval("EffectiveTo")) %>
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
                                                OnClientClick='<%# GetToggleConfirmScript(Eval("IsActive")) %>' />
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
            <div class="modal-content pe-modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">
                        <asp:Label ID="lblModalTitle" runat="server" Text="Policy Exception" />
                        <asp:Label ID="lblReadOnlyBadge" runat="server" CssClass="label label-default pe-readonly-badge" Visible="false" Text="READ ONLY" />
                    </h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:Panel ID="pnlForm" runat="server" CssClass="pe-form-panel">
                        <asp:HiddenField ID="hfExceptionId" runat="server" Value="0" />
                        <asp:HiddenField ID="hfRegionId" runat="server" />
                        <asp:HiddenField ID="hfBranchId" runat="server" />
                        <asp:HiddenField ID="hfGradeId" runat="server" />
                        <asp:HiddenField ID="hfDesignationId" runat="server" />
                        <asp:HiddenField ID="hfIsEditMode" runat="server" Value="0" />

                        <asp:Panel ID="pnlReadOnlyBanner" runat="server" Visible="false" CssClass="alert alert-secondary pe-readonly-banner">
                            <strong>READ ONLY</strong> — Viewing Policy Exception details. History, Compare and Export remain available.
                        </asp:Panel>

                        <asp:Panel ID="pnlSummary" runat="server" Visible="false" CssClass="card mb-3 pe-summary-card">
                            <div class="card-header"><strong>Policy Exception Summary</strong></div>
                            <div class="card-body">
                                <div class="row pe-summary-grid">
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Employee</span><asp:Label ID="lblSummaryEmployee" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Status</span><asp:Literal ID="litSummaryStatus" runat="server" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Priority</span><asp:Label ID="lblSummaryPriority" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Current Version</span><asp:Label ID="lblSummaryVersion" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Effective</span><asp:Label ID="lblSummaryEffective" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Expiry</span><asp:Label ID="lblSummaryExpiry" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Last Updated</span><asp:Label ID="lblSummaryLastUpdated" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">Updated By</span><asp:Label ID="lblSummaryUpdatedBy" runat="server" CssClass="pe-summary-value" /></div>
                                    <div class="col-md-3 col-sm-6"><span class="pe-summary-label">History Entries</span><asp:Label ID="lblSummaryHistoryCount" runat="server" CssClass="pe-summary-value" /></div>
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlEffectivePeriod" runat="server" Visible="false" CssClass="pe-period-banner mb-3">
                            <asp:Literal ID="litEffectivePeriod" runat="server" />
                        </asp:Panel>

                        <div class="card mb-3">
                            <div class="card-header"><strong>Employee Information</strong></div>
                            <div class="card-body">
                                <div class="row">
                                    <div class="col-md-4 form-group">
                                        <label>Employee Code <span class="text-danger">*</span></label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtEmployeeCode" runat="server" CssClass="form-control pe-employee-autocomplete" autocomplete="off" />
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
                            <div class="card-header clearfix">
                                <strong>Policy Information</strong>
                                <span class="pull-right"><asp:Literal ID="litPolicyStatus" runat="server" /></span>
                            </div>
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

                        <div class="card mb-2 pe-override-card">
                            <div class="card-header"><strong>Policy Override</strong></div>
                            <div class="card-body">
                                <p class="text-muted small">Enter at least one override value. Leave blank fields unchanged in the resolved policy.</p>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">HQ/Ex HQ DA <asp:Literal ID="litOverrideDA" runat="server" /></label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtDA" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Outstation DA <asp:Literal ID="litOverrideOutstationDA" runat="server" /></label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtOutstationDA" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Own Arrangement <asp:Literal ID="litOverrideOwnAllowance" runat="server" /></label>
                                    <div class="col-md-8">
                                        <div class="input-group input-group-sm">
                                            <span class="input-group-addon">Rs.</span>
                                            <asp:TextBox ID="txtOwnAllowance" runat="server" CssClass="form-control" placeholder="Amount" />
                                        </div>
                                    </div>
                                </div>

                                <div class="form-group row align-items-center">
                                    <label class="col-md-4 col-form-label">Hotel Cap <asp:Literal ID="litOverrideHotelCap" runat="server" /></label>
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
                                                <asp:Literal ID="litOverrideRatePerKm" runat="server" />
                                            </div>
                                            <div class="col-sm-6" style="margin-bottom: 6px;">
                                                <div class="input-group input-group-sm">
                                                    <span class="input-group-addon">up to</span>
                                                    <asp:TextBox ID="txtMaxKM" runat="server" CssClass="form-control" placeholder="KM" />
                                                    <span class="input-group-addon">kms per month</span>
                                                </div>
                                                <asp:Literal ID="litOverrideMaxKM" runat="server" />
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

                        <asp:Panel ID="pnlAuditInfo" runat="server" Visible="false" CssClass="small text-muted pe-audit-info">
                            <hr />
                            <div>Created By: <asp:Label ID="lblCreatedBy" runat="server" /> on <asp:Label ID="lblCreatedOn" runat="server" /></div>
                            <div>Modified By: <asp:Label ID="lblModifiedBy" runat="server" /> on <asp:Label ID="lblModifiedOn" runat="server" /></div>
                        </asp:Panel>

                        <asp:Panel ID="pnlHistory" runat="server" Visible="false" CssClass="mt-3">
                            <div class="card mb-2 pe-history-card">
                                <div class="card-header clearfix">
                                    <strong>History</strong>
                                    <asp:Button ID="btnExportHistory" runat="server" Text="Export Excel" CssClass="btn btn-xs btn-success pull-right"
                                        OnClick="btnExportHistory_Click" CausesValidation="false" />
                                </div>
                                <div class="card-body">
                                    <asp:Label ID="lblHistoryVersionNote" runat="server" CssClass="text-info small d-block mb-2" />

                                    <div class="pe-history-toolbar clearfix mb-2">
                                        <button type="button" class="btn btn-xs btn-default" id="btnToggleTimeline" onclick="peToggleTimeline(); return false;">Collapse Timeline</button>
                                        <label class="pe-inline-check"><input type="checkbox" id="chkNewestFirst" onchange="peApplyHistoryFilters();" /> Newest First</label>
                                        <input type="text" id="txtHistorySearch" class="form-control form-control-sm pe-history-search" placeholder="Search versions..." onkeyup="peApplyHistoryFilters();" />
                                        <div class="pe-history-filters">
                                            <label><input type="checkbox" class="pe-action-filter" value="Create" checked="checked" onchange="peApplyHistoryFilters();" /> Created</label>
                                            <label><input type="checkbox" class="pe-action-filter" value="Update" checked="checked" onchange="peApplyHistoryFilters();" /> Updated</label>
                                            <label><input type="checkbox" class="pe-action-filter" value="Activate" checked="checked" onchange="peApplyHistoryFilters();" /> Activated</label>
                                            <label><input type="checkbox" class="pe-action-filter" value="Deactivate" checked="checked" onchange="peApplyHistoryFilters();" /> Deactivated</label>
                                        </div>
                                    </div>

                                    <div id="peTimelineSection">
                                        <h6 class="text-muted">Timeline</h6>
                                        <div id="peTimelineContainer" class="pe-timeline-container" data-order="asc">
                                            <asp:Repeater ID="rptHistoryTimeline" runat="server" OnItemCommand="rptHistoryTimeline_ItemCommand">
                                                <HeaderTemplate>
                                                    <div class="policy-history-timeline" id="peTimelineList">
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <div class="policy-history-timeline-item pe-timeline-item"
                                                         data-action='<%# Eval("ActionType") %>'
                                                         data-version='<%# Eval("VersionNo") %>'
                                                         data-search='<%# Server.HtmlEncode(string.Format("{0} {1} {2} {3}", Eval("VersionNo"), Eval("ActionType"), Eval("ChangedBy"), Eval("ChangeReason"))) %>'>
                                                        <div class="policy-history-timeline-version">Version <%# Eval("VersionNo") %>
                                                            <%# GetStatusBadgeHtml(Eval("IsActive"), Eval("EffectiveFrom"), Eval("EffectiveTo")) %>
                                                        </div>
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
                                                    <div class="policy-history-timeline-arrow pe-timeline-arrow">↓</div>
                                                </SeparatorTemplate>
                                                <FooterTemplate>
                                                    </div>
                                                </FooterTemplate>
                                            </asp:Repeater>
                                        </div>
                                        <asp:Label ID="lblTimelineEmpty" runat="server" CssClass="text-muted small" Visible="false" Text="No history recorded yet." />
                                        <div id="peTimelineFilterEmpty" class="text-muted small" style="display:none;">No versions match the current filters.</div>
                                    </div>

                                    <h6 class="text-muted mt-3">History Grid</h6>
                                    <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="False"
                                        CssClass="table table-striped table-bordered table-sm pe-history-grid"
                                        DataKeyNames="VersionNo"
                                        OnRowCommand="gvHistory_RowCommand"
                                        OnRowDataBound="gvHistory_RowDataBound"
                                        EmptyDataText="No history recorded yet.">
                                        <Columns>
                                            <asp:BoundField DataField="VersionNo" HeaderText="Version" ItemStyle-HorizontalAlign="Center" />
                                            <asp:BoundField DataField="ActionType" HeaderText="Action" />
                                            <asp:TemplateField HeaderText="Status">
                                                <ItemTemplate>
                                                    <%# GetStatusBadgeHtml(Eval("IsActive"), Eval("EffectiveFrom"), Eval("EffectiveTo")) %>
                                                </ItemTemplate>
                                            </asp:TemplateField>
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
                    <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click"
                        ValidationGroup="SaveException" OnClientClick="return peConfirmSave();" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <style type="text/css">
        .pe-ops .pe-quick-search { position: relative; }
        .pe-recent-searches { margin-top: 4px; font-size: 11px; color: #666; }
        .pe-recent-searches a { margin-right: 6px; cursor: pointer; color: #337ab7; text-decoration: none; }
        .pe-recent-searches a:hover { text-decoration: underline; }
        .pe-status-badge { display: inline-block; padding: 3px 8px; border-radius: 3px; font-size: 11px; font-weight: 600; white-space: nowrap; }
        .pe-status-active { background: #dff0d8; color: #3c763d; border: 1px solid #d6e9c6; }
        .pe-status-future { background: #fcf8e3; color: #8a6d3b; border: 1px solid #faebcc; }
        .pe-status-expired { background: #f2dede; color: #a94442; border: 1px solid #ebccd1; }
        .pe-status-inactive { background: #f5f5f5; color: #777; border: 1px solid #ddd; }
        .pe-expiry-alert { color: #a94442; font-size: 11px; font-weight: 600; margin-top: 2px; }
        .pe-override-badge { display: inline-block; margin-left: 6px; padding: 1px 6px; border-radius: 3px; font-size: 10px; font-weight: 600; vertical-align: middle; }
        .pe-override-yes { background: #dff0d8; color: #3c763d; border: 1px solid #d6e9c6; }
        .pe-override-no { background: #f5f5f5; color: #777; border: 1px solid #ddd; }
        .pe-period-banner {
            border: 1px solid #c9d6e3;
            background: linear-gradient(180deg, #f4f8fb 0%, #eef3f8 100%);
            border-radius: 4px;
            padding: 12px 16px;
            text-align: center;
        }
        .pe-period-banner .pe-period-title { font-weight: 700; font-size: 13px; color: #31708f; margin-bottom: 4px; }
        .pe-period-banner .pe-period-date { font-size: 16px; font-weight: 600; color: #333; }
        .pe-period-banner .pe-period-arrow { color: #999; margin: 2px 0; }
        .pe-summary-card .pe-summary-label { display: block; font-size: 11px; color: #888; text-transform: uppercase; letter-spacing: .02em; }
        .pe-summary-card .pe-summary-value { display: block; font-size: 14px; font-weight: 600; color: #333; margin-bottom: 8px; }
        .pe-readonly-banner { margin-bottom: 12px; }
        .pe-readonly-badge { margin-left: 8px; vertical-align: middle; }
        .pe-form-panel.pe-readonly-mode {
            background: #f0f0f0;
            border-radius: 4px;
            padding: 10px;
        }
        .pe-form-panel.pe-readonly-mode .form-control[readonly],
        .pe-form-panel.pe-readonly-mode .form-control:disabled {
            background-color: #e9ecef !important;
            cursor: default;
        }
        .pe-history-toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
        .pe-history-search { max-width: 220px; display: inline-block; }
        .pe-history-filters label { margin-right: 10px; font-weight: normal; font-size: 12px; }
        .pe-inline-check { margin: 0 8px; font-weight: normal; font-size: 12px; }
        .policy-history-timeline { margin-bottom: 12px; }
        .policy-history-timeline-item {
            border-left: 3px solid #5bc0de;
            padding: 8px 12px;
            margin-left: 8px;
            background: #f9fbfd;
            border-radius: 0 4px 4px 0;
        }
        .policy-history-timeline-item[data-action="Create"] { border-left-color: #5cb85c; }
        .policy-history-timeline-item[data-action="Update"] { border-left-color: #5bc0de; }
        .policy-history-timeline-item[data-action="Activate"] { border-left-color: #f0ad4e; }
        .policy-history-timeline-item[data-action="Deactivate"] { border-left-color: #777; }
        .policy-history-timeline-arrow { margin: 2px 0 2px 18px; color: #999; font-size: 12px; }
        .history-compare-table tr.history-changed td { background: #fff3cd; font-weight: 600; }
        .history-compare-table tr.history-unchanged td { color: #999; }
        .ui-autocomplete { z-index: 2000; max-height: 220px; overflow-y: auto; }
        @media (max-width: 767px) {
            .pe-history-search { max-width: 100%; width: 100%; }
            .pe-summary-grid > div { margin-bottom: 6px; }
        }
    </style>
    <script type="text/javascript">
        var PE_RECENT_KEY = 'peRecentEmployeeCodes';

        function showPolicyExceptionModal() {
            $('#modalPolicyException').modal('show');
            setTimeout(peApplyHistoryFilters, 50);
        }
        function setPolicyExceptionReadOnly(isReadOnly) {
            var $form = $('#<%= pnlForm.ClientID %>');
            var $history = $('#<%= pnlHistory.ClientID %>');
            $form.toggleClass('pe-readonly-mode', !!isReadOnly);
            $form.find('input,textarea,select,button')
                .not('[data-dismiss="modal"]')
                .not($history.find('input,textarea,select,button,a'))
                .prop('disabled', isReadOnly);
            $history.find('input,textarea,select,button,a').prop('disabled', false);
            $('#<%= btnLoadEmployee.ClientID %>').prop('disabled', isReadOnly);
            $('#<%= btnSave.ClientID %>').toggle(!isReadOnly);
        }
        function peConfirmSave() {
            var isEdit = $('#<%= hfIsEditMode.ClientID %>').val() === '1';
            if (!isEdit) return true;
            return confirm('Updating this Policy Exception creates a new immutable history version.\n\nProceed?');
        }
        function peGetRecent() {
            try {
                var raw = localStorage.getItem(PE_RECENT_KEY);
                var arr = raw ? JSON.parse(raw) : [];
                return Array.isArray(arr) ? arr : [];
            } catch (e) { return []; }
        }
        function peSaveRecentSearch() {
            var code = ($('#<%= txtSearchEmployeeCode.ClientID %>').val() || '').trim();
            if (!code) return true;
            var arr = peGetRecent().filter(function (x) { return String(x).toLowerCase() !== code.toLowerCase(); });
            arr.unshift(code);
            if (arr.length > 10) arr = arr.slice(0, 10);
            try { localStorage.setItem(PE_RECENT_KEY, JSON.stringify(arr)); } catch (e) { }
            return true;
        }
        function peRenderRecent() {
            var $box = $('#peRecentSearches');
            if (!$box.length) return;
            var arr = peGetRecent();
            if (!arr.length) { $box.html(''); return; }
            var html = 'Recent: ';
            for (var i = 0; i < arr.length; i++) {
                html += '<a href="#" data-code="' + $('<div/>').text(arr[i]).html() + '">' + $('<div/>').text(arr[i]).html() + '</a>';
            }
            $box.html(html);
            $box.find('a').on('click', function (e) {
                e.preventDefault();
                $('#<%= txtSearchEmployeeCode.ClientID %>').val($(this).data('code'));
            });
        }
        function peInitAutocomplete() {
            var $inputs = $('.pe-employee-autocomplete');
            if (!$inputs.length) return;
            function bindAuto($el) {
                if ($el.data('pe-auto')) return;
                $el.data('pe-auto', true);
                $el.autocomplete({
                    minLength: 1,
                    source: function (request, response) {
                        var term = (request.term || '').toLowerCase();
                        var recent = peGetRecent().filter(function (c) {
                            return String(c).toLowerCase().indexOf(term) >= 0;
                        }).map(function (c) {
                            return { label: c + ' (recent)', value: c };
                        });
                        $.ajax({
                            url: 'ManagePolicyException.aspx/SuggestEmployees',
                            type: 'POST',
                            contentType: 'application/json; charset=utf-8',
                            dataType: 'json',
                            data: JSON.stringify({ term: request.term }),
                            success: function (data) {
                                var items = [];
                                if (data && data.d) {
                                    for (var i = 0; i < data.d.length; i++) {
                                        var row = data.d[i];
                                        items.push({
                                            label: row.EmployeeCode + (row.EmployeeName ? ' — ' + row.EmployeeName : ''),
                                            value: row.EmployeeCode
                                        });
                                    }
                                }
                                var seen = {};
                                var merged = recent.concat(items).filter(function (x) {
                                    var k = String(x.value).toLowerCase();
                                    if (seen[k]) return false;
                                    seen[k] = true;
                                    return true;
                                }).slice(0, 12);
                                response(merged);
                            },
                            error: function () { response(recent); }
                        });
                    }
                });
            }
            if (typeof $.ui === 'undefined' || typeof $.ui.autocomplete === 'undefined') {
                $.getScript('https://code.jquery.com/ui/1.12.1/jquery-ui.min.js')
                    .done(function () { $inputs.each(function () { bindAuto($(this)); }); })
                    .fail(function () { console.warn('jQuery UI autocomplete unavailable'); });
            } else {
                $inputs.each(function () { bindAuto($(this)); });
            }
        }
        function peToggleTimeline() {
            var $section = $('#peTimelineSection .pe-timeline-container');
            var $btn = $('#btnToggleTimeline');
            if ($section.is(':visible')) {
                $section.slideUp(150);
                $btn.text('Expand Timeline');
            } else {
                $section.slideDown(150);
                $btn.text('Collapse Timeline');
            }
        }
        function peApplyHistoryFilters() {
            var term = ($('#txtHistorySearch').val() || '').toLowerCase();
            var allowed = {};
            $('.pe-action-filter:checked').each(function () { allowed[$(this).val().toLowerCase()] = true; });
            var $items = $('#peTimelineList .pe-timeline-item');
            if (!$items.length) return;

            var newestFirst = $('#chkNewestFirst').is(':checked');
            var $list = $('#peTimelineList');
            if ($list.data('order') !== (newestFirst ? 'desc' : 'asc')) {
                var nodes = $list.children('.pe-timeline-item, .pe-timeline-arrow').get();
                // Rebuild as item/arrow pairs from items only
                var items = $list.children('.pe-timeline-item').get();
                if (newestFirst) items = items.reverse();
                $list.empty();
                for (var i = 0; i < items.length; i++) {
                    $list.append(items[i]);
                    if (i < items.length - 1) {
                        $list.append($('<div class="policy-history-timeline-arrow pe-timeline-arrow">↓</div>'));
                    }
                }
                $list.data('order', newestFirst ? 'desc' : 'asc');
                $items = $('#peTimelineList .pe-timeline-item');
            }

            var visible = 0;
            $items.each(function () {
                var $it = $(this);
                var action = String($it.data('action') || '').toLowerCase();
                var search = String($it.data('search') || '').toLowerCase();
                var okAction = !!allowed[action];
                var okSearch = !term || search.indexOf(term) >= 0;
                var show = okAction && okSearch;
                $it.toggle(show);
                $it.next('.pe-timeline-arrow').toggle(false);
                if (show) visible++;
            });
            // Show arrows only between consecutive visible items
            var $visibleItems = $items.filter(':visible');
            $visibleItems.each(function (idx) {
                var $arrow = $(this).nextAll('.pe-timeline-arrow').first();
                if (idx < $visibleItems.length - 1 && $arrow.length) $arrow.show();
            });
            $('#peTimelineFilterEmpty').toggle(visible === 0 && $items.length > 0);

            // Filter history grid rows client-side
            $('.pe-history-grid tbody tr').each(function () {
                var $tr = $(this);
                var text = $tr.text().toLowerCase();
                var actionCell = ($tr.find('td').eq(1).text() || '').trim().toLowerCase();
                var okAction = !actionCell || !!allowed[actionCell];
                var okSearch = !term || text.indexOf(term) >= 0;
                $tr.toggle(okAction && okSearch);
            });
        }
        $(function () {
            peRenderRecent();
            peInitAutocomplete();
        });
    </script>
</asp:Content>
