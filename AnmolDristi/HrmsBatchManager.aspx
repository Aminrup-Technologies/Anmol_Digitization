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
                                            <asp:ListItem>Processed</asp:ListItem>
                                            <asp:ListItem>Pending</asp:ListItem>
                                            <asp:ListItem>Uploaded</asp:ListItem>
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
                                                    AutoGenerateColumns="False" GridLines="None" OnRowDataBound="gvBatches_RowDataBound">
                                                    <Columns>
                                                        <asp:TemplateField HeaderText="Select">
                                                            <ItemTemplate>
                                                                <asp:LinkButton ID="lnkSelect" runat="server" Text="Select"
                                                                    OnClientClick='<%# string.Format("onRowSelect(this, \"{0}\"); return false;", Eval("ImportBatchId")) %>'
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

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label font-weight-bold">Selected Batch:</label>
                                                <div class="col-md-8">
                                                    <asp:Label ID="lblSelectedBatch" runat="server" CssClass="text-primary font-weight-bold">(none selected)</asp:Label>
                                                </div>
                                            </div>

                                            <div class="x_panel" id="syncConfigurationSection">

                                                <div class="x_title">
                                                    <h2 class="hrms-sync-config-title">
                                                        Synchronization Configuration
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

                                                    <!-- Processing Mode -->

                                                    <div class="form-group row">

                                                        <label class="col-md-3 control-label">
                                                            Processing Mode
                                                            <button type="button"
                                                                    class="hrms-help-trigger"
                                                                    data-hrms-help-trigger="1"
                                                                    data-hrms-help-section="hrms-help-section-modes"
                                                                    title="About Processing Modes"
                                                                    aria-label="About Processing Modes">
                                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                                            </button>
                                                        </label>

                                                        <div class="col-md-9">

                                                            <asp:RadioButtonList
                                                                ID="rblProcessingMode"
                                                                runat="server"
                                                                RepeatDirection="Vertical"
                                                                AutoPostBack="true"
                                                                OnSelectedIndexChanged="rblProcessingMode_SelectedIndexChanged">

                                                                <asp:ListItem Text="Insert New Employees Only" Value="0" />

                                                                <asp:ListItem Text="Update All Existing Employees" Value="1" />

                                                                <asp:ListItem Text="Update Selected Fields Only"
                                                                    Value="2"
                                                                    Selected="True" />

                                                            </asp:RadioButtonList>

                                                        </div>

                                                    </div>

                                                    <hr />

                                                    <!-- Modules -->

                                                    <div class="form-group row">

                                                        <label class="col-md-3 control-label">
                                                            Synchronization Modules
                                                            <button type="button"
                                                                    class="hrms-help-trigger"
                                                                    data-hrms-help-trigger="1"
                                                                    data-hrms-help-section="hrms-help-section-modules"
                                                                    title="About Synchronization Modules"
                                                                    aria-label="About Synchronization Modules">
                                                                <i class="fa fa-info-circle" aria-hidden="true"></i>
                                                            </button>
                                                        </label>

                                                        <div class="col-md-9">

                                                            <asp:CheckBoxList
                                                                ID="chkModules"
                                                                runat="server"
                                                                RepeatDirection="Vertical">

                                                                <asp:ListItem
                                                                    Text="Synchronize Masters"
                                                                    Value="MASTER"
                                                                    Selected="True" />

                                                                <asp:ListItem
                                                                    Text="Synchronize Employees"
                                                                    Value="EMPLOYEE"
                                                                    Selected="True" />

                                                                <asp:ListItem
                                                                    Text="Synchronize Reporting Hierarchy"
                                                                    Value="HIERARCHY"
                                                                    Selected="True" />

                                                            </asp:CheckBoxList>

                                                        </div>

                                                    </div>

                                                    <hr />

                                                    <!-- Overwrite -->

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

                                                </div>

                                            </div>

                                            <div class="form-group">
                                                <asp:Button ID="btnRun" runat="server" Text="Run Import" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClientClick="return confirmRun();" OnClick="btnRun_Click" />

                                                &nbsp;<asp:Button ID="btnExportReport" runat="server" Text="Download Batch Report" CssClass="btn btn-info btn-sm" CausesValidation="false" OnClick="btnExportReport_Click" />

                                                &nbsp;<asp:Label ID="lblRunMessage" runat="server" CssClass="font-weight-bold text-success"></asp:Label>
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
                            <h2>Last RUN Result</h2>
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
                                    <asp:AsyncPostBackTrigger ControlID="btnRun" EventName="Click" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <%-- Single reusable slide-out panel (outside UpdatePanels) --%>
    <uc:HrmsSyncInfoPanel ID="ucHrmsSyncInfoPanel" runat="server" />

    <script type="text/javascript">
        window.HrmsBatchManagerConfig = {
            hfSelectedBatchId: '<%= hfSelectedBatch.ClientID %>',
            lblSelectedBatchId: '<%= lblSelectedBatch.ClientID %>',
            gvBatchesId: '<%= gvBatches.ClientID %>',
            btnRunId: '<%= btnRun.ClientID %>'
        };
    </script>
    <script src="<%= ResolveUrl("~/Scripts/HrmsBatchManager.js") %>" type="text/javascript"></script>
</asp:Content>
