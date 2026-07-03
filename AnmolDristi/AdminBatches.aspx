<%@ Page Title="Staging to Final" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="AdminBatches.aspx.cs" Inherits="AnmolDristi.AdminBatches" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .selected-row {
            background-color: #eef !important;
            border-left: 4px solid #007bff;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Main Heading</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Manage Import Batches</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <asp:Label ID="lblPageError" runat="server" CssClass="text-danger font-weight-bold d-block mb-2"></asp:Label>

                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">From (IST)</label>
                                        <asp:TextBox ID="txtFrom" runat="server" CssClass="form-control form-control-sm" type="date" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">To (IST)</label>
                                        <asp:TextBox ID="txtTo" runat="server" CssClass="form-control form-control-sm" type="date" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">Status</label>
                                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Value="">-- All --</asp:ListItem>
                                            <asp:ListItem>Processed</asp:ListItem>
                                            <asp:ListItem>Error</asp:ListItem>
                                            <asp:ListItem>Inserted</asp:ListItem>
                                            <asp:ListItem>Imported</asp:ListItem>
                                            <asp:ListItem>Pending</asp:ListItem>
                                            <asp:ListItem>New</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-3" style="padding-top: 24px;">
                                    <asp:Button ID="btnFilter" runat="server" Text="Search" CssClass="btn btn-primary btn-sm" OnClick="btnFilter_Click" />
                                    &nbsp;<asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-default btn-sm" OnClick="btnClear_Click" />
                                </div>
                            </div>

                            <asp:UpdatePanel runat="server" ID="upBatches">
                                <ContentTemplate>
                                    <div class="row" style="margin-top: 10px;">
                                        <div class="col-md-12">
                                            <div class="card-box table-responsive">
                                                <asp:GridView ID="gvBatches" runat="server" CssClass="table table-striped table-hover table-bordered table-sm table-condensed"
                                                    AutoGenerateColumns="False" GridLines="None" OnRowDataBound="gvBatches_RowDataBound">
                                                    <Columns>
                                                        <asp:TemplateField HeaderText="Select">
                                                            <ItemTemplate>
                                                                <asp:LinkButton ID="lnkSelect" runat="server" Text="Select"
                                                                    OnClientClick='<%# "onRowSelect(this, \"" + Eval("ImportBatchId") + "\"); return false;" %>' CssClass="btn btn-info btn-sm m-0" />
                                                            </ItemTemplate>
                                                        </asp:TemplateField>

                                                        <asp:BoundField DataField="ImportBatchId" HeaderText="Batch ID" />
                                                        <asp:BoundField DataField="AnyFile" HeaderText="File Name" />
                                                        <asp:BoundField DataField="TotalRows" HeaderText="Total Rows" />
                                                        <asp:BoundField DataField="ProcessedCount" HeaderText="Processed" />
                                                        <asp:BoundField DataField="ErrorCount" HeaderText="Errors" />
                                                        <asp:BoundField DataField="WaitingCount" HeaderText="Waiting" />
                                                        <asp:BoundField DataField="LastModifiedIST" HeaderText="Last Modified (IST)" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                                                    </Columns>
                                                </asp:GridView>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="row" style="margin-top: 12px;">
                                        <div class="col-md-12">
                                            <div class="ln_solid"></div>
                                            <asp:HiddenField ID="hfSelectedBatch" runat="server" />

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label font-weight-bold">Selected Batch:</label>
                                                <div class="col-md-8">
                                                    <asp:Label ID="lblSelectedBatch" runat="server" CssClass="text-primary font-weight-bold">(none selected)</asp:Label>
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Rate Per Km</label>
                                                <div class="col-md-4">
                                                    <asp:TextBox ID="txtRatePerKm" runat="server" CssClass="form-control form-control-sm" Text="3.75" />
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Processed By</label>
                                                <div class="col-md-6">
                                                    <asp:TextBox ID="txtProcessedBy" runat="server" CssClass="form-control form-control-sm" placeholder="Optional User ID..." />
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Allow Overwrite</label>
                                                <div class="col-md-6">
                                                    <div class="checkbox">
                                                        <label>
                                                            <asp:CheckBox ID="chkAllowOverwrite" runat="server" Checked="true" />
                                                            Overwrite existing draft claims</label>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <asp:Button ID="btnRun" runat="server" Text="Run Import" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClientClick="return confirmRun();" OnClick="btnRun_Click" />

                                                &nbsp;<asp:Button ID="btnExportReport" runat="server" Text="Download Batch Report" CssClass="btn btn-info btn-sm" CausesValidation="false" OnClick="btnExportReport_Click" />

                                                &nbsp;<asp:Label ID="lblRunMessage" runat="server" CssClass="font-weight-bold"></asp:Label>
                                            </div>
                                        </div>
                                    </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnFilter" EventName="Click" />
                                    <asp:AsyncPostBackTrigger ControlID="btnClear" EventName="Click" />
                                    
                                    <asp:PostBackTrigger ControlID="btnExportReport" />
                                    
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Last RUN Result</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <asp:UpdatePanel runat="server" ID="upResult">
                                <ContentTemplate>
                                    <div class="row">
                                        <div class="col-md-12 col-sm-12">
                                            <div class="card-box table-responsive">
                                                <asp:GridView ID="gvResult" runat="server" CssClass="table table-striped table-hover table-bordered table-sm table-condensed"
                                                    AutoGenerateColumns="true" EmptyDataText="No result returned." />
                                            </div>
                                        </div>
                                    </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnRun" EventName="Click" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Keep UI selection state without full postback
        function onRowSelect(btnElement, batchId) {
            document.getElementById('<%= hfSelectedBatch.ClientID %>').value = batchId;
            document.getElementById('<%= lblSelectedBatch.ClientID %>').innerText = batchId;

            // Remove class from all rows
            var rows = document.querySelectorAll('#<%= gvBatches.ClientID %> tr');
            for (var i = 0; i < rows.length; i++) {
                rows[i].classList.remove('selected-row');
            }

            // Add class to clicked row
            var clickedRow = btnElement.closest("tr");
            if (clickedRow) {
                clickedRow.classList.add('selected-row');
            }
        }

        function confirmRun() {
            var sel = document.getElementById('<%= hfSelectedBatch.ClientID %>').value;
            if (!sel) {
                alert('Please select a batch to run from the grid.');
                return false;
            }
            if (!confirm('Execute processing for batch:\n' + sel + '?')) {
                return false;
            }

            var btn = document.getElementById('<%= btnRun.ClientID %>');
            if (btn) {
                // To prevent double click submissions
                btn.value = 'Running...';
                // Note: Disabling the button cancels the postback in ASP.NET, so we use a visual cue instead
                btn.classList.add('disabled');
            }
            return true;
        }
    </script>
</asp:Content>
