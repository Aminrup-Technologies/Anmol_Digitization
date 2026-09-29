<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ManageExpenseSettings.aspx.cs" Inherits="AnmolDristi.ManageExpenseSettings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Manage Expense Settings</h5>
                </div>
            </div>
            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Expense Settings</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <asp:Button ID="btnAddNew" runat="server" Text="Add New Setting" CssClass="btn btn-sm btn-primary" OnClick="btnAddNew_Click" CausesValidation="false" />
                            <br />
                            <br />

                            <asp:GridView ID="gvSettings" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                OnRowCommand="gvSettings_RowCommand" DataKeyNames="SettingId">
                                <Columns>
                                    <asp:BoundField DataField="SettingId" HeaderText="Id" Visible="false" />
                                    <asp:BoundField DataField="ExpenseType" HeaderText="Expense Type" />
                                    <asp:BoundField DataField="AttachmentRequired" HeaderText="Attachment Required" DataFormatString="{0}" />
                                    <asp:BoundField DataField="ThresholdAmount" HeaderText="Threshold (₹)" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="Notes" HeaderText="Notes" />
                                    <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                                    <asp:TemplateField HeaderText="Actions">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRow" CommandArgument='<%# Eval("SettingId") %>' CausesValidation="false" Text="Edit" CssClass="btn btn-sm btn-default" />
                                            &nbsp;
                                            <asp:LinkButton ID="lnkDelete" runat="server" CommandName="DeleteRow" CommandArgument='<%# Eval("SettingId") %>' CausesValidation="false" OnClientClick="return confirm('Delete this setting?');" Text="Delete" CssClass="btn btn-sm btn-danger" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>

                            <!-- Modal for add/edit -->
                            <div class="modal fade" id="modalExpenseSetting" tabindex="-1" role="dialog" aria-labelledby="modalLabel" aria-hidden="true">
                                <div class="modal-dialog" role="document">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                            <h5 class="modal-title" id="modalLabel">Add / Edit Expense Setting</h5>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span>&times;</span></button>
                                        </div>
                                        <div class="modal-body">
                                            <asp:Panel ID="pnlExpenseForm" runat="server">
                                                <asp:HiddenField ID="hfSettingId" runat="server" />
                                                <div class="form-group">
                                                    <label>Expense Type</label>
                                                    <asp:TextBox ID="txtExpenseType" runat="server" CssClass="form-control" />
                                                    <asp:RequiredFieldValidator ID="rfvExpenseType" runat="server" ControlToValidate="txtExpenseType" ErrorMessage="Required" CssClass="validation-error" Display="Dynamic" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Attachment Required</label><br />
                                                    <asp:CheckBox ID="chkAttachmentRequired" runat="server" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Threshold Amount (if any, ₹)</label>
                                                    <asp:TextBox ID="txtThreshold" runat="server" CssClass="form-control" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Notes</label>
                                                    <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" />
                                                </div>
                                                <asp:Label ID="lblExpenseMessage" runat="server" CssClass="text-danger" />
                                            </asp:Panel>
                                        </div>
                                        <div class="modal-footer">
                                            <asp:Button ID="btnSaveSetting" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSaveSetting_Click" />
                                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                                        </div>
                                    </div>
                                </div>
                            </div>

                        </div>
                        <!-- x_content -->
                    </div>
                    <!-- x_panel -->
                </div>
                <!-- col -->
            </div>
            <!-- row -->
        </div>
        <!-- container -->
    </div>
    <!-- right_col -->

    <script type="text/javascript">
        function ShowExpenseModal() {
            $('#modalExpenseSetting').modal('show');
        }
    </script>
</asp:Content>
