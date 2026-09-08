<%@ Page Title="WhatsApp Bulk Send" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="WhatsApp_BulkSend.aspx.cs" Inherits="AnmolDristi.WhatsApp_BulkSend" %>

<asp:Content ID="headStyle" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .wa-panel { background: #fff; border: 1px solid #e6e9ed; border-radius: 4px; padding: 18px 20px; margin-bottom: 16px; }
        .wa-panel h4 { margin: 0 0 12px; font-size: 15px; font-weight: 600; color: #333; border-bottom: 1px solid #eee; padding-bottom: 8px; }
        .wa-panel h4 .step { display: inline-block; width: 24px; height: 24px; line-height: 24px; text-align: center; border-radius: 50%; background: #1abc9c; color: #fff; font-size: 12px; margin-right: 8px; }
        .wa-table { width: 100%; border-collapse: collapse; font-size: 12px; }
        .wa-table th { background: #f7f7f7; border: 1px solid #ddd; padding: 6px 8px; font-weight: 600; text-align: left; }
        .wa-table td { border: 1px solid #ddd; padding: 5px 8px; }
        .wa-table tr:nth-child(even) { background: #fafafa; }
        .wa-result-ok { color: #27ae60; font-weight: 600; }
        .wa-result-err { color: #e74c3c; font-weight: 600; }
        .wa-warn { color: #e67e22; font-size: 12px; }
        .wa-muted { color: #999; font-size: 12px; }
        .wa-json { background: #f4f4f4; border: 1px solid #ddd; border-radius: 3px; padding: 10px; font-family: monospace; font-size: 11px; white-space: pre-wrap; word-break: break-all; max-height: 300px; overflow-y: auto; }
    </style>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="page-title">
        <div class="title_left"><h3><i class="fa fa-whatsapp"></i> WhatsApp Bulk Send</h3></div>
    </div>
    <div class="clearfix"></div>

    <!-- ============================================================ -->
    <!-- STEP 1 — CAMPAIGN DETAILS                                    -->
    <!-- ============================================================ -->
    <div class="wa-panel">
        <h4><span class="step">1</span> Campaign Details</h4>
        <div class="row">
            <div class="col-md-4">
                <div class="form-group">
                    <label>Campaign Name <span class="text-danger">*</span></label>
                    <asp:TextBox ID="txtCampaignName" runat="server" CssClass="form-control input-sm" />
                    <span class="wa-muted">Descriptive name for this run (e.g. "September Attendance Report").</span>
                </div>
            </div>
            <div class="col-md-4">
                <div class="form-group">
                    <label>Template Name <span class="text-danger">*</span></label>
                    <asp:TextBox ID="txtTemplateName" runat="server" CssClass="form-control input-sm" />
                    <span class="wa-muted">Exact name of the pre-approved WhatsApp template in CEOITBOX.</span>
                </div>
            </div>
            <div class="col-md-4">
                <div class="form-group">
                    <label>Language</label>
                    <asp:DropDownList ID="ddlLanguage" runat="server" CssClass="form-control input-sm">
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
                    <span class="wa-muted">Must match the template's configured language.</span>
                </div>
            </div>
        </div>
    </div>

    <!-- ============================================================ -->
    <!-- STEP 2 — UPLOAD EXCEL                                        -->
    <!-- ============================================================ -->
    <div class="wa-panel">
        <h4><span class="step">2</span> Upload Recipient Data</h4>
        <div class="row">
            <div class="col-md-8">
                <div class="form-group">
                    <asp:FileUpload ID="fuExcel" runat="server" accept=".xlsx" CssClass="form-control input-sm" />
                </div>
                <asp:Button ID="btnUpload" runat="server" Text="Upload &amp; Preview" CssClass="btn btn-info btn-sm"
                    OnClick="btnUpload_Click" />
                <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-default btn-sm"
                    OnClick="btnClear_Click" CausesValidation="false" />
            </div>
            <div class="col-md-4 text-right" style="padding-top:7px;">
                <span class="wa-muted">Supported format: <strong>.xlsx</strong> only &middot; Max ~10,000 rows recommended</span>
            </div>
        </div>

        <!-- Alerts -->
        <asp:Panel ID="pnlUploadOk" runat="server" Visible="false" CssClass="alert alert-success" style="margin-top:10px;">
            <asp:Label ID="lblUploadOk" runat="server" />
        </asp:Panel>
        <asp:Panel ID="pnlUploadErr" runat="server" Visible="false" CssClass="alert alert-danger" style="margin-top:10px;">
            <asp:Label ID="lblUploadErr" runat="server" />
        </asp:Panel>
    </div>

    <!-- ============================================================ -->
    <!-- STEP 3 — PREVIEW & COLUMN MAPPING                            -->
    <!-- ============================================================ -->
    <asp:Panel ID="pnlPreview" runat="server" Visible="false">
        <div class="wa-panel">
            <h4><span class="step">3</span> Data Preview &amp; Column Mapping</h4>
            <p class="wa-muted" style="margin-bottom:10px;">First 5 rows shown below. Select which column contains the <strong>phone number</strong> and check columns that are <strong>template variables</strong>.</p>

            <div style="overflow-x:auto; margin-bottom:14px;">
                <asp:GridView ID="gvPreview" runat="server" CssClass="wa-table table table-condensed"
                    AutoGenerateColumns="true" EmptyDataText="No data to preview." />
            </div>

            <!-- Column Mapping -->
            <asp:Panel ID="pnlMapping" runat="server">
                <strong>Column Mapping</strong>
                <div style="margin-top:8px;">
                    <asp:GridView ID="gvMapping" runat="server" CssClass="wa-table table table-condensed"
                        AutoGenerateColumns="false" OnRowDataBound="gvMapping_RowDataBound">
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
                                    <asp:Label ID="lblVarIndex" runat="server" CssClass="wa-muted" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </asp:Panel>

            <!-- Validation Summary -->
            <asp:Panel ID="pnlValidation" runat="server" Visible="false" style="margin-top:12px;">
                <strong>Validation Summary</strong>
                <div style="margin-top:6px;">
                    <asp:Label ID="lblValidCount" runat="server" CssClass="wa-result-ok" />
                    <asp:Label ID="lblInvalidCount" runat="server" CssClass="wa-result-err" style="margin-left:14px;" />
                    <asp:Label ID="lblDuplicateCount" runat="server" CssClass="wa-result-err" style="margin-left:14px;" />
                </div>
                <asp:Repeater ID="rptWarnings" runat="server">
                    <HeaderTemplate><ul class="wa-warn" style="margin-top:4px; padding-left:18px;"></HeaderTemplate>
                    <ItemTemplate><li><%# Eval("Text") %></li></ItemTemplate>
                    <FooterTemplate></ul></FooterTemplate>
                </asp:Repeater>
            </asp:Panel>

            <!-- Actions -->
            <div style="margin-top:14px;">
                <asp:Button ID="btnValidate" runat="server" Text="Validate Data" CssClass="btn btn-warning btn-sm"
                    OnClick="btnValidate_Click" />
                <asp:Button ID="btnSend" runat="server" Text="Send Bulk WhatsApp" CssClass="btn btn-success btn-sm"
                    Visible="false" OnClick="btnSend_Click"
                    OnClientClick="if(!confirm('Are you sure you want to send this WhatsApp campaign? This action cannot be undone.')) return false;" />
            </div>
        </div>
    </asp:Panel>

    <!-- ============================================================ -->
    <!-- STEP 4 — SEND RESULTS                                        -->
    <!-- ============================================================ -->
    <asp:Panel ID="pnlResults" runat="server" Visible="false">
        <div class="wa-panel">
            <h4><span class="step">4</span> Campaign Result</h4>

            <asp:Panel ID="pnlResultOk" runat="server" Visible="false" CssClass="alert alert-success">
                <strong><i class="fa fa-check-circle"></i> Success</strong>
                <asp:Label ID="lblResultOk" runat="server" />
            </asp:Panel>
            <asp:Panel ID="pnlResultErr" runat="server" Visible="false" CssClass="alert alert-danger">
                <strong><i class="fa fa-times-circle"></i> Failed</strong>
                <asp:Label ID="lblResultErr" runat="server" />
            </asp:Panel>

            <strong>API Response</strong>
            <div class="wa-json"><asp:Literal ID="litResponseBody" runat="server" /></div>

            <div style="margin-top:10px;">
                <strong>Sent Payload (jsonData)</strong>
                <div class="wa-json"><asp:Literal ID="litCampaignJson" runat="server" /></div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>