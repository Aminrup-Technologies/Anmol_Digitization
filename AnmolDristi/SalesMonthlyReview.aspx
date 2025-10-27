<%@ Page Title="AIL | SO Cliams Submission" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="SalesMonthlyReview.aspx.cs" Inherits="AnmolDristi.SalesMonthlyReview" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        /* ===== Finance Grid Styling (ASP.NET 4.5.2 compatible) ===== */
        /* scroll container for sticky header */
        .grid-container {
            max-height: 480px;
            overflow-y: auto;
            overflow-x: auto;
            border: 1px solid #bfc8d3;
            border-radius: 8px;
            box-shadow: 0 3px 8px rgba(0,0,0,0.08);
            background: #fff;
        }

        /* base table setup */
        .table-finance {
            width: 100%;
            border-collapse: collapse;
            table-layout: auto;
            font-size: 13px;
            border: 1px solid #d3d9e3;
        }

            /* HEADER - polished gradient, shadow & subtle glow */
            .table-finance thead th {
                position: sticky;
                top: 0;
                z-index: 50;
                background: linear-gradient(180deg,#415a77 0%,#2c3e55 100%);
                color: #f8f9fa;
                text-align: center;
                font-weight: 600;
                letter-spacing: 0.3px;
                text-transform: uppercase;
                padding: 9px 8px;
                border: 1px solid #2a4468;
                white-space: nowrap;
                box-shadow: 0 2px 5px rgba(0,0,0,0.15) inset;
            }

                .table-finance thead th:first-child {
                    border-top-left-radius: 6px;
                }

                .table-finance thead th:last-child {
                    border-top-right-radius: 6px;
                }

                /* header hover and transition */
                .table-finance thead th:hover {
                    background: linear-gradient(180deg,#496a91 0%,#3c5674 100%);
                    transition: background 0.25s ease-in-out;
                    cursor: default;
                }

            /* DATA ROWS */
            .table-finance td {
                border: 1px solid #e1e1e1;
                padding: 6px 8px;
                vertical-align: middle;
                color: #333;
                background-color: #fff;
            }

            /* ZEBRA STRIPING */
            .table-finance tr:nth-child(even) td {
                background-color: #fafbfc;
            }

            /* FOOTER ROW */
            .table-finance tfoot td, .table-finance .grid-footer {
                background-color: #eef3fa;
                font-weight: 600;
                color: #1a355e;
                border-top: 2px solid #b7c5da;
            }

            /* TOTAL HIGHLIGHT */
            .table-finance .grand-total-cell {
                background-color: #e2e8f6 !important;
                color: #002b59;
                font-weight: 700;
            }

            /* ALIGNMENT HELPERS */
            .table-finance .currency {
                text-align: right;
                white-space: nowrap;
                padding-right: 10px;
            }

        .km-input {
            width: 110px;
            text-align: right;
        }

        .tol-lower, .tol-upper {
            display: inline-block;
            min-width: 40px;
            text-align: center;
        }

        .table-finance th, .table-finance td {
            vertical-align: middle;
            padding: .5rem .75rem;
        }

        .table-finance .btn {
            margin-right: 4px;
        }

        /* RESPONSIVE */
        @media (max-width:768px) {
            .table-finance th, .table-finance td {
                font-size: 12px;
                padding: 5px;
            }
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Apply Monthly Claims</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Select Year <span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Year" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Select Month <span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Month" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>

                                <div class="col-md-3 align-self-end">
                                    <asp:Button ID="btn_fetch" runat="server" CssClass="btn btn-primary btn-sm" Text="Search" OnClick="btn_fetch_Click" />
                                </div>

                                <div id="Actionable" runat="server" visible="false" class="col-md-3 text-right align-self-end">
                                    <!-- Reopen Policy Button -->
                                    <button type="button" id="btnViewPolicyAgain"
                                        class="btn btn-info btn-sm"
                                        onclick="$('#policyModal').modal('show');">
                                        <i class="fa fa-info-circle"></i>View Policy
                                    </button>


                                    <asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm" Text="Save as Draft" OnClick="btn_save_draft_Click" />
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
                        <%--<div class="x_title">
                            <h2>Daily Sales Activity</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>--%>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive grid-container">
                                        <asp:GridView ID="gvActivities" runat="server" AutoGenerateColumns="false"
                                            CssClass="table table-bordered table-finance" HeaderStyle-CssClass="grid-header" FooterStyle-CssClass="grid-footer"
                                            DataKeyNames="ActivityId"
                                            OnRowCommand="gvActivities_RowCommand"
                                            OnRowEditing="gvActivities_RowEditing"
                                            OnRowCancelingEdit="gvActivities_RowCancelingEdit"
                                            OnRowUpdating="gvActivities_RowUpdating"
                                            OnRowDataBound="gvActivities_RowDataBound"
                                            ShowFooter="true">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Travel Date" ItemStyle-Width="10%">
                                                    <ItemTemplate>
                                                        <%# Eval("ActivityDate", "{0:dd-MM-yyyy}") %><br />
                                                        <small class="text-muted"><%# Eval("Day") %></small>
                                                    </ItemTemplate>
                                                    <FooterTemplate><strong>Total :</strong>&nbsp;</FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Travel Distance (KM)">
                                                    <ItemStyle HorizontalAlign="Left" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Left" />
                                                    <ItemTemplate>
                                                        <%# Eval("TotalDistanceKM", "{0:N2}") %> <small>km</small>
                                                        <br />
                                                        <small class="text-muted">
                                                            <asp:Label ID="lblTolRawMin" runat="server" Text='<%# Eval("MinAllowedKM") %>' Visible="false" />
                                                            <asp:Label ID="lblTolRawMax" runat="server" Text='<%# Eval("MaxAllowedKM") %>' Visible="false" />
                                                        </small>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtKM" runat="server" Text='<%# Bind("TotalDistanceKM") %>'
                                                            CssClass="form-control form-control-sm km-input" />
                                                        <small class="form-text text-muted">Allowed:
                                                            <asp:Label ID="lblTolLower_Edit" runat="server" Text="-" CssClass="tol-lower" />
                                                            &nbsp; - &nbsp;
                                                        <asp:Label ID="lblTolUpper_Edit" runat="server" Text="-" CssClass="tol-upper" />
                                                            <br />
                                                            <span class="text-muted">(Current: <%# Eval("TotalDistanceKM") %> ; Min: <%# Eval("MinAllowedKM") ?? "—" %> ; Max: <%# Eval("MaxAllowedKM") ?? "—" %> )</span>
                                                        </small>
                                                        <span class="text-danger km-error" style="display: none;">Value must be within allowed range.</span>
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterTotalKM" runat="server" CssClass="font-weight-bold" Text="0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="TA Amount (₹)" SortExpression="CalculatedClaimAmount">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblCalculatedClaim" runat="server"
                                                            Text='<%# Eval("CalculatedClaimAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                        <br />
                                                        <small class="text-muted" runat="server" visible="true">(<%# Eval("RatePerKm", "{0:N2}") %> / km)</small>
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterCalculated" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="DA Amount (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblDA" runat="server" Text='<%# Eval("DAAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterDA" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Hotel (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblHotel" runat="server" Text='<%# Eval("HotelAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterHotel" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Day Total (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblExpenseTotal" runat="server" Text='<%# Eval("ExpenseTotal", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterExpenseTotal" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:BoundField HeaderText="Claim Status" DataField="ClaimStatus" ReadOnly="true" ItemStyle-Width="10%" ItemStyle-HorizontalAlign="Center" />

                                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="20%">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lnkEdit" runat="server" CommandName="EditRow" CommandArgument='<%# Eval("ActivityId") %>'
                                                            CssClass="btn btn-sm btn-primary" Text="Edit KM"
                                                            Visible='<%# Eval("ClaimStatus").ToString() == "Draft" %>'></asp:LinkButton>

                                                        <asp:LinkButton ID="lnkExpenses" runat="server"
                                                            CommandName="OpenExpenses"
                                                            CommandArgument='<%# Eval("ActivityId") + "|" + Eval("ActivityDate") %>'
                                                            CssClass="btn btn-sm btn-info">Expenses</asp:LinkButton>
                                                    </ItemTemplate>

                                                    <EditItemTemplate>
                                                        <asp:LinkButton ID="lnkUpdate" runat="server" CommandName="UpdateRow" CommandArgument='<%# Eval("ActivityId") %>'
                                                            OnClientClick="return validateGridBeforeSubmit();" CssClass="btn btn-sm btn-success">Update</asp:LinkButton>
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
                        <h5 class="modal-title">DA / Hotel Expenses for
                            <asp:Label ID="lbl_traveldate" runat="server" Text="NA"></asp:Label></h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close" onclick="$('#expenseModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:Panel runat="server" ID="pnlExpense">
                            <asp:HiddenField ID="HF_ActivityId" runat="server" />
                            <asp:HiddenField ID="HF_ActivityDate" runat="server" />

                            <!-- START: Expense Type selector (NEW) -->
                            <div class="row mb-2">
                                <div class="col-md-12 form-group">
                                    <label>Expense Type</label>
                                    <asp:DropDownList ID="DDL_ExpenseType" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Value="DA">DA</asp:ListItem>
                                        <asp:ListItem Value="Hotel">Hotel</asp:ListItem>
                                        <asp:ListItem Value="Travel">Travel</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                            </div>
                            <!-- END: Expense Type selector (NEW) -->

                            <!-- DA section wrapper -->
                            <div id="divDASection" runat="server" class="row">
                                <div class="col-md-4 form-group">
                                    <label>DA Amount</label>
                                    <asp:TextBox ID="TXT_DAAmount" runat="server" CssClass="form-control form-control-sm" Enabled="false"></asp:TextBox>
                                </div>
                            </div>

                            <!-- Hotel section wrapper -->
                            <div id="divHotelSection" runat="server" class="form-row">

                                <div class="col-md-3 form-group">
                                    <label>Stay From Date</label>
                                    <asp:TextBox ID="TXT_HotelFromDate" runat="server" TextMode="Date"
                                        CssClass="form-control form-control-sm rounded" />
                                </div>

                                <div class="col-md-3 form-group">
                                    <label>Stay To Date</label>
                                    <asp:TextBox ID="TXT_HotelToDate" runat="server" TextMode="Date" Enabled="false"
                                        CssClass="form-control form-control-sm rounded" />
                                </div>

                                <div class="col-md-3 form-group">
                                    <label>Bill/ Invoice No</label>
                                    <asp:TextBox ID="TXT_HotelBillNo" runat="server" CssClass="form-control form-control-sm" />
                                </div>

                                <div class="col-md-3 form-group">
                                    <label>Hotel Amount</label>
                                    <asp:TextBox ID="TXT_HotelAmount" runat="server" CssClass="form-control form-control-sm"></asp:TextBox>
                                </div>

                                <div class="col-md-12 form-group">
                                    <label>Purpose of Stay</label>
                                    <asp:TextBox ID="TXT_HotelPurpose" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                </div>


                            </div>

                            <%--<div class="row">
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
                                        <label>Bill/ Invoice No</label>
                                        <asp:TextBox ID="TXT_HotelBillNo" runat="server" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-6 form-group">
                                        <label>Purpose of Stay</label>
                                        <asp:TextBox ID="TXT_HotelPurpose" runat="server" TextMode="MultiLine" Rows="2" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-12 form-group">
                                        <label>Attach Receipt</label>
                                        <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" />
                                        <small class="form-text text-muted">JPG, PNG, PDF. Max 5MB. Saves to UploadedFiles/ClaimsAttachment by default.</small>
                                    </div>
                                </div>

                            </div>--%>

                            <div id="divTravelSection" runat="server" class="form-row">
                                <div class="form-row">
                                    <div class="col-md-3 form-group">
                                        <label>Travel Type</label>
                                        <asp:DropDownList ID="DDL_TravelType" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Value="Train">Train</asp:ListItem>
                                            <asp:ListItem Value="Bus">Bus</asp:ListItem>
                                            <%--<asp:ListItem Value="Flight">Flight</asp:ListItem>
                                            <asp:ListItem Value="Taxi">Taxi</asp:ListItem>--%>
                                            <asp:ListItem Value="Other">Other</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>

                                    <div class="col-md-3 form-group">
                                        <label>Travel Date</label>
                                        <asp:TextBox ID="TXT_TravelDate" runat="server" CssClass="form-control form-control-sm" Enabled="false" TextMode="Date" />
                                    </div>

                                    <div class="col-md-3 form-group">
                                        <label>From Place</label>
                                        <asp:TextBox ID="TXT_FromPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="200" />
                                    </div>

                                    <div class="col-md-3 form-group">
                                        <label>To Place</label>
                                        <asp:TextBox ID="TXT_ToPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="200" />
                                    </div>



                                    <div class="col-md-4 form-group">
                                        <label>Amount</label>
                                        <asp:TextBox ID="TXT_TravelAmount" runat="server" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-8 form-group">
                                        <label>Purpose of Travel</label>
                                        <asp:TextBox ID="TXT_TravelPurpose" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                    </div>
                                </div>

                                <div class="col-md-4 form-group">
                                    <asp:CheckBox ID="CHK_TravelInvoice" runat="server" />
                                    <label class="mb-0 ml-4">With Invoice</label>
                                </div>

                                <div class="col-md-4 form-group" id="divTravelInvoiceNo" runat="server">
                                    <label>Invoice No</label>
                                    <asp:TextBox ID="TXT_TravelInvoiceNo" runat="server" CssClass="form-control form-control-sm" />
                                </div>


                            </div>
                            <!-- END: Travel Section (NEW) -->

                            <div class="col-md-12 form-group" id="divFUContainer" runat="server">
                                <label class="col-md-4">Attach Receipt</label>
                                <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" />
                                <small class="form-text text-muted">JPG, PNG, PDF. Max 5MB. Saves to UploadedFiles/ClaimsAttachment by default.</small>
                            </div>

                            <div>&nbsp;</div>
                            <br />

                            <!-- Expense Totals Card -->
                            <div class="card mb-3">
                                <div class="card-body p-2">
                                    <div class="row text-center">
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">DA Total</div>
                                            <div id="dvDA" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalDA" runat="server" Font-Bold="true" ForeColor="#0099ff" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">Hotel Total</div>
                                            <div id="dvHotel" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalHotel" runat="server" Font-Bold="true" ForeColor="#0099ff" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3 border-right">
                                            <div class="small text-muted">Travel Total</div>
                                            <div id="dvOther" runat="server" class="h6 mb-0">
                                                <asp:Label ID="lblTotalOther" runat="server" Font-Bold="true" ForeColor="#0099ff" Text="0.00" />
                                            </div>
                                        </div>
                                        <div class="col-6 col-md-3">
                                            <div class="small text-muted">Overall Total</div>
                                            <div id="dvOverall" runat="server" class="h5 mb-0">
                                                <asp:Label ID="lblOverallTotal" runat="server" Font-Bold="true" ForeColor="#0033cc" Text="0.00" />
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

                            <%--<asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand">
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
                            </asp:Repeater>--%>

                            <asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand">
                                <HeaderTemplate>
                                    <table class="table table-sm table-bordered">
                                        <thead>
                                            <tr>
                                                <th>Type</th>
                                                <th>Date</th>
                                                <th>Details</th>
                                                <th>Amount</th>
                                                <th>BillNo</th>
                                                <th>Attachment</th>
                                                <th style="width: 110px;">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                </HeaderTemplate>

                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("ExpenseType") %></td>

                                        <%-- Date: prefer TravelDate for Travel, else ExpenseDate --%>
                                        <td>
                                            <%# (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Travel")
                                                ? (Eval("TravelDate") == DBNull.Value ? "—" : Eval("TravelDate", "{0:yyyy-MM-dd}"))
                                                : (Eval("ExpenseDate") == DBNull.Value ? "—" : Eval("ExpenseDate", "{0:yyyy-MM-dd}")) %>
                                        </td>

                                        <%-- Details: Travel -> TravelType + From->To + Purpose; Hotel -> FromDate - ToDate + Purpose; DA/Other -> Purpose --%>
                                        <td>
                                            <%# 
                                            (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Travel")
                                            ? String.Format("{0} {1} -> {2} {3}",
                                                Eval("TravelType") ?? "",
                                                Eval("FromPlace") ?? "",
                                                Eval("ToPlace") ?? "",
                                                (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                              )
                                            : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Hotel")
                                                ? String.Format("{0} - {1} {2}",
                                                    (Eval("FromDate") == DBNull.Value ? "—" : String.Format("{0:yyyy-MM-dd}", Eval("FromDate"))),
                                                    (Eval("ToDate") == DBNull.Value ? "—" : String.Format("{0:yyyy-MM-dd}", Eval("ToDate"))),
                                                    (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                  )
                                                : (Eval("Purpose") ?? "—")
                                            %>
                                        </td>

                                        <td><%# Eval("Amount", "{0:N2}") %></td>
                                        <td><%# Eval("BillNo") ?? "—" %></td>

                                        <td>
                                            <%# Eval("AttachmentFileName") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("AttachmentFileName")))
                                            ? String.Format("<a href=\"DownloadExpense.ashx?expenseId={0}\" class=\"btn btn-sm btn-outline-secondary\" title=\"Download\"><i class=\"fa fa-download\"></i></a>", Eval("ExpenseId"))
                                            : "—" %>
                                        </td>

                                        <td>
                                            <asp:LinkButton ID="lnkEditExpense" runat="server"
                                                CssClass="btn btn-sm btn-outline-primary mr-1"
                                                CommandName="EditExpense"
                                                CommandArgument='<%# Eval("ExpenseId") %>'
                                                ToolTip="Edit" Visible="false" CausesValidation="false">
                                                <i class="fa fa-edit"></i>
                                            </asp:LinkButton>

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

        <div class="modal fade" id="policyModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
                <div class="modal-content shadow-lg border-0 rounded-lg overflow-hidden">

                    <!-- Header -->
                    <div class="modal-header bg-primary text-white py-2">
                        <h5 class="modal-title mb-0">
                            <i class="fa fa-file-alt mr-2"></i>Resolved Policy
                    <small class="text-light">(<asp:Label ID="lblPolicyEmployeeCode" runat="server" Text="—" CssClass="small"></asp:Label>)
                    </small>
                        </h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"
                            onclick="$('#policyModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>

                    <!-- Body -->
                    <div class="modal-body bg-light px-4 pt-3 pb-1">

                        <!-- Top Metadata -->
                        <div class="row mb-3">
                            <div class="col-md-4 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Source</label>
                                <asp:Label ID="lblPolicySource" runat="server" CssClass="d-block font-weight-bold text-dark">—</asp:Label>
                            </div>
                            <div class="col-md-2 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Record ID</label>
                                <asp:Label ID="lblPolicyRowId" runat="server" CssClass="d-block text-dark">—</asp:Label>
                            </div>
                            <div class="col-md-3 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Priority</label>
                                <asp:Label ID="lblPolicyPriority" runat="server" CssClass="d-block text-info font-weight-bold">—</asp:Label>
                            </div>
                            <div class="col-md-3 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Specificity</label>
                                <asp:Label ID="lblPolicySpecificity" runat="server" CssClass="d-block text-primary font-weight-semibold">—</asp:Label>
                            </div>
                        </div>

                        <!-- Effective Dates & Active -->
                        <div class="row mb-3">
                            <div class="col-md-6 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Effective</label>
                                <asp:Label ID="lblPolicyEffective" runat="server" CssClass="d-block text-dark">—</asp:Label>
                            </div>
                            <div class="col-md-3 col-sm-6 mb-2">
                                <label class="small text-secondary mb-0">Active</label>
                                <asp:Label ID="lblPolicyIsActive" runat="server" CssClass="d-block text-success font-weight-bold">—</asp:Label>
                            </div>
                            <div class="col-md-3 col-sm-12 mb-2">
                                <label class="small text-secondary mb-0">Designation / Grade</label>
                                <asp:Label ID="lblPolicyDesigGrade" runat="server" CssClass="d-block text-dark font-weight-semibold">—</asp:Label>
                            </div>
                        </div>

                        <!-- Resolved Numeric Values -->
                        <div class="card shadow-sm border-0 mb-3">
                            <div class="card-header bg-gradient-primary text-black py-1 px-3">
                                <strong><i class="fa fa-calculator mr-1"></i>Resolved Capping Limits</strong>
                            </div>
                            <div class="card-body bg-white p-3">
                                <div class="row text-center">
                                    <div class="col-6 col-md-3 mb-3 mb-md-0 border-md-right">
                                        <div class="small text-muted">Max KM</div>
                                        <div class="h6 font-weight-bold text-primary mb-0">
                                            <asp:Label ID="lblResolvedMaxKM" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-6 col-md-3 mb-3 mb-md-0 border-md-right">
                                        <div class="small text-muted">Rate / Km</div>
                                        <div class="h6 font-weight-bold text-info mb-0">
                                            <asp:Label ID="lblResolvedRatePerKm" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-6 col-md-3 mb-3 mb-md-0 border-md-right">
                                        <div class="small text-muted">D.A.</div>
                                        <div class="h6 font-weight-bold text-success mb-0">
                                            <asp:Label ID="lblResolvedDA" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-6 col-md-3">
                                        <div class="small text-muted">Hotel Cap.</div>
                                        <div class="h6 font-weight-bold text-danger mb-0">
                                            <asp:Label ID="lblResolvedHotelCap" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Remarks -->
                        <div class="form-group mb-3">
                            <label class="small text-secondary mb-1">Remarks</label>
                            <asp:Label ID="lblResolvedRemarks" runat="server" CssClass="d-block text-dark bg-white border rounded p-2">—</asp:Label>
                        </div>

                        <!-- Raw JSON Toggle -->
                        <div class="form-group" id="Toggler" runat="server" visible="false">
                            <asp:Button ID="btnToggleRawJson" runat="server" CssClass="btn btn-sm btn-outline-dark mb-2"
                                OnClientClick="$('#policyJsonRaw').toggle(); return false;" Text="Toggle Raw JSON" />
                            <asp:TextBox ID="policyJsonRaw" runat="server" TextMode="MultiLine" Rows="6"
                                CssClass="form-control form-control-sm font-monospace text-dark bg-light border border-secondary"
                                Visible="false"></asp:TextBox>
                        </div>

                        <!-- Hidden Context Fields -->
                        <asp:HiddenField ID="HF_Policy_RegionId" runat="server" />
                        <asp:HiddenField ID="HF_Policy_BranchId" runat="server" />
                        <asp:HiddenField ID="HF_Policy_GradeId" runat="server" />
                        <asp:HiddenField ID="HF_Policy_DesignationId" runat="server" />
                    </div>

                    <!-- Footer -->
                    <div class="modal-footer bg-light border-top">
                        <asp:LinkButton ID="lnkCopyJson" runat="server" CssClass="btn btn-sm btn-outline-primary" Visible="false"
                            OnClientClick="copyPolicyJson(); return false;">Copy JSON</asp:LinkButton>
                        <button type="button" class="btn btn-danger btn-sm" data-dismiss="modal"
                            onclick="$('#policyModal').modal('hide');">
                            <i class="fa fa-times mr-1"></i>Close
                        </button>
                    </div>

                </div>
            </div>
        </div>


    </div>

    <script type="text/javascript">

        function showPolicyBypassNotice() {
            new PNotify({
                title: 'Policy Disabled',
                text: 'Monthly processing policy is currently disabled. Validation skipped and process allowed.',
                type: 'info',
                delay: 4000,
                styling: 'bootstrap3'
            });
        }
        function showPNotify(title, message, type) {
            new PNotify({
                title: title,
                text: message,
                type: type,       // 'success', 'info', 'error'
                delay: 4000,
                styling: 'bootstrap3'
            });
        }
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

        (function () {
            function $id(id) { return document.getElementById(id); }

            // server control client IDs - adjust if your ASPX uses different IDs
            var ddlType = $id('<%= DDL_ExpenseType.ClientID %>');
            var divDA = $id('<%= divDASection.ClientID %>');
            var divHotel = $id('<%= divHotelSection.ClientID %>');
            var divTravel = $id('<%= divTravelSection.ClientID %>');

            var chkTravelInvoice = $id('<%= CHK_TravelInvoice.ClientID %>');
            var divTravelInvoiceNo = $id('<%= divTravelInvoiceNo.ClientID %>');
            var divFUContainer = $id('<%= divFUContainer.ClientID %>'); // the fileupload container outside

            // locate the actual file input element (FU_Attachment)
            var fuInput = $id('<%= FU_Attachment.ClientID %>') || (divFUContainer ? divFUContainer.querySelector('input[type=file]') : null);

            function hide(el) { if (!el) return; el.classList.add('d-none'); }
            function show(el) { if (!el) return; el.classList.remove('d-none'); }

            function updateVisibility() {
                var t = ddlType ? ddlType.value : 'DA';

                if (t === 'DA') {
                    if (divDA) show(divDA);
                    if (divHotel) hide(divHotel);
                    if (divTravel) hide(divTravel);
                    if (divFUContainer) hide(divFUContainer);
                    if (divTravelInvoiceNo) hide(divTravelInvoiceNo);
                    //if (divTravelAttachment) hide(divTravelAttachment);
                }
                else if (t === 'Hotel') {
                    if (divDA) hide(divDA);
                    if (divHotel) show(divHotel);
                    if (divTravel) hide(divTravel);
                    // show file upload container for Hotel
                    if (divFUContainer) show(divFUContainer);
                    if (divTravelInvoiceNo) hide(divTravelInvoiceNo);
                    //if (divTravelAttachment) show(divTravelAttachment); // optional: keep travel attachment area hidden or show a notice
                }
                else if (t === 'Travel') {
                    if (divDA) hide(divDA);
                    if (divHotel) hide(divHotel);
                    if (divTravel) show(divTravel);

                    // Travel: only show global FU container when invoice checked
                    if (chkTravelInvoice && chkTravelInvoice.checked) {
                        if (divFUContainer) show(divFUContainer);
                        if (divTravelInvoiceNo) show(divTravelInvoiceNo);
                    } else {
                        if (divFUContainer) hide(divFUContainer);
                        if (divTravelInvoiceNo) hide(divTravelInvoiceNo);
                    }
                } else {
                    // fallback: hide most
                    if (divDA) show(divDA);
                    if (divHotel) hide(divHotel);
                    if (divTravel) hide(divTravel);
                    if (divFUContainer) hide(divFUContainer);
                    if (divTravelInvoiceNo) hide(divTravelInvoiceNo);
                }
            }

            function onInvoiceToggle() {
                if (!chkTravelInvoice) return;
                if (chkTravelInvoice.checked) {
                    if (divFUContainer) show(divFUContainer);
                    if (divTravelInvoiceNo) show(divTravelInvoiceNo);
                    //if (divTravelAttachment) show(divTravelAttachment);
                } else {
                    if (divFUContainer) hide(divFUContainer);
                    if (divTravelInvoiceNo) hide(divTravelInvoiceNo);
                    //if (divTravelAttachment) hide(divTravelAttachment);
                    if (divFUContainer) {
                        // clear file input when hiding (best-effort; may be blocked by browsers)
                        try { if (fuInput) fuInput.value = ''; } catch (e) { /* ignore */ }
                        hide(divFUContainer);
                    }
                }
            }

            document.addEventListener('DOMContentLoaded', function () {
                if (ddlType) ddlType.addEventListener('change', updateVisibility);
                if (chkTravelInvoice) chkTravelInvoice.addEventListener('change', onInvoiceToggle);
                updateVisibility(); // initialize state
            });
        })();

        function copyPolicyJson() {
            var tb = document.getElementById('<%= policyJsonRaw.ClientID %>');
            if (!tb) return;
            tb.style.display = 'block'; // ensure visible for selection
            tb.select();
            try {
                document.execCommand('copy');
                // optional small feedback
                alert('Policy JSON copied to clipboard');
            } catch (e) {
                alert('Copy failed — select and copy manually');
            }
            tb.style.display = 'none';
        }

        // When the policy modal is shown, show the "View Policy Again" button
        $(document).on('shown.bs.modal', '#policyModal', function () {
            $('#btnViewPolicyAgain').fadeIn(200);
        });

        // Optional: hide it when modal is closed (if you prefer it to disappear)
        // Remove this block if you want the button to remain visible after close
        //$(document).on('hidden.bs.modal', '#policyModal', function () {
        //    $('#btnViewPolicyAgain').fadeOut(200);
        //});
    </script>

</asp:Content>
