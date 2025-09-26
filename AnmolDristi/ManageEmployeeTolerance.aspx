<%@ Page Title="Manage KM Tolerance" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ManageEmployeeTolerance.aspx.cs" Inherits="AnmolDristi.ManageEmployeeTolerance" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Manage Employee Tolerance</h5>
                </div>
            </div>

            <div class="clearfix"></div>
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Tolerance List</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <%--<asp:UpdatePanel ID="upTolerance" runat="server">
                                <ContentTemplate>--%>
                            <asp:Button ID="btnAddNew" runat="server" Text="Add New Tolerance" CausesValidation="false" CssClass="btn btn-sm btn-primary" OnClick="btnAddNew_Click" />
                            <br />
                            <br />
                            <asp:GridView ID="gvTolerance" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                OnRowCommand="gvTolerance_RowCommand" OnRowDataBound="gvTolerance_RowDataBound">
                                <Columns>
                                    <asp:BoundField DataField="ToleranceId" HeaderText="Id" Visible="false" />
                                    <asp:BoundField DataField="EmployeeCode" HeaderText="Employee (Code)" />
                                    <asp:BoundField DataField="MinAllowedKM" HeaderText="Min KM" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="MaxAllowedKM" HeaderText="Max KM" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="EffectiveFrom" HeaderText="Effective From" DataFormatString="{0:yyyy-MM-dd}" />
                                    <asp:BoundField DataField="EffectiveTo" HeaderText="Effective To" DataFormatString="{0:yyyy-MM-dd}" />
                                    <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                                    <asp:TemplateField HeaderText="Actions">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRow" CommandArgument='<%# Eval("ToleranceId") %>' CausesValidation="false" Text="Edit" CssClass="btn btn-sm btn-default" />
                                            &nbsp;
                                            <asp:LinkButton ID="lnkDelete" runat="server" CommandName="DeleteRow" CommandArgument='<%# Eval("ToleranceId") %>' CausesValidation="false" OnClientClick="return confirm('Delete this tolerance (soft delete)?');" Text="Delete" CssClass="btn btn-sm btn-danger" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                            <%--</ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnAddNew" EventName="Click" />
                                </Triggers>
                            </asp:UpdatePanel>--%>


                            <!-- Modal for Add/Edit (Bootstrap modal) -->
                            <div class="modal fade" id="modalEditTolerance" data-backdrop="static">
                                <div class="modal-dialog" role="document">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                            <h5 class="modal-title" id="modalLabel">Add / Edit Tolerance</h5>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span>&times;</span></button>
                                        </div>

                                        <div class="modal-body">
                                            <asp:Panel ID="pnlForm" runat="server">
                                                <asp:HiddenField ID="hfId" runat="server" />
                                                <div class="form-group">
                                                    <label>Login Id</label>
                                                    <asp:TextBox ID="txtLoginId" runat="server" CssClass="form-control" />
                                                    <asp:RequiredFieldValidator ID="rfvLoginId" runat="server" ControlToValidate="txtLoginId" ErrorMessage="Required" CssClass="validation-error" Display="Dynamic" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Min KM</label>
                                                    <asp:TextBox ID="txtMinKM" runat="server" CssClass="form-control" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Max KM</label>
                                                    <asp:TextBox ID="txtMaxKM" runat="server" CssClass="form-control" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Effective From</label>
                                                    <asp:TextBox ID="txtEffectiveFrom" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px" placeholder="yyyy-mm-dd" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Effective To</label>
                                                    <asp:TextBox ID="txtEffectiveTo" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px" placeholder="yyyy-mm-dd" />
                                                </div>
                                                <div class="form-group">
                                                    <label>Active</label>
                                                    <asp:CheckBox ID="chkIsActive" runat="server" />
                                                </div>
                                                <asp:Label ID="lblFormMessage" runat="server" CssClass="text-danger" />
                                            </asp:Panel>
                                        </div>

                                        <div class="modal-footer">
                                            <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
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
        function showToleranceModal() {
            $('#modalEditTolerance').modal('show');
        }
    </script>
</asp:Content>
