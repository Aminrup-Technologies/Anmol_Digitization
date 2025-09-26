<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="SalesMonthlyReview.aspx.cs" Inherits="AnmolDristi.SalesMonthlyReview" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <%--<div class="page-title">
                <div class="title_left">
                    <h5>Sales Monthly Review</h5>
                </div>
            </div>
            <div class="clearfix"></div>--%>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Filter & Actions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Year <span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Year" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Month <span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Month" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>

                                <div class="col-md-3 align-self-end">
                                    <asp:Button ID="btn_fetch" runat="server" CssClass="btn btn-primary btn-sm" Text="Fetch" OnClick="btn_fetch_Click" />
                                </div>

                                <div class="col-md-3 text-right align-self-end">
                                    <asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm" Text="Save Draft" OnClick="btn_save_draft_Click" />
                                    <asp:Button ID="btn_submit" runat="server" CssClass="btn btn-success btn-sm" Text="Submit for Approval" OnClick="btn_submit_Click" />
                                </div>
                            </div>

                            <div class="ln_solid"></div>

                            <div class="row">
                                <div class="col-md-6">
                                    <asp:Label ID="lbl_msg" runat="server" Text="" />
                                </div>
                            </div>

                            <asp:Button ID="ShowPopup" runat="server" CssClass="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <%--<asp:Button ID="ShowExpensePopup" runat="server" CssClass="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-lg" />--%>

                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">

                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body" id="MyPopupBody">
                                            <!-- server will fill via inner HTML or Label -->
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>

                                    </div>
                                </div>
                            </div>


                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Sales Daily Activity</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvActivities" runat="server" AutoGenerateColumns="false" CssClass="table table-striped table-bordered"
                                            DataKeyNames="ActivityId" OnRowCommand="gvActivities_RowCommand" OnRowEditing="gvActivities_RowEditing" OnRowCancelingEdit="gvActivities_RowCancelingEdit"
                                            OnRowUpdating="gvActivities_RowUpdating" OnRowDataBound="gvActivities_RowDataBound">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Date">
                                                    <ItemTemplate>
                                                        <%# Eval("ActivityDate", "{0:yyyy-MM-dd}") %><br />
                                                        <small><%# Eval("Day") %></small>
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="TotalDistanceKM">
                                                    <ItemTemplate>
                                                        <%# Eval("TotalDistanceKM") %>
                                                        <br />
                                                        <small>Allowed:
                                                            <asp:Label ID="lblTolLower" runat="server" Text="-" />
                                                            &nbsp; - &nbsp;
                                                            <asp:Label ID="lblTolUpper" runat="server" Text="-" />
                                                            &nbsp;
                                                            <span class="text-muted">(<asp:Label ID="lblTolRawMin" runat="server" Text='<%# Eval("MinAllowedKM") %>' Visible="true" />
                                                                /
                                                            <asp:Label ID="lblTolRawMax" runat="server" Text='<%# Eval("MaxAllowedKM") %>' Visible="true" />)</span>
                                                        </small>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtKM" runat="server" Text='<%# Bind("TotalDistanceKM") %>' CssClass="form-control form-control-sm km-input"></asp:TextBox>
                                                        <small class="form-text text-muted">Allowed:
                                                            <asp:Label ID="lblTolLower_Edit" runat="server" Text="-" CssClass="tol-lower" />
                                                            &nbsp; - &nbsp;
                                                            <asp:Label ID="lblTolUpper_Edit" runat="server" Text="-" CssClass="tol-upper" />
                                                            <br />
                                                            <span class="text-muted">(Current: <%# Eval("TotalDistanceKM") %> ; MinTol: <%# Eval("MinAllowedKM") ?? "—" %> ; MaxTol: <%# Eval("MaxAllowedKM") ?? "—" %> )</span>
                                                        </small>
                                                        <span class="text-danger km-error" style="display: none;">Value must be within allowed range.</span>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>

                                                <asp:BoundField HeaderText="ClaimStatus" DataField="ClaimStatus" ReadOnly="true" />

                                                <asp:TemplateField HeaderText="Actions">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRow" CommandArgument='<%# Eval("ActivityId") %>' CssClass="btn btn-sm btn-outline-primary" Text="Edit KM" Visible='<%# Eval("ClaimStatus").ToString()=="Draft" %>'></asp:LinkButton>

                                                        <asp:LinkButton ID="lnkExpenses" runat="server" CommandName="OpenExpenses" CommandArgument='<%# Eval("ActivityId") + "|" + Eval("ActivityDate") %>' CssClass="btn btn-sm btn-outline-info">Expenses</asp:LinkButton>
                                                    </ItemTemplate>

                                                    <EditItemTemplate>
                                                        <asp:LinkButton ID="lnkUpdate" runat="server" CommandName="UpdateRow" CommandArgument='<%# Eval("ActivityId") %>' OnClientClick="return validateGridBeforeSubmit();" CssClass="btn btn-sm btn-success">Update</asp:LinkButton>
                                                        <asp:LinkButton ID="lnkCancel" runat="server" CommandName="Cancel" CssClass="btn btn-sm btn-secondary">Cancel</asp:LinkButton>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade bs-example-modal-lg" tabindex="-1" role="dialog" aria-hidden="true" id="expenseModal">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">DA / Hotel Expense</h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close" onclick="$('#expenseModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:Panel runat="server" ID="pnlExpense">
                            <asp:HiddenField ID="HF_ActivityId" runat="server" />
                            <asp:HiddenField ID="HF_ActivityDate" runat="server" />

                            <div class="row">
                                <div class="col-md-4 form-group">
                                    <label>DA Amount</label>
                                    <asp:TextBox ID="TXT_DAAmount" runat="server" CssClass="form-control form-control-sm"></asp:TextBox>
                                </div>

                                <div class="col-md-8 form-row">
                                    <div class="col-md-4 form-group">
                                        <label>Hotel Amount</label>
                                        <asp:TextBox ID="TXT_HotelAmount" runat="server" CssClass="form-control form-control-sm"></asp:TextBox>
                                    </div>

                                    <div class="col-md-4 form-group">
                                        <label>From Date</label>
                                        <asp:TextBox ID="TXT_HotelFromDate" runat="server" TextMode="Date"
                                            CssClass="form-control form-control-sm rounded" />
                                    </div>

                                    <div class="col-md-4 form-group">
                                        <label>To Date</label>
                                        <asp:TextBox ID="TXT_HotelToDate" runat="server" TextMode="Date"
                                            CssClass="form-control form-control-sm rounded" />
                                    </div>


                                    <div class="col-md-6 form-group">
                                        <label>Bill No</label>
                                        <asp:TextBox ID="TXT_HotelBillNo" runat="server" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-6 form-group">
                                        <label>Purpose</label>
                                        <asp:TextBox ID="TXT_HotelPurpose" runat="server" TextMode="MultiLine" Rows="2" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-12 form-group">
                                        <label>Attach Receipt</label>
                                        <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" />
                                        <small class="form-text text-muted">JPG, PNG, PDF. Max 5MB. Saves to UploadedFiles/ClaimsAttachment by default.</small>
                                    </div>
                                </div>

                            </div>

                            <hr />

                            <!-- Expense Totals Card -->
                            <div class="card mb-3">
                                <div class="card-body p-2">
                                    <div class="row text-center">
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">DA Total</div>
                                            <div id="dvDA" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalDA" runat="server" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">Hotel Total</div>
                                            <div id="dvHotel" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalHotel" runat="server" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">Other Total</div>
                                            <div id="dvOther" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalOther" runat="server" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3">
                                            <div class="small text-muted">Overall Total</div>
                                            <div id="dvOverall" runat="server" class="h5 mb-0">
                                                <asp:Label ID="lblOverallTotal" runat="server" Text="0.00" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <hr />

                            <h6>Existing Expenses</h6>
                            <%--<asp:Repeater ID="rptExpenses" runat="server">
                                <HeaderTemplate>
                                    <table class="table table-sm table-bordered">
                                        <thead>
                                            <tr>
                                                <th>Type</th>
                                                <th>Date</th>
                                                <th>Amount</th>
                                                <th>BillNo</th>
                                                <th>Attachment</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("ExpenseType") %></td>
                                        <td><%# Eval("ExpenseDate", "{0:yyyy-MM-dd}") %></td>
                                        <td><%# Eval("Amount") %></td>
                                        <td><%# Eval("BillNo") %></td>
                                        <td>
                                            <%# Eval("AttachmentFileName") != DBNull.Value ?
                                                ("<a href=\"DownloadExpense.ashx?expenseId=" + Eval("ExpenseId") + "\">" + Eval("AttachmentFileName") + "</a>")
                                                : "—" %>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </tbody>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>--%>

                            <asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand">
                                <HeaderTemplate>
                                    <table class="table table-sm table-bordered">
                                        <thead>
                                            <tr>
                                                <th>Type</th>
                                                <th>Date</th>
                                                <th>Amount</th>
                                                <th>BillNo</th>
                                                <th>Attachment</th>
                                                <th style="width: 95px;">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("ExpenseType") %></td>
                                        <td><%# Eval("ExpenseDate", "{0:yyyy-MM-dd}") %></td>
                                        <td><%# Eval("Amount") %></td>
                                        <td><%# Eval("BillNo") ?? "—" %></td>

                                        <td>
                                            <%# Eval("AttachmentFileName") != DBNull.Value && !string.IsNullOrWhiteSpace(Eval("AttachmentFileName").ToString())
                                            ? String.Format("<a href=\"DownloadExpense.ashx?expenseId={0}\" class=\"btn btn-sm btn-outline-secondary\" title=\"Download\"><i class=\"fa fa-download\"></i></a>", Eval("ExpenseId"))
                                            : "—" %>
                                        </td>

                                        <td>
                                            <asp:LinkButton ID="lnkDeleteExpense" runat="server"
                                                CssClass="btn btn-sm btn-outline-danger"
                                                CommandName="DeleteExpense"
                                                CommandArgument='<%# Eval("ExpenseId") + "|" + (Eval("AttachmentFileName") ?? "") + "|" + (Eval("ActivityId") ?? "") %>'
                                                OnClientClick="return confirm('Delete this expense? This action cannot be undone.');"
                                                ToolTip="Delete">
                                                <i class="fa fa-trash"></i>
                                            </asp:LinkButton>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </tbody>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>


                            <asp:Label ID="LBL_ExpenseMsg" runat="server" ForeColor="Red"></asp:Label>
                        </asp:Panel>
                    </div>

                    <div class="modal-footer">
                        <asp:Button ID="BTN_SaveExpense" runat="server" CssClass="btn btn-primary btn-sm" Text="Save Expense" OnClick="BTN_SaveExpense_Click" />
                        <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal" onclick="$('#expenseModal').modal('hide');">Close</button>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function showExpenseModal() {
            $('#expenseModal').modal('show');
        }

        function hideExpenseModal() {
            $('#expenseModal').modal('hide');
        }

        $(document).on("input blur", ".km-input", function () {
            var $txt = $(this);
            var row = $txt.closest("tr");
            var val = parseFloat($txt.val());

            var lower = parseFloat(row.find(".tol-lower").text());
            var upper = parseFloat(row.find(".tol-upper").text());

            var $err = row.find(".km-error");

            if (isNaN(val)) {
                $txt.addClass("is-invalid");
                $err.text("Please enter a valid number.").show();
                return;
            }

            var inRange = true;
            if (!isNaN(lower) && val < lower) inRange = false;
            if (!isNaN(upper) && val > upper) inRange = false;

            if (!inRange) {
                $txt.addClass("is-invalid");
                $err.text("Value must be between " + lower + " and " + upper + ".").show();
            } else {
                $txt.removeClass("is-invalid");
                $err.hide();
            }
        });

        function validateGridBeforeSubmit() {
            var invalid = $(".km-input.is-invalid").length > 0;
            if (invalid) {
                alert("Please fix invalid KM entries before saving.");
                return false;
            }
            return true;
        }
    </script>

</asp:Content>
