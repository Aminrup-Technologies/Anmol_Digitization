<%@ Page Title="Organization Details" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="VMS_OrganizationDetails.aspx.cs" Inherits="AnmolDristi.VMS_OrganizationDetails" ValidateRequest="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .required {
            color: red;
        }

        .help-block {
            font-size: 12px;
            color: #73879C;
        }

        .preview-box {
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        
        .preview-visitor {
            border: 1px solid #ddd;
            background-color: #fcfcfc;
        }
        
        .preview-security {
            border: 2px dashed red;
            background-color: #fff9f9;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>Organization Details <small>Manage Office Configurations</small></h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">

                        <asp:Panel ID="pnlList" runat="server">
                            <div class="table-responsive">
                                <asp:GridView ID="gvOffices" runat="server" AutoGenerateColumns="False" CssClass="table table-striped jambo_table bulk_action" OnRowCommand="gvOffices_RowCommand" DataKeyNames="Branch_ID">
                                    <Columns>
                                        <asp:BoundField DataField="Branch_ID" HeaderText="Branch ID" />
                                        <asp:BoundField DataField="Branch_Description" HeaderText="ERP Branch Name" />
                                        <asp:BoundField DataField="AliasName" HeaderText="VMS Alias (Public Name)" />
                                        <asp:BoundField DataField="FullAddress" HeaderText="Office Details" />
                                        
                                        <asp:TemplateField HeaderText="Action">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="EditOffice" CommandArgument='<%# Eval("Branch_ID") %>' CssClass="btn btn-info btn-xs"><i class="fa fa-pencil"></i> Edit Details</asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Reception Kiosk">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnGenerateQR" runat="server" CommandName="GenerateQR"
                                                    CommandArgument='<%# Eval("Branch_ID") + "|" + Eval("AliasName") + "|" + Eval("FullAddress") + "|" + Eval("Branch_Description") %>'
                                                    CssClass="btn btn-danger btn-xs"><i class="fa fa-file-pdf-o"></i> Visitor QR
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Security Gate">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnGenerateGateQR" runat="server" CommandName="GenerateGateQR"
                                                    CommandArgument='<%# Eval("Branch_ID") + "|" + Eval("AliasName") + "|" + Eval("FullAddress") + "|" + Eval("Branch_Description") %>'
                                                    CssClass="btn btn-warning btn-xs"><i class="fa fa-qrcode"></i> Gate QR
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlEdit" runat="server" Visible="false">
                            <h4>Configuring:
                                <asp:Label ID="lblCurrentBranch" runat="server" Font-Bold="true" ForeColor="#1ABB9C"></asp:Label></h4>
                            <asp:HiddenField ID="hfBranchID" runat="server" />
                            <br />

                            <div class="" role="tabpanel" data-example-id="togglable-tabs">
                                <ul id="orgEditTabs" class="nav nav-tabs bar_tabs" role="tablist">
                                    <li role="presentation" class="active">
                                        <a href="#tab_general_policy" id="general-policy-tab" role="tab" data-toggle="tab" aria-expanded="true">General &amp; Policy</a>
                                    </li>
                                    <li role="presentation">
                                        <a href="#tab_hosts_comms" id="hosts-comms-tab" role="tab" data-toggle="tab" aria-expanded="false">Hosts &amp; Communications</a>
                                    </li>
                                    <li role="presentation">
                                        <a href="#tab_office_timings" id="office-timings-tab" role="tab" data-toggle="tab" aria-expanded="false">Office Timings</a>
                                    </li>
                                    <li role="presentation">
                                        <a href="#tab_qr_previews" id="qr-previews-tab" role="tab" data-toggle="tab" aria-expanded="false">QR Previews</a>
                                    </li>
                                </ul>

                                <div id="orgEditTabContent" class="tab-content">

                                    <%-- Tab 1: General & Policy --%>
                                    <div role="tabpanel" class="tab-pane fade active in" id="tab_general_policy" aria-labelledby="general-policy-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Alias Company Name <span class="required">*</span></label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:TextBox ID="txtAlias" runat="server" CssClass="form-control" placeholder="e.g. Anmol Industries Limited - Greater Noida"></asp:TextBox>
                                                    <span class="help-block">Use a hyphen (-) to separate lines. e.g., "Anmol Industries Limited - Corporate Office, Kolkata"</span>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Country and Timezone <span class="required">*</span></label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:DropDownList ID="ddlTimeZone" runat="server" CssClass="form-control">
                                                        <asp:ListItem Text="+05:30 India Time" Value="+05:30"></asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Office Details (Address)</label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" placeholder="Enter the full physical address of this plant..."></asp:TextBox>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">DOs &amp; DONTs / Visitor Policy</label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <span class="help-block">This policy will be shown to the visitor immediately after successful registration and printed on their pass.</span>
                                                    <asp:TextBox ID="txtPolicy" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="10" placeholder="1. Suit Up! Wear hair net...&#10;2. No Smoking or Gutkha allowed inside the premises..."></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <%-- Tab 2: Hosts & Communications --%>
                                    <div role="tabpanel" class="tab-pane fade" id="tab_hosts_comms" aria-labelledby="hosts-comms-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Pre-configured Default Hosts</label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:TextBox ID="txtPreconfiguredHosts" runat="server" CssClass="form-control" placeholder="e.g. AIL0102, AIL0405"></asp:TextBox>
                                                    <span class="help-block">Enter comma-separated Employee Codes for default branch hosts</span>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Escalation Email Addresses</label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:TextBox ID="txtEscalationEmails" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" placeholder="e.g. plant.hr@anmolindustries.com, security.head@anmolindustries.com"></asp:TextBox>
                                                    <span class="help-block">Enter comma-separated Email Addresses to be CC'd on all branch visit requests</span>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Escalation Threshold (Minutes)</label>
                                                <div class="col-md-3 col-sm-3 col-xs-12">
                                                    <asp:TextBox ID="txtEscalationTimeout" runat="server" CssClass="form-control" TextMode="Number" Text="15"></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <%-- Tab 3: Office Timings --%>
                                    <div role="tabpanel" class="tab-pane fade" id="tab_office_timings" aria-labelledby="office-timings-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <p class="text-muted"><i class="fa fa-info-circle"></i> Edit timings of all your offices for all days of the week. Passes will only be valid during these operational hours.</p>

                                            <div class="table-responsive">
                                                <table class="table table-striped jambo_table">
                                                    <thead>
                                                        <tr class="headings">
                                                            <th>Day of the Week</th>
                                                            <th>Is Open?</th>
                                                            <th>Open Time</th>
                                                            <th>Close Time</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody>
                                                        <asp:Repeater ID="rptTimings" runat="server">
                                                            <ItemTemplate>
                                                                <tr>
                                                                    <td>
                                                                        <strong>
                                                                            <asp:Label ID="lblDay" runat="server" Text='<%# Eval("DayOfWeek") %>'></asp:Label></strong>
                                                                        <asp:HiddenField ID="hfDay" runat="server" Value='<%# Eval("DayOfWeek") %>' />
                                                                    </td>
                                                                    <td>
                                                                        <asp:CheckBox ID="chkIsOpen" runat="server" Checked='<%# Eval("IsOpen").ToString() == "1" %>' />
                                                                    </td>
                                                                    <td>
                                                                        <asp:TextBox ID="txtOpenTime" runat="server" CssClass="form-control" TextMode="Time" Text='<%# Eval("OpenTime") %>'></asp:TextBox>
                                                                    </td>
                                                                    <td>
                                                                        <asp:TextBox ID="txtCloseTime" runat="server" CssClass="form-control" TextMode="Time" Text='<%# Eval("CloseTime") %>'></asp:TextBox>
                                                                    </td>
                                                                </tr>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </div>
                                    </div>

                                    <%-- Tab 4: QR Previews (read-only live preview) --%>
                                    <div role="tabpanel" class="tab-pane fade" id="tab_qr_previews" aria-labelledby="qr-previews-tab">
                                        <div class="row" style="margin-top: 20px;">
                                            
                                            <div class="col-md-6 col-sm-6 col-xs-12 text-center">
                                                <div class="preview-box preview-visitor">
                                                    <h3 style="font-weight: bold; color: #333;">WELCOME VISITORS</h3>
                                                    <p class="text-muted">Scan the QR &amp; generate your pass</p>
                                                    
                                                    <asp:Image ID="imgPreviewVisitor" runat="server" Width="220px" Height="220px" />
                                                    
                                                    <h4 style="font-weight: bold; margin-top: 15px;">
                                                        <asp:Label ID="lblPreviewVisitorAlias" runat="server"></asp:Label>
                                                    </h4>
                                                    <p class="text-muted" style="font-size: 11px;">Powered by Anmol-IT</p>
                                                </div>
                                            </div>
                                            
                                            <div class="col-md-6 col-sm-6 col-xs-12 text-center">
                                                <div class="preview-box preview-security">
                                                    <h3 style="font-weight: bold; color: red;">SECURITY DESK ONLY</h3>
                                                    <p class="text-muted" style="font-weight: bold;">Guards: Scan this QR to open Pass Scanner</p>
                                                    
                                                    <asp:Image ID="imgPreviewSecurity" runat="server" Width="220px" Height="220px" />
                                                    
                                                    <h4 style="font-weight: bold; margin-top: 15px; color: #333;">
                                                        <asp:Label ID="lblPreviewSecurityAlias" runat="server"></asp:Label>
                                                    </h4>
                                                    <p style="color: red; font-size: 11px; font-weight: bold;">RESTRICTED ACCESS - INTERNAL USE ONLY</p>
                                                </div>
                                            </div>

                                        </div>
                                        <div class="text-center" style="margin-top: 10px;">
                                            <p class="text-info"><i class="fa fa-info-circle"></i> <em>Previews are generated based on the currently saved configuration. Save changes to update the live preview.</em></p>
                                        </div>
                                    </div>

                                </div>
                            </div>

                            <%-- Shared actions: save settings from all tabs at once --%>
                            <div class="ln_solid"></div>
                            <div class="form-group">
                                <div class="col-md-12 col-sm-12 col-xs-12 text-center">
                                    <asp:Button ID="btnCancel" runat="server" CssClass="btn btn-primary" Text="Cancel" OnClick="btnCancel_Click" />
                                    <asp:Button ID="btnSave" runat="server" CssClass="btn btn-success" Text="Update Details" OnClick="btnSave_Click" />
                                </div>
                            </div>
                            <div class="text-center">
                                <asp:Label ID="lblMsg" runat="server" Font-Bold="true"></asp:Label>
                            </div>

                        </asp:Panel>

                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>