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

        .auto-save-indicator {
            min-height: 1.5rem;
        }

        .auto-save-indicator .save-flash {
            color: #28a745;
            font-weight: 600;
        }

        .monthly-summary-card .summary-line {
            display: flex;
            justify-content: space-between;
            padding: 0.35rem 0;
            border-bottom: 1px dashed #e9ecef;
        }

        .monthly-summary-card .summary-line:last-child {
            border-bottom: none;
            font-size: 1.05rem;
        }

        .progress-summary-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 0.5rem;
        }

        .progress-summary-item {
            background: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 4px;
            padding: 0.5rem 0.65rem;
            text-align: center;
        }

        .progress-summary-item .count {
            font-size: 1.15rem;
            font-weight: 700;
            color: #1a355e;
        }

        .submission-confidence-panel .check-item {
            display: flex;
            align-items: flex-start;
            gap: 0.5rem;
            margin-bottom: 0.35rem;
            font-size: 0.88rem;
        }

        .submission-confidence-panel .check-item:last-child {
            margin-bottom: 0;
        }

        @media (max-width: 767px) {
            .progress-summary-grid {
                grid-template-columns: 1fr;
            }
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

        #expenseModal .expense-category-desc {
            font-size: 0.85rem;
            line-height: 1.4;
        }

        #expenseModal .expense-help-panel {
            font-size: 0.85rem;
        }

        #expenseModal .expense-help-panel ul {
            padding-left: 1.1rem;
            margin-bottom: 0;
        }

        #expenseModal .expense-upload-guidance {
            font-size: 0.82rem;
            color: #495057;
        }

        #expenseModal .expense-upload-guidance ul {
            margin-bottom: 0.25rem;
            padding-left: 1.1rem;
        }

        #expenseModal .expense-upload-tip {
            background: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 4px;
            padding: 0.5rem 0.65rem;
            font-size: 0.82rem;
            color: #6c757d;
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

        .claims-badge { display:inline-block; padding:4px 10px; border-radius:12px; font-size:0.78rem; font-weight:600; }
        .claims-badge-pending { background:#fff3cd; color:#856404; border:1px solid #ffeeba; }
        .claims-badge-returned { background:#ffe8cc; color:#c45c00; border:1px solid #ffc78a; }
        .claims-badge-approved { background:#d4edda; color:#155724; border:1px solid #b9dfc0; }
        .claims-badge-completed { background:#cce5ff; color:#004085; border:1px solid #9ec5fe; }
        .claims-badge-ready { background:#e8d9ff; color:#4a148c; border:1px solid #c9a8ff; }
        .claims-badge-rejected { background:#f8d7da; color:#721c24; border:1px solid #f1b0b7; }
        .claims-badge-default { background:#e9ecef; color:#495057; border:1px solid #ced4da; }
        .claims-empty-state { text-align:center; padding:40px 20px; color:#6c757d; }
        .row-returned { border-left:3px solid #fd7e14; background-color:#fff8f0 !important; }
        #pageLoader {
            display: none;
            position: fixed;
            inset: 0;
            width: 100%;
            height: 100%;
            background: rgba(255,255,255,0.88);
            z-index: 20000;
            text-align: center;
            padding-top: 18vh;
            pointer-events: all;
            cursor: wait;
        }
        #pageLoader .page-loader-card {
            display: inline-block;
            background: #fff;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 28px 36px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.08);
        }

        /* Request #99 — KM Modification modal mobile UX (layout only) */
        html.km-modal-open,
        body.km-modal-open {
            overflow: hidden !important;
            overscroll-behavior: none;
        }

        body.km-modal-open {
            position: fixed;
            width: 100%;
            left: 0;
            right: 0;
        }

        #editKmModal.modal {
            overflow: hidden;
            z-index: 10500;
        }

        #routeDistanceModal.modal {
            z-index: 10600;
        }

        #editKmModal .modal-dialog {
            display: flex;
            flex-direction: column;
            max-height: 90vh;
            margin: 5vh auto;
            width: calc(100% - 1rem);
        }

        #editKmModal .modal-content {
            display: flex;
            flex-direction: column;
            max-height: 90vh;
            overflow: hidden;
        }

        #editKmModal .modal-header {
            flex: 0 0 auto;
        }

        #editKmModal .modal-body {
            flex: 1 1 auto;
            min-height: 0;
            overflow-x: hidden;
            overflow-y: auto;
            -webkit-overflow-scrolling: touch;
            overscroll-behavior: contain;
        }

        #editKmModal .modal-footer {
            flex: 0 0 auto;
            position: sticky;
            bottom: 0;
            z-index: 5;
            display: flex;
            justify-content: flex-end;
            flex-wrap: wrap;
            gap: 0.4rem;
            background: #fff;
            box-shadow: 0 -4px 12px rgba(0, 0, 0, 0.06);
        }

        #editKmModal .km-route-address {
            word-break: break-word;
            overflow-wrap: anywhere;
        }

        #editKmModal .km-accordion-toggle {
            color: #0c5460;
            font-weight: 600;
        }

        #editKmModal .km-route-info-btn {
            color: #0c5460;
            font-size: 1.2rem;
            line-height: 1;
            min-width: 32px;
            min-height: 32px;
            padding: 0;
        }

        #editKmModal .km-route-info-btn[aria-expanded="true"] {
            color: #063a42;
        }

        #editKmModal .km-accordion-chevron {
            display: inline-block;
            margin-left: 0.25rem;
            font-size: 0.75rem;
            transition: transform 0.15s ease;
        }

        #editKmModal .km-accordion-toggle[aria-expanded="true"] .km-accordion-chevron {
            transform: rotate(180deg);
        }

        #editKmModal .km-history-toggle {
            color: #00008b;
            font-weight: 700;
            font-size: 0.95rem;
            text-decoration: none;
        }

        #editKmModal tr.km-history-latest td {
            background-color: #e8f5e9;
        }

        #editKmModal tr.km-history-prior {
            display: none;
        }

        @media (max-width: 768px) {
            #editKmModal .modal-dialog {
                margin: 5vh 0.5rem;
                max-width: calc(100% - 1rem);
            }

            #editKmModal .modal-header {
                padding: 0.55rem 0.75rem;
            }

            #editKmModal .modal-title {
                font-size: 0.92rem;
                line-height: 1.3;
                padding-right: 1.25rem;
            }

            #editKmModal .modal-body {
                padding: 0.65rem 0.75rem;
            }

            #editKmModal .modal-footer {
                padding: 0.45rem 0.75rem;
            }

            #editKmModal .km-policy-panel {
                padding: 0.45rem 0.6rem;
                margin-bottom: 0.55rem !important;
            }

            #editKmModal .km-policy-panel .mb-2 {
                margin-bottom: 0.35rem !important;
            }

            #editKmModal .policy-row {
                margin-bottom: 0.15rem;
                font-size: 0.82rem;
            }

            #editKmModal .form-group {
                margin-bottom: 0.5rem;
            }

            #editKmModal hr {
                margin: 0.5rem 0;
            }

            #editKmModal .alert {
                padding: 0.45rem 0.6rem;
                margin-bottom: 0.5rem;
            }

            #editKmModal .km-route-summary {
                margin-top: 0.4rem !important;
            }

            #editKmModal textarea.form-control {
                min-height: 4.25rem;
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
                            <asp:Panel ID="pnlWorkflowBanner" runat="server" CssClass="claim-workflow-banner" Visible="true">
                                <div class="workflow-title">Monthly Claim Submission Process</div>
                                <div class="claim-workflow-steps">
                                    <span class="step">① Review Daily Activities</span>
                                    <span class="arrow">↓</span>
                                    <span class="step">② Update Travel / KM</span>
                                    <span class="arrow">↓</span>
                                    <span class="step">③ Add Expenses</span>
                                    <span class="arrow">↓</span>
                                    <span class="step">④ Review Monthly Summary</span>
                                    <span class="arrow">↓</span>
                                    <span class="step">⑤ Submit Monthly Claim</span>
                                </div>
                            </asp:Panel>

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
                                    <button type="button" id="btnViewPolicyAgain" class="btn btn-info btn-sm mb-3" onclick="openResolvedPolicyModal(); return false;"><i class="fa fa-info-circle"></i>View Policy</button>
                                    <%-- Deprecated: Save as Draft removed — application now auto-saves edits immediately.
                                         Draft is a workflow status only, maintained until Submit Claim.
                                    <asp:Button ID="btn_save_draft" runat="server" CssClass="btn btn-secondary btn-sm mb-3" Text="Save as Draft" OnClick="btn_save_draft_Click" />
                                    --%>
                                    <asp:Button ID="btn_submit" runat="server" CssClass="btn btn-success btn-sm mb-3 claims-ux-postback-btn" Text="Submit for Approval" OnClick="btn_submit_Click" OnClientClick="return showSubmitModal();" data-ux-processing="Submitting claim for approval..." data-ux-loading="Submitting..." />
                                </div>
                            </div>

                            <div class="ln_solid"></div>

                            <div class="row">
                                <div class="col-md-6">
                                    <asp:Label ID="lbl_msg" runat="server" Text="" />
                                    <asp:Panel ID="pnlAutoSaveIndicator" runat="server" Visible="false" CssClass="auto-save-indicator mt-1">
                                        <span id="spAutoSaveIcon"><i class="fa fa-check-circle text-success"></i></span>
                                        <span id="spAutoSaveFlash" class="save-flash" style="display:none;">Changes Saved</span>
                                        <span id="spAutoSaveMessage">Last Saved</span>
                                        <strong id="spAutoSaveTime"><asp:Literal ID="litLastSavedTime" runat="server" /></strong>
                                    </asp:Panel>
                                </div>
                                <div class="col-md-6">
                                    <asp:Panel ID="pnlSubmissionConfidence" runat="server" Visible="false" CssClass="submission-confidence-panel card border-success mb-0">
                                        <div class="card-body py-2 px-3">
                                            <div class="font-weight-bold text-success mb-1">Ready to Submit?</div>
                                            <div class="text-muted small mb-2">Review</div>
                                            <asp:PlaceHolder ID="phSubmissionConfidence" runat="server" />
                                        </div>
                                    </asp:Panel>
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
                                    <asp:Panel ID="pnlReturnedClaimsBanner" runat="server" Visible="false" CssClass="alert alert-warning returned-claims-banner mb-3" role="alert">
                                        <div class="d-flex align-items-start">
                                            <i class="fa fa-exclamation-triangle fa-lg mr-2 mt-1"></i>
                                            <div>
                                                <strong>Your monthly claim contains Returned items.</strong>
                                                <div class="mt-1">Please review the highlighted activities below, make the requested corrections, and submit again.</div>
                                            </div>
                                        </div>
                                    </asp:Panel>

                                    <asp:UpdatePanel ID="upKmActivities" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">
                                        <ContentTemplate>
                                    <div class="card-box table-responsive grid-container">
                                        <asp:GridView ID="gvActivities" runat="server" AutoGenerateColumns="false"
                                            CssClass="table table-bordered table-finance" HeaderStyle-CssClass="grid-header" FooterStyle-CssClass="grid-footer"
                                            DataKeyNames="ActivityId" OnRowCommand="gvActivities_RowCommand" OnRowEditing="gvActivities_RowEditing"
                                            OnRowCancelingEdit="gvActivities_RowCancelingEdit" OnRowUpdating="gvActivities_RowUpdating"
                                            OnRowDataBound="gvActivities_RowDataBound" ShowFooter="true">
                                            <EmptyDataTemplate>
                                                <div class="claims-empty-state border rounded bg-white my-3">
                                                    <i class="fa fa-calendar-o d-block" style="font-size:2.5rem;color:#adb5bd;"></i>
                                                    <h5 class="mb-1">No activities found</h5>
                                                    <p class="mb-0">Select a year and month, then click Search to load your working days.</p>
                                                </div>
                                            </EmptyDataTemplate>
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
                                                            <span class="text-muted">(Tolerance reduction: <%# Eval("MinAllowedKM") ?? "—" %> ; increase: <%# Eval("MaxAllowedKM") ?? "—" %> )</span>
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
                                                        <asp:Label ID="lblReturnedReasonInline" runat="server" Visible="false" CssClass="d-block small text-warning mt-1" style="max-width:160px;white-space:normal;" />
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Actions" ItemStyle-Width="5%">
                                                    <ItemStyle HorizontalAlign="Center" />
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lnkExpenses" runat="server"
                                                            CommandName="OpenExpenses" 
                                                            CommandArgument='<%# Eval("ActivityId") + "|" + Eval("ActivityDate", "{0:yyyy-MM-dd}") + "|" + Eval("Planned_Beat") + "|" + Eval("Order_Distributor_Name") + "|" + Eval("TotalDistanceKM") + "|" + HttpUtility.HtmlEncode(
                                                                ((System.Data.DataRowView)Container.DataItem).Row.Table.Columns.Contains("DetailRemarks") &&
                                                                ((System.Data.DataRowView)Container.DataItem)["DetailRemarks"] != DBNull.Value
                                                                    ? ((System.Data.DataRowView)Container.DataItem)["DetailRemarks"].ToString()
                                                                    : string.Empty) %>' 
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
                                        </ContentTemplate>
                                        <Triggers>
                                            <asp:AsyncPostBackTrigger ControlID="btnSaveKm" EventName="Click" />
                                            <asp:PostBackTrigger ControlID="gvActivities" />
                                        </Triggers>
                                    </asp:UpdatePanel>
                                </div>
                            </div>

                            <asp:Panel ID="pnlSubmissionChecklist" runat="server" Visible="false" CssClass="submission-checklist alert alert-light border mt-3 mb-0">
                                <div class="font-weight-bold text-primary mb-2">Submission Readiness</div>
                                <asp:PlaceHolder ID="phSubmissionChecklist" runat="server" />
                            </asp:Panel>
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

                            <div class="row mb-3">
                                <div class="col-lg-7 mb-3 mb-lg-0">
                                    <asp:Panel ID="pnlEnhancedMonthlySummary" runat="server" Visible="false" CssClass="card monthly-summary-card border-primary h-100">
                                        <div class="card-header py-2 bg-white">
                                            <strong class="text-primary">Monthly Claim Summary</strong>
                                        </div>
                                        <div class="card-body py-2">
                                            <div class="summary-line">
                                                <span>Travel</span>
                                                <strong><asp:Label ID="lblSummaryTravel" runat="server" Text="₹ 0.00" /></strong>
                                            </div>
                                            <div class="summary-line">
                                                <span>Daily Allowance</span>
                                                <strong><asp:Label ID="lblSummaryDA" runat="server" Text="₹ 0.00" /></strong>
                                            </div>
                                            <div class="summary-line">
                                                <span>Hotel</span>
                                                <strong><asp:Label ID="lblSummaryHotel" runat="server" Text="₹ 0.00" /></strong>
                                            </div>
                                            <div class="summary-line">
                                                <span>Other Expenses</span>
                                                <strong><asp:Label ID="lblSummaryOther" runat="server" Text="₹ 0.00" /></strong>
                                            </div>
                                            <div class="summary-line font-weight-bold text-primary">
                                                <span>Grand Total</span>
                                                <strong><asp:Label ID="lblSummaryGrandTotal" runat="server" Text="₹ 0.00" /></strong>
                                            </div>
                                        </div>
                                    </asp:Panel>
                                </div>
                                <div class="col-lg-5">
                                    <asp:Panel ID="pnlProgressSummary" runat="server" Visible="false" CssClass="card border-light h-100">
                                        <div class="card-header py-2 bg-white">
                                            <strong>Progress Summary</strong>
                                        </div>
                                        <div class="card-body py-2">
                                            <div class="progress-summary-grid">
                                                <div class="progress-summary-item">
                                                    <div class="small text-muted">Working Days</div>
                                                    <div class="count"><asp:Label ID="lblProgressWorkingDays" runat="server" Text="0" /></div>
                                                </div>
                                                <div class="progress-summary-item">
                                                    <div class="small text-muted">Completed</div>
                                                    <div class="count text-success"><asp:Label ID="lblProgressCompleted" runat="server" Text="0" /></div>
                                                </div>
                                                <div class="progress-summary-item">
                                                    <div class="small text-muted">Draft</div>
                                                    <div class="count text-warning"><asp:Label ID="lblProgressDraft" runat="server" Text="0" /></div>
                                                </div>
                                                <div class="progress-summary-item">
                                                    <div class="small text-muted">Returned</div>
                                                    <div class="count text-danger"><asp:Label ID="lblProgressReturned" runat="server" Text="0" /></div>
                                                </div>
                                                <div class="progress-summary-item" style="grid-column: span 2;">
                                                    <div class="small text-muted">Submitted</div>
                                                    <div class="count text-info"><asp:Label ID="lblProgressSubmitted" runat="server" Text="0" /></div>
                                                </div>
                                            </div>
                                        </div>
                                    </asp:Panel>
                                </div>
                            </div>

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
            <div class="modal-dialog modal-lg modal-dialog-scrollable" role="document">
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
                        <asp:UpdatePanel ID="upKmHistory" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="false">
                            <ContentTemplate>
                        <asp:HiddenField ID="hdnOriginalKm" runat="server" />
                        <asp:HiddenField ID="HF_KmSaveWorkflow" runat="server" Value="Manual" />

                        <asp:Panel ID="pnlKmPolicyInfo" runat="server" CssClass="km-policy-panel alert alert-info border mb-3">
                            <div class="font-weight-bold text-primary mb-2">KM Policy Information</div>
                            <div class="policy-row">
                                <span class="policy-label">Original KM</span>
                                <strong><asp:Label ID="lblModalOriginalKm" runat="server" Text="—" /></strong>
                            </div>
                            <div class="policy-row">
                                <span class="policy-label">Current KM</span>
                                <strong><asp:Label ID="lblModalCurrentKm" runat="server" Text="—" /></strong>
                            </div>
                            <div class="policy-row">
                                <span class="policy-label">Allowed Range</span>
                                <strong><asp:Label ID="lblModalAllowedRange" runat="server" Text="—" /></strong>
                            </div>
                            <div class="policy-row">
                                <span class="policy-label">Remaining Changes</span>
                                <strong><asp:Label ID="lblKmModRemaining" runat="server" Text="—" /></strong>
                            </div>
                            <asp:Label ID="lblKmModLimit" runat="server" Visible="false" />
                            <span id="spMinKm" style="display:none;">—</span>
                            <span id="spMaxKm" style="display:none;">—</span>
                        </asp:Panel>

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
                                    <span id="spClientKmError" class="text-danger small" style="display:none;"></span>
                                </div>

                                <div id="routeSummaryPanel"
                                    class="alert alert-info mt-2 km-route-summary"
                                    style="display: none;">

                                    <div class="km-route-summary-title d-flex align-items-center justify-content-between mb-1">
                                        <span class="font-weight-bold">Route Summary</span>
                                        <button type="button"
                                            class="btn btn-link km-route-info-btn collapsed"
                                            data-toggle="collapse"
                                            data-target="#kmRouteDetails"
                                            aria-expanded="false"
                                            aria-controls="kmRouteDetails"
                                            title="Route Details"
                                            aria-label="Route Details">
                                            <i class="fa fa-info-circle" aria-hidden="true"></i>
                                        </button>
                                    </div>
                                    <div>
                                        <strong>Distance:</strong>
                                        <span id="spRouteDistance"></span> KM
                                    </div>
                                    <div>
                                        <strong>Stops:</strong>
                                        <span id="spWaypointCount"></span>
                                    </div>
                                    <div id="kmRouteDetails" class="collapse km-route-details">
                                        <div class="km-route-address mt-1">
                                            <strong>Origin:</strong>
                                            <span id="spRouteOrigin"></span>
                                        </div>
                                        <div class="km-route-address">
                                            <strong>Destination:</strong>
                                            <span id="spRouteDestination"></span>
                                        </div>
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

                        <div class="km-history-section">
                            <button type="button"
                                id="kmHistoryToggle"
                                class="btn btn-link btn-sm p-0 mb-1 km-history-toggle km-accordion-toggle collapsed"
                                data-toggle="collapse"
                                data-target="#kmHistoryCollapse"
                                aria-expanded="false"
                                aria-controls="kmHistoryCollapse"
                                style="display: none;">
                                KM Change History
                                <i class="fa fa-chevron-down km-accordion-chevron"></i>
                            </button>
                            <div id="kmHistoryCollapse" class="collapse">
                                <div class="table-responsive km-history-table-wrap">
                                    <asp:GridView ID="gvKmHistory" runat="server" AutoGenerateColumns="false" ShowHeaderWhenEmpty="false" CssClass="table table-sm table-bordered table-finance"
                                        EmptyDataText="No previous KM changes.">
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
                            <p id="kmHistoryEmpty" class="text-muted small mb-0" style="display: none;">No previous KM changes.</p>
                        </div>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnSaveKm" EventName="Click" />
                            </Triggers>
                        </asp:UpdatePanel>
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
                            <asp:HiddenField ID="HF_FixedTravelAllowance" runat="server" />
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
                                <div class="col-md-5 form-group mb-md-0">
                                    <label class="font-weight-bold text-primary">2. Select Expense Type</label>
                                    <asp:DropDownList ID="DDL_ExpenseType" runat="server" ClientIDMode="Static" CssClass="form-control form-control-sm border-primary" Style="font-weight: 600;">
                                        <asp:ListItem Value="DA">DA</asp:ListItem>
                                        <asp:ListItem Value="Hotel">Hotel</asp:ListItem>
                                        <asp:ListItem Value="Travel">Travel</asp:ListItem>
                                        <asp:ListItem Value="Stationary">Stationary</asp:ListItem>
                                    </asp:DropDownList>
                                    <small id="spExpenseCategoryDesc" class="text-muted expense-category-desc mt-1 d-block" style="display: none;"></small>
                                    <small class="text-danger font-weight-bold mt-1 d-block" id="txtExpenseTypeHint" style="display: none;">
                                        <i class="fa fa-info-circle"></i>Restricted by selected Policy Mode.
                                    </small>
                                </div>
                                <div class="col-md-7">
                                    <div id="divExpensePolicyLimit" class="alert alert-light border small mb-0 h-100">
                                        <div class="font-weight-bold text-muted mb-1">Applicable Policy Limit</div>
                                        <div id="spExpensePolicyTitle" class="text-primary font-weight-bold">Select an expense type</div>
                                        <div id="spExpensePolicyDetail" class="text-dark">Resolved policy limits will appear here.</div>
                                    </div>
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
                                    <label class="d-block">
                                        Attach Receipt
                                        <span id="spReceiptRequirement" class="badge badge-secondary ml-1">—</span>
                                    </label>
                                    <asp:FileUpload ID="FU_Attachment" runat="server" CssClass="form-control-file" accept=".jpg,.jpeg,.png,.pdf" />
                                    <div id="divUploadGuidance" class="expense-upload-guidance mt-2" style="display: none;">
                                        <div class="row">
                                            <div class="col-sm-6">
                                                <strong>Accepted Formats</strong>
                                                <ul>
                                                    <li>PDF</li>
                                                    <li>JPG</li>
                                                    <li>PNG</li>
                                                </ul>
                                            </div>
                                            <div class="col-sm-6">
                                                <strong>Maximum Size</strong>
                                                <div>5 MB</div>
                                            </div>
                                        </div>
                                        <div class="expense-upload-tip mt-1">
                                            <i class="fa fa-info-circle"></i>
                                            Upload a clear image or PDF of the original receipt. Large or blurry files may delay claim approval.
                                        </div>
                                    </div>
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
                                    <tr runat="server" id="trExpenseRow">
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
                                            <asp:Literal ID="litExpenseReturnRemarks" runat="server" Visible="false" />
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

        <div id="pageLoader" aria-live="polite" aria-busy="true">
            <div class="page-loader-card">
                <i class="fa fa-spinner fa-spin fa-3x text-success mb-3"></i>
                <h4 id="pageLoaderTitle" class="text-success font-weight-bold mb-2">Processing, please wait...</h4>
                <p id="pageLoaderSubtitle" class="text-muted mb-0">Please wait.</p>
            </div>
        </div>

        <div class="modal fade" id="policyModal" tabindex="-1" role="dialog" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
                <div class="modal-content shadow-lg border-0 rounded-lg overflow-hidden">
                    <div class="modal-header bg-primary text-white py-2">
                        <h5 class="modal-title mb-0">
                            <i class="fa fa-file-alt mr-2"></i>Resolved Policy
                        </h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close" onclick="$('#policyModal').modal('hide');">
                            <span aria-hidden="true">×</span>
                        </button>
                    </div>
                    <div class="modal-body bg-light px-4 pt-3 pb-2">
                        <div id="resolvedPolicyLoadError" class="alert alert-danger" style="display:none;"></div>
                        <div id="resolvedPolicyLoading" class="text-muted small mb-2" style="display:none;">
                            <i class="fa fa-spinner fa-spin mr-1"></i>
                            <strong>Loading policy...</strong>
                            <span class="d-block">Please hold on.</span>
                        </div>
                        <div class="card border shadow-sm mb-3">
                            <div class="card-body py-3">
                                <div class="row">
                                    <div class="col-sm-7 mb-2 mb-sm-0">
                                        <div class="small text-muted text-uppercase">Resolved Policy</div>
                                        <div class="h5 mb-0 text-dark font-weight-bold">
                                            <span id="rpHeaderEmployeeCode"><asp:Label ID="lblPolicyEmployeeCode" runat="server" Text="—"></asp:Label></span>
                                        </div>
                                    </div>
                                    <div class="col-sm-5 text-right">
                                        <span id="rpHeaderIsActive"><asp:Label ID="lblPolicyIsActive" runat="server" CssClass="badge badge-success">—</asp:Label></span>
                                        <span id="rpHeaderSource"><asp:Label ID="lblPolicySource" runat="server" CssClass="badge badge-info" style="margin-left:4px;">—</asp:Label></span>
                                        <span id="rpHeaderPriority"><asp:Label ID="lblPolicyPriority" runat="server" CssClass="badge badge-secondary" style="margin-left:4px;">—</asp:Label></span>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <ul class="nav nav-tabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <a class="nav-link active" href="#policyDetailsTab" id="policyDetailsTabLink" aria-controls="policyDetailsTab" role="tab" data-toggle="tab" aria-selected="true">
                                    <i class="fa fa-info-circle"></i> Policy Details
                                </a>
                            </li>
                            <li class="nav-item" role="presentation">
                                <a class="nav-link" href="#claimLimitsTab" id="claimLimitsTabLink" aria-controls="claimLimitsTab" role="tab" data-toggle="tab" aria-selected="false">
                                    <i class="fa fa-calculator"></i> Claim Limits
                                </a>
                            </li>
                            <li class="nav-item" role="presentation">
                                <a class="nav-link" href="#approvalHierarchyTab" id="approvalHierarchyTabLink" aria-controls="approvalHierarchyTab" role="tab" data-toggle="tab" aria-selected="false">
                                    <i class="fa fa-user-check"></i> Approval Hierarchy
                                </a>
                            </li>
                        </ul>

                        <div class="tab-content bg-white border border-top-0 rounded-bottom p-3 mb-2">
                            <div role="tabpanel" class="tab-pane fade show active" id="policyDetailsTab" aria-labelledby="policyDetailsTabLink">
                                <%-- Phase 1: JS-owned render target. Server Labels kept for BindPolicyToModal backward compatibility. --%>
                                <div id="policyDetailsBody"></div>
                                <div id="serverPolicyDetailsFallback" style="display:none;">
                                    <div class="row">
                                        <div class="col-md-6 mb-3">
                                            <label class="small text-secondary mb-1">Record ID</label>
                                            <asp:Label ID="lblPolicyRowId" runat="server" CssClass="d-block text-dark font-weight-bold">—</asp:Label>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="small text-secondary mb-1">Effective Period</label>
                                            <asp:Label ID="lblPolicyEffective" runat="server" CssClass="d-block text-dark">—</asp:Label>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="small text-secondary mb-1">Specificity</label>
                                        <asp:Label ID="lblPolicySpecificity" runat="server" CssClass="d-block text-primary font-weight-bold">—</asp:Label>
                                    </div>
                                    <div class="row">
                                        <div class="col-md-6 mb-3">
                                            <label class="small text-secondary mb-1">Designation</label>
                                            <asp:Label ID="lblPolicyDesignation" runat="server" CssClass="d-block text-dark font-weight-bold">—</asp:Label>
                                        </div>
                                        <div class="col-md-6 mb-3">
                                            <label class="small text-secondary mb-1">Grade</label>
                                            <asp:Label ID="lblPolicyGrade" runat="server" CssClass="d-block text-dark font-weight-bold">—</asp:Label>
                                        </div>
                                    </div>
                                    <div class="mb-2" id="policyRemarksSection" runat="server" visible="false">
                                        <label class="small text-secondary mb-1">Policy Remarks</label>
                                        <div class="alert alert-info mb-0" style="white-space: pre-wrap; word-break: break-word;">
                                            <i class="fa fa-info-circle mr-1"></i>
                                            <asp:Label ID="lblResolvedRemarks" runat="server" Text=""></asp:Label>
                                        </div>
                                    </div>
                                    <div class="form-group mb-0" id="Toggler" runat="server" visible="false">
                                        <asp:Button ID="btnToggleRawJson" runat="server" CssClass="btn btn-sm btn-outline-dark mb-2" OnClientClick="$('#policyJsonRaw').toggle(); return false;" Text="Toggle Raw JSON" />
                                        <asp:TextBox ID="policyJsonRaw" runat="server" TextMode="MultiLine" Rows="6" CssClass="form-control form-control-sm font-monospace text-dark bg-light border border-secondary" Visible="false"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div role="tabpanel" class="tab-pane fade" id="claimLimitsTab" aria-labelledby="claimLimitsTabLink">
                                <div class="card border-0 bg-light mb-0">
                                    <div class="card-body p-2">
                                        <div id="claimLimitsBody"></div>
                                        <%-- Phase 1: PlaceHolder retained for BindPolicyToModal; not shown in UI. --%>
                                        <div style="display:none;"><asp:PlaceHolder ID="phClaimLimits" runat="server"></asp:PlaceHolder></div>
                                    </div>
                                </div>
                            </div>

                            <div role="tabpanel" class="tab-pane fade" id="approvalHierarchyTab" aria-labelledby="approvalHierarchyTabLink">
                                <div id="approvalHierarchyBody"></div>
                                <div style="display:none;"><asp:PlaceHolder ID="phApproverHierarchy" runat="server"></asp:PlaceHolder></div>
                            </div>
                        </div>

                        <div class="hidden">
                            <asp:Label ID="lblPolicyDesigGrade" runat="server" Text="—"></asp:Label>
                            <asp:Label ID="lblResolvedMaxKM" runat="server" Text="0"></asp:Label>
                            <asp:Label ID="lblResolvedRatePerKm" runat="server" Text="0"></asp:Label>
                            <asp:Label ID="lblResolvedDA" runat="server" Text="0"></asp:Label>
                            <asp:Label ID="lblResolvedHotelCap" runat="server" Text="0"></asp:Label>
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

    <script src="<%= ResolveUrl("~/Scripts/claims-ux-stabilization.js") %>"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            if (window.ClaimsUx && ClaimsUx.initOnce) {
                ClaimsUx.initOnce({
                    dirtySelector: '.claims-ux-track-dirty, .km-input, #expenseModal input, #expenseModal textarea, #expenseModal select, #txtModalKM, #txtKmReason'
                });
            }

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

        if (typeof Sys !== 'undefined' && Sys.Application) {
            Sys.Application.add_load(function () {
                if (window.ClaimsUx && ClaimsUx.initOnce) {
                    ClaimsUx.initOnce({
                        dirtySelector: '.claims-ux-track-dirty, .km-input, #expenseModal input, #expenseModal textarea, #expenseModal select, #txtModalKM, #txtKmReason'
                    });
                }
                initTooltipsAndIcons();
                keepSummaryPanelExpanded();
            });
        }

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
                if (window.ClaimsUx) ClaimsUx.hideProcessing();
                hidePageLoader();
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

            if (typeof updateExpensePolicyLimit === 'function') updateExpensePolicyLimit();

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

        function updateExpensePolicyLimit() {
            var $ddl = $resolve('DDL_ExpenseType');
            var type = $ddl.length ? $ddl.val() : '';
            var title = document.getElementById('spExpensePolicyTitle');
            var detail = document.getElementById('spExpensePolicyDetail');
            if (!title || !detail) return;

            var mode = ($resolve('HF_ResolvedPolicyMode').val() || 'HQ');
            var fmt = function (v) { return (v && v !== '0') ? ('₹ ' + parseFloat(v).toFixed(2)) : '—'; };

            if (type === 'Travel') {
                title.innerText = 'Travel';
                var rate = $resolve('HF_ResolvedRatePerKm').val();
                var fixed = $resolve('HF_FixedTravelAllowance').val();
                var parts = ['Rate per KM: ' + fmt(rate)];
                if (fixed && parseFloat(fixed) > 0) parts.push('Fixed travel allowance: ' + fmt(fixed) + ' (when travelled KM > 0)');
                detail.innerText = parts.join(' | ');
            } else if (type === 'Hotel') {
                title.innerText = 'Hotel';
                detail.innerText = 'Maximum hotel limit: ' + fmt($resolve('HF_ResolvedHotelCap').val()) + ' per night';
            } else if (type === 'DA') {
                title.innerText = 'Daily Allowance';
                var daVal = $resolve('HF_ResolvedDA').val();
                if (mode === 'Outstation') daVal = $resolve('HF_OutstationDA').val() || daVal;
                else if (mode === 'OwnArrangement') daVal = $resolve('HF_OwnArrangementDA').val() || daVal;
                detail.innerText = 'Daily allowance (' + mode + '): ' + fmt(daVal);
            } else if (type === 'Stationary') {
                title.innerText = 'Stationary';
                detail.innerText = 'No specific policy cap configured for stationary expenses.';
            } else {
                title.innerText = 'Select an expense type';
                detail.innerText = 'Resolved policy limits will appear here.';
            }
        }

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
                updateExpenseEntryGuidance(false);
                return;
            } else {
                if ($btnSave.length) $btnSave.prop('disabled', false);
            }

            var uploadVisible = false;

            if (type === 'DA') {
                safeShow($da);
            }
            else if (type === 'Hotel') {
                safeShow($ht);
                safeShow($fu);
                $fuInput.prop('disabled', false);
                uploadVisible = true;
            }
            else if (type === 'Travel') {
                safeShow($tr);
                if ($chk.length && $chk.prop('checked')) {
                    safeShow($fu);
                    safeShow($inv);
                    $fuInput.prop('disabled', false);
                    uploadVisible = true;
                }
            }
            else if (type === 'Stationary') {
                safeShow($st);
                safeShow($fu);
                $fuInput.prop('disabled', false);
                uploadVisible = true;
            }

            updateExpensePolicyLimit();
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
            if (window.__so_expenseSaveInFlight) return false;
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
            window.__so_expenseSaveInFlight = true;
            var $btnSave = $resolve('BTN_SaveExpense');
            if ($btnSave.length) {
                $btnSave.css({ 'pointer-events': 'none', 'opacity': '0.6' });
                setTimeout(function () { $btnSave.prop('disabled', true); }, 0);
            }
            if (typeof showPageLoader === 'function') {
                showPageLoader('Saving expense...', 'Please wait.');
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
            var activityId = $resolve('HF_ActivityId').val() || $resolve('hdnActivityId').val();
            restoreRouteDataToDAFields(activityId);
        });

        function activatePolicyModalFirstTab() {
            var $modal = $('#policyModal');
            if (!$modal.length) return;

            var $firstTabLink = $modal.find('.nav-tabs a[data-toggle="tab"]').first();
            if ($firstTabLink.length) {
                $firstTabLink.tab('show');
            } else {
                $modal.find('.tab-pane').removeClass('show active');
                $modal.find('#policyDetailsTab').addClass('show active');
            }
        }

        // ------------------------------------------------------------------
        // Resolved Policy self-loading component (Phase 1)
        // ------------------------------------------------------------------
        window.resolvedPolicyCache = window.resolvedPolicyCache || null;
        var resolvedPolicyRequestId = 0;
        var currentResolvedPolicyRequest = null;

        function getResolvedPolicyCacheKey() {
            var year = $.trim($('#<%= DDL_Year.ClientID %>').val() || '');
            var month = $.trim($('#<%= DDL_Month.ClientID %>').val() || '');
            return year + '|' + month;
        }

        function invalidateResolvedPolicyCache() {
            window.resolvedPolicyCache = null;
        }

        function isResolvedPolicyRequestCurrent(requestId) {
            return requestId === resolvedPolicyRequestId;
        }

        function abortInFlightResolvedPolicyRequest() {
            if (currentResolvedPolicyRequest && currentResolvedPolicyRequest.readyState !== 4) {
                try { currentResolvedPolicyRequest.abort(); } catch (e) { /* ignore */ }
            }
            currentResolvedPolicyRequest = null;
        }

        function escapeHtml(value) {
            if (value === null || value === undefined) return '';
            return String(value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function setResolvedPolicyLoading(isLoading) {
            if (isLoading) {
                $('#resolvedPolicyLoading').show();
                $('#resolvedPolicyLoadError').hide().text('');
            } else {
                $('#resolvedPolicyLoading').hide();
            }
        }

        function showResolvedPolicyError(message) {
            $('#resolvedPolicyLoadError').text(message || 'Unable to load resolved policy.').show();
            $('#policyDetailsBody').empty();
            $('#claimLimitsBody').empty();
            $('#approvalHierarchyBody').empty();
        }

        function applyResolvedPolicyHiddenFields(hiddenFields) {
            if (!hiddenFields) return;
            var map = [
                { id: '<%= HF_ResolvedHotelCap.ClientID %>', value: hiddenFields.hotelCap },
                { id: '<%= HF_ResolvedDA.ClientID %>', value: hiddenFields.da },
                { id: '<%= HF_OutstationDA.ClientID %>', value: hiddenFields.outstationDA },
                { id: '<%= HF_OwnArrangementDA.ClientID %>', value: hiddenFields.ownArrangementDA },
                { id: '<%= HF_ResolvedMaxKM.ClientID %>', value: hiddenFields.maxKM },
                { id: '<%= HF_ResolvedRatePerKm.ClientID %>', value: hiddenFields.ratePerKm },
                { id: '<%= HF_FixedTravelAllowance.ClientID %>', value: hiddenFields.fixedTravelAllowance },
                { id: '<%= HF_Policy_RegionId.ClientID %>', value: hiddenFields.regionId },
                { id: '<%= HF_Policy_BranchId.ClientID %>', value: hiddenFields.branchId },
                { id: '<%= HF_Policy_GradeId.ClientID %>', value: hiddenFields.gradeId },
                { id: '<%= HF_Policy_DesignationId.ClientID %>', value: hiddenFields.designationId }
            ];
            for (var i = 0; i < map.length; i++) {
                var el = document.getElementById(map[i].id);
                if (el) el.value = map[i].value != null ? map[i].value : '';
            }
        }

        function renderResolvedPolicyHeader(policy) {
            if (!policy) return;
            var $emp = $('#<%= lblPolicyEmployeeCode.ClientID %>');
            if ($emp.length) $emp.text(policy.employeeCode || '—');

            var $active = $('#<%= lblPolicyIsActive.ClientID %>');
            if ($active.length) {
                $active.text(policy.isActiveLabel || (policy.isActive ? 'Active' : 'Inactive'));
                $active.removeClass('badge-success badge-secondary')
                    .addClass(policy.isActive ? 'badge badge-success' : 'badge badge-secondary');
            }

            var $source = $('#<%= lblPolicySource.ClientID %>');
            if ($source.length) $source.text(policy.source || '—');

            var $priority = $('#<%= lblPolicyPriority.ClientID %>');
            if ($priority.length) {
                $priority.text(policy.priority != null && policy.priority !== ''
                    ? ('Priority ' + policy.priority)
                    : '—');
            }
        }

        function renderPolicyDetailsBody(policy) {
            var $body = $('#policyDetailsBody');
            $body.empty();
            if (!policy) {
                $body.html('<div class="text-muted small">No policy details available.</div>');
                return;
            }

            var remarksHtml = '';
            if (policy.remarks) {
                remarksHtml =
                    '<div class="mb-2">' +
                    '  <label class="small text-secondary mb-1">Policy Remarks</label>' +
                    '  <div class="alert alert-info mb-0" style="white-space: pre-wrap; word-break: break-word;">' +
                    '    <i class="fa fa-info-circle mr-1"></i>' + escapeHtml(policy.remarks) +
                    '  </div>' +
                    '</div>';
            }

            var html =
                '<div class="row">' +
                '  <div class="col-md-6 mb-3">' +
                '    <label class="small text-secondary mb-1">Record ID</label>' +
                '    <div class="d-block text-dark font-weight-bold">' + escapeHtml(policy.rowId != null ? policy.rowId : '—') + '</div>' +
                '  </div>' +
                '  <div class="col-md-6 mb-3">' +
                '    <label class="small text-secondary mb-1">Effective Period</label>' +
                '    <div class="d-block text-dark">' + escapeHtml(policy.effectivePeriod || '—') + '</div>' +
                '  </div>' +
                '</div>' +
                '<div class="mb-3">' +
                '  <label class="small text-secondary mb-1">Specificity</label>' +
                '  <div class="d-block text-primary font-weight-bold">' + escapeHtml(policy.specificity || '—') + '</div>' +
                '</div>' +
                '<div class="row">' +
                '  <div class="col-md-6 mb-3">' +
                '    <label class="small text-secondary mb-1">Designation</label>' +
                '    <div class="d-block text-dark font-weight-bold">' + escapeHtml(policy.designation || '—') + '</div>' +
                '  </div>' +
                '  <div class="col-md-6 mb-3">' +
                '    <label class="small text-secondary mb-1">Grade</label>' +
                '    <div class="d-block text-dark font-weight-bold">' + escapeHtml(policy.grade || '—') + '</div>' +
                '  </div>' +
                '</div>' +
                remarksHtml;

            $body.html(html);
        }

        function renderClaimLimitsBody(claimLimits) {
            var $body = $('#claimLimitsBody');
            $body.empty();
            if (!claimLimits || !claimLimits.length) {
                $body.html('<div class="alert alert-light border text-muted small mb-0">No resolved claim limits are available for this policy.</div>');
                return;
            }

            var html = '';
            for (var i = 0; i < claimLimits.length; i++) {
                var item = claimLimits[i] || {};
                html +=
                    '<div class="row py-2 border-bottom">' +
                    '  <div class="col-xs-6 col-sm-5 text-muted">' + escapeHtml(item.label || '') + '</div>' +
                    '  <div class="col-xs-6 col-sm-7"><strong>' + escapeHtml(item.value || '') + '</strong></div>' +
                    '</div>';
            }
            $body.html(html);
        }

        function renderApprovalHierarchyBody(levels) {
            var $body = $('#approvalHierarchyBody');
            $body.empty();
            if (!levels || !levels.length) {
                $body.html('<div class="text-muted small">No approvers configured for this employee.</div>');
                return;
            }

            var html = '';
            for (var i = 0; i < levels.length; i++) {
                var level = levels[i] || {};
                var nameHtml = level.name
                    ? '<div class="font-weight-bold text-dark">' + escapeHtml(level.name) + '</div>'
                    : '';
                var codeHtml = level.code
                    ? '<div class="text-muted small">' + escapeHtml(level.code) + '</div>'
                    : '';
                html +=
                    '<div class="card border shadow-sm mb-0">' +
                    '  <div class="card-body py-2 px-3">' +
                    '    <div class="small text-uppercase text-muted font-weight-bold mb-1">' + escapeHtml(level.title || '') + '</div>' +
                    nameHtml + codeHtml +
                    '  </div>' +
                    '</div>';
                if (i < levels.length - 1) {
                    html += '<div class="text-center text-muted py-1"><i class="fa fa-arrow-down"></i></div>';
                }
            }
            $body.html(html);
        }

        function renderResolvedPolicyResponse(payload) {
            if (!payload || !payload.success || !payload.policy) {
                showResolvedPolicyError((payload && payload.message) || 'No resolved policy found.');
                return;
            }

            $('#resolvedPolicyLoadError').hide().text('');
            renderResolvedPolicyHeader(payload.policy);
            renderPolicyDetailsBody(payload.policy);
            renderClaimLimitsBody(payload.claimLimits || []);
            renderApprovalHierarchyBody(payload.approvalHierarchy || []);
            applyResolvedPolicyHiddenFields(payload.policy.hiddenFields);
        }

        function fetchResolvedPolicy(forceRefresh, requestId) {
            var cacheKey = getResolvedPolicyCacheKey();
            if (!forceRefresh && window.resolvedPolicyCache && window.resolvedPolicyCache.key === cacheKey && window.resolvedPolicyCache.payload) {
                return $.Deferred().resolve(window.resolvedPolicyCache.payload).promise();
            }

            var year = parseInt($('#<%= DDL_Year.ClientID %>').val(), 10);
            var month = parseInt($('#<%= DDL_Month.ClientID %>').val(), 10);

            abortInFlightResolvedPolicyRequest();

            currentResolvedPolicyRequest = $.ajax({
                type: 'POST',
                url: 'SalesMonthlyReview.aspx/GetResolvedPolicy',
                data: JSON.stringify({ year: year, month: month }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json'
            });

            return currentResolvedPolicyRequest.then(function (response) {
                if (!isResolvedPolicyRequestCurrent(requestId)) {
                    return $.Deferred().reject({ stale: true }).promise();
                }

                var payload = response && response.d ? response.d : response;
                if (payload && payload.success) {
                    window.resolvedPolicyCache = { key: cacheKey, payload: payload };
                } else {
                    invalidateResolvedPolicyCache();
                }
                return payload;
            }, function (xhr, textStatus) {
                // Aborted prior requests and superseded responses must not surface as UI errors.
                if (textStatus === 'abort' || !isResolvedPolicyRequestCurrent(requestId)) {
                    return $.Deferred().reject({ stale: true, aborted: textStatus === 'abort' }).promise();
                }
                return $.Deferred().reject(xhr).promise();
            });
        }

        function openResolvedPolicyModal(forceRefresh) {
            var requestId = ++resolvedPolicyRequestId;
            showPageLoader('Loading policy...', 'Please hold on.');
            setResolvedPolicyLoading(true);
            $('#resolvedPolicyLoadError').hide().text('');
            $('#policyModal').modal('show');

            fetchResolvedPolicy(!!forceRefresh, requestId)
                .done(function (payload) {
                    if (!isResolvedPolicyRequestCurrent(requestId)) return;
                    renderResolvedPolicyResponse(payload);
                    activatePolicyModalFirstTab();
                })
                .fail(function (err) {
                    if (!isResolvedPolicyRequestCurrent(requestId)) return;
                    if (err && (err.stale || err.aborted)) return;

                    var msg = 'Unable to load resolved policy.';
                    if (err && err.responseJSON && err.responseJSON.Message) msg = err.responseJSON.Message;
                    showResolvedPolicyError(msg);
                })
                .always(function () {
                    if (!isResolvedPolicyRequestCurrent(requestId)) return;
                    setResolvedPolicyLoading(false);
                    hidePageLoader();
                    currentResolvedPolicyRequest = null;
                });
        }

        $('#policyModal').on('shown.bs.modal', function () {
            activatePolicyModalFirstTab();
        });

        $(function () {
            $('#<%= btn_fetch.ClientID %>').on('click', function () {
                invalidateResolvedPolicyCache();
            });
            $('#<%= DDL_Year.ClientID %>, #<%= DDL_Month.ClientID %>').on('change', function () {
                invalidateResolvedPolicyCache();
            });
        });

        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initExpenseModal();
                activatePolicyModalFirstTab();
                var kmOpen = $('#editKmModal').hasClass('show') || $('#editKmModal').hasClass('in');
                if (typeof bindKmModalScrollLock === 'function') {
                    bindKmModalScrollLock();
                }
                if (kmOpen && typeof lockKmModalPageScroll === 'function') {
                    lockKmModalPageScroll();
                }
                if (kmOpen && typeof refreshKmHistoryPresentation === 'function') {
                    refreshKmHistoryPresentation({
                        expand: window.__kmHistoryExpandAfterSave === true
                            || window.__kmHistoryStayExpandedUntilHidden === true
                    });
                }
            });
        }

        window.openEditKmModal = function (aid, cur, min, max, modLimit, modRemaining) {
            $resolve('hdnActivityId').val(aid); $resolve('txtModalKM').val(cur);
            $resolve('hdnOriginalKm').val(cur);
            $('#<%=HF_RouteActivityId.ClientID%>').val(aid);
            document.getElementById('spMinKm').innerText = min || '—';
            document.getElementById('spMaxKm').innerText = max || '—';
            $resolve('lblModalOriginalKm').text((cur || '—') + ' km');
            $resolve('lblModalCurrentKm').text((cur || '—') + ' km');
            if (min && max) $resolve('lblModalAllowedRange').text(min + ' – ' + max + ' km');
            if (modLimit) $resolve('lblKmModLimit').text(modLimit);
            if (modRemaining) $resolve('lblKmModRemaining').text(modRemaining);
            var canSave = !modRemaining || modRemaining.indexOf('0 of') !== 0;
            $resolve('btnSaveKm').prop('disabled', !canSave);
            $('#spClientKmError').hide();
            if (typeof initializeKmModalSessionClient === 'function') initializeKmModalSessionClient(aid, true);
            $('#editKmModal').modal('show');
            if (typeof lockKmModalPageScroll === 'function') lockKmModalPageScroll();
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
            if (window.__so_submitInFlight) return;
            window.__so_submitInFlight = true;
            $('#submitModalConfirm, #submitModalCancel, [id$="btn_submit"]').prop('disabled', true).css('opacity', '0.6');
            $('#submitConfirmModal').modal('hide');
            if (window.ClaimsUx) ClaimsUx.clearDirty();
            showPageLoader('Submitting your claim...', 'Please wait while we send your claim for approval.');
            __doPostBack('<%= btn_submit.UniqueID %>', '');
        };
        window.enableButtonsAfterModalClose = function () {
            if (window.__so_submitInFlight) return;
            $('#submitModalConfirm, #submitModalCancel, [id$="btn_submit"]').prop('disabled', false).css('opacity', '1');
            hidePageLoader();
        };

        window.showPageLoader = function (title, subtitle) {
            var t = document.getElementById('pageLoaderTitle');
            var s = document.getElementById('pageLoaderSubtitle');
            if (t) t.textContent = title || 'Processing, please wait...';
            if (s) s.textContent = subtitle || '';
            var e = document.getElementById('pageLoader');
            if (e) e.style.display = 'block';
        };
        window.hidePageLoader = function () {
            var e = document.getElementById('pageLoader');
            if (e) e.style.display = 'none';
        };

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

        window.applyExpenseModalActivityContext = function (activityId) {
            if (!activityId) return;
            var nextId = String(activityId);
            $resolve('HF_ActivityId').val(nextId);
            $resolve('hdnActivityId').val(nextId);
            if (typeof initializeKmModalSessionClient === 'function') {
                initializeKmModalSessionClient(nextId, true);
            } else {
                $('#<%=HF_RouteActivityId.ClientID%>').val(nextId);
            }
        };

        window.openExpenseModalSafe = function (activityId, savedMode, remark) {
            if (typeof applyExpenseModalActivityContext === 'function') {
                applyExpenseModalActivityContext(activityId);
            }

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
            $('#<%=HF_KmSaveWorkflow.ClientID%>').val(KmSaveWorkflowManual);
            if (hideRouteSummary) $('#routeSummaryPanel').hide();
        };

        window.refreshKmHistoryPresentation = function (options) {
            options = options || {};
            var expand = options.expand === true
                || window.__kmHistoryExpandAfterSave === true
                || window.__kmHistoryStayExpandedUntilHidden === true;
            var $table = $('#editKmModal table[id$="gvKmHistory"]');
            var $wrap = $('#editKmModal .km-history-table-wrap');
            var $toggle = $('#kmHistoryToggle');
            var $empty = $('#kmHistoryEmpty');
            var $collapse = $('#kmHistoryCollapse');
            var $dataRows = $table.find('tr').filter(function () {
                return $(this).children('td').length >= 4;
            });
            var hasRows = $dataRows.length > 0;

            $dataRows.removeClass('km-history-latest km-history-prior');
            if (expand && hasRows) {
                $dataRows.eq(0).addClass('km-history-latest');
                $dataRows.slice(1).addClass('km-history-prior');
            }

            if (hasRows) {
                $empty.hide();
                $wrap.show();
                $toggle.show();
                if (expand) {
                    if ($collapse.collapse) $collapse.collapse('show');
                    else $collapse.addClass('show').addClass('in');
                    $toggle.removeClass('collapsed').attr('aria-expanded', 'true');
                    if (options.expand === true || window.__kmHistoryExpandAfterSave === true) {
                        window.__kmHistoryStayExpandedUntilHidden = true;
                    }
                    var latest = $dataRows.get(0);
                    if (latest && latest.scrollIntoView) {
                        latest.scrollIntoView({ block: 'nearest', inline: 'nearest' });
                    }
                } else {
                    if ($collapse.collapse) $collapse.collapse('hide');
                    else $collapse.removeClass('show').removeClass('in');
                    $toggle.addClass('collapsed').attr('aria-expanded', 'false');
                }
            } else {
                $toggle.hide();
                if ($collapse.collapse) $collapse.collapse('hide');
                else $collapse.removeClass('show').removeClass('in');
                $wrap.hide();
                $empty.show();
            }
        };

        (function bindKmModalMobileUx() {
            var scrollY = 0;
            var viewportHandler = null;

            function viewportHeight() {
                return (window.visualViewport && window.visualViewport.height)
                    ? window.visualViewport.height
                    : window.innerHeight;
            }

            function fitKmModalToViewport() {
                var dialog = document.querySelector('#editKmModal.show .modal-dialog');
                var content = document.querySelector('#editKmModal.show .modal-content');
                if (!dialog || !content) return;
                var max = Math.round(viewportHeight() * 0.9) + 'px';
                dialog.style.maxHeight = max;
                content.style.maxHeight = max;
            }

            function lockPageScroll() {
                var alreadyLocked = document.body.classList.contains('km-modal-open');
                if (!alreadyLocked) {
                    scrollY = window.pageYOffset || document.documentElement.scrollTop || 0;
                    document.body.style.top = '-' + scrollY + 'px';
                }
                document.documentElement.classList.add('km-modal-open');
                document.body.classList.add('km-modal-open');
            }

            function unlockPageScroll() {
                var wasLocked = document.body.classList.contains('km-modal-open')
                    || document.documentElement.classList.contains('km-modal-open');
                document.documentElement.classList.remove('km-modal-open', 'modal-open');
                document.body.classList.remove('km-modal-open', 'modal-open');
                document.body.style.top = '';
                document.body.style.overflow = '';
                document.documentElement.style.overflow = '';
                document.body.style.paddingRight = '';
                document.documentElement.style.paddingRight = '';
                if (window.jQuery) $('.modal-backdrop').remove();
                if (wasLocked) window.scrollTo(0, scrollY || 0);
            }

            window.lockKmModalPageScroll = lockPageScroll;
            window.unlockKmModalPageScroll = unlockPageScroll;

            function onKmModalShow() {
                lockPageScroll();
                if (typeof refreshKmHistoryPresentation === 'function') {
                    refreshKmHistoryPresentation({
                        expand: window.__kmHistoryExpandAfterSave === true
                            || window.__kmHistoryStayExpandedUntilHidden === true
                    });
                }
            }

            function onKmModalShown() {
                if (typeof refreshKmHistoryPresentation === 'function') {
                    refreshKmHistoryPresentation({
                        expand: window.__kmHistoryExpandAfterSave === true
                            || window.__kmHistoryStayExpandedUntilHidden === true
                    });
                }
                if (!window.__kmHistoryStayExpandedUntilHidden) {
                    window.__kmHistoryExpandAfterSave = false;
                }
                fitKmModalToViewport();
                viewportHandler = fitKmModalToViewport;
                if (window.visualViewport) {
                    window.visualViewport.addEventListener('resize', viewportHandler);
                    window.visualViewport.addEventListener('scroll', viewportHandler);
                }
                $(window).off('resize.kmModalUx').on('resize.kmModalUx', viewportHandler);
            }

            function onKmModalHidden() {
                window.__kmHistoryExpandAfterSave = false;
                window.__kmHistoryStayExpandedUntilHidden = false;
                if (window.visualViewport && viewportHandler) {
                    window.visualViewport.removeEventListener('resize', viewportHandler);
                    window.visualViewport.removeEventListener('scroll', viewportHandler);
                }
                $(window).off('resize.kmModalUx');
                viewportHandler = null;
                unlockPageScroll();
            }

            function bindKmModalScrollLock() {
                var $km = $('#editKmModal');
                if (!$km.length) return;

                $km.off('show.bs.modal.kmUx shown.bs.modal.kmUx hidden.bs.modal.kmUx');
                $(document).off('show.bs.modal.kmUx', '#editKmModal');
                $(document).off('shown.bs.modal.kmUx', '#editKmModal');
                $(document).off('hidden.bs.modal.kmUx', '#editKmModal');
                $(document).off('hidden.bs.modal', '#editKmModal', onKmModalHidden);

                $(document).on('show.bs.modal.kmUx', '#editKmModal', onKmModalShow);
                $(document).on('shown.bs.modal.kmUx', '#editKmModal', onKmModalShown);
                $(document).on('hidden.bs.modal', '#editKmModal', onKmModalHidden);

                if ($km.hasClass('show') || $km.hasClass('in')) {
                    lockPageScroll();
                }
            }

            window.bindKmModalScrollLock = bindKmModalScrollLock;
            bindKmModalScrollLock();
            if (window.jQuery) $(bindKmModalScrollLock);
        })();

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
            var wps = window.waypoints;
            var n = wps.length;
            if (n > 0) {
                if (wps[n - 1].name === name) {
                    alert('Location already added.');
                    return;
                }
                var isRoundTripClose = (n >= 2 && wps[0].name === name);
                var blocked = wps.some(function (x, i) {
                    if (isRoundTripClose && i === 0) return false;
                    return x.name === name;
                });
                if (blocked) {
                    alert('Location already added.');
                    return;
                }
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
            if ($('#kmRouteDetails').collapse) $('#kmRouteDetails').collapse('hide');
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
