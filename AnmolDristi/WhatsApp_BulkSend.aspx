<%@ Page Title="WhatsApp Bulk Send" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="WhatsApp_BulkSend.aspx.cs" Inherits="AnmolDristi.WhatsApp_BulkSend" %>

<asp:Content ID="headStyle" ContentPlaceHolderID="head" runat="server">
    <style>
        .validation-stat { display: inline-block; margin-right: 18px; font-size: 13px; }
        .validation-stat strong { font-size: 14px; }
    </style>
    <script type="text/javascript">
        function waToggleContentMode() {
            var none = document.getElementById('<%= rbModeNone.ClientID %>');
            var common = document.getElementById('<%= rbModeCommon.ClientID %>');
            var individual = document.getElementById('<%= rbModeIndividual.ClientID %>');
            var workbook = document.getElementById('<%= rbModeWorkbook.ClientID %>');
            var url = document.getElementById('<%= rbModeUrl.ClientID %>');
            if (!none) return;
            document.getElementById('<%= pnlCommonContent.ClientID %>').style.display = common.checked ? '' : 'none';
            document.getElementById('<%= pnlIndividualContent.ClientID %>').style.display = individual.checked ? '' : 'none';
            document.getElementById('<%= pnlWorkbookContent.ClientID %>').style.display = workbook.checked ? '' : 'none';
            document.getElementById('<%= pnlUrlContent.ClientID %>').style.display = url.checked ? '' : 'none';
        }
        if (window.jQuery) {
            jQuery(document).ready(function () { waToggleContentMode(); });
            if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () { waToggleContentMode(); });
            }
        }
    </script>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5><i class="fa fa-whatsapp"></i> WhatsApp Bulk Send</h5>
                </div>
            </div>
            <div class="clearfix"></div>

            <!-- ============================================================ -->
            <!-- SECTION 1 — Campaign Configuration                           -->
            <!-- ============================================================ -->
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Campaign Configuration</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="form-horizontal form-label-left">
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Campaign Name <span class="required">*</span></label>
                                    <div class="col-md-4 col-sm-6">
                                        <asp:TextBox ID="txtCampaignName" runat="server" CssClass="form-control form-control-sm" />
                                    </div>
                                    <div class="col-md-5 col-sm-3">
                                        <span class="text-muted">Descriptive name for this run (e.g. "September Attendance Report").</span>
                                    </div>
                                </div>
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Template Name <span class="required">*</span></label>
                                    <div class="col-md-4 col-sm-6">
                                        <asp:TextBox ID="txtTemplateName" runat="server" CssClass="form-control form-control-sm" />
                                    </div>
                                    <div class="col-md-5 col-sm-3">
                                        <span class="text-muted">Exact name of the pre-approved WhatsApp template in CEOITBOX.</span>
                                    </div>
                                </div>
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Language</label>
                                    <div class="col-md-4 col-sm-6">
                                        <asp:DropDownList ID="ddlLanguage" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Text="English (en)" Value="en" Selected="True" />
                                            <asp:ListItem Text="Hindi (hi)" Value="hi" />
                                            <asp:ListItem Text="Tamil (ta)" Value="ta" />
                                            <asp:ListItem Text="Telugu (te)" Value="te" />
                                            <asp:ListItem Text="Marathi (mr)" Value="mr" />
                                            <asp:ListItem Text="Kannada (kn)" Value="kn" />
                                            <asp:ListItem Text="Bengali (bn)" Value="bn" />
                                            <asp:ListItem Text="Gujarati (gu)" Value="gu" />
                                            <asp:ListItem Text="Malayalam (ml)" Value="ml" />
                                            <asp:ListItem Text="Punjabi (pa)" Value="pa" />
                                            <asp:ListItem Text="Urdu (ur)" Value="ur" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="col-md-5 col-sm-3">
                                        <span class="text-muted">Must match the template's configured language.</span>
                                    </div>
                                </div>
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Template Header</label>
                                    <div class="col-md-4 col-sm-6">
                                        <asp:DropDownList ID="ddlTemplateHeader" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Text="Body only (no media)" Value="BodyOnly" Selected="True" />
                                            <asp:ListItem Text="IMAGE header" Value="Image" />
                                            <asp:ListItem Text="DOCUMENT header (unvalidated)" Value="Document" />
                                            <asp:ListItem Text="VIDEO header (unvalidated)" Value="Video" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="col-md-5 col-sm-3">
                                        <span class="text-muted">Must match the template's configured header format in WhatsApp.</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============================================================ -->
            <!-- SECTION 2 — Upload Recipient Data                            -->
            <!-- ============================================================ -->
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Upload Recipient Data</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:Label ID="lblUploadOk" runat="server" CssClass="text-success font-weight-bold d-block mb-2" Visible="false" />
                            <asp:Label ID="lblUploadErr" runat="server" CssClass="text-danger font-weight-bold d-block mb-2" Visible="false" />

                            <div class="form-horizontal form-label-left">
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Select Excel File <span class="required">*</span></label>
                                    <div class="col-md-6 col-sm-6">
                                        <asp:FileUpload ID="fuExcel" runat="server" CssClass="form-control" accept=".xlsx" />
                                        <span class="text-muted">Supported format: <strong>.xlsx</strong> only &middot; Max ~10,000 rows recommended</span>
                                    </div>
                                </div>
                                <div class="ln_solid"></div>
                                <div class="form-group row">
                                    <div class="col-md-6 col-sm-6 offset-md-3">
                                        <asp:Button ID="btnUpload" runat="server" Text="Upload &amp; Preview" CssClass="btn btn-primary" OnClick="btnUpload_Click" />
                                        &nbsp;<asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-default" OnClick="btnClear_Click" CausesValidation="false" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============================================================ -->
            <!-- SECTION 3 — Data Preview & Column Mapping                    -->
            <!-- ============================================================ -->
            <asp:Panel ID="pnlPreview" runat="server" Visible="false">
                <div class="row">
                    <div class="col-md-12 col-sm-12">
                        <div class="x_panel">
                            <div class="x_title">
                                <h2>Data Preview &amp; Column Mapping</h2>
                                <ul class="nav navbar-right panel_toolbox">
                                    <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                </ul>
                                <div class="clearfix"></div>
                            </div>
                            <div class="x_content">
                                <p class="text-muted">First 5 rows shown below. Select which column contains the <strong>phone number</strong> and check columns that are <strong>template variables</strong>.</p>

                                <!-- Preview Grid -->
                                <div class="card-box table-responsive">
                                    <asp:GridView ID="gvPreview" runat="server" CssClass="table table-striped table-hover table-bordered table-sm"
                                        AutoGenerateColumns="true" EmptyDataText="No data to preview." GridLines="None" />
                                </div>

                                <!-- Column Mapping -->
                                <asp:Panel ID="pnlMapping" runat="server">
                                    <h5 class="font-weight-bold" style="margin-top: 15px;">Column Mapping</h5>
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvMapping" runat="server" CssClass="table table-striped table-hover table-bordered table-sm"
                                            AutoGenerateColumns="false" OnRowDataBound="gvMapping_RowDataBound" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="ExcelColumn" HeaderText="Column Name" />
                                                <asp:TemplateField HeaderText="Phone Number">
                                                    <ItemTemplate>
                                                        <asp:RadioButton ID="rbPhone" runat="server" GroupName="phoneCol" AutoPostBack="false" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Template Variable?">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="chkVariable" runat="server" Checked="true" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Variable #">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblVarIndex" runat="server" CssClass="text-muted" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </asp:Panel>

                                <!-- Validation Summary -->
                                <asp:Panel ID="pnlValidation" runat="server" Visible="false" style="margin-top: 12px;">
                                    <div class="alert alert-info">
                                        <strong>Validation Summary</strong><br />
                                        <span class="validation-stat"><strong><asp:Label ID="lblTotalCount" runat="server" /></strong></span>
                                        <span class="validation-stat"><strong class="text-success"><asp:Label ID="lblValidCount" runat="server" /></strong></span>
                                        <span class="validation-stat"><strong class="text-danger"><asp:Label ID="lblInvalidCount" runat="server" /></strong></span>
                                        <span class="validation-stat"><strong class="text-danger"><asp:Label ID="lblDuplicateCount" runat="server" /></strong></span>
                                        <asp:Label ID="lblWarningsInfo" runat="server" CssClass="text-warning" Style="font-size: 11px; display: block; margin-top: 4px;" Visible="false" />
                                        <br /><asp:Label ID="lblParseInfo" runat="server" CssClass="text-muted" Style="font-size: 11px; display: block; margin-top: 4px;" Visible="false" />
                                    </div>
                                    <asp:Repeater ID="rptWarnings" runat="server">
                                        <HeaderTemplate><ul class="text-warning" style="margin-top: 4px; padding-left: 18px; font-size: 12px;"></HeaderTemplate>
                                        <ItemTemplate><li><%# Eval("Text") %></li></ItemTemplate>
                                        <FooterTemplate></ul></FooterTemplate>
                                    </asp:Repeater>
                                </asp:Panel>

                                <!-- Actions -->
                                <div class="ln_solid"></div>
                                <div class="form-group">
                                    <asp:Button ID="btnValidate" runat="server" Text="Validate Data" CssClass="btn btn-primary" OnClick="btnValidate_Click" />
                                    &nbsp;<asp:Button ID="btnSend" runat="server" Text="Send Bulk WhatsApp" CssClass="btn btn-success" Visible="false" OnClick="btnSend_Click"
                                        OnClientClick="if(!confirm('Are you sure you want to send this WhatsApp campaign? This action cannot be undone.')) return false;" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <!-- ============================================================ -->
            <!-- SECTION 4 — Content & Attachment Configuration (Phase 2)     -->
            <!-- ============================================================ -->
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Content &amp; Attachment Configuration</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:Label ID="lblContentOk" runat="server" CssClass="text-success font-weight-bold d-block mb-2" Visible="false" />
                            <asp:Label ID="lblContentErr" runat="server" CssClass="text-danger font-weight-bold d-block mb-2" Visible="false" />

                            <div class="form-group">
                                <label class="control-label font-weight-bold">Message Content Mode</label>
                                <div class="radio">
                                    <asp:RadioButton ID="rbModeNone" runat="server" GroupName="contentMode" Text="No Attachment / Template Only" Checked="true" onclick="waToggleContentMode();" />
                                </div>
                                <div class="radio">
                                    <asp:RadioButton ID="rbModeCommon" runat="server" GroupName="contentMode" Text="Common Attachment for All Recipients" onclick="waToggleContentMode();" />
                                </div>
                                <div class="radio">
                                    <asp:RadioButton ID="rbModeIndividual" runat="server" GroupName="contentMode" Text="Individual File per Recipient" onclick="waToggleContentMode();" />
                                </div>
                                <div class="radio">
                                    <asp:RadioButton ID="rbModeWorkbook" runat="server" GroupName="contentMode" Text="Excel Workbook Sheet per Recipient" onclick="waToggleContentMode();" />
                                </div>
                                <div class="radio">
                                    <asp:RadioButton ID="rbModeUrl" runat="server" GroupName="contentMode" Text="Personalized URL from Excel" onclick="waToggleContentMode();" />
                                </div>
                            </div>

                            <!-- Common attachment -->
                            <asp:Panel ID="pnlCommonContent" runat="server" Style="display: none;">
                                <div class="form-horizontal form-label-left">
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Attachment File</label>
                                        <div class="col-md-6 col-sm-6">
                                            <asp:FileUpload ID="fuCommonContent" runat="server" CssClass="form-control" accept=".png,.jpg,.jpeg,.webp,.pdf,.xlsx,.xls,.docx,.doc,.pptx,.txt,.mp4" />
                                            <span class="text-muted">Images: png/jpg/webp (IMAGE templates) &middot; Documents require a DOCUMENT template (unvalidated)</span>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <div class="col-md-6 col-sm-6 offset-md-3">
                                            <asp:Button ID="btnUploadCommon" runat="server" Text="Upload Attachment" CssClass="btn btn-primary" OnClick="btnUploadCommon_Click" CausesValidation="false" />
                                        </div>
                                    </div>
                                    <div class="form-group row" id="trCommonInfo" runat="server" Visible="false">
                                        <label class="control-label col-md-3 col-sm-3 text-right">Uploaded</label>
                                        <div class="col-md-9 col-sm-9">
                                            <asp:Label ID="lblCommonFileInfo" runat="server" CssClass="text-success" />
                                        </div>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Individual files -->
                            <asp:Panel ID="pnlIndividualContent" runat="server" Style="display: none;">
                                <div class="form-horizontal form-label-left">
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Map Recipient Using</label>
                                        <div class="col-md-4 col-sm-6">
                                            <asp:DropDownList ID="ddlIndividualMappingColumn" runat="server" CssClass="form-control form-control-sm" />
                                        </div>
                                        <div class="col-md-5 col-sm-3">
                                            <span class="text-muted">Excel column whose value matches each file name (e.g. EmployeeID → EMP001.pdf).</span>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Recipient Files</label>
                                        <div class="col-md-6 col-sm-6">
                                            <asp:FileUpload ID="fuIndividualFiles" runat="server" CssClass="form-control" AllowMultiple="true"
                                                accept=".png,.jpg,.jpeg,.webp,.pdf,.xlsx,.xls,.docx,.doc,.pptx,.txt,.mp4" />
                                            <span class="text-muted">Select all files at once. File base name (without extension) must equal the mapping key.</span>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <div class="col-md-6 col-sm-6 offset-md-3">
                                            <asp:Button ID="btnUploadIndividual" runat="server" Text="Upload Files" CssClass="btn btn-primary" OnClick="btnUploadIndividual_Click" CausesValidation="false" />
                                        </div>
                                    </div>
                                    <div class="form-group row" id="trIndividualInfo" runat="server" Visible="false">
                                        <label class="control-label col-md-3 col-sm-3 text-right">Uploaded</label>
                                        <div class="col-md-9 col-sm-9">
                                            <asp:Label ID="lblIndividualFileInfo" runat="server" CssClass="text-success" />
                                        </div>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Workbook sheets -->
                            <asp:Panel ID="pnlWorkbookContent" runat="server" Style="display: none;">
                                <div class="form-horizontal form-label-left">
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Sheet Name Matches</label>
                                        <div class="col-md-4 col-sm-6">
                                            <asp:DropDownList ID="ddlWorkbookMappingColumn" runat="server" CssClass="form-control form-control-sm" />
                                        </div>
                                        <div class="col-md-5 col-sm-3">
                                            <span class="text-muted">Excel column whose value equals a worksheet name in the uploaded workbook.</span>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Multi-Sheet Workbook</label>
                                        <div class="col-md-6 col-sm-6">
                                            <asp:FileUpload ID="fuWorkbook" runat="server" CssClass="form-control" accept=".xlsx" />
                                            <span class="text-muted">One .xlsx workbook; each sheet name = one recipient's mapping key.</span>
                                        </div>
                                    </div>
                                    <div class="form-group row">
                                        <div class="col-md-6 col-sm-6 offset-md-3">
                                            <asp:Button ID="btnUploadWorkbook" runat="server" Text="Upload Workbook" CssClass="btn btn-primary" OnClick="btnUploadWorkbook_Click" CausesValidation="false" />
                                        </div>
                                    </div>
                                    <div class="form-group row" id="trWorkbookInfo" runat="server" Visible="false">
                                        <label class="control-label col-md-3 col-sm-3 text-right">Uploaded</label>
                                        <div class="col-md-9 col-sm-9">
                                            <asp:Label ID="lblWorkbookFileInfo" runat="server" CssClass="text-success" />
                                        </div>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- URL column -->
                            <asp:Panel ID="pnlUrlContent" runat="server" Style="display: none;">
                                <div class="form-horizontal form-label-left">
                                    <div class="form-group row">
                                        <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">URL Source Column</label>
                                        <div class="col-md-4 col-sm-6">
                                            <asp:DropDownList ID="ddlUrlColumn" runat="server" CssClass="form-control form-control-sm" />
                                        </div>
                                        <div class="col-md-5 col-sm-3">
                                            <span class="text-muted">Excel column containing a full http(s) URL for each recipient.</span>
                                        </div>
                                    </div>
                                </div>
                            </asp:Panel>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============================================================ -->
            <!-- SECTION 5 — Dispatch Plan (Phase 2)                          -->
            <!-- ============================================================ -->
            <asp:Panel ID="pnlDispatchPlan" runat="server" Visible="false">
                <div class="row">
                    <div class="col-md-12 col-sm-12">
                        <div class="x_panel">
                            <div class="x_title">
                                <h2>Dispatch Plan</h2>
                                <ul class="nav navbar-right panel_toolbox">
                                    <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                </ul>
                                <div class="clearfix"></div>
                            </div>
                            <div class="x_content">
                                <div class="alert alert-info">
                                    <strong>Validation Summary</strong><br />
                                    <asp:Label ID="lblDispatchSummary" runat="server" />
                                </div>

                                <div class="card-box table-responsive">
                                    <asp:GridView ID="gvDispatchPlan" runat="server" CssClass="table table-striped table-hover table-bordered table-sm"
                                        AutoGenerateColumns="false" GridLines="None" EmptyDataText="No dispatch items.">
                                        <Columns>
                                            <asp:BoundField DataField="RowNumber" HeaderText="Row" />
                                            <asp:BoundField DataField="PhoneNumber" HeaderText="Phone" />
                                            <asp:BoundField DataField="MappingKey" HeaderText="Mapping Key" />
                                            <asp:BoundField DataField="MediaFileName" HeaderText="Content" />
                                            <asp:BoundField DataField="Status" HeaderText="Status" />
                                            <asp:BoundField DataField="ErrorMessage" HeaderText="Detail" />
                                        </Columns>
                                    </asp:GridView>
                                </div>

                                <div class="ln_solid"></div>
                                <div class="form-group">
                                    <asp:Button ID="btnSendDispatch" runat="server" Text="Send Valid Recipients" CssClass="btn btn-success" OnClick="btnSendDispatch_Click"
                                        OnClientClick="if(!confirm('Send to all READY recipients now? Blocked recipients will be skipped. This action cannot be undone.')) return false;" />
                                </div>

                                <!-- Per-recipient send results -->
                                <asp:Panel ID="pnlDispatchResults" runat="server" Visible="false">
                                    <h5 class="font-weight-bold">Send Results</h5>
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvDispatchResults" runat="server" CssClass="table table-striped table-hover table-bordered table-sm"
                                            AutoGenerateColumns="false" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="PhoneNumber" HeaderText="Phone" />
                                                <asp:BoundField DataField="Status" HeaderText="Status" />
                                                <asp:BoundField DataField="CeoitboxSystemId" HeaderText="System ID" />
                                                <asp:BoundField DataField="WhatsAppMessageId" HeaderText="WhatsApp Msg ID" />
                                                <asp:BoundField DataField="ErrorMessage" HeaderText="Error" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                    <div class="alert alert-success" id="divDispatchOk" runat="server" Visible="false">
                                        <strong><i class="fa fa-check-circle"></i></strong>
                                        <asp:Label ID="lblDispatchOk" runat="server" />
                                    </div>
                                    <div class="alert alert-danger" id="divDispatchErr" runat="server" Visible="false">
                                        <strong><i class="fa fa-times-circle"></i></strong>
                                        <asp:Label ID="lblDispatchErr" runat="server" />
                                    </div>
                                </asp:Panel>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <!-- ============================================================ -->
            <!-- SECTION 6 — Campaign Result (Phase 1 bulk)                   -->
            <!-- ============================================================ -->
            <asp:Panel ID="pnlResults" runat="server" Visible="false">
                <div class="row">
                    <div class="col-md-12 col-sm-12">
                        <div class="x_panel">
                            <div class="x_title">
                                <h2>Campaign Result</h2>
                                <ul class="nav navbar-right panel_toolbox">
                                    <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                </ul>
                                <div class="clearfix"></div>
                            </div>
                            <div class="x_content">
                                <asp:Panel ID="pnlResultOk" runat="server" Visible="false" CssClass="alert alert-success">
                                    <strong><i class="fa fa-check-circle"></i> Success</strong>
                                    <asp:Label ID="lblResultOk" runat="server" />
                                </asp:Panel>
                                <asp:Panel ID="pnlResultErr" runat="server" Visible="false" CssClass="alert alert-danger">
                                    <strong><i class="fa fa-times-circle"></i> Failed</strong>
                                    <asp:Label ID="lblResultErr" runat="server" />
                                </asp:Panel>

                                <h5 class="font-weight-bold">API Response</h5>
                                <pre style="background: #f5f5f5; border: 1px solid #ddd; border-radius: 3px; padding: 10px; font-size: 11px; max-height: 300px; overflow-y: auto; white-space: pre-wrap; word-break: break-all;"><asp:Literal ID="litResponseBody" runat="server" /></pre>

                                <h5 class="font-weight-bold" style="margin-top: 10px;">Sent Payload (jsonData)</h5>
                                <pre style="background: #f5f5f5; border: 1px solid #ddd; border-radius: 3px; padding: 10px; font-size: 11px; max-height: 300px; overflow-y: auto; white-space: pre-wrap; word-break: break-all;"><asp:Literal ID="litCampaignJson" runat="server" /></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

        </div>
    </div>
</asp:Content>