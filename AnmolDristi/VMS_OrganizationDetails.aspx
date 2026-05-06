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
                                                    CommandArgument='<%# Eval("Branch_ID") + "|" + Eval("AliasName") + "|" + Eval("FullAddress") %>'
                                                    CssClass="btn btn-danger btn-xs"><i class="fa fa-file-pdf-o"></i> Download PDF
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
                                <ul id="myTab" class="nav nav-tabs bar_tabs" role="tablist">
                                    <li role="presentation" class="active"><a href="#tab_content1" id="basic-tab" role="tab" data-toggle="tab" aria-expanded="true">Basic Details Office</a></li>
                                    <li role="presentation" class=""><a href="#tab_content2" role="tab" id="policy-tab" data-toggle="tab" aria-expanded="false">Visitor Policy</a></li>
                                    <li role="presentation" class=""><a href="#tab_content3" role="tab" id="timing-tab" data-toggle="tab" aria-expanded="false">Office Timings</a></li>
                                </ul>

                                <div id="myTabContent" class="tab-content">

                                    <div role="tabpanel" class="tab-pane fade active in" id="tab_content1" aria-labelledby="basic-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <div class="form-group">
                                                <label class="control-label col-md-3 col-sm-3 col-xs-12">Alias Company Name <span class="required">*</span></label>
                                                <div class="col-md-6 col-sm-6 col-xs-12">
                                                    <asp:TextBox ID="txtAlias" runat="server" CssClass="form-control" placeholder="e.g. Anmol Industries Limited - Greater Noida"></asp:TextBox>
                                                    <span class="help-block">This name will appear on the visitor's mobile app and digital pass.</span>
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
                                        </div>
                                    </div>

                                    <div role="tabpanel" class="tab-pane fade" id="tab_content2" aria-labelledby="policy-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <div class="form-group">
                                                <label class="control-label col-md-2 col-sm-2 col-xs-12">DOs & DONTs</label>
                                                <div class="col-md-9 col-sm-9 col-xs-12">
                                                    <span class="help-block">This policy will be shown to the visitor immediately after successful registration and printed on their pass.</span>
                                                    <asp:TextBox ID="txtPolicy" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="10" placeholder="1. Suit Up! Wear hair net...&#10;2. No Smoking or Gutkha allowed inside the premises..."></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <div role="tabpanel" class="tab-pane fade" id="tab_content3" aria-labelledby="timing-tab">
                                        <div class="form-horizontal form-label-left" style="margin-top: 20px;">
                                            <p class="text-muted"><i class="fa fa-info-circle"></i>Edit timings of all your offices for all days of the week. Passes will only be valid during these operational hours.</p>

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

                                            <div class="ln_solid"></div>
                                            <div class="form-group text-center">
                                                <asp:Button ID="btnSaveTimings" runat="server" CssClass="btn btn-warning" Text="Update Timings" OnClick="btnSaveTimings_Click" />
                                            </div>

                                        </div>
                                    </div>

                                </div>
                            </div>

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

    <div class="modal fade" id="qrModal" tabindex="-1" role="dialog" aria-hidden="true">
        <div class="modal-dialog modal-sm">
            <div class="modal-content">

                <div class="modal-header">
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">×</span></button>
                    <h4 class="modal-title">Reception Desk Poster</h4>
                </div>

                <div class="modal-body text-center" id="printArea">
                    <h3 style="color: #1ABB9C; font-weight: bold;">WELCOME TO</h3>
                    <h4 style="font-weight: bold;">
                        <asp:Label ID="lblModalPlantName" runat="server"></asp:Label></h4>
                    <br />
                    <p>Please scan this QR code using your smartphone camera to register your visit and generate your Gate Pass.</p>
                    <br />

                    <asp:Image ID="imgKioskQR" runat="server" Width="250px" Height="250px" />

                    <br />
                    <br />
                    <p class="text-muted">
                        <small>Powered by VMS | Plant ID:
                        <asp:Label ID="lblModalBranchId" runat="server"></asp:Label></small>
                    </p>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
                    <button type="button" class="btn btn-success" onclick="printQRPoster()"><i class="fa fa-print"></i>Print Poster</button>
                </div>

            </div>
        </div>
    </div>

    <script type="text/javascript">
        function printQRPoster() {
            var printContents = document.getElementById('printArea').innerHTML;
            var originalContents = document.body.innerHTML;
            document.body.innerHTML = printContents;
            window.print();
            document.body.innerHTML = originalContents;
            location.reload(); // Reload to restore event handlers after DOM swap
        }
    </script>
</asp:Content>
