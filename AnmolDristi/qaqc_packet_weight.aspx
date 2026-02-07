<%@ Page Title="AIL | QAQC-Add Packet Weight Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_packet_weight.aspx.cs" Inherits="AnmolDristi.qaqc_packet_weight" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Interval header */
        .interval-header {
            background-color: #f5f7fa;
            padding: 6px 10px;
            margin-bottom: 6px;
            font-weight: 600;
            border-left: 4px solid #337ab7;
        }

        /* Packet table tweaks */
        .packet-table th {
            text-align: center;
            font-size: 12px;
            background-color: #fafafa;
        }

        .packet-table td {
            padding: 4px;
        }

        /* Numeric input alignment */
        .packet-input {
            text-align: right;
        }

        /* Invalid field highlight */
        .is-invalid {
            border: 1px solid #d9534f !important;
            background-color: #fff5f5;
        }

        /* Success panel spacing */
        .alert-success {
            margin-top: 15px;
        }
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        function allowNumericOnly(evt) {
            const charCode = evt.which ? evt.which : evt.keyCode;

            // Allow digits and dot
            if ((charCode >= 48 && charCode <= 57) || charCode === 46)
                return true;

            evt.preventDefault();
            return false;
        }

        function validateNotEmpty(input) {
            if (input.value.trim() === "") {
                input.classList.add("is-invalid");
            } else {
                input.classList.remove("is-invalid");
            }
        }
    </script>

    <script>
        function validateBeforeSubmit() {
            let invalid = false;

            document.querySelectorAll(".packet-input").forEach(function (tb) {
                if (tb.value.trim() === "") {
                    tb.classList.add("is-invalid");
                    invalid = true;
                }
            });

            if (invalid) {
                alert("All packet weights are mandatory for every interval.");
                return false;
            }

            return true;
        }
    </script>

    <script>
        function recalculatePreview() {

            const stdWeightField = document.getElementById('<%= hdnStdWeight.ClientID %>');
            if (!stdWeightField || stdWeightField.value === "") return;

            const standardWeight = parseFloat(stdWeightField.value);
            if (isNaN(standardWeight) || standardWeight <= 0) return;

            const inputs = document.querySelectorAll('.packet-input');
            let values = [];

            inputs.forEach(tb => {
                const val = tb.value.trim();
                if (val !== "" && !val.includes(",")) {
                    const num = parseFloat(val);
                    if (!isNaN(num)) {
                        values.push(num);
                    }
                }
            });

            if (values.length === 0) {
                clearPreview();
                return;
            }

            const min = Math.min(...values);
            const max = Math.max(...values);
            const avg = values.reduce((a, b) => a + b, 0) / values.length;
            const giveAway = ((avg - standardWeight) / standardWeight) * 100;

            document.getElementById('<%= lblMinWeight.ClientID %>').innerText = min.toFixed(2);
            document.getElementById('<%= lblMaxWeight.ClientID %>').innerText = max.toFixed(2);
            document.getElementById('<%= lblAverage.ClientID %>').innerText = avg.toFixed(2);
            document.getElementById('<%= lblGiveAway.ClientID %>').innerText = giveAway.toFixed(2) + " %";
        }

        function clearPreview() {
            document.getElementById('<%= lblMinWeight.ClientID %>').innerText = "-";
            document.getElementById('<%= lblMaxWeight.ClientID %>').innerText = "-";
            document.getElementById('<%= lblAverage.ClientID %>').innerText = "-";
            document.getElementById('<%= lblGiveAway.ClientID %>').innerText = "-";
        }
    </script>


    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>Packet Weight Capture</h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>QC Context</h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-3">
                                    <b>Plant:</b>
                                    <asp:Label ID="lblPlant" runat="server" Font-Bold="true" />
                                </div>
                                <div class="col-md-3">
                                    <b>Line:</b>
                                    <asp:Label ID="lblLine" runat="server" Font-Bold="true"  />
                                </div>
                                <div class="col-md-3">
                                    <b>SKU:</b>
                                    <asp:Label ID="lblSKU" runat="server" Font-Bold="true"  />
                                </div>
                                <div class="col-md-3">
                                    <b>Variety:</b>
                                    <asp:Label ID="lblVariety" runat="server" Font-Bold="true"  />
                                </div>
                            </div>

                            <hr />

                            <div class="row">
                                <div class="col-md-4">
                                    <b>QC Submitted At:</b>
                                    <asp:Label ID="lblQCSubmittedAt" runat="server" Font-Bold="true"  />
                                </div>
                                <div class="col-md-4">
                                    <b>Packet Window:</b>
                                    <asp:Label ID="lblWindow" runat="server" Font-Bold="true"  />
                                </div>
                                <div class="col-md-4">
                                    <b>Shift:</b>
                                    <asp:Label ID="lblShift" runat="server" Font-Bold="true"  />
                                </div>
                            </div>
                        </div>
                    </div>

                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Packet Weight Entry (5 Samples per Interval)</h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="alert alert-info">
                                <strong>Instruction:</strong>
                                Enter <b>one packet weight per field</b>.  
                                All 5 packet weights are mandatory for each 30-minute interval.
                                <asp:HiddenField ID="hdnStdWeight" runat="server" />
                                <asp:HiddenField ID="hdnQCInspectorID" runat="server" />
                            </div>

                            <asp:Repeater ID="rptIntervals" runat="server">
                                <ItemTemplate>

                                    <div class="interval-header">
                                        Interval:
                                        <%# Eval("IntervalStartTime") %> – <%# Eval("IntervalEndTime") %>
                                    </div>



                                    <asp:HiddenField ID="hfStart" runat="server"
                                        Value='<%# Eval("IntervalStartTime") %>' />

                                    <asp:HiddenField ID="hfEnd" runat="server"
                                        Value='<%# Eval("IntervalEndTime") %>' />


                                    <table class="table table-bordered table-sm">
                                        <thead>
                                            <tr>
                                                <th>Packet 1</th>
                                                <th>Packet 2</th>
                                                <th>Packet 3</th>
                                                <th>Packet 4</th>
                                                <th>Packet 5</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td>
                                                    <asp:TextBox ID="txtPacket1" runat="server"
                                                        CssClass="form-control form-control-sm packet-input"
                                                        placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>

                                                <td>
                                                    <asp:TextBox ID="txtPacket2" runat="server"
                                                        CssClass="form-control form-control-sm packet-input"
                                                        placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>

                                                <td>
                                                    <asp:TextBox ID="txtPacket3" runat="server"
                                                        CssClass="form-control form-control-sm packet-input"
                                                        placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>

                                                <td>
                                                    <asp:TextBox ID="txtPacket4" runat="server"
                                                        CssClass="form-control form-control-sm packet-input"
                                                        placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>

                                                <td>
                                                    <asp:TextBox ID="txtPacket5" runat="server"
                                                        CssClass="form-control form-control-sm packet-input"
                                                        placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
                                            </tr>
                                        </tbody>

                                    </table>

                                </ItemTemplate>
                            </asp:Repeater>

                        </div>
                    </div>

                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>System Calculations</h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="text-muted" style="font-size: 12px; margin-bottom: 5px;">
                                * Preview only. Final values will be calculated and locked after submission.
                            </div>

                            <div class="row">
                                <div class="col-md-3">
                                    <strong>Min Weight (g):</strong><br />
                                    <asp:Label ID="lblMinWeight" runat="server" Text="-" />
                                </div>

                                <div class="col-md-3">
                                    <strong>Max Weight (g):</strong><br />
                                    <asp:Label ID="lblMaxWeight" runat="server" Text="-" />
                                </div>

                                <div class="col-md-3">
                                    <strong>Average Weight (g):</strong><br />
                                    <asp:Label ID="lblAverage" runat="server" Text="-" />
                                </div>

                                <div class="col-md-3">
                                    <strong>Give-Away (%):</strong><br />
                                    <asp:Label ID="lblGiveAway" runat="server" Text="-" />
                                </div>
                            </div>
                        </div>
                    </div>

                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_content">
                            <asp:Button
                                ID="btnSubmitPacket"
                                runat="server"
                                Text="Submit Packet Weight"
                                CssClass="btn btn-primary btn-sm"
                                ValidationGroup="Submit"
                                OnClick="btnSubmitPacket_Click" />

                            <asp:Label ID="lblError" runat="server" CssClass="text-danger" />
                        </div>
                    </div>

                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success">
                        <strong>Packet Weight Capture completed successfully.</strong><br />
                        You may close this tab and return to the QC Inspection page.
                    </asp:Panel>

                </div>
            </div>
        </div>
    </div>
</asp:Content>
