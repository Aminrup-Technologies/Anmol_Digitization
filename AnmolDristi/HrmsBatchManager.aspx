<%@ Page Title="HRMS Master Data Import" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="HrmsBatchManager.aspx.cs" Inherits="AnmolDristi.HrmsBatchManager" %>
<%@ Register Src="~/Controls/HrmsSyncInfoPanel.ascx" TagPrefix="uc" TagName="HrmsSyncInfoPanel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="<%= ResolveUrl("~/css/HrmsBatchManager.css") %>" rel="stylesheet" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>HRMS Data Import Pipeline</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Manage Import Batches</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <asp:Label ID="lblPageError" runat="server" CssClass="text-danger font-weight-bold d-block mb-2"></asp:Label>

                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">From (IST)</label>
                                        <asp:TextBox ID="txtFrom" runat="server" CssClass="form-control form-control-sm" type="date" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">To (IST)</label>
                                        <asp:TextBox ID="txtTo" runat="server" CssClass="form-control form-control-sm" type="date" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Status</label>
                                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Value="">-- All --</asp:ListItem>
                                            <asp:ListItem>Uploaded</asp:ListItem>
                                            <asp:ListItem>MastersSynced</asp:ListItem>
                                            <asp:ListItem>Processing</asp:ListItem>
                                            <asp:ListItem>Processed</asp:ListItem>
                                            <asp:ListItem>Failed</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-3" style="padding-top: 24px;">
                                    <asp:Button ID="btnFilter" runat="server" Text="Search" CssClass="btn btn-primary btn-sm" OnClick="btnFilter_Click" />
                                    &nbsp;<asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-default btn-sm" OnClick="btnClear_Click" />
                                </div>
                            </div>

                            <asp:UpdatePanel runat="server" ID="upBatches">
                                <ContentTemplate>
                                    <div class="row" style="margin-top: 10px;">
                                        <div class="col-md-12">
                                            <div class="card-box table-responsive">
                                                <asp:GridView ID="gvBatches" runat="server" CssClass="table table-striped table-hover table-bordered table-sm table-condensed"
                                                    AutoGenerateColumns="False" GridLines="None" OnRowDataBound="gvBatches_RowDataBound" OnRowCommand="gvBatches_RowCommand">
                                                    <Columns>
                                                        <asp:TemplateField HeaderText="Select">
                                                            <ItemTemplate>
                                                                <asp:LinkButton ID="lnkSelect" runat="server" Text="Select"
                                                                    CommandName="SelectBatch"
                                                                    CommandArgument='<%# Eval("ImportBatchId") %>'
                                                                    CssClass="btn btn-info btn-sm m-0" />
                                                            </ItemTemplate>
                                                        </asp:TemplateField>

                                                        <asp:BoundField DataField="ImportBatchId" HeaderText="Batch ID" />
                                                        <asp:BoundField DataField="AnyFile" HeaderText="File Name" />
                                                        <asp:BoundField DataField="TotalRows" HeaderText="Total Rows" />
                                                        <asp:BoundField DataField="ProcessedCount" HeaderText="Processed" />
                                                        <asp:BoundField DataField="ErrorCount" HeaderText="Errors" />
                                                        <asp:BoundField DataField="WaitingCount" HeaderText="Waiting" />
                                                        <asp:BoundField DataField="Status" HeaderText="Status" />
                                                        <asp:BoundField DataField="MasterSyncLabel" HeaderText="Masters" />
                                                        <asp:BoundField DataField="LastModifiedIST" HeaderText="Upload Date (IST)" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                                                    </Columns>
                                                </asp:GridView>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="row" style="margin-top: 12px;">
                                        <div class="col-md-12">
                                            <div class="ln_solid"></div>
                                            <asp:HiddenField ID="hfSelectedBatch" runat="server" />
                                            <asp:HiddenField ID="hfMasterSyncCompleted" runat="server" Value="0" />

                                            <%-- Section A: Batch Information --%>
                                            <div class="x_panel hrms-step-panel">
                                                <div class="x_title">
                                                    <h2>Batch Information</h2>
                                                    <div class="clearfix"></div>
                                                </div>
                                                <div class="x_content">
                                                    <div class="row hrms-batch-meta">
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">Selected Batch</span>
                                                            <asp:Label ID="lblSelectedBatch" runat="server" CssClass="hrms-meta-value text-primary">(none selected)</asp:Label>
                                                        </div>
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">File Name</span>
                                                            <asp:Label ID="lblBatchFileName" runat="server" CssClass="hrms-meta-value">—</asp:Label>
                                                        </div>
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">Import Date (IST)</span>
                                                            <asp:Label ID="lblBatchUploadDate" runat="server" CssClass="hrms-meta-value">—</asp:Label>
                                                        </div>
                                                    </div>
                                                    <div class="row hrms-batch-meta">
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">Total Records</span>
                                                            <asp:Label ID="lblBatchTotalRows" runat="server" CssClass="hrms-meta-value">—</asp:Label>
                                                        </div>
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">Processing Status</span>
                                                            <asp:Label ID="lblBatchStatus" runat="server" CssClass="hrms-meta-value">—</asp:Label>
                                                        </div>
                                                        <div class="col-md-4">
                                                            <span class="hrms-meta-label">Master Synchronization</span>
                                                            <asp:Label ID="lblMasterSyncStatus" runat="server" CssClass="hrms-meta-value">Not completed</asp:Label>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="x_panel hrms-step-panel" id="syncConfigurationSection">
                                                <div class="x_title">
                                                    <h2 class="hrms-sync-config-title">
                                                        Synchronization Workflow
                                                        <button type="button"
                                                                class="hrms-help-trigger"
                                                                data-hrms-help-trigger="1"
                                                                data-hrms-help-section="hrms-help-section-flow"
                                                                title="Synchronization information"
                                                                aria-label="Open synchronization information">
                                                            <i class="fa fa-info-circle" aria-hidden="true"></i>
                                                        </button>
                                                    </h2>
                                                    <div class="clearfix"></div>
                                                </div>

                                                <div class="x_content">

                                                    <%-- Section B: Step 1 Master Sync --%>
                                                    <div class="hrms-step-block">
                                                        <h4>
                                                            <span class="hrms-step-badge">STEP 1</span>
                                                            Synchronize Master Data
                                                        </h4>
                                                        <p class="text-muted">
                                                            Insert missing organization masters and update descriptions from this batch.
                                                            Employee actions stay disabled until this step succeeds.
                                                        </p>
                                                        <asp:Button ID="btnSyncMasters" runat="server"
                                                            Text="Synchronize Masters"
                                                            CssClass="btn btn-primary btn-sm"
                                                            CausesValidation="false"
                                                            OnClientClick="return confirmSyncMasters();"
                                                            OnClick="btnSyncMasters_Click" />
                                                    </div>

                                                    <hr />

                                                    <%-- Section C: Step 2 Select Action --%>
                                                    <asp:Panel ID="pnlEmployeeActions" runat="server">
                                                        <div class="hrms-step-block">
                                                            <h4>
                                                                <span class="hrms-step-badge">STEP 2</span>
                                                                Select Action
                                                            </h4>
                                                            <asp:Label ID="lblEmployeeActionsLocked" runat="server"
                                                                CssClass="hrms-lock-note"
                                                                Visible="false"
                                                                Text="Employee actions are locked until master synchronization succeeds for the selected batch." />

                                                            <asp:RadioButtonList
                                                                ID="rblAction"
                                                                runat="server"
                                                                RepeatDirection="Vertical"
                                                                AutoPostBack="true"
                                                                OnSelectedIndexChanged="rblAction_SelectedIndexChanged">
                                                                <asp:ListItem Text="Insert New Employees" Value="INSERT" />
                                                                <asp:ListItem Text="Update Existing Employees" Value="UPDATE" />
                                                                <asp:ListItem Text="Update Reporting Hierarchy" Value="HIERARCHY" />
                                                            </asp:RadioButtonList>
                                                        </div>

                                                        <hr />

                                                        <%-- Section D: Configure selected action --%>
                                                        <div class="hrms-step-block">
                                                            <h4>
                                                                <span class="hrms-step-badge">STEP 3</span>
                                                                Configure Selected Action
                                                            </h4>

                                                            <asp:Panel ID="pnlInsertConfig" runat="server" Visible="false" CssClass="alert alert-info">
                                                                <strong>Insert New Employees</strong>
                                                                <p class="mb-0">
                                                                    Only EmpCodes that do not already exist in MST_UserMaster will be inserted.
                                                                    Existing employees will not be updated. The dedicated hierarchy procedure will not run.
                                                                    New rows may still receive reporting-manager values that were present on the staging row (existing insert semantics).
                                                                </p>
                                                            </asp:Panel>

                                                            <asp:Panel ID="pnlHierarchyConfig" runat="server" Visible="false" CssClass="alert alert-info">
                                                                <strong>Update Reporting Hierarchy</strong>
                                                                <p>Only these MST_UserMaster columns are updated, and only when the manager codes resolve:</p>
                                                                <ul class="mb-0">
                                                                    <li>ReportingManager</li>
                                                                    <li>ReportingManagerCode</li>
                                                                    <li>ReportingManagersManagerName</li>
                                                                    <li>ReportingManagersManagerCode</li>
                                                                </ul>
                                                            </asp:Panel>

                                                            <asp:Panel ID="pnlUpdateConfig" runat="server" Visible="false">
                                                                <div
                                                                    id="divOverwriteFields"
                                                                    runat="server">

                                                                    <div class="form-group row">
                                                                        <label class="col-md-3 control-label">
                                                                            Fields to Overwrite
                                                                            <button type="button"
                                                                                    class="hrms-help-trigger"
                                                                                    data-hrms-help-trigger="1"
                                                                                    data-hrms-help-section="hrms-help-section-overwrite"
                                                                                    title="About Fields to Overwrite"
                                                                                    aria-label="About Fields to Overwrite">
                                                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                                                            </button>
                                                                        </label>
                                                                        <div class="col-md-9">
                                                                            <p class="text-muted">
                                                                                Only checked fields are sent to the database. Unchecked columns are not updated.
                                                                            </p>
                                                                            <asp:Button
                                                                                ID="btnSelectAll"
                                                                                runat="server"
                                                                                Text="Select All"
                                                                                CssClass="btn btn-info btn-xs"
                                                                                OnClick="btnSelectAll_Click" />
                                                                            <asp:Button
                                                                                ID="btnClearAll"
                                                                                runat="server"
                                                                                Text="Clear"
                                                                                CssClass="btn btn-warning btn-xs"
                                                                                OnClick="btnClearAll_Click" />
                                                                            <hr />
                                                                            <asp:Repeater
                                                                                ID="rptOverwriteFields"
                                                                                runat="server">
                                                                                <ItemTemplate>
                                                                                    <div class="checkbox">
                                                                                        <label>
                                                                                            <asp:CheckBox
                                                                                                ID="chkField"
                                                                                                runat="server"
                                                                                                Checked='<%# Convert.ToBoolean(Eval("IsDefault")) %>' />
                                                                                            <span class="text-muted"><%# Eval("Category") %> — </span>
                                                                                            <%# Eval("DisplayName") %>
                                                                                            <asp:HiddenField
                                                                                                ID="hfFieldCode"
                                                                                                runat="server"
                                                                                                Value='<%# Eval("FieldCode") %>' />
                                                                                        </label>
                                                                                    </div>
                                                                                </ItemTemplate>
                                                                            </asp:Repeater>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            </asp:Panel>

                                                            <asp:Panel ID="pnlNoActionConfig" runat="server" Visible="true" CssClass="text-muted">
                                                                Select an action in Step 2 to configure and execute it.
                                                            </asp:Panel>
                                                        </div>

                                                        <hr />

                                                        <%-- Section E: Execute --%>
                                                        <div class="hrms-step-block">
                                                            <h4>
                                                                <span class="hrms-step-badge">STEP 4</span>
                                                                Execute Selected Action
                                                            </h4>
                                                            <asp:Button ID="btnExecuteInsert" runat="server"
                                                                Text="Insert New Employees"
                                                                CssClass="btn btn-success btn-sm"
                                                                Visible="false"
                                                                CausesValidation="false"
                                                                OnClientClick="return confirmExecute('Insert New Employees');"
                                                                OnClick="btnExecuteInsert_Click" />
                                                            <asp:Button ID="btnExecuteUpdate" runat="server"
                                                                Text="Update Selected Employee Fields"
                                                                CssClass="btn btn-success btn-sm"
                                                                Visible="false"
                                                                CausesValidation="false"
                                                                OnClientClick="return confirmExecute('Update Selected Employee Fields');"
                                                                OnClick="btnExecuteUpdate_Click" />
                                                            <asp:Button ID="btnExecuteHierarchy" runat="server"
                                                                Text="Update Reporting Hierarchy"
                                                                CssClass="btn btn-success btn-sm"
                                                                Visible="false"
                                                                CausesValidation="false"
                                                                OnClientClick="return confirmExecute('Update Reporting Hierarchy');"
                                                                OnClick="btnExecuteHierarchy_Click" />
                                                        </div>
                                                    </asp:Panel>

                                                    <div class="form-group" style="margin-top: 16px;">
                                                        <asp:Button ID="btnExportReport" runat="server" Text="Download Batch Report" CssClass="btn btn-info btn-sm" CausesValidation="false" OnClick="btnExportReport_Click" />
                                                        &nbsp;<asp:Label ID="lblRunMessage" runat="server" CssClass="font-weight-bold text-success"></asp:Label>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnFilter" EventName="Click" />
                                    <asp:AsyncPostBackTrigger ControlID="btnClear" EventName="Click" />
                                    <asp:PostBackTrigger ControlID="btnExportReport" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Last Action Result</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:UpdatePanel runat="server" ID="upResult">
                                <ContentTemplate>
                                    <div class="row">
                                        <div class="col-md-12 col-sm-12">
                                            <div class="card-box table-responsive">
                                                <asp:GridView ID="gvResult" runat="server" CssClass="table table-striped table-hover table-bordered table-sm table-condensed"
                                                    AutoGenerateColumns="true" EmptyDataText="No result returned." />
                                            </div>
                                        </div>
                                    </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnSyncMasters" EventName="Click" />
                                    <asp:AsyncPostBackTrigger ControlID="btnExecuteInsert" EventName="Click" />
                                    <asp:AsyncPostBackTrigger ControlID="btnExecuteUpdate" EventName="Click" />
                                    <asp:AsyncPostBackTrigger ControlID="btnExecuteHierarchy" EventName="Click" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <uc:HrmsSyncInfoPanel ID="ucHrmsSyncInfoPanel" runat="server" />

    <script type="text/javascript">
        window.HrmsBatchManagerConfig = {
            hfSelectedBatchId: '<%= hfSelectedBatch.ClientID %>',
            lblSelectedBatchId: '<%= lblSelectedBatch.ClientID %>',
            gvBatchesId: '<%= gvBatches.ClientID %>',
            btnSyncMastersId: '<%= btnSyncMasters.ClientID %>',
            btnExecuteInsertId: '<%= btnExecuteInsert.ClientID %>',
            btnExecuteUpdateId: '<%= btnExecuteUpdate.ClientID %>',
            btnExecuteHierarchyId: '<%= btnExecuteHierarchy.ClientID %>'
        };
    </script>
    <script src="<%= ResolveUrl("~/Scripts/HrmsBatchManager.js") %>" type="text/javascript"></script>
</asp:Content>
