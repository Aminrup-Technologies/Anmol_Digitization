<%@ Page Title="AIL | SO Claims Submission" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="SalesMonthlyReview.aspx.cs" Inherits="AnmolDristi.SalesMonthlyReview" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=" crossorigin="anonymous" />
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <style type="text/css">
        .grid-container {
            max-height: 65vh;
            overflow-y: auto;
            overflow-x: auto;
            border: 1px solid #bfc8d3;
            border-radius: 8px;
            box-shadow: 0 3px 8px rgba(0,0,0,0.08);
            background: #fff;
        }

        .table-finance {
            width: 100%;
            border-collapse: collapse;
            table-layout: auto;
            font-size: 13px;
            border: 1px solid #d3d9e3;
        }

            .table-finance thead th {
                position: sticky;
                top: 0;
                z-index: 50;
                background: linear-gradient(180deg,#415a77 0%,#2c3e55 100%);
                color: #f8f9fa;
                text-align: center;
                font-weight: 600;
                letter-spacing: 0.3px;
                text-transform: none;
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

                .table-finance thead th:hover {
                    background: linear-gradient(180deg,#496a91 0%,#3c5674 100%);
                    transition: background 0.25s ease-in-out;
                    cursor: default;
                }

            .table-finance td {
                border: 1px solid #e1e1e1;
                padding: 6px 8px;
                vertical-align: middle;
                color: #333;
                background-color: #fff;
            }

            .table-finance tr:nth-child(even) td {
                background-color: #fafbfc;
            }

            .table-finance tfoot td, .table-finance .grid-footer {
                background-color: #eef3fa;
                font-weight: 600;
                color: #1a355e;
                border-top: 2px solid #b7c5da;
                position: sticky;
                bottom: 0; /* Keep footer sticky if table overflows */
                z-index: 40;
            }

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

        @media (max-width:768px) {
            .table-finance th, .table-finance td {
                font-size: 12px;
                padding: 5px;
            }
        }

        .badge {
            font-size: 11px;
            padding: 3px 6px;
            border-radius: 4px;
        }

            .badge i {
                margin-right: 3px;
            }

        button.btn i.fa-spinner {
            margin-right: 6px;
        }

        #expenseModal .modal-content {
            border-radius: 0.6rem;
            box-shadow: 0 6px 28px rgba(0,0,0,0.12);
            overflow: hidden;
        }

        #expenseModal .modal-header {
            background: linear-gradient(90deg,#f7fbff,#eef7ff);
            border-bottom: 1px solid rgba(0,0,0,0.05);
            padding: 1rem 1.25rem;
        }

            #expenseModal .modal-header .modal-title {
                font-weight: 700;
                font-size: 1rem;
                color: #1a3b6b;
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }

        #expenseModal .modal-body {
            padding: 1rem 1.25rem;
            background: #fff;
        }

        #expenseModal .policy-summary {
            border: 2px solid #dbe9ff;
            background: linear-gradient(180deg,#f7fbff 0%, #ffffff 100%);
            box-shadow: 0 4px 14px rgba(11,73,163,0.06);
            border-radius: .5rem;
            padding: .65rem .75rem;
            display: flex;
            gap: .8rem;
            align-items: center;
            flex-wrap: wrap;
        }

            #expenseModal .policy-summary .policy-mode {
                background: rgba(13,110,253,0.08);
                border: 1px solid rgba(13,110,253,0.12);
                color: #083e8a;
                padding: .28rem .6rem;
                border-radius: .35rem;
                font-weight: 700;
                display: inline-flex;
                align-items: center;
                gap: .4rem;
            }

            #expenseModal .policy-summary .chip {
                font-size: .9rem;
                padding: .25rem .5rem;
                border-radius: .35rem;
                background: rgba(13,110,253,0.06);
                color: #0d6efd;
                font-weight: 600;
            }

        #divAutoDAInfo .card-like {
            border-radius: .35rem;
            padding: .45rem;
            border: 1px solid #eef6ff;
            background: #ffffff;
        }

        #expenseModal .form-control {
            height: calc(1.9rem + .6rem);
            padding: .35rem .6rem;
        }

        #expenseModal .card.totals-card {
            border-radius: .4rem;
            box-shadow: none;
            border: 1px solid #eef3ff;
        }

            #expenseModal .card.totals-card .small {
                color: #6b7280;
            }

        #expenseModal table.table {
            margin-bottom: 0;
            border-collapse: separate;
            border-spacing: 0;
        }

            #expenseModal table.table thead th {
                background: #fbfdff;
                font-weight: 700;
                font-size: .9rem;
                color: #1b2b45;
                border-bottom: 2px solid #e6eefc;
            }

            #expenseModal table.table tbody tr td {
                vertical-align: middle;
                font-size: .92rem;
            }

            #expenseModal table.table tbody tr:nth-child(odd) {
                background: #ffffff;
            }

            #expenseModal table.table tbody tr:nth-child(even) {
                background: #fbfcff;
            }

        .expense-type-badge {
            font-weight: 600;
            font-size: .78rem;
            padding: .25rem .45rem;
            border-radius: .35rem;
        }

        .text-truncate-ellipsis {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            display: block;
            max-width: 24rem;
        }

        #expenseModal .btn-sm {
            padding: .3rem .55rem;
            border-radius: .35rem;
        }

        #expenseModal .form-control-file {
            font-size: .85rem;
        }

        #expenseDebugLog {
            font-family: Consolas, monospace;
            font-size: 12px;
            color: #333;
            background: #fbfbfb;
            border-radius: .3rem;
        }

        #expenseModal #divAutoDAInfo {
            display: block;
        }

            #expenseModal #divAutoDAInfo .auto-da-card {
                background: linear-gradient(90deg,#fff8e6,#fffef9);
                border: 1px solid #ffe9a8;
                padding: .6rem .8rem;
                border-radius: .45rem;
                box-shadow: 0 5px 18px rgba(255,193,7,0.06);
                min-width: 220px;
                font-weight: 700;
            }

        #expenseModal #spAutoDAAmount {
            color: #b45f00;
            font-size: 1.05rem;
        }

        #expenseModal #spAutoDAType {
            font-size: .8rem;
            color: #6b4d00;
            font-weight: 600;
        }

        #expenseModal .form-control, #expenseModal .form-control-sm {
            padding: .28rem .46rem;
            font-size: .88rem;
            height: calc(1.7rem + .6rem);
            border-radius: .33rem;
        }

        #expenseModal input[type="date"] {
            padding: .2rem .4rem;
        }

        #expenseModal .form-control-file {
            font-size: .83rem;
            padding: .18rem 0;
        }

        #expenseModal table.table thead th {
            text-align: center;
            vertical-align: middle;
            font-weight: 700;
            font-size: .9rem;
            background: #f8fbff;
            border-bottom: 1px solid #e9f1ff;
            padding: .5rem .6rem;
        }

        #expenseModal table.table tbody td {
            padding: .42rem .55rem;
            font-size: .88rem;
            vertical-align: middle;
        }

        #expenseModal table.table tbody tr {
            transition: background .12s;
        }

            #expenseModal table.table tbody tr:hover {
                background: #f4f7ff;
            }

        #expenseModal table.table tbody td:nth-child(4) {
            text-align: right;
            font-weight: 700;
        }

        @media (max-width: 576px) {
            #expenseModal table.table thead th {
                font-size: .82rem;
                padding: .32rem .35rem;
            }

            #expenseModal table.table tbody td {
                font-size: .82rem;
                padding: .28rem .35rem;
            }

            .text-truncate-ellipsis {
                max-width: 12rem;
            }
        }

        .row-rejected {
            border-left: 3px solid #d9534f;
            background-color: #fffaf9;
        }

        .row-locked-nofade {
            background-color: #fafafa;
        }

        .btn-light .fa {
            font-size: 14px;
        }

        .status-icon {
            display: inline-block;
            width: 20px;
            height: 20px;
            line-height: 20px;
            text-align: center;
            font-weight: 700;
            font-size: 14px;
            border-radius: 50%;
            margin-left: 8px;
            vertical-align: middle;
            cursor: default;
            user-select: none;
        }

            .status-icon.present {
                background: #e6f6ea;
                color: #118a32;
            }

            .status-icon.absent {
                background: #fdecea;
                color: #c92121;
            }

            .status-icon.leave {
                background: #fff7e6;
                color: #b86a00;
            }

            .status-icon.wfh {
                background: #e9f3ff;
                color: #0b63c9;
            }

            .status-icon.unknown {
                background: #f1f1f1;
                color: #666;
            }

        #divKeyValueHeader {
            position: sticky;
            top: 0;
            z-index: 1050;
            background: #ffffff;
            padding: 1rem;
            border: 1px solid #d0d0d0;
            border-radius: 6px;
            margin-bottom: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }

            #divKeyValueHeader .kv-row {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
                gap: 1rem;
                border-top: 1px solid #e4e4e4;
                padding-top: .8rem;
            }

            #divKeyValueHeader .kv-item {
                padding: .4rem .8rem;
                border: 1px solid #e0e0e0;
                border-radius: 4px;
                background: #fafafa;
                position: relative;
            }

                #divKeyValueHeader .kv-item:not(:last-child) {
                    border-right: 2px solid #d7d7d7;
                }

            #divKeyValueHeader .kv-key {
                font-size: .75rem;
                color: #6d6d6d;
                font-weight: 500;
                text-transform: uppercase;
                letter-spacing: .5px;
                margin-bottom: 2px;
            }

            #divKeyValueHeader .kv-value {
                font-size: .95rem;
                font-weight: 700;
                color: #004085;
                background: #eef5ff;
                padding: 2px 5px;
                border-radius: 4px;
            }

        @media (min-width: 768px) {
            .col-md-2-4 {
                flex: 0 0 20%;
                max-width: 20%;
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
                                        <label class="label-align">Select Year <span class="text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Year" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Select Month <span class="text-danger">*</span></label>
                                        <asp:DropDownList ID="DDL_Month" CssClass="form-control form-control-sm rounded" runat="server" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-3 align-self-end">
                                    <asp:Button ID="btn_fetch" runat="server" CssClass="btn btn-primary btn-sm mb-3" Text="Search" OnClick="btn_fetch_Click" />
                                </div>

                                <div id="Actionable" runat="server" visible="false" class="col-md-3 text-right align-self-end">
                                    <button type="button" id="btnViewPolicyAgain" class="btn btn-info btn-sm mb-3" onclick="$('#policyModal').modal('show');"><i class="fa fa-info-circle"></i>View Policy</button>
                                    <%-- Deprecated: Save as Draft removed — application now auto-saves edits immediately.
                                         Draft is a workflow status only, maintained until Submit Claim.
                                    <asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm mb-3" Text="Save as Draft" OnClick="btn_save_draft_Click" />
                                    --%>
                                    <asp:Button ID="btn_submit" runat="server" CssClass="btn btn-success btn-sm mb-3" Text="Submit for Approval" OnClick="btn_submit_Click" OnClientClick="return showSubmitModal();" />
                                </div>
                            </div>

                            <div class="ln_solid"></div>

                            <div class="row">
                                <div class="col-md-6">
                                    <asp:Label ID="lbl_msg" runat="server" Text="" />
                                </div>
                            </div>

                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body" id="MyPopupBody"></div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12" id="Activities" runat="server" visible="false">
                    <div class="x_panel">
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive grid-container">
                                        <asp:GridView ID="gvActivities" runat="server" AutoGenerateColumns="false"
                                            CssClass="table table-bordered table-finance" HeaderStyle-CssClass="grid-header" FooterStyle-CssClass="grid-footer"
                                            DataKeyNames="ActivityId" OnRowCommand="gvActivities_RowCommand" OnRowEditing="gvActivities_RowEditing"
                                            OnRowCancelingEdit="gvActivities_RowCancelingEdit" OnRowUpdating="gvActivities_RowUpdating"
                                            OnRowDataBound="gvActivities_RowDataBound" ShowFooter="true">
                                            <Columns>

                                                <asp:TemplateField HeaderText="Date & Day (Status)" ItemStyle-Width="10%">
                                                    <ItemTemplate>
                                                        <span class="activity-date"><%# Eval("ActivityDate", "{0:dd-MM-yyyy}") %></span><br />
                                                        <small style="font-weight: bold;" class="text-muted day-text"><%# Eval("Day") %></small>
                                                        <span class="status-icon" data-status='<%# Eval("Attendance_Status") %>' title='<%# Eval("Attendance_Status") %>' role="img" aria-label='<%# Eval("Attendance_Status") %>'></span>
                                                    </ItemTemplate>
                                                    <FooterTemplate><strong>Total :</strong>&nbsp;</FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Distance (KM)">
                                                    <ItemStyle HorizontalAlign="Left" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Left" />
                                                    <ItemTemplate>
                                                        <%# Eval("TotalDistanceKM", "{0:N2}") %> <small>km</small><br />
                                                        <small class="text-muted">
                                                            <asp:Label ID="lblTolRawMin" runat="server" Text='<%# Eval("MinAllowedKM") %>' Visible="false" />
                                                            <asp:Label ID="lblTolRawMax" runat="server" Text='<%# Eval("MaxAllowedKM") %>' Visible="false" />
                                                        </small>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="txtKM" runat="server" Text='<%# Bind("TotalDistanceKM") %>' type="number" step="0.01"
                                                            CssClass="form-control form-control-sm km-input" Width="100%" />
                                                        <small class="form-text text-muted">
                                                            <asp:Label ID="lblTolLower_Edit" runat="server" Text="-" CssClass="tol-lower" />
                                                            &nbsp; - &nbsp;
                                                            <asp:Label ID="lblTolUpper_Edit" runat="server" Text="-" CssClass="tol-upper" /><br />
                                                            <span class="text-muted">(Current: <%# Eval("TotalDistanceKM") %> ; Min: <%# Eval("MinAllowedKM") ?? "—" %> ; Max: <%# Eval("MaxAllowedKM") ?? "—" %> )</span>
                                                        </small>
                                                        <span class="text-danger km-error" style="display: none;">Value must be within allowed range.</span>
                                                    </EditItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterTotalKM" runat="server" CssClass="font-weight-bold" Text="0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="TA (₹)" SortExpression="CalculatedClaimAmount">
                                                    <ItemStyle HorizontalAlign="Right" Width="8%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblCalculatedClaim" runat="server" Text='<%# Eval("CalculatedClaimAmount", "₹ {0:N2}") %>' CssClass="currency" /><br />
                                                        <small class="text-muted" runat="server" visible="true">(<%# Eval("RatePerKm", "{0:N2}") %>/ km)</small>
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterCalculated" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Status" ItemStyle-Width="5%" ItemStyle-HorizontalAlign="Center">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblClaimStatus" runat="server" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="5%">
                                                    <ItemStyle HorizontalAlign="Center" />
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lnkExpenses" runat="server"
                                                            CommandName="OpenExpenses" 
                                                            CommandArgument='<%# Eval("ActivityId") + "|" + Eval("ActivityDate") + "|" + Eval("Planned_Beat") + "|" + Eval("Order_Distributor_Name") + "|" + Eval("TotalDistanceKM") + "|" + HttpUtility.HtmlEncode(Eval("DetailRemarks").ToString()) %>' 
                                                            CssClass="btn btn-sm btn-primary" ToolTip="View / Add Expenses">
                                                            <i class="fa fa-briefcase"></i>
                                                        </asp:LinkButton>
                                                    </ItemTemplate>

                                                    <EditItemTemplate>
                                                        <asp:LinkButton ID="lnkUpdate" runat="server"
                                                            CommandName="UpdateRow" CommandArgument='<%# Eval("ActivityId") %>' OnClientClick="return validateGridBeforeSubmit();" CssClass="btn btn-sm btn-success" ToolTip="Save Changes">
                                                            <i class="fa fa-check"></i>
                                                        </asp:LinkButton>

                                                        <asp:LinkButton ID="lnkCancel" runat="server"
                                                            CommandName="Cancel" CssClass="btn btn-sm btn-outline-danger" ToolTip="Cancel Edit">
                                                            <i class="fa fa-times"></i>
                                                        </asp:LinkButton>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="DA (₹)">
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

                                                <asp:TemplateField HeaderText="Travel (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblTravel" runat="server" Text='<%# Eval("TravelAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterTravel" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Stationary (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblStationary" runat="server" Text='<%# Eval("StationaryAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterStationary" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Total (₹)">
                                                    <ItemStyle HorizontalAlign="Right" Width="10%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblExpenseTotal" runat="server" Text='<%# Eval("ExpenseTotal", "₹ {0:N2}") %>' CssClass="currency" />
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterExpenseTotal" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12" id="Summary" runat="server" visible="true">

                    <div class="x_panel">

                        <div class="x_title">

                            <h2>Summary by status</h2>

                            <ul class="nav navbar-right panel_toolbox">

                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>

                            </ul>

                            <div class="clearfix"></div>

                        </div>



                        <div class="x_content">

                            <div class="row">

                                <div class="col-md-12 col-sm-12">

                                    <div class="mb-3">

                                        <div class="table-responsive">
                                            <table class="table table-sm table-bordered" style="max-width: 1200px;">
                                                <thead class="thead-light">
                                                    <tr>
                                                        <th>Status</th>
                                                        <th class="text-right">Total KM</th>
                                                        <th class="text-right">TA (₹)</th>
                                                        <th class="text-right">DA (₹)</th>
                                                        <th class="text-right">Hotel (₹)</th>
                                                        <th class="text-right">Travel (₹)</th>
                                                        <th class="text-right">Stationary (₹)</th>
                                                        <th class="text-right">Expense (₹)</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    <tr>
                                                        <td><span class="badge badge-warning">Draft</span></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_KM" runat="server" Text="0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_TA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_DA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_Hotel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_Travel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_Stationary" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblDraft_Expense" runat="server" Text="₹ 0.00" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td><span class="badge badge-info">Submitted</span></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_KM" runat="server" Text="0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_TA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_DA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_Hotel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_Travel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_Stationary" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblSubmitted_Expense" runat="server" Text="₹ 0.00" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td><span class="badge badge-success">Approved</span></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_KM" runat="server" Text="0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_TA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_DA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_Hotel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_Travel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_Stationary" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblApproved_Expense" runat="server" Text="₹ 0.00" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td><span class="badge badge-danger">Editable</span> <small>(reopened)</small></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_KM" runat="server" Text="0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_TA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_DA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_Hotel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_Travel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_Stationary" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblEditable_Expense" runat="server" Text="₹ 0.00" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td><span class="badge badge-secondary">Other</span></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_KM" runat="server" Text="0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_TA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_DA" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_Hotel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_Travel" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_Stationary" runat="server" Text="₹ 0.00" /></td>
                                                        <td class="text-right">
                                                            <asp:Label ID="lblOther_Expense" runat="server" Text="₹ 0.00" /></td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </div>

                                    </div>

                                </div>

                            </div>

                        </div>

                    </div>

                </div>
            </div>
        </div>

        <div class="modal fade" id="editKmModal" tabindex="-1" role="dialog" aria-labelledby="editKmModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="editKmModalLabel">Modify Travelled Distance (in KM) for
                            <asp:Label ID="lbl_kmmodal_date" runat="server" Text="NA" Font-Bold="true" ForeColor="#009933"></asp:Label></h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:HiddenField ID="hdnActivityId" runat="server" />
                        <asp:HiddenField ID="hdnOriginalKm" runat="server" />
                        <asp:HiddenField ID="HF_KmSaveWorkflow" runat="server" Value="Manual" />
                        <div class="row">
                            <div class="col-md-6">
                                <div class="input-group">

                                    <asp:TextBox ID="txtModalKM"
                                        runat="server"
                                        CssClass="form-control"
                                        Width="50px"
                                        step="0.01" />
                                    <label for="txtModalKM">
                                        Distance Travelled (in KM)
                                    </label>
                                    <span class="text-danger">*</span>
                                    <br />
                                    <span class="input-group-btn">
                                        <button type="button"
                                            class="btn btn-info"
                                            onclick="openRouteCalculator();">

                                            <i class="fa fa-map-marker"></i>
                                            Calculate Route

                                        </button>
                                    </span>

                                </div>

                                <div class="form-group">
                                    <small>Allowed range: <span id="spMinKm">—</span> to <span id="spMaxKm">—</span> km</small>
                                </div>

                                <asp:Label ID="lblModalCurrentKm" runat="server" CssClass="text-muted"></asp:Label>

                                <div id="routeSummaryPanel"
                                    class="alert alert-info mt-2"
                                    style="display: none;">

                                    <div>
                                        <strong>Origin:</strong>
                                        <span id="spRouteOrigin"></span>
                                    </div>

                                    <div>
                                        <strong>Destination:</strong>
                                        <span id="spRouteDestination"></span>
                                    </div>
                                    <div>
                                        <strong>Distance:</strong>
                                        <span id="spRouteDistance"></span>KM
                                    </div>
                                    <div>
                                        <strong>Stops:</strong>
                                        <span id="spWaypointCount"></span>
                                    </div>

                                </div>
                            </div>

                            <div class="col-md-6">
                                <div class="form-group">
                                    <label>Change for Reason (Mandatory)</label>
                                    <span class="text-danger">*</span>
                                    <asp:TextBox ID="txtKmReason" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" />
                                </div>
                            </div>
                        </div>

                        <hr />

                        <h6 style="color: darkblue;">KM Change History</h6>
                        <div class="table-responsive">
                            <asp:GridView ID="gvKmHistory" runat="server" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" CssClass="table table-sm table-bordered table-finance"
                                EmptyDataText="No history available">
                                <Columns>
                                    <asp:BoundField DataField="HistoryId" HeaderText="ID" ItemStyle-Width="5%" />
                                    <asp:BoundField DataField="ModifiedDateTime" HeaderText="Changed On" DataFormatString="{0:dd-MM-yyyy HH:mm:ss tt}" />
                                    <asp:BoundField DataField="ModifiedBy" HeaderText="By" />
                                    <asp:BoundField DataField="OldKM" HeaderText="Old KM" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField DataField="NewKM" HeaderText="New KM" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                                    <asp:BoundField DataField="Reason" HeaderText="Reason" />
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>

                    <div class="modal-footer">
                        <asp:Button ID="btnSaveKm" runat="server" CssClass="btn btn-success btn-sm" Text="Save changes" OnClick="btnSaveKm_Click" />
                        <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="routeDistanceModal" class="modal fade" tabindex="-1">
            <div class="modal-dialog" style="width: 95%; max-width: 1200px;">
                <div class="modal-content">
                    <div class="modal-header">
                        <h4 class="modal-title">Route Distance Calculator
                        </h4>
                        <button type="button"
                            class="close"
                            data-dismiss="modal">
                            &times;

                        </button>
                    </div>

                    <div class="modal-body">
                        <div class="row">

                            <div class="col-md-4">

                                <div class="form-group">

                                    <label>Search Location</label>

                                    <input type="text"
                                        id="txtPlaceSearch"
                                        class="form-control" />

                                </div>

                                <button type="button"
                                    id="btnSearchLocation"
                                    class="btn btn-primary btn-sm">
                                    Search

                                </button>

                                <hr />

                                <div id="searchResults"
                                    class="list-group"
                                    style="max-height: 180px; overflow-y: auto;">
                                </div>

                                <hr />

                                <div class="form-group">

                                    <label>Travel Mode</label>

                                    <select id="ddlTravelMode"
                                        class="form-control">

                                        <option value="driving">Driving</option>
                                        <option value="cycling">Bike</option>
                                        <option value="foot">Walking</option>

                                    </select>

                                </div>

                                <hr />

                                <label>Selected Stops</label>

                                <div id="waypointList"
                                    class="list-group"
                                    style="max-height: 250px; overflow-y: auto;">
                                </div>

                                <hr />

                                <div class="alert alert-success">

                                    <strong>Total Distance:</strong>

                                    <span id="lblDistance">0.00</span>

                                    KM

                                </div>

                            </div>

                            <div class="col-md-8">
                                <div id="routeMap"
                                    style="height: 550px;">
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="modal-footer">
                        <button type="button"
                            id="btnApplyRoute"
                            class="btn btn-success">
                            Apply Route
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade" id="submitConfirmModal" tabindex="-1" role="dialog" aria-labelledby="submitConfirmModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="submitConfirmModalLabel">Confirm Submit for Approval</h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close" onclick="enableButtonsAfterModalClose();">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        Are you sure you want to submit all listed activities for approval? Once submitted, they will be locked for editing.
                    </div>
                    <div class="modal-footer">
                        <button type="button" id="submitModalCancel" class="btn btn-secondary btn-sm" data-dismiss="modal" onclick="enableButtonsAfterModalClose();">Cancel</button>
                        <button type="button" id="submitModalConfirm" class="btn btn-success btn-sm" onclick="performSubmitFromModal();">Yes, Submit</button>
                    </div>
                </div>
            </div>
        </div>


        <div class="modal fade bs-example-modal-lg" tabindex="-1" role="dialog" aria-hidden="true" id="expenseModal">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">DA / Hotel / Travel Expenses for
                    <asp:Label ID="lbl_traveldate" runat="server" Text="NA" Font-Bold="true" ForeColor="#009933"></asp:Label>
                        </h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close" onclick="$('#expenseModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>

                    <div class="modal-body">
                        <asp:Panel runat="server" ID="pnlExpense">
                            <asp:HiddenField ID="HF_ActivityId" runat="server" />
                            <asp:HiddenField ID="HF_ActivityDate" runat="server" />
                            <asp:HiddenField ID="HF_Policy_RegionId" runat="server" />
                            <asp:HiddenField ID="HF_Policy_BranchId" runat="server" />
                            <asp:HiddenField ID="HF_Policy_GradeId" runat="server" />
                            <asp:HiddenField ID="HF_Policy_DesignationId" runat="server" />
                            <asp:HiddenField ID="HF_ResolvedHotelCap" runat="server" />
                            <asp:HiddenField ID="HF_ResolvedDA" runat="server" />
                            <asp:HiddenField ID="HF_ResolvedMaxKM" runat="server" />
                            <asp:HiddenField ID="HF_ResolvedRatePerKm" runat="server" />
                            <asp:HiddenField ID="HF_ResolvedPolicyMode" runat="server" />
                            <asp:HiddenField ID="HF_OutstationDA" runat="server" />
                            <asp:HiddenField ID="HF_OwnArrangementDA" runat="server" />

                            <asp:HiddenField ID="HF_RouteActivityId" runat="server" />
                            <asp:HiddenField ID="HF_RouteOrigin" runat="server" />
                            <asp:HiddenField ID="HF_RouteDestination" runat="server" />
                            <asp:HiddenField ID="HF_RouteWaypoints" runat="server" />
                            <asp:HiddenField ID="HF_RouteDistance" runat="server" />
                            <asp:HiddenField ID="HF_RouteTravelMode" runat="server" />
                            <div id="divKeyValueHeader">
                                <div class="kv-row">
                                    <div class="kv-item">
                                        <div class="kv-key">Beat Travelled</div>
                                        <div id="spBeatTravelled" class="kv-value">
                                            <asp:Label ID="lbl_BeatTravelled" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="kv-item">
                                        <div class="kv-key">Distributor Name</div>
                                        <div id="spDistributorName" class="kv-value">
                                            <asp:Label ID="lbl_DistributorName" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="kv-item">
                                        <div class="kv-key">Travel Distance</div>
                                        <div id="spHQTravelDistance" class="kv-value">
                                            <asp:Label ID="lbl_HQTravelDistance" runat="server" Text="0.00"></asp:Label>&nbsp;Kms&nbsp;&nbsp;
                                    <asp:Button ID="btnOpenEditKmFromExpense" runat="server" CssClass="btn btn-sm btn-primary py-0" Text="Modify Km." OnClick="btnOpenEditKmFromExpense_Click" />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="p-3 mb-3 bg-light border rounded">
                                <div class="row align-items-end">
                                    <div class="col-md-4 form-group mb-0">
                                        <label class="font-weight-bold text-primary mb-1">1. Select Policy Mode</label>
                                        <select id="SEL_PolicyMode" class="form-control form-control-sm border-primary" style="font-weight: 600;">
                                            <option value="HQ">HQ</option>
                                            <option value="Ex-HQ">Ex-HQ</option>
                                            <option value="Outstation">Outstation</option>
                                            <option value="OwnArrangement">Own Arrangement</option>
                                        </select>
                                    </div>

                                    <div class="col-md-8">
                                        <label class="mb-1 text-muted font-weight-bold" style="font-size: 0.85rem;">Policy Limits</label>
                                        <div id="policyValues" class="d-flex flex-wrap align-items-center" style="gap: 10px; font-size: 0.85rem;">
                                            <span class="badge badge-light border p-2 text-dark">DA: <strong id="spDAValue">—</strong></span>
                                            <span class="badge badge-light border p-2 text-dark">Hotel Cap: <strong id="spHotelCap">—</strong></span>
                                            <span class="badge badge-info p-2" id="spAllowedTypes">Allowed: —</span>
                                        </div>
                                    </div>
                                </div>
                                <div class="row mt-2">
                                    <div class="col-12">
                                        <small class="text-muted font-weight-bold" id="spPolicyNote"></small>
                                    </div>
                                </div>
                            </div>

                            <div id="divAutoDAInfo" style="display: none; margin-bottom: 15px;">
                                <div class="auto-da-card border-warning">
                                    <div>Auto DA Amount: <span id="spAutoDAAmount">—</span></div>
                                    <div id="spAutoDAType" class="small">—</div>
                                </div>
                            </div>

                            <div class="row mb-3">
                                <div class="col-md-5 form-group">
                                    <label class="font-weight-bold text-primary">2. Select Expense Type</label>
                                    <asp:DropDownList ID="DDL_ExpenseType" runat="server" ClientIDMode="Static" CssClass="form-control form-control-sm border-primary" Style="font-weight: 600;">
                                        <asp:ListItem Value="DA">DA</asp:ListItem>
                                        <asp:ListItem Value="Hotel">Hotel</asp:ListItem>
                                        <asp:ListItem Value="Travel">Travel</asp:ListItem>
                                        <asp:ListItem Value="Stationary">Stationary</asp:ListItem>
                                    </asp:DropDownList>
                                    <small class="text-danger font-weight-bold mt-1 d-block" id="txtExpenseTypeHint" style="display: none;">
                                        <i class="fa fa-info-circle"></i>Restricted by selected Policy Mode.
                                    </small>
                                </div>
                            </div>
                            <div id="divDASection" runat="server" class="row">
                                <div class="col-md-4 form-group">
                                    <label>DA Amount</label>
                                    <asp:TextBox ID="TXT_DAAmount" runat="server" CssClass="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                                </div>
                                <div class="col-md-4 form-group">
                                    <label>From Place</label>
                                    <asp:TextBox ID="TXT_DAFromPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="200" TextMode="MultiLine" />
                                </div>
                                <div class="col-md-4 form-group">
                                    <label>To Place</label>
                                    <asp:TextBox ID="TXT_DAToPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="200" TextMode="MultiLine" />
                                </div>
                                <div class="col-md-8 form-group">
                                    <label>Remarks</label>
                                    <asp:TextBox ID="TXT_TravelDescription" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                </div>
                            </div>

                            <div id="divHotelSection" runat="server" class="row">
                                <div class="col-md-3 form-group">
                                    <label>Stay From Date</label>
                                    <asp:TextBox ID="TXT_HotelFromDate" runat="server" TextMode="Date" CssClass="form-control form-control-sm rounded" />
                                </div>
                                <div class="col-md-3 form-group">
                                    <label>Stay To Date</label>
                                    <asp:TextBox ID="TXT_HotelToDate" runat="server" TextMode="Date" Enabled="false" CssClass="form-control form-control-sm rounded" />
                                </div>
                                <div class="col-md-3 form-group">
                                    <label>Bill/ Invoice No</label>
                                    <asp:TextBox ID="TXT_HotelBillNo" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-3 form-group">
                                    <label>Hotel Amount</label>
                                    <asp:TextBox ID="TXT_HotelAmount" runat="server" CssClass="form-control form-control-sm" type="number" step="0.01"></asp:TextBox>
                                </div>
                                <div class="col-md-12 form-group">
                                    <label>Purpose of Stay</label>
                                    <asp:TextBox ID="TXT_HotelPurpose" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                </div>
                            </div>

                            <div id="divTravelSection" runat="server" class="row">
                                <div class="col-md-2 form-group">
                                    <label>Travel Type</label>
                                    <asp:DropDownList ID="DDL_TravelType" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Value="Train">Train</asp:ListItem>
                                        <asp:ListItem Value="Bus">Bus</asp:ListItem>
                                        <asp:ListItem Value="Other">Other</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="col-md-2 form-group">
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
                                <div class="col-md-2 form-group">
                                    <label>Amount</label>
                                    <asp:TextBox ID="TXT_TravelAmount" runat="server" CssClass="form-control form-control-sm" type="number" step="0.01" />
                                </div>
                                <div class="col-md-12 form-group">
                                    <label>Purpose of Travel</label>
                                    <asp:TextBox ID="TXT_TravelPurpose" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                </div>
                                <div class="col-md-4 form-group d-flex align-items-center">
                                    <asp:CheckBox ID="CHK_TravelInvoice" runat="server" CssClass="mr-2" />
                                    <label class="mb-0">With Invoice</label>
                                </div>
                                <div class="col-md-4 form-group" id="divTravelInvoiceNo" runat="server">
                                    <label>Invoice No</label>
                                    <asp:TextBox ID="TXT_TravelInvoiceNo" runat="server" CssClass="form-control form-control-sm" />
                                </div>
                            </div>

                            <div id="divStationary" style="display: none;" class="expense-section">
                                <div class="row">
                                    <div class="col-md-6 form-group">
                                        <label>Amount (₹) <span class="text-danger">*</span></label>
                                        <asp:TextBox ID="TXT_StationaryAmount" runat="server" CssClass="form-control numeric-only"></asp:TextBox>
                                    </div>
                                    <div class="col-md-6 form-group">
                                        <label>Bill / Invoice No</label>
                                        <asp:TextBox ID="TXT_StationaryBillNo" runat="server" CssClass="form-control"></asp:TextBox>
                                    </div>
                                    <div class="col-md-12 form-group">
                                        <label>Description / Items Purchased <span class="text-danger">*</span></label>
                                        <asp:TextBox ID="TXT_StationaryPurpose" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-12 form-group" id="divFUContainer" runat="server">
                                    <label class="d-block">Attach Receipt</label>
                                    <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" accept=".jpg,.jpeg,.png,.pdf" />
                                    <small class="form-text text-muted">JPG, PNG, PDF. Max 5MB.</small>
                                </div>
                            </div>

                            <br />
                            <div class="card shadow-sm mb-4 border-0">
                                <div class="card-body p-3">
                                    <div class="row text-center g-3">

                                        <style>
                                            .metric-box {
                                                border-right: 1px solid #e9ecef;
                                                transition: background 0.2s;
                                            }

                                                .metric-box:last-child {
                                                    border-right: none;
                                                }

                                                .metric-box:hover {
                                                    background-color: #f8f9fa;
                                                    border-radius: 4px;
                                                }
                                        </style>

                                        <div class="col-6 col-md-2-4 metric-box">
                                            <div class="small text-muted text-uppercase font-weight-bold">DA Total</div>
                                            <div id="dvDA" runat="server" class="h5 mb-0 font-weight-bold text-primary">
                                                <asp:Label ID="lblTotalDA" runat="server" Text="0.00" />
                                            </div>
                                        </div>

                                        <div class="col-6 col-md-2-4 metric-box">
                                            <div class="small text-muted text-uppercase font-weight-bold">Hotel Total</div>
                                            <div id="dvHotel" runat="server" class="h5 mb-0 font-weight-bold text-primary">
                                                <asp:Label ID="lblTotalHotel" runat="server" Text="0.00" />
                                            </div>
                                        </div>

                                        <div class="col-6 col-md-2-4 metric-box">
                                            <div class="small text-muted text-uppercase font-weight-bold">Travel Total</div>
                                            <div id="dvOther" runat="server" class="h5 mb-0 font-weight-bold text-primary">
                                                <asp:Label ID="lblTotalOther" runat="server" Text="0.00" />
                                            </div>
                                        </div>

                                        <div class="col-6 col-md-2-4 metric-box">
                                            <div class="small text-muted text-uppercase font-weight-bold">Stationary</div>
                                            <div class="h5 mb-0 font-weight-bold text-dark">
                                                <asp:Label ID="lblTotalStationary" runat="server" Text="0.00" />
                                            </div>
                                        </div>

                                        <div class="col-6 col-md-2-4 metric-box">
                                            <div class="small text-muted text-uppercase font-weight-bold">Day Total</div>
                                            <div id="dvOverall" runat="server" class="h4 mb-0 font-weight-bold text-success">
                                                <asp:Label ID="lblOverallTotal" runat="server" Text="0.00" />
                                            </div>
                                        </div>

                                    </div>
                                </div>
                            </div>

                            <hr />
                            <h6 class="font-weight-bold">Existing Day Expenses</h6>

                            <asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand" OnItemDataBound="rptExpenses_ItemDataBound">
                                <HeaderTemplate>
                                    <div class="table-responsive">
                                        <table class="table table-sm table-bordered">
                                            <thead>
                                                <tr>
                                                    <th>Type</th>
                                                    <th>Date</th>
                                                    <th>Details</th>
                                                    <th>Amount</th>
                                                    <th>Bill No</th>
                                                    <th>Uploads</th>
                                                    <th style="width: 110px;">Actions</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td>
                                            <span class="expense-type-badge badge badge-light">
                                                <%# Eval("ExpenseType") %>
                                            </span>
                                        </td>
                                        <td>
                                            <%# (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Travel")
                                        ? (Eval("TravelDate") == DBNull.Value ? "—" : Eval("TravelDate", "{0:dd-MM-yyyy}"))
                                        : (Eval("ExpenseDate") == DBNull.Value ? "—" : Eval("ExpenseDate", "{0:dd-MM-yyyy}")) %>
                                        </td>
                                        <td style="max-width: 280px;">
                                            <span class="text-truncate-ellipsis" title='<%# HttpUtility.HtmlEncode(
                                     (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Travel")
                                        ? String.Format("{0} {1} -> {2} {3}", Eval("TravelType") ?? "", Eval("FromPlace") ?? "", Eval("ToPlace") ?? "", (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                     : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Hotel")
                                        ? String.Format("{0} - {1} {2}", (Eval("FromDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("FromDate"))), (Eval("ToDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("ToDate"))), (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                     : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "DA")
                                        ? String.Format("{0} -> {1} {2}", Eval("FromPlace") ?? "—", Eval("ToPlace") ?? "—", (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                     : (Eval("Purpose") ?? "—")
                                        ) %>'>
                                                <%# HttpUtility.HtmlEncode(
                                        (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Travel")
                                        ? String.Format("{0} {1} -> {2} {3}", Eval("TravelType") ?? "", Eval("FromPlace") ?? "", Eval("ToPlace") ?? "", (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                        : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Hotel")
                                          ? String.Format("{0} - {1} {2}", (Eval("FromDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("FromDate"))), (Eval("ToDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("ToDate"))), (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                          : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "DA")
                                            ? String.Format("{0} -> {1} {2}", Eval("FromPlace") ?? "—", Eval("ToPlace") ?? "—", (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : ""))
                                            : (Eval("Purpose") ?? "—")
                                        ) %>
                                            </span>
                                        </td>
                                        <td class="text-right font-weight-bold"><%# Eval("Amount", "{0:N2}") %></td>
                                        <td><%# Eval("BillNo") ?? "—" %></td>
                                        <td>
                                            <%# Eval("AttachmentFileName") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("AttachmentFileName")))
                                      ? String.Format("<a href=\"DownloadExpense.ashx?expenseId={0}\" class=\"btn btn-outline-secondary btn-sm\" title=\"Download\"><i class=\"fa fa-download\"></i></a>", Eval("ExpenseId"))
                                      : "—" %>
                                        </td>
                                        <td class="text-center">
                                            <asp:LinkButton ID="lnkDeleteExpense" runat="server" CssClass="btn btn-outline-danger btn-sm" CommandName="DeleteExpense" CommandArgument='<%# Eval("ExpenseId") + "|" + (Eval("AttachmentPath") ?? "") + "|" + (Eval("ActivityId") ?? "") %>' OnClientClick="return confirm('Delete this expense? This action cannot be undone.');" ToolTip="Delete"><i class="fa fa-trash"></i></asp:LinkButton>
                                            <asp:LinkButton ID="lnkDeleteExpenseDisabled" runat="server" CssClass="btn btn-outline-secondary btn-sm disabled" Visible="false" OnClientClick="return false;" ToolTip="Cannot delete: submitted/auto-applied"><i class="fa fa-lock"></i></asp:LinkButton>
                                            <asp:Literal ID="litPolicyBadges" runat="server"></asp:Literal>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </tbody>
                            </table>
                            </div>
                                </FooterTemplate>
                            </asp:Repeater>

                            <asp:Label ID="LBL_ExpenseMsg" runat="server" CssClass="text-danger mt-2 d-block"></asp:Label>
                        </asp:Panel>
                    </div>

                    <div class="modal-footer">
                        <asp:Button ID="BTN_SaveExpense" runat="server" CssClass="btn btn-primary btn-sm" ClientIDMode="Static" Text="Save Expense" OnClick="BTN_SaveExpense_Click" />
                        <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal" onclick="$('#expenseModal').modal('hide');">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="pageLoader" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.8); z-index: 9999; text-align: center; padding-top: 20%;">
            <i class="fa fa-spinner fa-spin fa-4x text-success"></i>
            <br />
            <br />
            <span class="text-success h4 font-weight-bold">Processing, please wait...</span>
        </div>

        <div class="modal fade" id="policyModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
                <div class="modal-content shadow-lg border-0 rounded-lg overflow-hidden">
                    <div class="modal-header bg-primary text-white py-2">
                        <h5 class="modal-title mb-0">
                            <i class="fa fa-file-alt mr-2"></i>Resolved Policy
                            <small class="text-light">(<asp:Label ID="lblPolicyEmployeeCode" runat="server" Text="—" CssClass="small"></asp:Label>)</small>
                        </h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close" onclick="$('#policyModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>
                    <div class="modal-body bg-light px-4 pt-3 pb-1">
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
                                <asp:Label ID="lblPolicySpecificity" runat="server" CssClass="d-block text-primary font-weight-bold">—</asp:Label>
                            </div>
                        </div>
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
                                <asp:Label ID="lblPolicyDesigGrade" runat="server" CssClass="d-block text-dark font-weight-bold">—</asp:Label>
                            </div>
                        </div>
                        <div class="card shadow-sm border-0 mb-3">
                            <div class="card-header bg-gradient-primary text-black py-1 px-3">
                                <strong><i class="fa fa-calculator mr-1"></i>Resolved Capping Limits</strong>
                            </div>
                            <div class="card-body bg-white p-3">
                                <div class="row text-center">
                                    <div class="col-6 col-md-3 mb-3 mb-md-0 border-md-right" id="MaxKM" runat="server" visible="false">
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
                                    <div class="col-6 col-md-3" id="HotelCap" runat="server" visible="false">
                                        <div class="small text-muted">Hotel Cap.</div>
                                        <div class="h6 font-weight-bold text-danger mb-0">
                                            <asp:Label ID="lblResolvedHotelCap" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="form-group mb-3">
                            <label class="small text-secondary mb-1">Remarks</label>
                            <asp:Label ID="lblResolvedRemarks" runat="server" CssClass="d-block text-dark bg-white border rounded p-2">—</asp:Label>
                        </div>
                        <div class="form-group" id="Toggler" runat="server" visible="false">
                            <asp:Button ID="btnToggleRawJson" runat="server" CssClass="btn btn-sm btn-outline-dark mb-2" OnClientClick="$('#policyJsonRaw').toggle(); return false;" Text="Toggle Raw JSON" />
                            <asp:TextBox ID="policyJsonRaw" runat="server" TextMode="MultiLine" Rows="6" CssClass="form-control form-control-sm font-monospace text-dark bg-light border border-secondary" Visible="false"></asp:TextBox>
                        </div>

                        <div class="card shadow-sm border-0 mb-3">
                            <div class="card-header bg-gradient-secondary text-black py-1 px-3">
                                <strong><i class="fa fa-user-check mr-1"></i>Approvers</strong>
                            </div>
                            <div class="card-body bg-white p-3">
                                <div class="row text-center">
                                    <div class="col-12 col-md-4 mb-2" id="approverL1Container" runat="server" visible="false">
                                        <div class="small text-muted">Approver - Level 1</div>
                                        <div class="h6 font-weight-bold text-dark mb-0">
                                            <asp:Label ID="lblApproverL1" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-12 col-md-4 mb-2" id="approverL2Container" runat="server" visible="false">
                                        <div class="small text-muted">Approver - Level 2</div>
                                        <div class="h6 font-weight-bold text-dark mb-0">
                                            <asp:Label ID="lblApproverL2" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-12 col-md-4 mb-2" id="approverAuditorContainer" runat="server" visible="false">
                                        <div class="small text-muted">Auditor / Final</div>
                                        <div class="h6 font-weight-bold text-dark mb-0">
                                            <asp:Label ID="lblApproverAuditor" runat="server" Text="—"></asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="modal-footer bg-light border-top">
                        <asp:LinkButton ID="lnkCopyJson" runat="server" CssClass="btn btn-sm btn-outline-primary" Visible="false" OnClientClick="copyPolicyJson(); return false;">Copy JSON</asp:LinkButton>
                        <button type="button" class="btn btn-danger btn-sm" data-dismiss="modal" onclick="$('#policyModal').modal('hide');">
                            <i class="fa fa-times mr-1"></i>Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        $(document).ready(function () {
            $('.collapse-link').on('click', function () {
                var $BOX_PANEL = $(this).closest('.x_panel');
                var $ICON = $(this).find('i');
                var $BOX_CONTENT = $BOX_PANEL.find('.x_content');

                if ($BOX_PANEL.hasClass('collapsed')) {
                    $BOX_CONTENT.slideDown(200);
                    $BOX_PANEL.removeClass('collapsed');
                    $ICON.removeClass('fa-chevron-down').addClass('fa-chevron-up');
                } else {
                    $BOX_CONTENT.slideUp(200);
                    $BOX_PANEL.addClass('collapsed');
                    $ICON.removeClass('fa-chevron-up').addClass('fa-chevron-down');
                }
            });

            initTooltipsAndIcons();
            keepSummaryPanelExpanded();
        });

        function initTooltipsAndIcons() {
            $('[data-toggle="tooltip"]').tooltip();

            var map = {
                present: { glyph: '✓', cls: 'present', label: 'Present' },
                absent: { glyph: '✖', cls: 'absent', label: 'Absent' },
                leave: { glyph: '⛱', cls: 'leave', label: 'Leave' },
                wfh: { glyph: '🏠', cls: 'wfh', label: 'WFH' }
            };

            document.querySelectorAll('.status-icon').forEach(function (el) {
                var raw = (el.dataset.status || '').trim();
                var key = raw.toLowerCase();
                if (key === 'working from home' || key === 'work from home') key = 'wfh';
                if (key === 'on leave') key = 'leave';

                var cfg = map[key];
                if (!cfg) {
                    el.textContent = '•';
                    el.classList.add('unknown');
                    el.title = raw || 'Unknown';
                    el.setAttribute('aria-label', raw || 'Unknown');
                    return;
                }

                el.textContent = cfg.glyph;
                el.classList.add(cfg.cls);
                el.title = raw || cfg.label;
                el.setAttribute('aria-label', raw || cfg.label);
            });
        }

        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initTooltipsAndIcons();
                keepSummaryPanelExpanded();
            });
        }

        function keepSummaryPanelExpanded() {
            var summaryRoot = document.getElementById('<%= Summary.ClientID %>');
            if (!summaryRoot) return;
            var panel = summaryRoot.querySelector('.x_panel');
            if (!panel) return;
            panel.classList.remove('collapsed');
            var content = panel.querySelector('.x_content');
            if (content) content.style.display = '';
            var icon = panel.querySelector('.collapse-link i');
            if (icon) {
                icon.classList.remove('fa-chevron-down');
                icon.classList.add('fa-chevron-up');
            }
        }

        function showPNotify(title, message, type) {
            if (typeof PNotify !== 'undefined') {
                new PNotify({ title: title, text: message, type: type, delay: 4000, styling: 'bootstrap3' });
            } else {
                alert(title + "\n" + message);
            }
        }
        window.showPNotifyLocal = showPNotify;
        window.notify = function (t, m, y) { showPNotify(t, m, y); };

        function resolve(id) {
            var el = document.getElementById(id);
            if (el) return el;
            return document.querySelector('[id$="_' + id + '"],[id$="' + id + '"]') || null;
        }
        function $resolve(id) { var el = resolve(id); return el ? $(el) : $(); }
        window.resolve = resolve; window.$resolve = $resolve;

        function parseNumber(v) {
            if (v == null) return NaN;
            v = String(v).replace(/,/g, '').trim();
            return v === '' ? NaN : parseFloat(v);
        }
        function isValidDateString(s) { return s ? /^\d{4}-\d{2}-\d{2}$/.test(s) : false; }
        function fmtAmount(v) {
            var n = parseNumber(v);
            return isNaN(n) ? '—' : n.toFixed(2);
        }

        var css = ".so-err { border-color: #e55353 !important; box-shadow: 0 0 0 0.15rem rgba(229,83,83,0.15) !important; } .so-err-msg { color:#e55353; font-size:12px; margin-top:4px; } .so-toast-wrap { position: fixed; top: 16px; right: 16px; z-index: 2100; } .so-toast { background: #2b2f33; color: #fff; padding: 10px 14px; margin-bottom:8px; border-radius:4px; box-shadow: 0 6px 18px rgba(0,0,0,0.15); min-width:260px; } .so-toast .so-toast-title { font-weight:600; margin-bottom:4px; } .so-toast.success { background:#1b7a3a; } .so-toast.error { background:#a81b1b; }";
        var s = document.createElement('style'); s.appendChild(document.createTextNode(css)); document.head.appendChild(s);

        function showInlineError(el, msg) {
            if (!el) return;
            var $el = el.jquery ? el : $(el);
            removeInlineError($el);
            $el.addClass('so-err');
            var msgEl = $('<div class="so-err-msg"></div>').text(msg);
            var parent = $el.closest('.form-group');
            if (parent.length) msgEl.appendTo(parent); else $el.after(msgEl);
        }
        function removeInlineError(jq) {
            var $el = jq.jquery ? jq : $(jq);
            $el.removeClass('so-err');
            var parent = $el.closest('.form-group');
            if (parent.length) parent.find('.so-err-msg').remove(); else $el.nextAll('.so-err-msg').first().remove();
        }
        function clearAllInlineErrors() { $('.so-err').removeClass('so-err'); $('.so-err-msg').remove(); }
        window.showInlineError = showInlineError; window.removeInlineError = removeInlineError; window.clearAllInlineErrors = clearAllInlineErrors;

        $(document).off('change', '#SEL_PolicyMode').on('change', '#SEL_PolicyMode', function () {
            applyPolicyMode();
            updateAutoDADisplay();
            updateVisibility();

            var selVal = $(this).val();
            document.dispatchEvent(new CustomEvent('policy.updated', { detail: { mode: selVal, source: 'client' } }));
        });

        function applyPolicyMode() {
            var $sel = $('#SEL_PolicyMode');
            var mode = $sel.val();
            if (!mode) { mode = 'HQ'; $sel.val('HQ'); }

            var $hf = $resolve('HF_ResolvedPolicyMode');
            if ($hf.length) $hf.val(mode);

            // --- DYNAMIC DA RESOLUTION FIX ---
            var baseDA = $resolve('HF_ResolvedDA').val() || '—';
            var daVal = baseDA; // Default for HQ and Ex-HQ

            if (mode === 'Outstation') {
                daVal = $resolve('HF_OutstationDA').val() || baseDA;
            }
            else if (mode === 'OwnArrangement') {
                daVal = $resolve('HF_OwnArrangementDA').val() || baseDA;
            }

            var hcVal = $resolve('HF_ResolvedHotelCap').val() || '—';

            var sd = document.getElementById('spDAValue');
            var sh = document.getElementById('spHotelCap');
            var sa = document.getElementById('spAllowedTypes');
            var sn = document.getElementById('spPolicyNote');

            if (sd) sd.innerText = daVal;
            if (sh) sh.innerText = hcVal;
            if (sn) sn.innerHTML = '';

            var existingDA = false;
            var existingStationary = false;
            var totalExpenses = 0;

            $('#expenseModal .table-responsive table tbody tr').each(function () {
                var tp = $(this).find('.expense-type-badge').text().trim();
                if (tp) {
                    totalExpenses++;
                    if (tp === 'DA')
                        existingDA = true;
                    if (tp === 'Stationary')
                        existingStationary = true;
                }
            });

            if (totalExpenses > 0) {
                $sel.prop('disabled', true).css({ 'background-color': '#e9ecef', 'cursor': 'not-allowed' });
            } else {
                $sel.prop('disabled', false).css({ 'background-color': '', 'cursor': 'default' });
            }

            var $ddl = $resolve('DDL_ExpenseType');
            var $txtDA = $resolve('TXT_DAAmount');

            if ($ddl.length) {
                $ddl.find('option').prop('disabled', false).css('color', '#000');

                var allowed = [];
                var lockWarningHtml = totalExpenses > 0 ? '<br/><span class="text-danger mt-1 d-block"><i class="fa fa-lock"></i> Policy Mode is locked. Delete existing claims to change mode.</span>' : '';

                if (mode === 'HQ') {

                    allowed = [];

                    if (!existingDA)
                        allowed.push('DA');

                    if (!existingStationary)
                        allowed.push('Stationary');

                    // Disable everything first
                    $ddl.find('option')
                        .prop('disabled', true)
                        .css('color', '#ccc');

                    // Enable only allowed options
                    allowed.forEach(function (item) {
                        $ddl.find('option[value="' + item + '"]')
                            .prop('disabled', false)
                            .css('color', '#000');
                    });

                    if (allowed.length > 0) {

                        if (allowed.indexOf($ddl.val()) < 0) {
                            $ddl.val(allowed[0]).trigger('change');
                        }

                        if (sn)
                            sn.innerHTML =
                                'HQ: Only standard DA claims and Stationary are allowed.' +
                                lockWarningHtml;
                    }
                    else {

                        $ddl.val('');

                        if (sn)
                            sn.innerHTML =
                                'HQ: All eligible expenses have already been claimed.' +
                                lockWarningHtml;
                    }

                    if ($txtDA.length)
                        $txtDA.val($resolve('HF_ResolvedDA').val() || '');
                } else if (mode === 'Ex-HQ') {
                    if (existingDA) {
                        allowed = ['Travel', 'Stationary'];
                        $ddl.find('option[value="DA"], option[value="Hotel"]').prop('disabled', true).css('color', '#ccc');
                        if ($ddl.val() !== 'Travel' && $ddl.val() !== 'Stationary') $ddl.val('Travel').trigger('change');
                        if (sn) sn.innerHTML = 'Ex-HQ: DA already claimed. Only Travel and Stationary claims are allowed.' + lockWarningHtml;
                    } else {
                        allowed = ['DA', 'Travel', 'Stationary'];
                        $ddl.find('option[value="Hotel"]').prop('disabled', true).css('color', '#ccc');
                        if ($ddl.val() === 'Hotel') $ddl.val('DA').trigger('change');
                        if (sn) sn.innerHTML = 'Ex-HQ: DA, Travel & Stationary claims are allowed.' + lockWarningHtml;
                        if ($txtDA.length) $txtDA.val($resolve('HF_ResolvedDA').val() || '');
                    }
                } else if (mode === 'Outstation') {
                    allowed = ['Hotel', 'Travel', 'Stationary'];
                    $ddl.find('option[value="DA"]').prop('disabled', true).css('color', '#ccc');
                    if ($ddl.val() === 'DA') $ddl.val('Hotel').trigger('change');
                    if (sn) sn.innerHTML = 'Outstation: Hotel, Travel & Stationary allowed. (Outstation DA is auto-applied).' + lockWarningHtml;
                    if ($txtDA.length) $txtDA.val($resolve('HF_OutstationDA').val() || '');

                } else if (mode === 'OwnArrangement') {
                    allowed = ['Travel', 'Stationary'];
                    $ddl.find('option[value="DA"], option[value="Hotel"]').prop('disabled', true).css('color', '#ccc');
                    if ($ddl.val() !== 'Travel' && $ddl.val() !== 'Stationary') $ddl.val('Travel').trigger('change');
                    if (sn) sn.innerHTML = 'Own Arrangement: Only Travel & Stationary allowed. (Own Arrangement DA is auto-applied).' + lockWarningHtml;
                    if ($txtDA.length) $txtDA.val($resolve('HF_OwnArrangementDA').val() || '');
                } else {
                    allowed = ['DA', 'Hotel', 'Travel', 'Stationary'];
                }

                if (sa) sa.innerText = 'Allowed: ' + (allowed.length > 0 ? allowed.join(', ') : 'None');

                if (allowed.length < 4) {
                    $('#txtExpenseTypeHint').show();
                } else {
                    $('#txtExpenseTypeHint').hide();
                }
            }
        }

        window.applyPolicyModeFromServer = function (mode) {
            window.__so_policy_pending_mode = mode;
            mode = $.trim(mode) || 'HQ';

            var hf = document.getElementById('<%= HF_ResolvedPolicyMode.ClientID %>');
            if (hf) hf.value = mode;

            var sel = document.getElementById('SEL_PolicyMode');
            if (sel) {
                sel.disabled = false;
                sel.value = mode;
            }

            if (typeof applyPolicyMode === 'function') applyPolicyMode();
            window.__so_policy_pending_mode = null;
        };

        document.addEventListener('policy.updated', function () {
            applyPolicyMode();
            document.dispatchEvent(new CustomEvent('policy.visibility.refresh'));
        });

        function safeShow($el) { if ($el && $el.length) $el.show(); }
        function safeHide($el) { if ($el && $el.length) $el.hide(); }

        function updateVisibility() {
            var $ddl = $resolve('DDL_ExpenseType');
            var type = $ddl.length ? $ddl.val() : null;

            var $da = $resolve('divDASection'),
                $ht = $resolve('divHotelSection'),
                $tr = $resolve('divTravelSection'),
                $st = $('#divStationary'),
                $fu = $resolve('divFUContainer'),
                $inv = $resolve('divTravelInvoiceNo'),
                $chk = $resolve('CHK_TravelInvoice'),
                $btnSave = $resolve('BTN_SaveExpense'),
                $fuInput = $resolve('FU_Attachment'); // ⬅️ 1. Target the file input

            safeHide($da);
            safeHide($ht);
            safeHide($tr);
            safeHide($st);
            safeHide($fu);
            safeHide($inv);

            // ⬅️ 2. NEW: Force disable the file input by default to prevent "Ghost Uploads"
            if ($fuInput.length) {
                $fuInput.prop('disabled', true);
            }

            if (!type) {
                if ($btnSave.length) $btnSave.prop('disabled', true);
                return;
            } else {
                if ($btnSave.length) $btnSave.prop('disabled', false);
            }

            if (type === 'DA') {
                safeShow($da);
            }
            else if (type === 'Hotel') {
                safeShow($ht);
                safeShow($fu);
                $fuInput.prop('disabled', false); // ⬅️ 3. Re-enable only when needed
            }
            else if (type === 'Travel') {
                safeShow($tr);
                if ($chk.length && $chk.prop('checked')) {
                    safeShow($fu);
                    safeShow($inv);
                    $fuInput.prop('disabled', false); // ⬅️ 3. Re-enable only when needed
                }
            }
            else if (type === 'Stationary') {
                safeShow($st);
                safeShow($fu);
                $fuInput.prop('disabled', false); // ⬅️ 3. Re-enable only when needed
            }
        }

        function updateAutoDADisplay() {
            var c = resolve('divAutoDAInfo'); if (!c) return;
            var mode = ($resolve('HF_ResolvedPolicyMode').val() || '').toLowerCase();
            var outV = $resolve('HF_OutstationDA').val(), ownV = $resolve('HF_OwnArrangementDA').val();

            if (mode === 'outstation' && outV && outV > 0) { c.style.display = ''; resolve('spAutoDAAmount').innerText = '₹ ' + fmtAmount(outV); resolve('spAutoDAType').innerText = 'Outstation DA (auto)'; return; }
            if (mode === 'ownarrangement' && ownV && ownV > 0) { c.style.display = ''; resolve('spAutoDAAmount').innerText = '₹ ' + fmtAmount(ownV); resolve('spAutoDAType').innerText = 'Own Arrangement DA (auto)'; return; }
            c.style.display = 'none';
        }

        document.addEventListener('policy.visibility.refresh', function () { updateAutoDADisplay(); updateVisibility(); });

        $(document).on('change', '[id$="DDL_ExpenseType"]', function () { updateVisibility(); });
        $(document).on('change', '[id$="CHK_TravelInvoice"]', function () { updateVisibility(); });

        window.validateExpenseForm = function () {
            clearAllInlineErrors();
            var type = $resolve('DDL_ExpenseType').val() || 'DA';

            if (type === 'DA') {
                var dAmt = parseNumber($resolve('TXT_DAAmount').val()),
                    fP = $resolve('TXT_DAFromPlace').val().trim(),
                    tP = $resolve('TXT_DAToPlace').val().trim(),
                    tD = $resolve('TXT_TravelDescription').val().trim();

                if (isNaN(dAmt) || dAmt <= 0) { showInlineError($resolve('TXT_DAAmount'), 'DA Amount required'); return false; }
                if (!fP) { showInlineError($resolve('TXT_DAFromPlace'), 'Required'); return false; }
                if (!tP) { showInlineError($resolve('TXT_DAToPlace'), 'Required'); return false; }
                if (fP.toLowerCase() === tP.toLowerCase()) { showInlineError($resolve('TXT_DAToPlace'), 'Cannot be same as From Place'); return false; }
                if (!tD || tD.length < 5) { showInlineError($resolve('TXT_TravelDescription'), 'Remarks (min 5 chars) required'); return false; }
            } else if (type === 'Hotel') {
                var from = $resolve('TXT_HotelFromDate').val(), to = $resolve('TXT_HotelToDate').val(), bno = $resolve('TXT_HotelBillNo').val().trim(), hamt = parseNumber($resolve('TXT_HotelAmount').val()), pur = $resolve('TXT_HotelPurpose').val().trim();
                if (!from) { showInlineError($resolve('TXT_HotelFromDate'), 'Required'); return false; }
                if (!to) { showInlineError($resolve('TXT_HotelToDate'), 'Required'); return false; }
                if (new Date(from) > new Date(to)) { showInlineError($resolve('TXT_HotelToDate'), 'Invalid range'); return false; }
                if (!bno) { showInlineError($resolve('TXT_HotelBillNo'), 'Required'); return false; }
                if (isNaN(hamt) || hamt <= 0) { showInlineError($resolve('TXT_HotelAmount'), 'Required > 0'); return false; }
                if (!pur) { showInlineError($resolve('TXT_HotelPurpose'), 'Required'); return false; }
                var files = $resolve('FU_Attachment')[0].files; if (!files || files.length === 0) { showInlineError($resolve('FU_Attachment'), 'Attachment required'); return false; }
            } else if (type === 'Travel') {
                var tdt = $resolve('TXT_TravelDate').val(), tfp = $resolve('TXT_FromPlace').val().trim(), ttp = $resolve('TXT_ToPlace').val().trim(), tamt = parseNumber($resolve('TXT_TravelAmount').val()), tpur = $resolve('TXT_TravelPurpose').val().trim();
                if (!tdt) { showInlineError($resolve('TXT_TravelDate'), 'Required'); return false; }
                if (!tfp) { showInlineError($resolve('TXT_FromPlace'), 'Required'); return false; }
                if (!ttp) { showInlineError($resolve('TXT_ToPlace'), 'Required'); return false; }
                if (isNaN(tamt) || tamt <= 0) { showInlineError($resolve('TXT_TravelAmount'), 'Required > 0'); return false; }
                if (!tpur) { showInlineError($resolve('TXT_TravelPurpose'), 'Required'); return false; }
                if ($resolve('CHK_TravelInvoice').prop('checked')) {
                    if (!$resolve('TXT_TravelInvoiceNo').val().trim()) { showInlineError($resolve('TXT_TravelInvoiceNo'), 'Required'); return false; }
                    var f2 = $resolve('FU_Attachment')[0].files; if (!f2 || f2.length === 0) { showInlineError($resolve('FU_Attachment'), 'Attachment required'); return false; }
                }
            } else if (type === 'Stationary') {
                var stAmt = parseNumber($resolve('TXT_StationaryAmount').val()),
                    stPurp = $resolve('TXT_StationaryPurpose').val().trim();

                if (isNaN(stAmt) || stAmt <= 0) { showInlineError($resolve('TXT_StationaryAmount'), 'Amount required > 0'); return false; }
                if (!stPurp) { showInlineError($resolve('TXT_StationaryPurpose'), 'Required'); return false; }

                // ADDED: Require File Attachment for Stationary
                var f3 = $resolve('FU_Attachment')[0].files;
                if (!f3 || f3.length === 0) { showInlineError($resolve('FU_Attachment'), 'Invoice attachment is mandatory for Stationary'); return false; }
            }
            return true;
        };

        $(document).on('input change', '.so-err, input, select, textarea', function () { removeInlineError($(this)); });

        function initExpenseModal() {
            var $sel = $('#SEL_PolicyMode');

            var serverMode = window.__so_policy_pending_mode || $.trim($resolve('HF_SnapPolicyMode').val()) || $.trim($resolve('HF_ResolvedPolicyMode').val()) || 'HQ';

            if ($sel.length) {
                $sel.prop('disabled', false);
                $sel.val(serverMode);
            }

            applyPolicyMode();
            updateVisibility();
            updateAutoDADisplay();
            $resolve('BTN_SaveExpense').attr('onclick', 'return validateExpenseForm();');

            $('.form-control-file').each(function () {
                var $f = $(this);
                $f.off('change.file-chosen');
                if (!$f.next('.file-chosen').length) {
                    $f.after('<div class="file-chosen small text-muted mt-1">No file chosen</div>');
                }
                $f.on('change.file-chosen', function () {
                    var n = this.files && this.files.length ? this.files[0].name : 'No file chosen';
                    $(this).next('.file-chosen').text(n);
                });
            });

            var dt = 0, ht = 0, ot = 0, st = 0;
            $('#expenseModal .table-responsive table tbody tr').each(function () {
                var tp = $(this).find('.expense-type-badge').text().trim();
                var a = parseFloat($(this).find('td:nth-child(4)').text().replace(/,/g, '')) || 0;

                if (tp === 'DA') dt += a;
                else if (tp === 'Hotel') ht += a;
                else if (tp === 'Travel') ot += a;
                else if (tp === 'Stationary') st += a; // Segregated Stationary
            });

            try {
                $resolve('lblTotalDA').text(dt.toFixed(2));
                $resolve('lblTotalHotel').text(ht.toFixed(2));
                $resolve('lblTotalOther').text(ot.toFixed(2)); // This is now strictly Travel

                // NEW: Update the Stationary Label
                if ($resolve('lblTotalStationary').length) {
                    $resolve('lblTotalStationary').text(st.toFixed(2));
                }

                // Grand Total includes all 4 buckets now
                $resolve('lblOverallTotal').text((dt + ht + ot + st).toFixed(2));
            } catch (e) { console.error("Error updating totals: ", e); }
        }

        $(function () { initExpenseModal(); });

        $('#expenseModal').on('shown.bs.modal', function () {
            initExpenseModal();
            clearAllInlineErrors();
            var activityId = $('#<%=hdnActivityId.ClientID%>').val();
            restoreRouteDataToDAFields(activityId);
        });

        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initExpenseModal);
        }

        window.openEditKmModal = function (aid, cur, min, max) {
            $resolve('hdnActivityId').val(aid); $resolve('txtModalKM').val(cur);
            $('#<%=HF_RouteActivityId.ClientID%>').val(aid);
            document.getElementById('spMinKm').innerText = min || '—'; document.getElementById('spMaxKm').innerText = max || '—';
            $resolve('lblModalCurrentKm').text('Current: ' + cur + ' km');
            $('#spClientKmError').hide();
            if (typeof initializeKmModalSessionClient === 'function') initializeKmModalSessionClient(aid, true);
            $('#editKmModal').modal('show');
        };

        window.validateGridBeforeSubmit = function () {
            if ($(".km-input.is-invalid").length > 0) { alert("Please fix invalid KM entries before saving."); return false; }
            return true;
        };

        $(document).on("input blur", ".km-input", function () {
            var $t = $(this), r = $t.closest("tr"), v = parseFloat($t.val()), min = parseFloat(r.find(".tol-lower").text()), max = parseFloat(r.find(".tol-upper").text()), $e = r.find(".km-error");
            if (isNaN(v)) { $t.addClass("is-invalid"); $e.text("Invalid").show(); return; }
            if ((!isNaN(min) && v < min) || (!isNaN(max) && v > max)) { $t.addClass("is-invalid"); $e.text("[" + min + " to " + max + "]").show(); }
            else { $t.removeClass("is-invalid"); $e.hide(); }
        });

        window.showSubmitModal = function () { $('#submitConfirmModal').modal({ backdrop: 'static', keyboard: false }); return false; };
        window.performSubmitFromModal = function () {
            $('#submitModalConfirm, #submitModalCancel, [id$="btn_submit"]').prop('disabled', true).css('opacity', '0.6');
            $('#submitConfirmModal').modal('hide'); showPageLoader();
            __doPostBack('<%= btn_submit.UniqueID %>', '');
        };
        window.enableButtonsAfterModalClose = function () { $('#submitModalConfirm, #submitModalCancel, [id$="btn_submit"]').prop('disabled', false).css('opacity', '1'); };

        // Deprecated: handleSaveDraftClick — Save as Draft button removed (auto-save model).
        // window.handleSaveDraftClick = function (btn) { btn.disabled = true; btn.value = "Saving..."; showPageLoader(); return true; };
        window.showPageLoader = function () { var e = document.getElementById("pageLoader"); if (e) e.style.display = "block"; };
        window.hidePageLoader = function () { var e = document.getElementById("pageLoader"); if (e) e.style.display = "none"; };

        window.copyPolicyJson = function () {
            var t = document.getElementById('<%= policyJsonRaw.ClientID %>'); if (!t) return;
            t.style.display = 'block'; t.select(); try { document.execCommand('copy'); alert('Copied!'); } catch (e) { } t.style.display = 'none';
        };

        window.clearExpenseModalInputs = function () {
            var modal = $('#expenseModal');
            modal.find('input:text, textarea, input[type="number"], input[type="date"]').val('');
            modal.find('input:checkbox').prop('checked', false);
            modal.find('select').prop('selectedIndex', 0);
            clearAllInlineErrors();
        }

        window.openExpenseModalSafe = function (activityId, savedMode, remark) {
            // 1. Handle Policy Modes
            window.__so_policy_pending_mode = savedMode;

            var $sel = $('#SEL_PolicyMode');
            if ($sel.length) {
                $sel.prop('disabled', false);
                $sel.val(savedMode);
            }

            var $hf = $resolve('HF_ResolvedPolicyMode');
            if ($hf.length) $hf.val(savedMode);

            // 2. ⬅️ NEW: Handle Manager's Remarks
            if (remark && remark.trim() !== '') {
                $('#managerRemarkText').text(remark);
                $('#managerRemarkBanner').slideDown(200);
            } else {
                $('#managerRemarkBanner').hide();
            }

            // 3. Show Modal
            $('#expenseModal').modal('show');

            if (typeof initExpenseModal === 'function') {
                initExpenseModal();
            }
        };

        window.routeMap = null;
        window.routeMapInitialized = false;
        window.waypoints = [];
        window.routeMarkers = [];
        window.routePolyline = null;
        window.totalRouteDistance = 0;
        window._kmApplyingRoute = false;

        var KmSaveWorkflowManual = 'Manual';
        var KmSaveWorkflowRoute = 'Route';

        window.clearKmRouteMetadataFields = function () {
            $('#<%=HF_RouteOrigin.ClientID%>').val('');
            $('#<%=HF_RouteDestination.ClientID%>').val('');
            $('#<%=HF_RouteWaypoints.ClientID%>').val('');
            $('#<%=HF_RouteDistance.ClientID%>').val('');
            $('#<%=HF_RouteTravelMode.ClientID%>').val('');
            $('#spRouteOrigin, #spRouteDestination, #spRouteDistance, #spWaypointCount').text('');
            $('#routeSummaryPanel').hide();
            window.waypoints = [];
            window.totalRouteDistance = 0;
        };

        window.setKmSaveWorkflowManual = function (clearRouteMetadata) {
            $('#<%=HF_KmSaveWorkflow.ClientID%>').val(KmSaveWorkflowManual);
            if (clearRouteMetadata) clearKmRouteMetadataFields();
            else $('#routeSummaryPanel').hide();
        };

        window.setKmSaveWorkflowRoute = function () {
            $('#<%=HF_KmSaveWorkflow.ClientID%>').val(KmSaveWorkflowRoute);
        };

        window.initializeKmModalSessionClient = function (activityId, hideRouteSummary) {
            var prevActivity = $('#<%=HF_RouteActivityId.ClientID%>').val();
            if (activityId && prevActivity && prevActivity !== String(activityId)) {
                clearKmRouteMetadataFields();
            }
            if (activityId) $('#<%=HF_RouteActivityId.ClientID%>').val(activityId);
            setKmSaveWorkflowManual(false);
            if (hideRouteSummary) $('#routeSummaryPanel').hide();
        };

        $(document).on('input', '#<%=txtModalKM.ClientID%>', function () {
            if (window._kmApplyingRoute) return;
            setKmSaveWorkflowManual(true);
        });

        function initializeRouteMap() {
            if (window.routeMap) return;

            window.routeMap = L.map('routeMap').setView([22.5726, 88.3639], 12);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap contributors'
            }).addTo(window.routeMap);
        }

        function openRouteCalculator() {
            $('#routeDistanceModal').modal({ backdrop: 'static', keyboard: false });

            setTimeout(function () {
                if (!window.routeMapInitialized) {
                    initializeRouteMap();
                    window.routeMapInitialized = true;
                }

                if (window.routeMap) {
                    window.routeMap.invalidateSize();
                }

                if ($('#<%=HF_RouteTravelMode.ClientID%>').val()) {
                    $('#ddlTravelMode').val($('#<%=HF_RouteTravelMode.ClientID%>').val());
                }
            }, 300);

            restoreSavedRoute();
        }

        function restoreSavedRoute() {
            var savedJson = $('#<%=HF_RouteWaypoints.ClientID%>').val();
            if (!savedJson) return;

            try {
                window.waypoints = JSON.parse(savedJson);
                updateWaypointListUI();
                calculateAndDrawRoute();
            }
            catch (e) {
                console.log(e);
            }
        }

        function searchLocation() {
            var q = $('#txtPlaceSearch').val().trim();
            if (!q) return;

            $.getJSON('https://nominatim.openstreetmap.org/search', { q: q, format: 'json', limit: 10 },
                function (data) {
                    var html = '';

                    if (!data || data.length === 0) {
                        html = '<div class="list-group-item text-danger">No locations found</div>';
                        $('#searchResults').html(html);
                        return;
                    }

                    data.forEach(function (item) {
                        html += '<a href="#" class="list-group-item route-search-item" data-lat="' + item.lat + '" data-lon="' + item.lon + '" data-name="' + item.display_name.replace(/"/g, '&quot;') + '">' + item.display_name + '</a>';
                    });

                    $('#searchResults').html(html);
                });
        }

        $(document).on('click', '.route-search-item', function (e) {
            e.preventDefault();
            addWaypoint($(this).data('lat'), $(this).data('lon'), $(this).data('name'));
            $('#searchResults').empty();
            $('#txtPlaceSearch').val('');
        });

        function addWaypoint(lat, lon, name) {
            var exists = window.waypoints.some(function (x) { return x.name === name; });
            if (exists) {
                alert('Location already added.');
                return;
            }

            window.waypoints.push({ lat: parseFloat(lat), lon: parseFloat(lon), name: name });
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function removeWaypoint(index) {
            window.waypoints.splice(index, 1);
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function moveWaypointUp(index) {
            if (index <= 0) return;
            var temp = window.waypoints[index];
            window.waypoints[index] = window.waypoints[index - 1];
            window.waypoints[index - 1] = temp;
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function moveWaypointDown(index) {
            if (index >= window.waypoints.length - 1) return;
            var temp = window.waypoints[index];
            window.waypoints[index] = window.waypoints[index + 1];
            window.waypoints[index + 1] = temp;
            updateWaypointListUI();
            calculateAndDrawRoute();
        }

        function updateWaypointListUI() {
            var html = '';
            for (var i = 0; i < window.waypoints.length; i++) {
                html += '<div class="list-group-item">';
                html += '<strong>' + (i + 1) + '</strong> ' + window.waypoints[i].name + '<br/>';
                html += '<button type="button" class="btn btn-xs btn-primary" onclick="moveWaypointUp(' + i + ')">↑</button> ';
                html += '<button type="button" class="btn btn-xs btn-primary" onclick="moveWaypointDown(' + i + ')">↓</button> ';
                html += '<button type="button" class="btn btn-xs btn-danger" onclick="removeWaypoint(' + i + ')">Remove</button>';
                html += '</div>';
            }
            $('#waypointList').html(html);
        }

        function clearRouteGraphics() {
            if (window.routePolyline) {
                window.routeMap.removeLayer(window.routePolyline);
                window.routePolyline = null;
            }
            if (window.routeMarkers) {
                window.routeMarkers.forEach(function (m) { window.routeMap.removeLayer(m); });
                window.routeMarkers = [];
            }
        }

        function getDistanceKm(lat1, lon1, lat2, lon2) {
            var R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
            var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
            return R * c;
        }

        function calculateAndDrawRoute() {
            clearRouteGraphics();

            if (!window.waypoints || window.waypoints.length < 2) return;

            var profile = $('#ddlTravelMode').val() || 'driving';
            var coords = window.waypoints.map(function (p) { return p.lon + ',' + p.lat; }).join(';');
            var url = 'https://router.project-osrm.org/route/v1/' + profile + '/' + coords + '?overview=full&geometries=geojson';

            $.getJSON(url)
                .done(function (res) {
                    if (!res.routes || res.routes.length === 0) return;
                    drawActualRoute(res.routes[0]);
                })
                .fail(function () {
                    alert('Unable to calculate route.');
                });
        }

        function drawActualRoute(route) {
            clearRouteGraphics();

            var latlngs = route.geometry.coordinates.map(function (c) { return [c[1], c[0]]; });

            window.routePolyline = L.polyline(latlngs, { color: 'blue', weight: 5 }).addTo(window.routeMap);
            window.routeMap.fitBounds(window.routePolyline.getBounds());
            window.totalRouteDistance = (route.distance / 1000).toFixed(2);
            $('#lblDistance').text(window.totalRouteDistance);

            drawWaypointMarkers();
        }

        function drawWaypointMarkers() {
            if (!window.routeMarkers) window.routeMarkers = [];

            window.routeMarkers.forEach(function (m) { window.routeMap.removeLayer(m); });
            window.routeMarkers = [];

            window.waypoints.forEach(function (p, idx) {
                var marker = L.marker([p.lat, p.lon]).addTo(window.routeMap).bindPopup((idx + 1) + '. ' + p.name);
                window.routeMarkers.push(marker);
            });
        }

        function applyRouteData(route) {
            window._kmApplyingRoute = true;
            $('#<%=txtModalKM.ClientID%>').val(route.TotalDistanceKM);
            $('#<%=TXT_DAFromPlace.ClientID%>').val(route.OriginName);
            $('#<%=TXT_DAToPlace.ClientID%>').val(route.DestinationName);
            $('#<%=HF_RouteOrigin.ClientID%>').val(route.OriginName);
            $('#<%=HF_RouteDestination.ClientID%>').val(route.DestinationName);
            $('#<%=HF_RouteWaypoints.ClientID%>').val(route.WaypointsJSON);
            $('#<%=HF_RouteDistance.ClientID%>').val(route.TotalDistanceKM);
            $('#<%=HF_RouteTravelMode.ClientID%>').val(route.TravelMode);
            var activityId = $('#<%=hdnActivityId.ClientID%>').val();
            if (activityId) $('#<%=HF_RouteActivityId.ClientID%>').val(activityId);
            setKmSaveWorkflowRoute();
            $('#spRouteDistance').text(route.TotalDistanceKM);
            $('#spRouteOrigin').text(route.OriginName);
            $('#spRouteDestination').text(route.DestinationName);
            $('#spWaypointCount').text(JSON.parse(route.WaypointsJSON).length);
            $('#routeSummaryPanel').show();
            $('#routeDistanceModal').modal('hide');
            window._kmApplyingRoute = false;
        }

        $(document).on('click', '#btnApplyRoute', function () {
            if (window.waypoints.length < 2) {
                alert('Please select Origin and Destination.');
                return;
            }

            var route = {
                OriginName: window.waypoints[0].name,
                DestinationName: window.waypoints[window.waypoints.length - 1].name,
                TotalDistanceKM: window.totalRouteDistance,
                TravelMode: $('#ddlTravelMode').val(),
                WaypointsJSON: JSON.stringify(window.waypoints)
            };

            applyRouteData(route);
        });

        $(document).on('click', '#btnSearchLocation', function () {
            searchLocation();
        });

        $(document).on('change', '#ddlTravelMode', function () {
            if (window.waypoints && window.waypoints.length >= 2) {
                calculateAndDrawRoute();
            }
        });

        function restoreRouteDataToDAFields(activityId) {
            if (!window.routeCache) return;

            if (!window.routeCache[activityId]) return;

            var route = window.routeCache[activityId];

            $('#<%=TXT_DAFromPlace.ClientID%>').val(route.OriginName);
            $('#<%=TXT_DAToPlace.ClientID%>').val(route.DestinationName);
        }
    </script>

</asp:Content>
