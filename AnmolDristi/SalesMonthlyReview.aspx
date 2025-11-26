<%@ Page Title="AIL | SO Cliams Submission" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" MaintainScrollPositionOnPostback="true" CodeBehind="SalesMonthlyReview.aspx.cs" Inherits="AnmolDristi.SalesMonthlyReview" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .grid-container {
            max-height: 480px;
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
            }

            .table-finance .grand-total-cell {
                background-color: #e2e8f6 !important;
                color: #002b59;
                font-weight: 700;
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
                box-shadow: inset 0 -1px 0 rgba(13,110,253,0.02);
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

        /* Small badges for type */
        .expense-type-badge {
            font-weight: 600;
            font-size: .78rem;
            padding: .25rem .45rem;
            border-radius: .35rem;
        }

        /* details truncation */
        .text-truncate-ellipsis {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            display: block;
            max-width: 24rem;
        }

        /* Buttons */
        #expenseModal .btn-sm {
            padding: .3rem .55rem;
            border-radius: .35rem;
        }

        /* Make file input slimmer */
        #expenseModal .form-control-file {
            font-size: .85rem;
        }

        /* small debug log */
        #expenseDebugLog {
            font-family: Consolas, monospace;
            font-size: 12px;
            color: #333;
            background: #fbfbfb;
            border-radius: .3rem;
        }

        /* Auto DA card - prominent */
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

        /* subtle label under auto-da */
        #expenseModal #spAutoDAType {
            font-size: .8rem;
            color: #6b4d00;
            font-weight: 600;
        }

        /* ---------- Compact input sizing ---------- */
        #expenseModal .form-control,
        #expenseModal .form-control-sm {
            padding: .28rem .46rem;
            font-size: .88rem;
            height: calc(1.7rem + .6rem);
            border-radius: .33rem;
        }

        /* shrink date inputs slightly */
        #expenseModal input[type="date"] {
            padding: .2rem .4rem;
        }

        /* Reduce FileUpload height */
        #expenseModal .form-control-file {
            font-size: .83rem;
            padding: .18rem 0;
        }

        /* ---------- Repeater table header centered & compact rows ---------- */
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

        /* Amount right align for better readability */
        #expenseModal table.table tbody td:nth-child(4) {
            text-align: right;
            font-weight: 700;
        }

        /* small pill-style badges for type */
        .expense-type-badge {
            font-size: .75rem;
            padding: .18rem .38rem;
            border-radius: .32rem;
            background: #eef6ff;
            color: #0b66d0;
            font-weight: 700;
        }

        /* action buttons consistent small size */
        #expenseModal .btn-sm {
            padding: .28rem .5rem;
            border-radius: .33rem;
            font-size: .85rem;
        }

        /* overall totals card smaller and cleaner */
        #expenseModal .card.totals-card {
            padding: .45rem .5rem;
            border-radius: .35rem;
        }

            #expenseModal .card.totals-card .h6 {
                font-size: 1rem;
            }

        /* make details column truncated in narrow layouts */
        .text-truncate-ellipsis {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            display: block;
            max-width: 26rem;
        }

        /* responsive tweak: make table font tighter on small screens */
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

            /* colors for statuses */
            .status-icon.present {
                background: #e6f6ea;
                color: #118a32;
            }
            /* light green bg, green glyph */
            .status-icon.absent {
                background: #fdecea;
                color: #c92121;
            }
            /* light red bg, red glyph */
            .status-icon.leave {
                background: #fff7e6;
                color: #b86a00;
            }
            /* light orange bg */
            .status-icon.wfh {
                background: #e9f3ff;
                color: #0b63c9;
            }
            /* light blue */
            .status-icon.unknown {
                background: #f1f1f1;
                color: #666;
            }
        /* neutral */
    </style>
    <!-- ADD THIS AT THE TOP OF .modal-body (just below <div class="modal-body">) -->
    <style>
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

            /* GRID LAYOUT WITH AUTO WRAP */
            #divKeyValueHeader .kv-row {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
                gap: 1rem;
                border-top: 1px solid #e4e4e4;
                padding-top: .8rem;
            }

            /* EACH ITEM BOX */
            #divKeyValueHeader .kv-item {
                padding: .4rem .8rem;
                border: 1px solid #e0e0e0;
                border-radius: 4px;
                background: #fafafa;
                position: relative;
            }

                /* VERTICAL SEPARATOR (right border) */
                #divKeyValueHeader .kv-item:not(:last-child) {
                    border-right: 2px solid #d7d7d7;
                }

            /* KEY */
            #divKeyValueHeader .kv-key {
                font-size: .75rem;
                color: #6d6d6d;
                font-weight: 500;
                text-transform: uppercase;
                letter-spacing: .5px;
                margin-bottom: 2px;
            }

            /* VALUE */
            #divKeyValueHeader .kv-value {
                font-size: .95rem;
                font-weight: 700;
                color: #004085;
                background: #eef5ff;
                padding: 2px 5px;
                border-radius: 4px;
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
                                    <button type="button" id="btnViewPolicyAgain" class="btn btn-info btn-sm" onclick="$('#policyModal').modal('show');"><i class="fa fa-info-circle"></i>View Policy</button>
                                    <asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm" Text="Save as Draft" OnClick="btn_save_draft_Click" />
                                    <%--<asp:Button ID="btn_save_draft" runat="server" Text="Save as Draft" CssClass="btn btn-success" UseSubmitBehavior="false" OnClick="btn_save_draft_Click" OnClientClick="return handleSaveDraftClick(this);" />--%>
                                    <%--<asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm" Text="Save as Draft" OnClick="btn_save_draft_Click" OnClientClick="this.disabled=true; this.style.opacity='0.6';" />--%>
                                    <%--<asp:Button ID="btn_submit" runat="server" CssClass="btn btn-success btn-sm" Text="Submit for Approval" OnClick="btn_submit_Click" />--%>
                                    <asp:Button ID="btn_submit" runat="server" CssClass="btn btn-success btn-sm" Text="Submit for Approval" OnClick="btn_submit_Click" OnClientClick="return showSubmitModal();" />
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

                <div class="col-md-12 col-sm-12" id="Activities" runat="server" visible="false">
                    <div class="x_panel">
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">

                                    <div class="card-box table-responsive grid-container">
                                        <asp:GridView ID="gvActivities" runat="server" AutoGenerateColumns="false"
                                            CssClass="table table-bordered table-finance table-responsive" HeaderStyle-CssClass="grid-header" FooterStyle-CssClass="grid-footer"
                                            DataKeyNames="ActivityId" OnRowCommand="gvActivities_RowCommand" OnRowEditing="gvActivities_RowEditing"
                                            OnRowCancelingEdit="gvActivities_RowCancelingEdit" OnRowUpdating="gvActivities_RowUpdating"
                                            OnRowDataBound="gvActivities_RowDataBound" ShowFooter="true">
                                            <Columns>

                                                <asp:TemplateField HeaderText="Date & Day (Status)" ItemStyle-Width="10%">
                                                    <ItemTemplate>
                                                        <span class="activity-date"><%# Eval("ActivityDate", "{0:dd-MM-yyyy}") %></span>
                                                        <br />
                                                        <small style="font-weight: bold;" class="text-muted day-text"><%# Eval("Day") %></small>
                                                        <span class="status-icon" data-status='<%# Eval("Attendance_Status") %>' title='<%# Eval("Attendance_Status") %>' role="img" aria-label='<%# Eval("Attendance_Status") %>'></span>
                                                    </ItemTemplate>
                                                    <FooterTemplate><strong>Total :</strong>&nbsp;</FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Distance (KM)">
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
                                                            CssClass="form-control form-control-sm km-input" Width="100%" />
                                                        <small class="form-text text-muted"><%--Allowed:--%>
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

                                                <asp:TemplateField HeaderText="TA (₹)" SortExpression="CalculatedClaimAmount">
                                                    <ItemStyle HorizontalAlign="Right" Width="8%" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblCalculatedClaim" runat="server"
                                                            Text='<%# Eval("CalculatedClaimAmount", "₹ {0:N2}") %>' CssClass="currency" />
                                                        <br />
                                                        <small class="text-muted" runat="server" visible="true">(<%# Eval("RatePerKm", "{0:N2}") %>/ km)</small>
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblFooterCalculated" runat="server" CssClass="font-weight-bold currency" Text="₹ 0.00" />
                                                    </FooterTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Status" ItemStyle-Width="5%" ItemStyle-HorizontalAlign="Center">
                                                    <ItemTemplate>
                                                        <span data-toggle="tooltip"
                                                            title='<%# 
                                                                Eval("ClaimStatus").ToString() == "Draft" ? "Draft – not submitted yet" :
                                                                Eval("ClaimStatus").ToString() == "Submitted" ? "Submitted – awaiting approval" :
                                                                Eval("ClaimStatus").ToString() == "Approved" ? "Approved by manager" :
                                                                Eval("ClaimStatus").ToString() == "Rejected" ? "Rejected – please review" :
                                                                Eval("ClaimStatus").ToString() == "Processed" ? "Processed in accounts" :
                                                                "Unknown status" %>'
                                                            class='<%# 
                                                                Eval("ClaimStatus").ToString() == "Draft" ? "badge badge-warning" :
                                                                Eval("ClaimStatus").ToString() == "Submitted" ? "badge badge-info" :
                                                                Eval("ClaimStatus").ToString() == "Approved" ? "badge badge-success" :
                                                                Eval("ClaimStatus").ToString() == "Rejected" ? "badge badge-danger" :
                                                                Eval("ClaimStatus").ToString() == "Processed" ? "badge badge-primary" :
                                                                "badge badge-secondary" %>'>

                                                            <i class='<%# 
                                                                Eval("ClaimStatus").ToString() == "Draft" ? "fa fa-pencil-square-o" :
                                                                Eval("ClaimStatus").ToString() == "Submitted" ? "fa fa-paper-plane" :
                                                                Eval("ClaimStatus").ToString() == "Approved" ? "fa fa-check-circle" :
                                                                Eval("ClaimStatus").ToString() == "Rejected" ? "fa fa-times-circle" :
                                                                Eval("ClaimStatus").ToString() == "Processed" ? "fa fa-cogs" :
                                                                "fa fa-clock-o" %>'></i>
                                                        </span>
                                                    </ItemTemplate>
                                                </asp:TemplateField>



                                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="5%">
                                                    <ItemStyle HorizontalAlign="Center" />
                                                    <ItemTemplate>
                                                        <%--<asp:LinkButton ID="lnkEdit" runat="server"
                                                            CommandName="EditRow" CommandArgument='<%# Eval("ActivityId") %>' CssClass="btn btn-sm btn-primary" Visible='<%# Eval("ClaimStatus").ToString() == "Draft" %>' ToolTip="Edit Kilometers">
                                                            <i class="fa fa-pencil"></i>
                                                        </asp:LinkButton>--%>

                                                        <%--<asp:LinkButton ID="lnkEdit" runat="server" CommandName="OpenEditModal" CommandArgument='<%# Eval("ActivityId") + "|" + Eval("TotalDistanceKM") + "|" + Eval("MinAllowedKM") + "|" + Eval("MaxAllowedKM") + "|" + Eval("ActivityDate")%>' CssClass="btn btn-sm btn-primary" Visible="false" ToolTip="Edit Kms"><i class="fa fa-pencil"></i></asp:LinkButton>--%>

                                                        <asp:LinkButton ID="lnkExpenses" runat="server"
                                                            CommandName="OpenExpenses" CommandArgument='<%# Eval("ActivityId") + "|" + Eval("ActivityDate") %>' CssClass="btn btn-sm btn-primary" ToolTip="View / Add Expenses">
                                                            <i class="fa fa-briefcase"></i>
                                                        </asp:LinkButton>
                                                    </ItemTemplate>

                                                    <EditItemTemplate>
                                                        <asp:LinkButton ID="lnkUpdate" runat="server"
                                                            CommandName="UpdateRow" CommandArgument='<%# Eval("ActivityId") %>' OnClientClick="return validateGridBeforeSubmit();" CssClass="btn btn-sm btn-success" ToolTip="Save Changes">
                                                            <i class="fa fa-check"></i>
                                                        </asp:LinkButton>

                                                        <asp:LinkButton ID="lnkCancel" runat="server"
                                                            CommandName="Cancel" CssClass="btn btn-sm btn-default btn-outline-danger"
                                                            ToolTip="Cancel Edit">
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

                <div class="col-md-12 col-sm-12" id="Summary" runat="server" visible="false">
                    <div class="x_panel collapsed">
                        <div class="x_title">
                            <h2>Summary by status</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-down"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content" style="display: none;">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="mb-3">
                                        <div class="table-responsive">
                                            <table class="table table-sm table-bordered" style="max-width: 1000px;">
                                                <thead class="thead-light">
                                                    <tr>
                                                        <th>Status</th>
                                                        <th class="text-right">Total KM</th>
                                                        <th class="text-right">TA (₹)</th>
                                                        <th class="text-right">DA (₹)</th>
                                                        <th class="text-right">Hotel (₹)</th>
                                                        <th class="text-right">Travel (₹)</th>
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
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <label for="txtModalKM">Distance Travelled (in KM)</label><span style="color: red;">*</span>
                                    <asp:TextBox ID="txtModalKM" runat="server" CssClass="form-control" />
                                    <span id="spClientKmError" class="text-danger" style="display: none;"></span>
                                </div>

                                <div class="form-group">
                                    <small>Allowed range: <span id="spMinKm">—</span> to <span id="spMaxKm">—</span> km</small>
                                </div>

                                <asp:Label ID="lblModalCurrentKm" runat="server" CssClass="text-muted"></asp:Label>
                            </div>

                            <div class="col-md-6">
                                <div class="form-group">
                                    <label>Change for Reason (Mandatory)</label><span style="color: red;">*</span>
                                    <asp:TextBox ID="txtKmReason" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" />
                                </div>
                            </div>
                        </div>

                        <hr />

                        <h6 style="color: darkblue;">KM Change History</h6>
                        <div class="table-responsive">
                            <asp:GridView ID="gvKmHistory" runat="server" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" CssClass="table table-sm table-bordered table-finance table-responsive"
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
                        <asp:Button ID="btnSaveKm" runat="server" CssClass="btn btn-success" Text="Save changes" OnClick="btnSaveKm_Click" />
                        <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
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
                        <button type="button" id="submitModalConfirm" class="btn btn-success btn-sm" onclick="performSubmitFromModal();">
                            Yes, Submit
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade bs-example-modal-lg" tabindex="-1" role="dialog" aria-hidden="true" id="expenseModal">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">DA / Hotel / Travel Expenses for
                            <asp:Label ID="lbl_traveldate" runat="server" Text="NA" Font-Bold="true" ForeColor="#009933"></asp:Label></h5>
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

                            <div id="divKeyValueHeader">
                                <div class="kv-row">

                                    <div class="kv-item">
                                        <div class="kv-key">Beat Travelled</div>
                                        <div id="spBeatTravelled" class="kv-value">
                                            <asp:Label ID="lbl_BeatTravelled" runat="server" Text="Khordha-Jamnui-Khirpai"></asp:Label>
                                        </div>
                                    </div>

                                    <div class="kv-item">
                                        <div class="kv-key">Distributor Name</div>
                                        <div id="spDistributorName" class="kv-value">
                                            <asp:Label ID="lbl_DistributorName" runat="server" Text="Trader Name"></asp:Label>
                                        </div>
                                    </div>

                                    <div class="kv-item">
                                        <div class="kv-key">Travel Distance</div>
                                        <div id="spHQTravelDistance" class="kv-value">
                                            <asp:Label ID="lbl_HQTravelDistance" runat="server" Text="43"></asp:Label>&nbsp;Kms&nbsp;&nbsp;<asp:Button ID="btnOpenEditKmFromExpense" runat="server" CssClass="btn btn-sm btn-primary" Text="Modify Km." OnClick="btnOpenEditKmFromExpense_Click" />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="row mb-2 align-items-center policy-summary">
                                <div class="col-md-12">
                                    <label class="mb-1">Policy Mode & Summary</label>
                                    <div id="policyValues" class="d-flex flex-wrap align-items-center bg-light border rounded p-2">
                                        <span class="mr-3">DA: <strong id="spDAValue">—</strong></span>
                                        <span class="mr-3">Hotel cap/night: <strong id="spHotelCap">—</strong></span>
                                        <span class="mr-3" id="spAllowedTypes">Allowed: —</span>
                                        <span class="text-muted ml-2" id="spPolicyNote" style="font-size: .85rem;"></span>
                                    </div>
                                </div>

                                <div class="col-md-4 text-right" id="btn_rfpolicy" runat="server" visible="false">
                                    <button type="button" id="btnRefreshPolicy" class="btn btn-sm btn-outline-primary">Refresh policy</button>
                                </div>
                            </div>

                            <%--<div id="divAutoDAInfo" style="display: none; margin-left: 10px;">
                                <div class="p-2 border rounded bg-light" style="min-width: 200px;">
                                    <div><strong>Auto DA Amount:</strong> <span id="spAutoDAAmount">—</span></div>
                                    <div id="spAutoDAType" class="text-muted text-primary small">—</div>
                                </div>
                            </div>--%>

                            <div id="divAutoDAInfo" style="display: none;">
                                <div class="auto-da-card">
                                    <div>Auto DA Amount: <span id="spAutoDAAmount">—</span></div>
                                    <div id="spAutoDAType" class="small">—</div>
                                </div>
                            </div>

                            <br />

                            <div class="row mb-2">
                                <div class="col-md-12 form-group">
                                    <label style="font-weight: bold; color: darkblue;">Expense Type</label>
                                    <asp:DropDownList ID="DDL_ExpenseType" runat="server" ClientIDMode="Static" CssClass="form-control form-control-sm">
                                        <asp:ListItem Value="DA">DA</asp:ListItem>
                                        <asp:ListItem Value="Hotel">Hotel</asp:ListItem>
                                        <asp:ListItem Value="Travel">Travel</asp:ListItem>
                                    </asp:DropDownList>
                                    <small class="form-text text-muted" id="txtExpenseTypeHint"></small>
                                </div>
                            </div>

                            <div id="divDASection" runat="server" class="row">
                                <div class="col-md-4 form-group">
                                    <label>DA Amount</label>
                                    <asp:TextBox ID="TXT_DAAmount" runat="server" CssClass="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                                </div>

                                <div class="col-md-4 form-group">
                                    <label>From Place</label>
                                    <asp:TextBox ID="TXT_DAFromPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="100" />
                                </div>

                                <div class="col-md-4 form-group">
                                    <label>To Place</label>
                                    <asp:TextBox ID="TXT_DAToPlace" runat="server" CssClass="form-control form-control-sm" MaxLength="100" />
                                </div>
                                <div class="col-md-8 form-group">
                                    <label>Remarks </label>
                                    <asp:TextBox ID="TXT_TravelDescription" runat="server" TextMode="MultiLine" Rows="1" CssClass="form-control form-control-sm" />
                                </div>
                            </div>

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

                            <div id="divTravelSection" runat="server" class="form-row">
                                <div class="form-row">
                                    <div class="col-md-2 form-group">
                                        <label>Travel Type</label>
                                        <asp:DropDownList ID="DDL_TravelType" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Value="Train">Train</asp:ListItem>
                                            <asp:ListItem Value="Bus">Bus</asp:ListItem>
                                            <%--<asp:ListItem Value="Flight">Flight</asp:ListItem>
                                            <asp:ListItem Value="Taxi">Taxi</asp:ListItem>--%>
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
                                        <asp:TextBox ID="TXT_TravelAmount" runat="server" CssClass="form-control form-control-sm" />
                                    </div>

                                    <div class="col-md-12 form-group">
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

                            <div class="col-md-12 form-group" id="divFUContainer" runat="server">
                                <label class="col-md-4">Attach Receipt</label>
                                <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" />
                                <small class="form-text text-muted">JPG, PNG, PDF. Max 5MB. Saves to UploadedFiles/ClaimsAttachment by default.</small>
                            </div>

                            <br />
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
                                            <div class="small text-muted">Day Total</div>
                                            <div id="dvOverall" runat="server" class="h5 mb-0">
                                                <asp:Label ID="lblOverallTotal" runat="server" Font-Bold="true" ForeColor="#0033cc" Text="0.00" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <hr />
                            <h6>Existing Day Expenses</h6>

                            <asp:Repeater ID="rptExpenses" runat="server" OnItemCommand="rptExpenses_ItemCommand" OnItemDataBound="rptExpenses_ItemDataBound">
                                <HeaderTemplate>
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
                                            <%# Eval("ExpenseType") %>
                                            <span class="ml-2 expense-type-badge badge badge-light">
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
                                                ? String.Format("{0} {1} -> {2} {3}",
                                                    Eval("TravelType") ?? "",
                                                    Eval("FromPlace") ?? "",
                                                    Eval("ToPlace") ?? "",
                                                    (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                  )
                                             : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "Hotel")
                                                ? String.Format("{0} - {1} {2}",
                                                    (Eval("FromDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("FromDate"))),
                                                    (Eval("ToDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("ToDate"))),
                                                    (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                  )
                                             : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "DA")
                                                ? String.Format("{0} -> {1} {2}",
                                                    Eval("FromPlace") ?? "—",
                                                    Eval("ToPlace") ?? "—",
                
                                                     (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                  )
                                             : (Eval("Purpose") ?? "—")
                                        ) %>'>
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
                                                      (Eval("FromDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("FromDate"))),
                                                      (Eval("ToDate") == DBNull.Value ? "—" : String.Format("{0:dd-MM-yyyy}", Eval("ToDate"))),
                                                      (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                    )
                                                  : (Eval("ExpenseType") != null && Eval("ExpenseType").ToString() == "DA")
                                                    ? String.Format("{0} -> {1} {2}",
                                                        Eval("FromPlace") ?? "—",
                                                        Eval("ToPlace") ?? "—",
                                                         (Eval("Purpose") != DBNull.Value && !String.IsNullOrWhiteSpace(Convert.ToString(Eval("Purpose"))) ? " | " + Convert.ToString(Eval("Purpose")) : "")
                                                      )
                                                    : (Eval("Purpose") ?? "—")
                                                %>
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
                                            <asp:LinkButton ID="lnkEditExpense" runat="server" CssClass="btn btn-outline-primary mr-1" CommandName="EditExpense" CommandArgument='<%# Eval("ExpenseId") %>' OnClientClick="return false;" Visible="false"><i class="fa fa-edit"></i></asp:LinkButton>

                                            <asp:LinkButton ID="lnkDeleteExpense" runat="server" CssClass="btn btn-outline-danger" CommandName="DeleteExpense" CommandArgument='<%# Eval("ExpenseId") + "|" + (Eval("AttachmentPath") ?? "") + "|" + (Eval("ActivityId") ?? "") %>' OnClientClick="return confirm('Delete this expense? This action cannot be undone.');"><i class="fa fa-trash"></i></asp:LinkButton>

                                            <asp:LinkButton ID="lnkDeleteExpenseDisabled" runat="server" CssClass="btn btn-outline-secondary btn-sm disabled" Visible="false" OnClientClick="return false;" ToolTip="Cannot delete: submitted/auto-applied"><i class="fa fa-lock"></i></asp:LinkButton>

                                            <asp:Literal ID="litPolicyBadges" runat="server"></asp:Literal>
                                        </td>
                                    </tr>
                                </ItemTemplate>


                                <FooterTemplate>
                                    </tbody>
                                    </table>
                                </FooterTemplate>
                            </asp:Repeater>

                            <asp:HiddenField ID="HF_SnapPolicyMode" runat="server" />
                            <asp:HiddenField ID="HF_SnapResolvedDA" runat="server" />
                            <asp:HiddenField ID="HF_SnapHotelCap" runat="server" />
                            <asp:HiddenField ID="HF_SnapOutstationDA" runat="server" />
                            <asp:HiddenField ID="HF_SnapOwnArrangementDA" runat="server" />

                            <asp:Label ID="LBL_ExpenseMsg" runat="server" ForeColor="Red"></asp:Label>
                        </asp:Panel>
                    </div>

                    <div id="expenseDebugLog" style="max-height: 260px; overflow: auto; font-size: 12px; padding: 6px; border: 1px solid #eee; background: #fafafa;"></div>

                    <div class="modal-footer">
                        <asp:Button ID="BTN_SaveExpense" runat="server" CssClass="btn btn-primary btn-sm" ClientIDMode="Static" Text="Save Expense" OnClick="BTN_SaveExpense_Click" />
                        <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal" onclick="$('#expenseModal').modal('hide');">Close</button>
                    </div>
                </div>
            </div>
        </div>

        <div id="pageLoader" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.7); z-index: 9999; text-align: center; padding-top: 20%;">
            <i class="fa fa-spinner fa-spin fa-3x text-success"></i>
            <br />
            <span class="text-success font-weight-bold mt-2">Saving draft, please wait...</span>
        </div>

        <div class="modal fade" id="policyModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
                <div class="modal-content shadow-lg border-0 rounded-lg overflow-hidden">
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
                                <asp:Label ID="lblPolicySpecificity" runat="server" CssClass="d-block text-primary font-weight-semibold">—</asp:Label>
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
                                <asp:Label ID="lblPolicyDesigGrade" runat="server" CssClass="d-block text-dark font-weight-semibold">—</asp:Label>
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
                            <asp:Button ID="btnToggleRawJson" runat="server" CssClass="btn btn-sm btn-outline-dark mb-2"
                                OnClientClick="$('#policyJsonRaw').toggle(); return false;" Text="Toggle Raw JSON" />
                            <asp:TextBox ID="policyJsonRaw" runat="server" TextMode="MultiLine" Rows="6"
                                CssClass="form-control form-control-sm font-monospace text-dark bg-light border border-secondary"
                                Visible="false"></asp:TextBox>
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
        /* =========================================================
           Expense Modal & Page JS — ORGANIZED & COMMENTED
           - This file is a reorganized version of your original scripts.
           - NO business-logic changes were made; only grouping, ordering
             and explanatory comments were added for maintainability.
           - Keep server-side ClientID markers as-is.
         ========================================================= */

        /* ===========================
           0. Top-level document ready
           - Generic initializers (collapse links, tooltips, PRM endRequest)
           - Kept minimal so the rest of modules run safely after DOM ready
           =========================== */
        $(document).ready(function () {
            // Collapse panels: toggles .x_panel collapsed state & icon
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

            // Bootstrap tooltip initialisation
            $('[data-toggle="tooltip"]').tooltip();
        });

        // Re-init tooltips after ASP.NET partial postback (UpdatePanel)
        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                $('[data-toggle="tooltip"]').tooltip();
            });
        }

        /* ===========================
           1. Small UI helpers & toasts
           - PNotify wrapper(s), showToast fallback, small dbg helper
           - Note: original code had two showPNotify declarations (kept both to preserve call sites)
           =========================== */

        /* 1A. Simple showPNotify (global) - used in many places */
        function showPNotify(title, message, type) {
            new PNotify({
                title: title,
                text: message,
                type: type,       // 'success', 'info', 'error'
                delay: 4000,
                styling: 'bootstrap3'
            });
        }

        /* 1B. Another local showPNotify declared inside a block in original — kept for compatibility.
               (Both definitions behave the same; leaving both avoids surprises.) */
        (function () {
            'use strict';
            function showPNotifyLocal(title, message, type) {
                new PNotify({
                    title: title,
                    text: message,
                    type: type,       // 'success', 'info', 'error'
                    delay: 4000,
                    styling: 'bootstrap3'
                });
            }
            // expose local alias if needed
            window.showPNotifyLocal = showPNotifyLocal;
        }());

        /* 1C. Generic notification wrapper used as fallback (notify -> showPNotify) */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[notify] ' + msg); } catch (e) { } }

            // notify() delegates to showPNotify; fallback to alert
            function notify(title, text, type) {
                try {
                    showPNotify(title || '', text || '', type || 'notice');
                } catch (e) {
                    dbg('notify -> showPNotify failed: ' + e);
                    try { alert((title ? title + '\n' : '') + text); } catch (ex) { }
                }
            }

            // expose minimal API
            window.notify = notify;
        })();


        /* ===========================
           2. Utility helpers / DOM resolution
           - resolve / $resolve wrappers that find by ID or by ASP.NET ClientID suffix
           - parseNumber, isValidDateString, fmtAmount etc.
           =========================== */
        (function () {
            'use strict';
            // Resolve DOM element by exact id or by suffix used by ASP.NET ClientIDs
            function resolve(id) {
                var el = document.getElementById(id);
                if (el) return el;
                var sel = '[id$="_' + id + '"],[id$="' + id + '"]';
                return document.querySelector(sel) || null;
            }
            function $resolve(id) {
                var el = resolve(id);
                return el ? $(el) : $();
            }

            // Expose globally for modules below to reuse
            window.resolve = resolve;
            window.$resolve = $resolve;

            // numeric parsing helper (commas tolerated)
            function parseNumber(v) {
                if (v === null || v === undefined) return NaN;
                v = String(v).replace(/,/g, '').trim();
                return v === '' ? NaN : parseFloat(v);
            }

            // simple YYYY-MM-DD check (used by hotel date validation)
            function isValidDateString(s) {
                if (!s) return false;
                return /^\d{4}-\d{2}-\d{2}$/.test(s);
            }

            // amount formatter (2 decimals)
            function fmtAmount(v) {
                if (v === null || v === undefined) return '—';
                var s = String(v).replace(/,/g, '').trim();
                if (s === '') return '—';
                var n = parseFloat(s);
                if (isNaN(n)) return '—';
                return n.toFixed(2);
            }

            window.parseNumber = parseNumber;
            window.isValidDateString = isValidDateString;
            window.fmtAmount = fmtAmount;

        })();

        /* ===========================
           3. Style injection (validation/toast CSS)
           - Adds helpful CSS classes used by inline validation and fallback toasts
           =========================== */
        (function () {
            'use strict';
            var css = "\
              .so-err { border-color: #e55353 !important; box-shadow: 0 0 0 0.15rem rgba(229,83,83,0.15) !important; }\
              .so-err-msg { color:#e55353; font-size:12px; margin-top:4px; }\
              .so-toast-wrap { position: fixed; top: 16px; right: 16px; z-index: 2100; }\
              .so-toast { background: #2b2f33; color: #fff; padding: 10px 14px; margin-bottom:8px; border-radius:4px; box-shadow: 0 6px 18px rgba(0,0,0,0.15); min-width:260px; }\
              .so-toast .so-toast-title { font-weight:600; margin-bottom:4px; }\
              .so-toast.success { background:#1b7a3a; }\
              .so-toast.error { background:#a81b1b; }";
            var s = document.createElement('style'); s.appendChild(document.createTextNode(css)); document.head.appendChild(s);
        })();

        /* ===========================
           4. Inline error helpers (showInlineError / removeInlineError)
           - Present a standard way to add/remove inline validation UI
           - Used by wrapped validator and other places
           =========================== */
        (function () {
            'use strict';
            function showInlineError(el, msg) {
                if (!el) return;
                var $el = (el.jquery ? el : $(el));
                removeInlineError($el);
                $el.addClass('so-err');
                var msgEl = document.createElement('div'); msgEl.className = 'so-err-msg'; msgEl.innerText = msg;
                try {
                    var parent = $el.closest('.form-group');
                    if (parent && parent.length) {
                        $(msgEl).appendTo(parent);
                    } else {
                        $el.after(msgEl);
                    }
                } catch (e) {
                    $el.after(msgEl);
                }
            }
            function removeInlineError(jqOrEl) {
                var $el = (jqOrEl.jquery ? jqOrEl : $(jqOrEl));
                $el.removeClass('so-err');
                var parent = $el.closest('.form-group');
                if (parent && parent.length) parent.find('.so-err-msg').remove();
                else $el.nextAll('.so-err-msg').first().remove();
            }
            function clearAllInlineErrors() {
                $('.so-err').removeClass('so-err');
                $('.so-err-msg').remove();
            }

            window.showInlineError = showInlineError;
            window.removeInlineError = removeInlineError;
            window.clearAllInlineErrors = clearAllInlineErrors;
        })();

        /* ===========================
           5. Policy Mode UI + application
           - Creates the policy-mode selector UI (ensurePolicyModeControl)
           - applyPolicyMode: applies settings based on HF_ResolvedPolicyMode and related hidden fields
           - applyPolicyModeFromServer: idempotent server-invoked method to set mode + dispatch policy.updated
           - All event dispatches preserved as in original code
           =========================== */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[policy] ' + msg); } catch (e) { } }

            // ensure selector + apply button inside the modal summary (adds #SEL_PolicyMode and #BTN_ApplyPolicyMode)
            function ensurePolicyModeControl() {
                var summary = document.querySelector('.modal-body .d-flex');
                if (!summary) summary = document.querySelector('#expenseModal .modal-body'); // fallback
                if (!summary) return;

                if (document.getElementById('SEL_PolicyMode')) return; // already exists

                var container = document.createElement('div');
                container.style.marginLeft = 'auto';
                container.style.display = 'flex';
                container.style.alignItems = 'center';
                container.style.gap = '8px';

                var sel = document.createElement('select');
                sel.id = 'SEL_PolicyMode';
                sel.className = 'form-control form-control-sm';
                sel.style.width = '160px';
                ['HQ', 'Ex-HQ', 'Outstation', 'OwnArrangement'].forEach(function (m) {
                    var o = document.createElement('option'); o.value = m; o.text = m === 'OwnArrangement' ? 'Own Arrangement' : m; sel.appendChild(o);
                });

                var btn = document.createElement('button');
                btn.type = 'button';
                btn.id = 'BTN_ApplyPolicyMode';
                btn.className = 'btn btn-sm btn-outline-primary';
                btn.innerText = 'Apply Mode';

                container.appendChild(sel);
                container.appendChild(btn);

                summary.parentNode.appendChild(container);

                // wire apply behaviour: set hidden field, dispatch event, toast
                $(btn).on('click', function () {
                    var selVal = sel.value;
                    dbg('User applied policy mode: ' + selVal);
                    var hf = resolve('HF_ResolvedPolicyMode');
                    if (hf) {
                        try {
                            if (hf.tagName.toUpperCase() === 'INPUT' || hf.tagName.toUpperCase() === 'SELECT' || hf.tagName.toUpperCase() === 'TEXTAREA') hf.value = selVal;
                            else $(hf).val(selVal);
                        } catch (e) { dbg('set hf failed: ' + e); }
                    }
                    var $hf = $resolve('HF_ResolvedPolicyMode'); if ($hf && $hf.length) $hf.val(selVal);

                    var ev = new CustomEvent('policy.updated', { detail: { mode: selVal, source: 'client' } });
                    document.dispatchEvent(ev);

                    showToast('Policy Mode', 'Applied "' + (selVal === 'OwnArrangement' ? 'Own Arrangement' : selVal) + '" locally. UI updated.', 'success', 2500);
                });

                // preselect according to HF if exists
                var hfV = $resolve('HF_ResolvedPolicyMode').val();
                if (hfV) sel.value = hfV;
            }

            // applyPolicyMode: read hidden fields and enable/disable UI & prefill according to policy mode
            function applyPolicyMode() {
                var $hfPolicyMode = $resolve('HF_ResolvedPolicyMode');
                var mode = ($hfPolicyMode && $hfPolicyMode.length) ? $.trim($hfPolicyMode.val()) : '';
                if (!mode) mode = 'HQ';
                dbg('[PolicyMode] Detected -> ' + mode);

                var $hfDA = $resolve('HF_ResolvedDA'), $hfOutDA = $resolve('HF_OutstationDA'), $hfOwnDA = $resolve('HF_OwnArrangementDA'), $hfHotelCap = $resolve('HF_ResolvedHotelCap');
                var $ddlType = $resolve('DDL_ExpenseType');
                var $txtDA = $resolve('TXT_DAAmount');

                var daVal = ($hfDA && $hfDA.length && $hfDA.val()) ? $hfDA.val() : ($hfOutDA && $hfOutDA.length && $hfOutDA.val() ? $hfOutDA.val() : ($hfOwnDA && $hfOwnDA.length && $hfOwnDA.val() ? $hfOwnDA.val() : '—'));
                var hotelCapVal = ($hfHotelCap && $hfHotelCap.length && $hfHotelCap.val()) ? $hfHotelCap.val() : '—';
                var $spDAValue = resolve('spDAValue'), $spHotelCap = resolve('spHotelCap'), $spAllowedTypes = resolve('spAllowedTypes'), $spPolicyNote = resolve('spPolicyNote');

                if ($spDAValue) $spDAValue.innerText = daVal;
                if ($spHotelCap) $spHotelCap.innerText = hotelCapVal;
                if ($spPolicyNote) $spPolicyNote.innerText = '';

                // Reset options enabled by default
                try { if ($ddlType && $ddlType.length) $ddlType.find('option').prop('disabled', false); } catch (e) { dbg('ddl reset error: ' + e); }

                var allowed = [];
                if (mode === 'HQ') {
                    allowed = ['DA'];
                    if ($ddlType && $ddlType.length) {
                        $ddlType.find('option[value="Hotel"]').prop('disabled', true);
                        $ddlType.find('option[value="Travel"]').prop('disabled', true);
                        var cur = $ddlType.val();
                        if (cur !== 'DA') { $ddlType.val('DA').trigger('change'); }
                    }
                    if ($txtDA && $txtDA.length && daVal && daVal !== '—') $txtDA.val(daVal);
                    if ($spPolicyNote) $spPolicyNote.innerText = 'HQ mode: Only DA allowed; Hotel/Travel not applicable.';
                }
                    // --- add this block just after the existing `if (mode === 'HQ') { ... }` block ---
                else if (mode === 'Ex-HQ') {
                    // Ex-HQ: DA and Travel allowed; Hotel not applicable
                    allowed = ['DA', 'Travel'];

                    // Disable Hotel option in the dropdown, but keep DA & Travel enabled
                    if ($ddlType && $ddlType.length) {
                        $ddlType.find('option[value="Hotel"]').prop('disabled', true);
                        $ddlType.find('option[value="DA"]').prop('disabled', false);
                        $ddlType.find('option[value="Travel"]').prop('disabled', false);

                        // If current selection is Hotel (now disabled), force a valid selection (prefer DA)
                        var curSel = $ddlType.val();
                        if (curSel === 'Hotel') {
                            // Prefer DA if a DA value exists (prefill handled below), otherwise pick Travel
                            if ($txtDA && $txtDA.length) {
                                $ddlType.val('DA').trigger('change');
                            } else {
                                $ddlType.val('Travel').trigger('change');
                            }
                        }
                    }

                    // Prefill DA textbox if resolved DA exists (uses same HF_ResolvedDA as HQ)
                    if ($hfDA && $hfDA.length && $txtDA && $txtDA.length && $hfDA.val() && $hfDA.val() !== '—') {
                        $txtDA.val($hfDA.val());
                    }

                    // Optional short policy note visible in policy summary
                    if ($spPolicyNote) $spPolicyNote.innerText = 'Ex-HQ: DA and Travel allowed; Hotel not applicable.';
                }


                else if (mode === 'Outstation') {
                    allowed = ['Hotel', 'Travel'];
                    if ($ddlType && $ddlType.length) $ddlType.find('option[value="DA"]').prop('disabled', true);
                    if ($hfOutDA && $hfOutDA.length && $txtDA && $txtDA.length) $txtDA.val($hfOutDA.val());
                    if ($spPolicyNote) $spPolicyNote.innerText = 'Outstation policy: DA replaced with Outstation DA.';
                } else if (mode === 'OwnArrangement') {
                    allowed = ['Travel'];
                    if ($ddlType && $ddlType.length) {
                        $ddlType.find('option[value="DA"]').prop('disabled', true);
                        $ddlType.find('option[value="Hotel"]').prop('disabled', true);
                        var cur = $ddlType.val();
                        if (cur !== 'Travel') { $ddlType.val('Travel').trigger('change'); }
                    }
                    if ($hfOwnDA && $hfOwnDA.length && $txtDA && $txtDA.length) $txtDA.val($hfOwnDA.val());
                    if ($spPolicyNote) $spPolicyNote.innerText = 'Own arrangement: OS DA & Travel Claims Allowed; Hotel not applicable.';
                } else {
                    allowed = ['DA', 'Hotel', 'Travel'];
                }

                if ($spAllowedTypes) $spAllowedTypes.innerText = 'Allowed: ' + (allowed.join(', ') || '—');
                dbg('policy applied. Allowed: ' + allowed.join(', '));
            }

            // applyPolicyModeFromServer: idempotent server-driven setter that dispatches policy.updated and tries to apply soon
            function applyPolicyModeFromServer(mode) {
                console.log("%c[applyPolicyModeFromServer] Invoked from server.", "color:#2196f3;font-weight:bold;");
                console.log("   ► Parameter (mode):", mode);

                try {
                    mode = mode || '';

                    if (window.__so_policy_last_applied && window.__so_policy_last_applied === mode) {
                        console.log("   ℹ Mode already applied previously, skipping re-apply:", mode);
                        return;
                    }

                    window.__so_policy_pending_mode = mode;

                    // 1) Set HF_ResolvedPolicyMode if present
                    var hf = document.getElementById('<%= HF_ResolvedPolicyMode.ClientID %>');
                    if (hf) {
                        try { hf.value = mode; console.log("   ✔ Hidden field set to:", hf.value); } catch (e) { console.warn("   ⚠ Failed to set HF:", e); }
                    } else {
                        console.warn("   ⚠ Hidden field not found (HF_ResolvedPolicyMode).");
                    }

                    // 2) save to localStorage (best-effort)
                    try { localStorage.setItem('so_policy_mode', mode); console.log("   ✔ Saved to localStorage → so_policy_mode:", mode); } catch (e) { console.warn("   ⚠ localStorage set failed:", e); }

                    // 3) dispatch event
                    try {
                        var ev = new CustomEvent('policy.updated', { detail: { mode: mode, source: 'server' } });
                        document.dispatchEvent(ev);
                        console.log("   ✔ Dispatched 'policy.updated' event");
                    } catch (ex) {
                        console.error("   ❌ Event dispatch failed:", ex);
                    }

                    // Attempt immediate apply (poll + MutationObserver fallback)
                    (function waitAndApply() {
                        var applied = false;
                        var observer = null;
                        var pollIv = null;

                        function trySetAndApply() {
                            try {
                                var sel = document.getElementById('SEL_PolicyMode')
                                       || document.querySelector('[id$="_SEL_PolicyMode"]')
                                       || document.querySelector('[id$="SEL_PolicyMode"]');

                                if (sel) {
                                    try { sel.value = mode; console.log("   ✔ Selector found, value set to:", sel.value); } catch (e) { console.warn("   ⚠ Could not set selector value:", e); }
                                }

                                if (typeof applyPolicyMode === 'function') {
                                    try { applyPolicyMode(); console.log("   ✔ applyPolicyMode() called."); } catch (e) { console.warn("   ⚠ applyPolicyMode() call failed:", e); }
                                }

                                if (sel || typeof applyPolicyMode === 'function') {
                                    applied = true;
                                    window.__so_policy_last_applied = mode;
                                    window.__so_policy_pending_mode = null;
                                    cleanup();
                                }
                            } catch (err) {
                                console.error("   ❌ trySetAndApply error:", err);
                            }
                        }

                        function cleanup() {
                            try { if (observer) { observer.disconnect(); observer = null; console.log("   ✔ MutationObserver disconnected."); } } catch (e) { }
                            try { if (pollIv) { clearInterval(pollIv); pollIv = null; console.log("   ✔ Polling cleared."); } } catch (e) { }
                        }

                        trySetAndApply();

                        if (applied) return;

                        var elapsed = 0;
                        var pollInterval = 150;
                        var timeout = 4000;
                        pollIv = setInterval(function () {
                            elapsed += pollInterval;
                            trySetAndApply();
                            if (window.__so_policy_last_applied === mode) return;
                            if (elapsed >= timeout) {
                                cleanup();
                                console.warn("   ⚠ Timed out waiting for selector/applyPolicyMode. Mode queued for later.");
                                return;
                            }
                        }, pollInterval);

                        try {
                            observer = new MutationObserver(function (mutations) {
                                trySetAndApply();
                                if (window.__so_policy_last_applied === mode) { /* cleanup called by trySetAndApply */ }
                            });
                            observer.observe(document.body, { childList: true, subtree: true });
                            console.log("   ✔ MutationObserver attached to body.");
                        } catch (obsErr) {
                            console.warn("   ⚠ MutationObserver not available or failed:", obsErr);
                        }
                    })();

                    // attach modal handler to apply queued mode when modal shown
                    (function attachModalHandlerIfNeeded() {
                        try {
                            if (window.__so_policy_modal_handler_attached) return;
                            window.__so_policy_modal_handler_attached = true;

                            $('#expenseModal').on('shown.bs.modal', function () {
                                var pending = window.__so_policy_pending_mode;
                                if (pending) {
                                    console.log('▶ modal shown — applying queued policy mode:', pending);
                                    try { applyPolicyModeFromServer(pending); } catch (e) { console.error('   ❌ modal apply failed', e); }
                                }
                            });
                        } catch (e) { /* ignore if jQuery/modal not present */ }
                    })();

                    console.log("%c[applyPolicyModeFromServer] Completed (scheduling follow-ups).", "color:green;font-weight:bold;");
                } catch (err) {
                    console.error("❌ [applyPolicyModeFromServer] Exception:", err);
                }
            }

            // Expose key APIs
            window.ensurePolicyModeControl = ensurePolicyModeControl;
            window.applyPolicyMode = applyPolicyMode;
            window.applyPolicyModeFromServer = applyPolicyModeFromServer;

            // Bind: listen for policy.updated events so other modules can react
            document.addEventListener('policy.updated', function (e) {
                dbg('policy.updated event -> applyPolicyMode & updateVisibility');
                // re-resolve hidden fields happens inside applyPolicyMode which uses resolve()
                applyPolicyMode();
                // updateVisibility is defined later; dispatch a custom event to notify visibility module
                var ev = new CustomEvent('policy.visibility.refresh');
                document.dispatchEvent(ev);
            });

        })();

        /* ===========================
           6. Expense modal visibility & behavior
           - updateVisibility: shows/hides DA/Hotel/Travel sections based on DDL_ExpenseType and checkboxes
           - updateAutoDADisplay: shows auto-DA block depending on policy HF values (Own/Outstation)
           - bindHandlers: hooks change events for DDL and invoice checkbox etc.
           - attachSaveHook: sets OnClientClick for BTN_SaveExpense to run client validator
           =========================== */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[visibility] ' + msg); } catch (e) { } }

            // resolve frequently used elements (will be re-resolved on modal show)
            function resolveElements() {
                return {
                    $ddlType: $resolve('DDL_ExpenseType'),
                    $divDA: $resolve('divDASection'),
                    $divHotel: $resolve('divHotelSection'),
                    $divTravel: $resolve('divTravelSection'),
                    $chkTravelInvoice: $resolve('CHK_TravelInvoice'),
                    $divTravelInvoiceNo: $resolve('divTravelInvoiceNo'),
                    $divFUContainer: $resolve('divFUContainer'),
                    $hfPolicyMode: $resolve('HF_ResolvedPolicyMode'),
                    $hfDA: $resolve('HF_ResolvedDA'),
                    $hfOutDA: $resolve('HF_OutstationDA'),
                    $hfOwnDA: $resolve('HF_OwnArrangementDA'),
                    $hfHotelCap: $resolve('HF_ResolvedHotelCap'),
                    $txtDA: $resolve('TXT_DAAmount'),
                    $txtHotelAmount: $resolve('TXT_HotelAmount'),
                    $txtHotelFrom: $resolve('TXT_HotelFromDate'),
                    $txtHotelTo: $resolve('TXT_HotelToDate'),
                    $txtHotelBillNo: $resolve('TXT_HotelBillNo'),
                    $txtTravelDate: $resolve('TXT_TravelDate'),
                    $txtFromPlace: $resolve('TXT_FromPlace'),
                    $txtToPlace: $resolve('TXT_ToPlace'),
                    $txtTravelAmount: $resolve('TXT_TravelAmount'),
                    $txtTravelInvoiceNo: $resolve('TXT_TravelInvoiceNo'),
                    $fu: $resolve('FU_Attachment'),
                    $btnSave: $resolve('BTN_SaveExpense'),
                    $spDAValue: resolve('spDAValue'),
                    $spHotelCap: resolve('spHotelCap'),
                    $spAllowedTypes: resolve('spAllowedTypes'),
                    $spPolicyNote: resolve('spPolicyNote')
                };
            }

            // safe show/hide helpers (try jQuery then fallback)
            function safeHide($jq) {
                if (!$jq || !$jq.length) return;
                try { $jq.hide(); return; } catch (e) { }
                try { $jq.css('display', 'none'); return; } catch (e) { }
                var el = $jq[0];
                if (el) el.style.display = 'none';
            }
            function safeShow($jq) {
                if (!$jq || !$jq.length) return;
                try { $jq.show(); return; } catch (e) { }
                try { $jq.css('display', ''); return; } catch (e) { }
                var el = $jq[0];
                if (el) el.style.display = '';
            }

            // update visibility based on selected type + invoice checkbox
            function updateVisibility() {
                var els = resolveElements();
                var $ddlType = els.$ddlType;
                if (!$ddlType || !$ddlType.length) { dbg('DDL not found, aborting updateVisibility'); return; }

                var type = $ddlType.val();
                dbg('Applying visibility for ' + type);

                if (type === 'DA') {
                    safeShow(els.$divDA); safeHide(els.$divHotel); safeHide(els.$divTravel); safeHide(els.$divFUContainer); safeHide(els.$divTravelInvoiceNo); 
                } else if (type === 'Hotel') {
                    safeHide(els.$divDA); safeShow(els.$divHotel); safeHide(els.$divTravel); safeShow(els.$divFUContainer); safeHide(els.$divTravelInvoiceNo); enableAttachmentInput();
                } else if (type === 'Travel') {
                    safeHide(els.$divDA); safeHide(els.$divHotel); safeShow(els.$divTravel);
                    if (els.$chkTravelInvoice && els.$chkTravelInvoice.length && els.$chkTravelInvoice.prop('checked')) {
                        safeShow(els.$divFUContainer); safeShow(els.$divTravelInvoiceNo); enableAttachmentInput();
                    } else {
                        safeHide(els.$divFUContainer); safeHide(els.$divTravelInvoiceNo);
                    }
                } else {
                    // fallback: show minimal (DA)
                    safeShow(els.$divDA); safeHide(els.$divHotel); safeHide(els.$divTravel); safeHide(els.$divFUContainer); safeHide(els.$divTravelInvoiceNo);
                }
            }

            // invoice checkbox toggle handler
            function onInvoiceToggle() {
                var els = resolveElements();
                if (!els.$chkTravelInvoice || !els.$chkTravelInvoice.length) return;
                if (els.$chkTravelInvoice.prop('checked')) {
                    safeShow(els.$divFUContainer); safeShow(els.$divTravelInvoiceNo); enableAttachmentInput();
                } else {
                    safeHide(els.$divFUContainer); safeHide(els.$divTravelInvoiceNo);
                    // Optionally clear selection when hidden:
                    var $fu = $resolve('FU_Attachment');
                    if ($fu && $fu.length) { try { $fu.val(null); $fu.next('.file-chosen').text('No file chosen'); } catch (e) { } }
                }
            }

            // binds DDL change + invoice checkbox + refresh policy button
            function bindHandlers() {
                var els = resolveElements();
                if (els.$ddlType && els.$ddlType.length) {
                    els.$ddlType.off('change.expense').on('change.expense', function () { updateVisibility(); });
                    dbg('bound change on DDL_ExpenseType');
                } else dbg('DDL not present - cannot bind change');

                if (els.$chkTravelInvoice && els.$chkTravelInvoice.length) {
                    els.$chkTravelInvoice.off('change.expense').on('change.expense', onInvoiceToggle);
                    dbg('bound change on CHK_TravelInvoice');
                } else dbg('CHK_TravelInvoice not present - invoice toggle not bound');

                var $btnRefresh = $resolve('btnRefreshPolicy');
                if ($btnRefresh && $btnRefresh.length) {
                    $btnRefresh.off('click.policy').on('click.policy', function () {
                        dbg('Refresh policy clicked');
                        // reapply policy + update visibility (server may have changed HF values)
                        applyPolicyMode();
                        updateVisibility();
                    });
                }
            }

            function enableAttachmentInput() {
                try {
                    var $fu = $resolve('FU_Attachment');
                    if ($fu && $fu.length) {
                        // remove disabled attribute (server may set it); also ensure input is not readonly
                        $fu.prop('disabled', false);
                        $fu.removeAttr('disabled');
                        try { $fu.prop('readonly', false); $fu.removeAttr('readonly'); } catch (e) { }
                        // If a custom "file-chosen" placeholder was added, ensure it updates on change
                        $fu.off('change.ensure').on('change.ensure', function () {
                            var name = this.files && this.files.length ? this.files[0].name : 'No file chosen';
                            $(this).next('.file-chosen').text(name);
                        });
                    }
                } catch (e) { console && console.log && console.log('enableAttachmentInput error', e); }
            }

            // attach client-side validation to Save button to prevent server trip when invalid
            function attachSaveHook() {
                try {
                    var $btnSave = $resolve('BTN_SaveExpense');
                    if (!$btnSave || !$btnSave.length) { dbg('Save button not found to attach client validator'); return; }
                    $btnSave.attr('onclick', 'return (function(){ return validateExpenseForm(); })();');
                    dbg('attached OnClientClick to BTN_SaveExpense');
                } catch (e) { dbg('attachSaveHook error: ' + e); }
            }

            // Auto-DA display block: shows auto DA information depending on HF fields and policy mode
            function updateAutoDADisplay() {
                var container = resolve('divAutoDAInfo');
                if (!container) { dbg('divAutoDAInfo not present'); return; }

                // read policy mode from HF_ResolvedPolicyMode
                var hfMode = '';
                var hfEl = resolve('HF_ResolvedPolicyMode');
                if (hfEl && (hfEl.value !== undefined)) hfMode = String(hfEl.value).trim();
                if (!hfMode) {
                    var $hf = $resolve('HF_ResolvedPolicyMode');
                    if ($hf && $hf.length) hfMode = String($hf.val() || '').trim();
                }
                hfMode = (hfMode || '').toLowerCase();

                // read outstation / own arrangement DA HFs
                var outV = '';
                var ownV = '';
                var outEl = resolve('HF_OutstationDA');
                var ownEl = resolve('HF_OwnArrangementDA');
                if (outEl && (outEl.value !== undefined)) outV = outEl.value;
                else { var $o = $resolve('HF_OutstationDA'); if ($o && $o.length) outV = $o.val(); }
                if (ownEl && (ownEl.value !== undefined)) ownV = ownEl.value;
                else { var $w = $resolve('HF_OwnArrangementDA'); if ($w && $w.length) ownV = $w.val(); }

                var hasOut = outV !== undefined && outV !== null && String(outV).trim() !== '' && String(outV).trim() !== '0' && String(outV).trim() !== '—';
                var hasOwn = ownV !== undefined && ownV !== null && String(ownV).trim() !== '' && String(ownV).trim() !== '0' && String(ownV).trim() !== '—';

                if (hfMode === 'outstation' && hasOut) {
                    container.style.display = '';
                    var amtEl = resolve('spAutoDAAmount'), typeEl = resolve('spAutoDAType');
                    if (amtEl) amtEl.innerText = '₹ ' + fmtAmount(outV);
                    if (typeEl) typeEl.innerText = 'Outstation DA (auto)';
                    dbg('AutoDA shown: Outstation -> ' + outV);
                    return;
                }

                if (hfMode === 'ownarrangement' && hasOwn) {
                    container.style.display = '';
                    var a2 = resolve('spAutoDAAmount'), t2 = resolve('spAutoDAType');
                    if (a2) a2.innerText = '₹ ' + fmtAmount(ownV);
                    if (t2) t2.innerText = 'Own Arrangement DA (auto)';
                    dbg('AutoDA shown: OwnArrangement -> ' + ownV);
                    return;
                }

                container.style.display = 'none';
                dbg('AutoDA hidden (mode=' + (hfMode || '') + ')');
            }

            // init called on ready and on modal show (resolves elements again because UpdatePanel may replace DOM)
            function init() {
                bindHandlers();
                applyPolicyMode(); // apply policy defaults to UI
                updateVisibility();
                enableAttachmentInput();
                attachSaveHook();
                updateAutoDADisplay();
            }

            $(document).ready(function () {
                dbg('init expense modal script');
                init();
            });

            // re-resolve and init when modal shown (handles UpdatePanel DOM replacement)
            $('#expenseModal').on('shown.bs.modal', function () {
                dbg('modal shown -> rebind + policy apply + update');
                init();
            });

            $(document).on('change', '[id$="_CHK_TravelInvoice"], #CHK_TravelInvoice, [id$="CHK_TravelInvoice"]', function (e) {
                try {
                    // call the same toggle logic used elsewhere (uses fresh resolution)
                    onInvoiceToggle();
                    // Also update visibility to ensure invoice-no and file container show/hide correctly
                    updateVisibility();
                } catch (err) {
                    console && console.log && console.log('invoice toggle delegated handler error', err);
                }
            });

            // react to external policy visibility refresh event (dispatched earlier)
            document.addEventListener('policy.visibility.refresh', function () {
                updateAutoDADisplay();
                updateVisibility();
            });

            // expose for debugging if needed
            window.updateVisibility = updateVisibility;
            window.updateAutoDADisplay = updateAutoDADisplay;
            window.attachSaveHook = attachSaveHook;

        })();

        /* ===========================
           7. Expense form client-side validation
           - validateExpenseForm: heavy-lifter with separate blocks for DA, Hotel, Travel
           - All original checks & messages preserved exactly
           - Exposes window.validateExpenseForm
           =========================== */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[validator] ' + msg); } catch (e) { } }

            // helper to fail with notify and return false
            function fail(msg) { notify('Validation error', msg, 'error'); dbg('validation fail: ' + msg); return false; }

            window.validateExpenseForm = function () {
                try {
                    var $ddlType = $resolve('DDL_ExpenseType');
                    var type = ($ddlType && $ddlType.length) ? $ddlType.val() : 'DA';
                    dbg('validateExpenseForm -> type=' + type);

                    if (type === 'DA') {
                        var $txtDAAmount = $resolve('TXT_DAAmount');
                        var $txtDAFromPlace = $resolve('TXT_DAFromPlace');
                        var $txtDAToPlace = $resolve('TXT_DAToPlace');
                        var $txtTravelDesc = $resolve('TXT_TravelDescription');

                        var daAmt = parseNumber($txtDAAmount.val().trim());
                        var fromPlace = $txtDAFromPlace.val().trim();
                        var toPlace = $txtDAToPlace.val().trim();
                        var travelDesc = $txtTravelDesc.val().trim();

                        if (isNaN(daAmt) || daAmt <= 0) return fail('DA Amount is required and must be greater than 0.');
                        if (!fromPlace) return fail('Travel From Place is required for DA.');
                        if (fromPlace.length > 100) return fail('From Place cannot exceed 100 characters.');
                        if (!toPlace) return fail('Travel To Place is required for DA.');
                        if (toPlace.length > 100) return fail('To Place cannot exceed 100 characters.');
                        if (fromPlace.toLowerCase() === toPlace.toLowerCase()) return fail('From Place and To Place cannot be the same.');
                        if (!travelDesc) return fail('Travel Description is required for DA.');
                        if (travelDesc.length < 5) return fail('Travel Description must be at least 5 characters.');
                        return true;
                    }

                    if (type === 'Hotel') {
                        var $txtHotelFrom = $resolve('TXT_HotelFromDate');
                        var $txtHotelTo = $resolve('TXT_HotelToDate');
                        var $txtHotelBillNo = $resolve('TXT_HotelBillNo');
                        var $txtHotelAmount = $resolve('TXT_HotelAmount');
                        var purpose = ($resolve('TXT_HotelPurpose') && $resolve('TXT_HotelPurpose').length) ? $resolve('TXT_HotelPurpose').val().trim() : '';

                        var from = ($txtHotelFrom && $txtHotelFrom.length) ? $txtHotelFrom.val() : '';
                        var to = ($txtHotelTo && $txtHotelTo.length) ? $txtHotelTo.val() : '';

                        if (!isValidDateString(from)) return fail('Stay From Date is required (yyyy-mm-dd).');
                        if (!isValidDateString(to)) return fail('Stay To Date is required (yyyy-mm-dd).');

                        var fromDate = new Date(from);
                        var toDate = new Date(to);
                        if (fromDate > toDate) return fail('Stay From Date cannot be after Stay To Date.');

                        var dayMs = 24 * 60 * 60 * 1000;
                        var diffMs = toDate.setHours(0, 0, 0, 0) - fromDate.setHours(0, 0, 0, 0);
                        var diffDays = Math.floor(diffMs / dayMs);
                        var days = diffDays >= 0 ? diffDays + 1 : 1;

                        var billNo = ($txtHotelBillNo && $txtHotelBillNo.length) ? $txtHotelBillNo.val().trim() : '';
                        if (!billNo) return fail('Bill/Invoice No is required.');
                        var hamt = parseNumber(($txtHotelAmount && $txtHotelAmount.length) ? $txtHotelAmount.val() : '');
                        if (isNaN(hamt) || hamt <= 0) return fail('Hotel amount is required and must be greater than 0.');

                        if (!purpose) return fail('Purpose of Stay is required.');

                        var $fu = $resolve('FU_Attachment');
                        if ($fu && $fu.length) {
                            var files = $fu[0].files;
                            if (!files || files.length === 0) return fail('Please attach a valid hotel receipt.');
                            var f = files[0];
                            var maxBytes = 5 * 1024 * 1024;
                            if (f.size > maxBytes) return fail('Attachment size must be ≤ 5 MB.');
                            var allowedExt = ['jpg', 'jpeg', 'png', 'pdf'];
                            var ext = (f.name || '').split('.').pop().toLowerCase();
                            if (allowedExt.indexOf(ext) === -1) return fail('Attachment must be JPG, PNG, or PDF.');
                        } else {
                            return fail('Attachment control not found. Please re-upload receipt.');
                        }

                        var cap = parseNumber(($resolve('HF_ResolvedHotelCap') && $resolve('HF_ResolvedHotelCap').length) ? $resolve('HF_ResolvedHotelCap').val() : '');
                        if (!isNaN(cap) && cap > 0) {
                            var allowedTotal = cap * days;
                            if (hamt > allowedTotal) {
                                return fail('Hotel amount exceeds per-day cap of ₹' + cap.toFixed(2) +
                                    ' (Allowed ₹' + allowedTotal.toFixed(2) + ' for ' + days + ' day(s)).');
                            }
                        }

                        return true;
                    }

                    if (type === 'Travel') {
                        var $txtFromPlace = $resolve('TXT_FromPlace');
                        var $txtToPlace = $resolve('TXT_ToPlace');
                        var $txtTravelAmount = $resolve('TXT_TravelAmount');
                        var $chkTravelInvoice = $resolve('CHK_TravelInvoice');

                        var fromPlace = ($txtFromPlace && $txtFromPlace.length) ? $txtFromPlace.val().trim() : '';
                        var toPlace = ($txtToPlace && $txtToPlace.length) ? $txtToPlace.val().trim() : '';

                        if (!fromPlace) { if ($txtFromPlace && $txtFromPlace.length) $txtFromPlace.focus(); return fail('From Place is required for Travel.'); }
                        if (!toPlace) { if ($txtToPlace && $txtToPlace.length) $txtToPlace.focus(); return fail('To Place is required for Travel.'); }

                        var travAmt = parseNumber(($txtTravelAmount && $txtTravelAmount.length) ? $txtTravelAmount.val() : '');
                        if (isNaN(travAmt) || travAmt <= 0) { if ($txtTravelAmount && $txtTravelAmount.length) $txtTravelAmount.focus(); return fail('Travel Amount is required and must be greater than 0.'); }

                        var $txtTravelPurpose = $resolve('TXT_TravelPurpose');
                        var travelPurpose = ($txtTravelPurpose && $txtTravelPurpose.length) ? $txtTravelPurpose.val().trim() : ($txtTravelPurpose && $txtTravelPurpose.value ? $txtTravelPurpose.value.trim() : '');
                        if (!travelPurpose) { try { if ($txtTravelPurpose && $txtTravelPurpose.length) $txtTravelPurpose.focus(); } catch (e) { } return fail('Purpose of Travel is required.'); }

                        if ($chkTravelInvoice && $chkTravelInvoice.length && $chkTravelInvoice.prop('checked')) {
                            var invNo = ($resolve('TXT_TravelInvoiceNo') && $resolve('TXT_TravelInvoiceNo').length) ? $resolve('TXT_TravelInvoiceNo').val().trim() : '';
                            if (!invNo) { if ($resolve('TXT_TravelInvoiceNo') && $resolve('TXT_TravelInvoiceNo').length) $resolve('TXT_TravelInvoiceNo').focus(); return fail('Invoice No is required when "With Invoice" is checked.'); }

                            var $fu2 = $resolve('FU_Attachment');
                            if ($fu2 && $fu2.length) {
                                var files = $fu2[0].files;
                                if (!files || files.length === 0) { try { $fu2.focus(); } catch (e) { } return fail('Please attach invoice/receipt when "With Invoice" is checked.'); }
                                var f = files[0];
                                var maxBytes = 5 * 1024 * 1024;
                                if (f.size > maxBytes) { try { $fu2.focus(); } catch (e) { } return fail('Attachment size must be <= 5MB.'); }
                                var allowedExt = ['jpg', 'jpeg', 'png', 'pdf'];
                                var name = f.name || '';
                                var ext = name.split('.').pop().toLowerCase();
                                if (allowedExt.indexOf(ext) === -1) { try { $fu2.focus(); } catch (e) { } return fail('Attachment must be JPG, PNG or PDF.'); }
                            } else {
                                return fail('Attachment control not found. Please re-open the form and attach invoice.');
                            }
                        }
                        return true;
                    }

                    // fallback allow
                    return true;

                } catch (ex) {
                    dbg('validateExpenseForm exception: ' + ex);
                    notify('Validation', 'Validation failed unexpectedly. See console.', 'error');
                    return false;
                }
            };
        })();

        /* ===========================
           8. Validator wrapper & UX helpers
           - wrapValidator wraps existing validateExpenseForm to add inline highlighting
           - ensurePolicyModeControl is called here to add the selector to DOM
           =========================== */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[wrapValidator] ' + msg); } catch (e) { } }

            function wrapValidator() {
                var original = window.validateExpenseForm;
                if (typeof original !== 'function') {
                    dbg('No original validateExpenseForm found to wrap; creating lightweight validator stub.');
                    window.validateExpenseForm = function () { return true; };
                    return;
                }

                window.validateExpenseForm = function () {
                    clearAllInlineErrors();
                    var ok = original(); // run original validator (keeps messages + notify)
                    if (ok) return true;

                    // Heuristics to highlight first invalid field (best-effort)
                    try {
                        var t = $resolve('DDL_ExpenseType').val();
                        if (t === 'Hotel') {
                            var hAmt = $resolve('TXT_HotelAmount'), from = $resolve('TXT_HotelFromDate'), to = $resolve('TXT_HotelToDate');
                            if (hAmt && hAmt.length && (!hAmt.val() || parseFloat(hAmt.val()) <= 0)) { showInlineError(hAmt, 'Hotel amount is required and must be greater than 0.'); hAmt.focus(); return false; }
                            if (from && from.length && !from.val()) { showInlineError(from, 'From date required.'); from.focus(); return false; }
                            if (to && to.length && !to.val()) { showInlineError(to, 'To date required.'); to.focus(); return false; }
                        } else if (t === 'Travel') {
                            var tAmt = $resolve('TXT_TravelAmount'), fromPlace = $resolve('TXT_FromPlace'), toPlace = $resolve('TXT_ToPlace'), tdate = $resolve('TXT_TravelDate');
                            if (tdate && tdate.length && !tdate.val()) { showInlineError(tdate, 'Travel date required'); tdate.focus(); return false; }
                            if (fromPlace && fromPlace.length && !fromPlace.val()) { showInlineError(fromPlace, 'From Place required'); fromPlace.focus(); return false; }
                            if (toPlace && toPlace.length && !toPlace.val()) { showInlineError(toPlace, 'To Place required'); toPlace.focus(); return false; }
                            if (tAmt && tAmt.length && (!tAmt.val() || parseFloat(tAmt.val()) <= 0)) { showInlineError(tAmt, 'Travel amount is required and must be greater than 0.'); tAmt.focus(); return false; }
                            var chkInv = $resolve('CHK_TravelInvoice');
                            if (chkInv && chkInv.length && chkInv.prop('checked')) {
                                var invNo = $resolve('TXT_TravelInvoiceNo');
                                if (invNo && invNo.length && !invNo.val()) { showInlineError(invNo, 'Invoice no required'); invNo.focus(); return false; }
                                var fu = $resolve('FU_Attachment');
                                if (fu && fu.length) {
                                    var files = fu[0].files;
                                    if (!files || files.length === 0) { showInlineError(fu, 'Attachment required'); fu.focus(); return false; }
                                }
                            }
                        } else { // DA
                            var da = $resolve('TXT_DAAmount');
                            if (da && da.length && (!da.val() || parseFloat(da.val()) <= 0)) { showInlineError(da, 'DA amount required'); da.focus(); return false; }
                        }
                    } catch (e) { dbg('wrapValidator heuristics failed: ' + e); }

                    showToast('Validation', 'Please correct highlighted fields.', 'error', 3500);
                    return false;
                };
            }

            // showToast: uses PNotify if available; fallback to custom DOM toast
            function showToast(title, text, type, ttl) {
                ttl = ttl || 3500;
                try {
                    if (typeof showPNotify === 'function') {
                        showPNotify(title, text, (type === 'error' ? 'error' : (type === 'success' ? 'success' : 'info')));
                        return;
                    }
                } catch (e) { dbg('showToast -> showPNotify failed: ' + e); }

                var wrap = document.querySelector('.so-toast-wrap');
                if (!wrap) { wrap = document.createElement('div'); wrap.className = 'so-toast-wrap'; document.body.appendChild(wrap); }
                var t = document.createElement('div'); t.className = 'so-toast ' + (type || 'notice');
                t.innerHTML = '<div class="so-toast-title">' + (title || '') + '</div><div class="so-toast-text">' + (text || '') + '</div>';
                wrap.appendChild(t);
                setTimeout(function () { t.style.opacity = 0; setTimeout(function () { try { t.remove(); } catch (e) { } }, 300); }, ttl);
            }

            // Init on ready + on modal shown
            $(document).ready(function () {
                ensurePolicyModeControl();
                wrapValidator();
                dbg('policy-mode control ensured and validator wrapped.');
            });

            $('#expenseModal').on('shown.bs.modal', function () {
                ensurePolicyModeControl();
                wrapValidator();
            });

            // remove inline errors as user interacts
            $(document).on('input change', '.so-err, input, select, textarea', function () {
                try { removeInlineError($(this)); } catch (e) { }
            });

            // expose for inspection if needed
            window.wrapValidator = wrapValidator;
            window.showToast = showToast;

        })();

        /* ===========================
           9. Misc UI helpers & page init behaviour
           - pageInitExpenseModal: small UX tweaks (truncate long texts, totals, file chosen placeholder)
           - recalcTotals: sum up amounts from rptExpenses
           =========================== */
        (function ($) {
            function pageInitExpenseModal() {
                // init bootstrap tooltips (if bootstrap loaded)
                if (typeof bootstrap !== 'undefined' && $('[data-toggle="tooltip"]').length) {
                    $('[data-toggle="tooltip"]').tooltip({ trigger: 'hover' });
                }

                // show/hide expense sections using DDL_ExpenseType (keeps readonly behaviour)
                var $ddlType = $('#DDL_ExpenseType');
                function applyExpenseTypeVisibility() {
                    var v = $ddlType.val();
                    $('#divDASection').toggle(v === 'DA');
                    $('#divHotelSection').toggle(v === 'Hotel');
                    $('#divTravelSection').toggle(v === 'Travel' || v === 'Other');
                    if (v === 'DA') {
                        $('#TXT_DAAmount').prop('readonly', true);
                    } else {
                        $('#TXT_DAAmount').prop('readonly', true); // original had readonly true by default
                    }
                }
                $ddlType.off('change.expense').on('change.expense', applyExpenseTypeVisibility);
                applyExpenseTypeVisibility();

                // truncate long cells but keep full title attribute
                $('#expenseModal table tbody td').each(function () {
                    var $cell = $(this);
                    if ($cell.find('.text-truncate-ellipsis').length) return;
                    var txt = $cell.text().trim();
                    if (txt.length > 40) {
                        $cell.html('<span class="text-truncate-ellipsis" title="' + $('<div>').text(txt).html() + '">' + $('<div>').text(txt).html() + '</span>');
                    }
                });

                // small UX: make modal buttons smaller
                $('#expenseModal .btn').each(function () { $(this).addClass('btn-sm'); });

                // recalc totals from a repeater table (rptExpenses)
                function recalcTotals() {
                    var daTotal = 0, hotelTotal = 0, otherTotal = 0;
                    $('#rptExpenses tbody tr').each(function () {
                        var typ = $(this).find('td').eq(0).text().trim();
                        var amountText = $(this).find('td').eq(3).text().replace(/,/g, '').trim();
                        var amt = parseFloat(amountText) || 0;
                        if (typ === 'DA') daTotal += amt;
                        else if (typ === 'Hotel') hotelTotal += amt;
                        else otherTotal += amt;
                    });
                    function fmt(n) { return n.toFixed(2); }
                    try {
                        $('#<%= lblTotalDA.ClientID %>').text(fmt(daTotal));
                        $('#<%= lblTotalHotel.ClientID %>').text(fmt(hotelTotal));
                        $('#<%= lblTotalOther.ClientID %>').text(fmt(otherTotal));
                        $('#<%= lblOverallTotal.ClientID %>').text(fmt(daTotal + hotelTotal + otherTotal));
                    } catch (e) {
                        // fallback: ignore if server labels not present client-side
                    }
                }
                try { recalcTotals(); } catch (e) { /* ignore */ }

                // file upload UI: show "No file chosen" or file name
                $('.form-control-file').each(function () {
                    var $f = $(this);
                    if (!$f.next('.file-chosen').length) {
                        $f.after('<div class="file-chosen small text-muted mt-1">No file chosen</div>');
                        $f.on('change', function () {
                            var name = $(this)[0].files && $(this)[0].files.length ? $(this)[0].files[0].name : 'No file chosen';
                            $(this).next('.file-chosen').text(name);
                        });
                    }
                });
            }

            // run on initial load and after partial postbacks
            $(function () { pageInitExpenseModal(); });

            if (typeof (Sys) !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                var prm = Sys.WebForms.PageRequestManager.getInstance();
                if (prm) {
                    prm.add_endRequest(function () { pageInitExpenseModal(); });
                }
            }

            $('#expenseModal').on('shown.bs.modal', function () { pageInitExpenseModal(); });

        })(jQuery);

        /* ===========================
           10. Highlight & UI polish for policy area
           - highlightPolicyArea: visual chip and AutoDA emphasis
           =========================== */
        (function ($) {
            function dbg(msg) { try { console.log('[highlight] ' + msg); } catch (e) { } }

            function highlightPolicyArea() {
                try {
                    var policyMode = ($('#HF_ResolvedPolicyMode').length ? $('#HF_ResolvedPolicyMode').val() : '') || '';
                    var ownArr = ($('#HF_OwnArrangementDA').length ? $('#HF_OwnArrangementDA').val() : '') || '';
                    var outst = ($('#HF_OutstationDA').length ? $('#HF_OutstationDA').val() : '') || '';

                    var $ps = $('#expenseModal .policy-summary');
                    $ps.find('.policy-mode').remove();

                    if (policyMode) {
                        var modeText = $('<span>').addClass('policy-mode').text(policyMode);
                        $ps.prepend(modeText);
                    }

                    if (ownArr && parseFloat(ownArr) > 0) {
                        $('#divAutoDAInfo').show();
                        $('#spAutoDAAmount').text('₹ ' + parseFloat(ownArr).toFixed(2));
                        $('#spAutoDAType').text('Own Arrangement DA (auto)');
                    } else if (outst && parseFloat(outst) > 0) {
                        $('#divAutoDAInfo').show();
                        $('#spAutoDAAmount').text('₹ ' + parseFloat(outst).toFixed(2));
                        $('#spAutoDAType').text('Outstation DA (auto)');
                    } else {
                        $('#divAutoDAInfo').hide();
                    }

                    $ps.addClass('pulse-highlight');
                    setTimeout(function () { $ps.removeClass('pulse-highlight'); }, 900);

                } catch (e) { console && console.log && console.log('highlightPolicyArea error', e); }
            }

            $(function () { highlightPolicyArea(); });

            $('#btnRefreshPolicy').off('click.policy').on('click.policy', function (e) { highlightPolicyArea(); });

            if (typeof (Sys) !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () { highlightPolicyArea(); });
            }

            $('#expenseModal').on('mouseenter', '.text-truncate-ellipsis', function () {
                var $t = $(this);
                if ($t[0].offsetWidth < $t[0].scrollWidth) {
                    $t.attr('title', $t.text());
                } else $t.removeAttr('title');
            });

        })(jQuery);

        /* ===========================
           11. applyPolicyModeFromServer modal-queued handling is declared earlier.
                (No further changes here.)
           =========================== */


        /* ===========================
           12. KM modal helpers (openEditKmModal / validateModalKm / Save button hook)
           - These are small, self-contained helpers that drive the KM edit modal
           =========================== */
        (function () {
            'use strict';
            function dbg(msg) { try { console.log('[km] ' + msg); } catch (e) { } }

            window.openEditKmModal = function (activityId, currentKm, minKm, maxKm) {
                var hdn = document.getElementById('<%= hdnActivityId.ClientID %>');
                var txt = document.getElementById('<%= txtModalKM.ClientID %>');
                var spMin = document.getElementById('spMinKm');
                var spMax = document.getElementById('spMaxKm');
                var lblCurrent = document.getElementById('<%= lblModalCurrentKm.ClientID %>');

        hdn.value = activityId;
        txt.value = currentKm;
        spMin.innerText = (minKm === '' || minKm === 'null') ? '—' : minKm;
        spMax.innerText = (maxKm === '' || maxKm === 'null') ? '—' : maxKm;
        lblCurrent.innerText = 'Current: ' + currentKm + ' km';

        document.getElementById('spClientKmError').style.display = 'none';
        $('#editKmModal').modal('show');
            };

            window.validateModalKm = function () {
                var txt = document.getElementById('<%= txtModalKM.ClientID %>');
        var min = parseFloat(document.getElementById('spMinKm').innerText || NaN);
        var max = parseFloat(document.getElementById('spMaxKm').innerText || NaN);
        var val = parseFloat(txt.value);

        if (!isNaN(min) && val < min) {
            document.getElementById('spClientKmError').style.display = '';
            document.getElementById('spClientKmError').innerText = 'Value cannot be less than ' + min;
            return false;
        }
        if (!isNaN(max) && val > max) {
            document.getElementById('spClientKmError').style.display = '';
            document.getElementById('spClientKmError').innerText = 'Value cannot exceed ' + max;
            return false;
        }
        document.getElementById('spClientKmError').style.display = 'none';
        return true;
    };

            // Hook Save button to validate client-side before postback
            $(function () {
                $('#<%= btnSaveKm.ClientID %>').on('click', function (e) {
            if (!validateModalKm()) {
                e.preventDefault();
                return false;
            }
            // else let server handle it
        });
    });

            $(document).on('hidden.bs.modal', '#editKmModal', function () {
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open');
            });

            $(document).on('shown.bs.modal', '#editKmModal', function () {
                $('#spClientKmError').hide();
            });

        })();

/* ===========================
   13. Submit modal / perform submit helpers
   - showSubmitModal (shows confirm modal or fallback confirm)
   - performSubmitFromModal triggers __doPostBack with btn_submit control UniqueID
   - enableButtonsAfterModalClose resets UI on cancel
   =========================== */
(function () {
    'use strict';

    window.showSubmitModal = function () {
        var modal = $('#submitConfirmModal');
        if (!modal || modal.length === 0) {
            return confirm('Are you sure you want to submit all listed activities for approval?');
        }
        modal.modal({ backdrop: 'static', keyboard: false });
        return false; // prevent original postback
    };

    window.performSubmitFromModal = function () {
        var confirmBtn = document.getElementById('submitModalConfirm');
        var cancelBtn = document.getElementById('submitModalCancel');
        if (confirmBtn) { confirmBtn.disabled = true; confirmBtn.style.opacity = '0.6'; }
        if (cancelBtn) { cancelBtn.disabled = true; cancelBtn.style.opacity = '0.6'; }

        var btnSubmit = document.getElementById('<%= btn_submit.ClientID %>');
        var btnDraft = document.getElementById('<%= btn_save_draft.ClientID %>');
        if (btnSubmit) { btnSubmit.disabled = true; btnSubmit.style.opacity = '0.6'; btnSubmit.style.cursor = 'not-allowed'; }
        if (btnDraft) { btnDraft.disabled = true; btnDraft.style.opacity = '0.6'; btnDraft.style.cursor = 'not-allowed'; }

        if (typeof showPNotify === 'function') {
            showPNotify('Submitting...', 'Please wait while your activities are submitted for approval.', 'info');
        }

        $('#submitConfirmModal').modal('hide');

        __doPostBack('<%= btn_submit.UniqueID.Replace("$", "$") %>', '');
    };

    window.enableButtonsAfterModalClose = function () {
        var btnSubmit = document.getElementById('<%= btn_submit.ClientID %>');
        var btnDraft = document.getElementById('<%= btn_save_draft.ClientID %>');
        if (btnSubmit) { btnSubmit.disabled = false; btnSubmit.style.opacity = '1'; btnSubmit.style.cursor = 'pointer'; }
        if (btnDraft) { btnDraft.disabled = false; btnDraft.style.opacity = '1'; btnDraft.style.cursor = 'pointer'; }

        var confirmBtn = document.getElementById('submitModalConfirm');
        var cancelBtn = document.getElementById('submitModalCancel');
        if (confirmBtn) { confirmBtn.disabled = false; confirmBtn.style.opacity = '1'; }
        if (cancelBtn) { cancelBtn.disabled = false; cancelBtn.style.opacity = '1'; }
    };

    // Fallback __doPostBack if not provided by ASP.NET
    if (typeof __doPostBack === 'undefined') {
        window.__doPostBack = function (eventTarget, eventArgument) {
            var btn = document.getElementById('<%= btn_submit.ClientID %>');
            if (btn) {
                var onClick = btn.getAttribute('onclick');
                if (onClick) btn.removeAttribute('onclick');
                btn.click();
                if (onClick) btn.setAttribute('onclick', onClick);
            }
        };
        }
})();

    /* ===========================
       14. Save Draft helpers (button UX + page loader)
       - handleSaveDraftClick disables button, shows spinner text, wires PageRequestManager to re-enable after async postback
       =========================== */
    (function () {
        'use strict';
        window.handleSaveDraftClick = function (btn) {
            try {
                btn.disabled = true;
                if (btn.tagName.toLowerCase() === "button") {
                    btn.innerHTML = '<i class="fa fa-spinner fa-spin"></i> Saving...';
                } else {
                    btn.value = "Saving...";
                }

                showPageLoader();

                var prm = Sys.WebForms.PageRequestManager.getInstance();
                if (prm) {
                    prm.add_endRequest(function () {
                        btn.disabled = false;
                        if (btn.tagName.toLowerCase() === "button") {
                            btn.innerHTML = "Save as Draft";
                        } else {
                            btn.value = "Save as Draft";
                        }
                        hidePageLoader();
                    });
                }

                return true;
            } catch (err) {
                console.error("handleSaveDraftClick error:", err);
                return true;
            }
        };

        window.showPageLoader = function () {
            var el = document.getElementById("pageLoader");
            if (el) el.style.display = "block";
        };
        window.hidePageLoader = function () {
            var el = document.getElementById("pageLoader");
            if (el) el.style.display = "none";
        };
    })();

    /* ===========================
       15. Misc small helpers & copy policy JSON
       - copyPolicyJson shows the textbox, selects and copies using document.execCommand
       - showPolicyBypassNotice: sample informational PNotify call
       =========================== */
    (function () {
        'use strict';
        window.copyPolicyJson = function () {
            var tb = document.getElementById('<%= policyJsonRaw.ClientID %>');
            if (!tb) return;
            tb.style.display = 'block';
            tb.select();
            try {
                document.execCommand('copy');
                alert('Policy JSON copied to clipboard');
            } catch (e) {
                alert('Copy failed — select and copy manually');
            }
            tb.style.display = 'none';
        };

        window.showPolicyBypassNotice = function () {
            new PNotify({
                title: 'Policy Disabled',
                text: 'Monthly processing policy is currently disabled. Validation skipped and process allowed.',
                type: 'info',
                delay: 4000,
                styling: 'bootstrap3'
            });
        };

        // small helpers to show/hide expense modal
        window.showExpenseModal = function () { $('#expenseModal').modal('show'); };
        window.hideExpenseModal = function () { $('#expenseModal').modal('hide'); };

    })();

    /* ===========================
       16. KM grid-input validation (inline km inputs)
       - Input/blur handler for .km-input and client-side validateGridBeforeSubmit
       =========================== */
    (function () {
        'use strict';
        $(document).on("input blur", ".km-input", function () {
            var $txt = $(this);
            var row = $txt.closest("tr");
            var val = parseFloat($txt.val());

            var lower = parseFloat(row.find(".tol-lower").text());
            var upper = parseFloat(row.find(".tol-upper").text());

            var $err = row.find(".km-error");

            if (isNaN(val)) {
                $txt.addClass("is-invalid");
                $err.text("Invalid").show();
                return;
            }

            var inRange = true;
            if (!isNaN(lower) && val < lower) inRange = false;
            if (!isNaN(upper) && val > upper) inRange = false;

            if (!inRange) {
                $txt.addClass("is-invalid");
                $err.text("[" + lower + " to " + upper + "]").show();
            } else {
                $txt.removeClass("is-invalid");
                $err.hide();
            }
        });

        window.validateGridBeforeSubmit = function () {
            var invalid = $(".km-input.is-invalid").length > 0;
            if (invalid) {
                alert("Please fix invalid KM entries before saving.");
                return false;
            }
            return true;
        };
    })();

    </script>


    <script type="text/javascript">
        (function () {
            // helper: get element by server control client id
            var btnSaveId = '<%= btnSaveKm.ClientID %>';
            var txtKmId = '<%= txtModalKM.ClientID %>';
            var hdnOrigKmId = '<%= hdnOriginalKm.ClientID %>';
            var txtReasonId = '<%= txtKmReason.ClientID %>';
            var spErrId = 'spClientKmError';

            function showError(msg) {
                var el = document.getElementById(spErrId);
                if (el) {
                    el.style.display = '';
                    el.innerText = msg;
                } else {
                    alert(msg);
                }
            }

            function clearError() {
                var el = document.getElementById(spErrId);
                if (el) {
                    el.style.display = 'none';
                    el.innerText = '';
                }
            }

            function parseDecimal(n) {
                if (n === null || n === undefined) return NaN;
                // remove commas and trim
                n = String(n).replace(/,/g, '').trim();
                return parseFloat(n);
            }

            function validateBeforeSave() {
                clearError();

                var txt = document.getElementById(txtKmId);
                var orig = document.getElementById(hdnOrigKmId);
                var reason = document.getElementById(txtReasonId);

                if (!txt) { showError('KM input not found.'); return false; }

                var newKm = parseDecimal(txt.value);
                if (isNaN(newKm)) {
                    showError('Please enter a valid numeric KM value.');
                    txt.focus();
                    return false;
                }

                var origKm = orig ? parseDecimal(orig.value) : NaN;
                if (!isNaN(origKm) && newKm === origKm) {
                    showError('KM value has not changed. Please modify the KM before saving.');
                    txt.focus();
                    return false;
                }

                // Reason required when KM changed
                if (!reason) {
                    showError('Reason control not found.');
                    return false;
                }
                var reasonVal = String(reason.value || '').trim();
                if (reasonVal.length === 0) {
                    showError('Please provide a reason for changing the KM.');
                    reason.focus();
                    return false;
                }

                // Optional: additional client-side min/max check could be placed here using spMinKm/spMaxKm text.

                // all good
                clearError();
                return true;
            }

            // Attach handler on page load
            document.addEventListener('DOMContentLoaded', function () {
                var btn = document.getElementById(btnSaveId);
                if (!btn) return;

                // Use click handler to block postback if invalid
                btn.addEventListener('click', function (ev) {
                    if (!validateBeforeSave()) {
                        // prevent the ASP.NET postback
                        ev.preventDefault();
                        return false;
                    }
                    // allow postback to continue
                });

                // Clear error when user edits fields
                var txt = document.getElementById(txtKmId);
                var reason = document.getElementById(txtReasonId);
                if (txt) txt.addEventListener('input', clearError);
                if (reason) reason.addEventListener('input', clearError);
            });
        })();
    </script>

    <script type="text/javascript">
        document.addEventListener('DOMContentLoaded', function () {
            var map = {
                present: { glyph: '✓', cls: 'present', label: 'Present' },
                absent: { glyph: '✖', cls: 'absent', label: 'Absent' },
                leave: { glyph: '⛱', cls: 'leave', label: 'Leave' },
                wfh: { glyph: '🏠', cls: 'wfh', label: 'WFH' }
            };

            var icons = document.querySelectorAll('.status-icon');

            icons.forEach(function (el) {
                var raw = (el.dataset.status || '').trim();
                var key = raw.toLowerCase();
                if (key === 'working from home') key = 'wfh';
                if (key === 'work from home') key = 'wfh';
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
        });
    </script>

</asp:Content>
