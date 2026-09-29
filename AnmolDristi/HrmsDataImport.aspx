<%@ Page Title="Upload & Map HRMS Data" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="HrmsDataImport.aspx.cs" Inherits="AnmolDristi.HrmsDataImport" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/select2/3.5.3/select2.min.css" rel="stylesheet" />
    
    <style>
        .mapping-grid { margin-top: 15px; }
        .mapping-row { padding: 8px 0; border-bottom: 1px solid #eee; }
        .mapping-label { font-weight: bold; color: #333; padding-top: 6px; }
        
        /* Make Select2 v3 match Bootstrap's form-control-sm styling */
        .select2-container .select2-choice {
            height: 30px;
            line-height: 28px;
            border: 1px solid #ccc;
            border-radius: 3px;
            background-image: none;
        }
        .select2-container .select2-choice .select2-arrow {
            background-image: none;
            border-left: 1px solid #ccc;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>

    <script type="text/javascript">
        function initSearchableDropdowns() {
            // Check if Select2 loaded successfully before executing
            if ($.fn.select2) {
                $('.searchable-dropdown').select2({
                    width: '100%',
                    placeholder: "-- Ignore / Do Not Map --"
                });
            }
        }

        $(document).ready(function () {
            // Dynamically load Select2 JS to ensure jQuery 1.7.2 is fully loaded first
            if (!$.fn.select2) {
                $.getScript("https://cdnjs.cloudflare.com/ajax/libs/select2/3.5.3/select2.min.js", function () {
                    initSearchableDropdowns();
                });
            } else {
                initSearchableDropdowns();
            }
        });

        // Re-bind after ASP.NET AJAX Postbacks (Step 2 Panel View)
        if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initSearchableDropdowns();
            });
        }
    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>HRMS Data Import Pipeline</h5>
                </div>
            </div>
            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Step 1: Upload HRMS Data (CSV)</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:Label ID="lblMessage" runat="server" CssClass="font-weight-bold d-block mb-3"></asp:Label>

                            <asp:Panel ID="pnlUpload" runat="server" CssClass="form-horizontal form-label-left">
                                <div class="form-group row">
                                    <label class="control-label col-md-3 col-sm-3 text-right font-weight-bold">Select CSV File <span class="required">*</span></label>
                                    <div class="col-md-6 col-sm-6">
                                        <asp:FileUpload ID="fileUploadCsv" runat="server" CssClass="form-control" accept=".csv" />
                                    </div>
                                </div>
                                <div class="ln_solid"></div>
                                <div class="form-group row">
                                    <div class="col-md-6 col-sm-6 offset-md-3">
                                        <asp:Button ID="btnReadHeaders" runat="server" Text="Analyze File Headers" CssClass="btn btn-primary" OnClick="btnReadHeaders_Click" />
                                    </div>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlMapping" runat="server" Visible="false">
                                <div class="alert alert-info">
                                    <strong>File Analyzed Successfully!</strong> Please verify the column mappings below. EmpCode is required. After upload you will be redirected to Batch Manager: synchronize masters first, then choose one employee action.
                                </div>

                                <div class="row mapping-grid bg-light p-3 border">
                                    <div class="col-md-5 font-weight-bold text-uppercase">Target Database Column</div>
                                    <div class="col-md-2 text-center"><i class="fa fa-arrow-right"></i></div>
                                    <div class="col-md-5 font-weight-bold text-uppercase">Source CSV Column</div>
                                </div>

                                <asp:Repeater ID="rptMapping" runat="server" OnItemDataBound="rptMapping_ItemDataBound">
                                    <ItemTemplate>
                                        <div class="row mapping-row">
                                            <div class="col-md-5 mapping-label">
                                                <asp:Label ID="lblDbColumn" runat="server" Text='<%# Eval("DbColumn") %>'></asp:Label>
                                                <asp:HiddenField ID="hfDbColumn" runat="server" Value='<%# Eval("DbColumn") %>' />
                                            </div>
                                            <div class="col-md-2 text-center pt-2"><i class="fa fa-arrow-right text-muted"></i></div>
                                            <div class="col-md-5">
                                                <asp:DropDownList ID="ddlExcelColumn" runat="server" CssClass="form-control form-control-sm searchable-dropdown"></asp:DropDownList>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>

                                <div class="form-group row mt-4">
                                    <div class="col-md-12">
                                        <asp:Button ID="btnProcess" runat="server" Text="Upload to Staging" CssClass="btn btn-success" OnClick="btnProcess_Click" OnClientClick="this.value='Uploading...'; this.classList.add('disabled');" UseSubmitBehavior="false" />
                                        <asp:Button ID="btnCancel" runat="server" Text="Cancel & Upload New" CssClass="btn btn-default" OnClick="btnCancel_Click" />
                                    </div>
                                </div>
                            </asp:Panel>

                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
