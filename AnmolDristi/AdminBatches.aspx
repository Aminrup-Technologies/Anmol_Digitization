<%@ Page Title="Stagging to Final" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="AdminBatches.aspx.cs" Inherits="AnmolDristi.AdminBatches" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hfRetryConfirmed" runat="server" />
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Main Heading</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <!-- Panel: Filters + Batch list -->
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
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">From (IST)</label>
                                        <asp:TextBox ID="txtFrom" runat="server" CssClass="form-control form-control-sm" />
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group">
                                        <label class="label-align">To (IST)</label>
                                        <asp:TextBox ID="txtTo" runat="server" CssClass="form-control form-control-sm" />
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
                                                <asp:GridView ID="gvBatches" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                                    AutoGenerateColumns="False" GridLines="None">
                                                    <Columns>
                                                        <asp:TemplateField HeaderText="Select">
                                                            <ItemTemplate>
                                                                <asp:LinkButton ID="lnkSelect" runat="server" Text="Select"
                                                                    OnClientClick='<%# "onRowSelect(\"" + Eval("ImportBatchId") + "\"); return false;" %>' CssClass="btn btn-info btn-sm" />
                                                            </ItemTemplate>
                                                        </asp:TemplateField>

                                                        <asp:BoundField DataField="ImportBatchId" HeaderText="ImportBatchId" />
                                                        <asp:BoundField DataField="AnyFile" HeaderText="AnyFile" />
                                                        <asp:BoundField DataField="TotalRows" HeaderText="TotalRows" />
                                                        <asp:BoundField DataField="ProcessedCount" HeaderText="Processed" />
                                                        <asp:BoundField DataField="ErrorCount" HeaderText="Errors" />
                                                        <asp:BoundField DataField="WaitingCount" HeaderText="Waiting" />
                                                        <asp:BoundField DataField="LastModifiedIST" HeaderText="LastModifiedIST" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                                                    </Columns>
                                                </asp:GridView>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- Execute area -->
                                    <div class="row" style="margin-top: 12px;">
                                        <div class="col-md-12">
                                            <div class="ln_solid"></div>
                                            <asp:HiddenField ID="hfSelectedBatch" runat="server" />
                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Selected Batch</label>
                                                <div class="col-md-8">
                                                    <asp:Label ID="lblSelectedBatch" runat="server" CssClass="form-control-static">(none selected)</asp:Label>
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Rate Per Km</label>
                                                <div class="col-md-4">
                                                    <asp:TextBox ID="txtRatePerKm" runat="server" CssClass="form-control form-control-sm" Text="2.75" />
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Processed By</label>
                                                <div class="col-md-6">
                                                    <asp:TextBox ID="txtProcessedBy" runat="server" CssClass="form-control form-control-sm" />
                                                </div>
                                            </div>

                                            <div class="form-group row">
                                                <label class="col-md-4 control-label">Allow Overwrite</label>
                                                <div class="col-md-6">
                                                    <asp:CheckBox ID="chkAllowOverwrite" runat="server" />
                                                </div>
                                            </div>

                                            <div class="form-group">
                                                <asp:Button ID="btnRun" runat="server" Text="Run Import" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btnRun_Click" />
                                                &nbsp;<asp:Label ID="lblRunMessage" runat="server" CssClass="muted"></asp:Label>
                                            </div>
                                        </div>
                                    </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="btnRun" EventName="Click" />
                                </Triggers>
                            </asp:UpdatePanel>

                            <!-- small modal kept as in your layout -->
                            <asp:Button ID="ShowPopup" runat="server" Text="Button" CssClass="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">
                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body">
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <!-- modal end -->

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
                            <%--<div class="clearfix"></div>--%>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvResult" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                            AutoGenerateColumns="true" EmptyDataText="No result returned." />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- client scripts -->
    <script type="text/javascript">
        function confirmRun() {
            var sel = document.getElementById('<%= hfSelectedBatch.ClientID %>').value;
            if (!sel) {
                alert('Please select a batch to run.');
                return false;
            }
            if (!confirm('Execute processing for batch: ' + sel + '?')) return false;
            var btn = document.getElementById('<%= btnRun.ClientID %>');
            if (btn) { btn.disabled = true; btn.innerText = 'Running...'; }
            return true;
        }

        function onRowSelect(batchId) {
            document.getElementById('<%= hfSelectedBatch.ClientID %>').value = batchId;
            var lbl = document.getElementById('<%= lblSelectedBatch.ClientID %>');
            if (lbl) lbl.innerText = batchId;
            // basic row highlighting (optional)
            var rows = document.querySelectorAll('#<%= gvBatches.ClientID %> tr');
            for (var i = 0; i < rows.length; i++) rows[i].style.backgroundColor = '';
            var r = document.getElementById('row_' + batchId);
            if (r) r.style.backgroundColor = '#eef';
        }
    </script>
</asp:Content>
