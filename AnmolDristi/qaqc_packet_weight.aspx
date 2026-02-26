<%@ Page Title="AIL | QAQC-Add Packet Weight Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_packet_weight.aspx.cs" Inherits="AnmolDristi.qaqc_packet_weight" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .interval-header {
            background-color: #f5f7fa;
            padding: 6px 10px;
            margin-bottom: 6px;
            font-weight: 600;
            border-left: 4px solid #337ab7;
        }

        .packet-table th {
            text-align: center;
            font-size: 12px;
            background-color: #fafafa;
        }

        .packet-table td {
            padding: 4px;
        }

        .packet-input {
            text-align: right;
        }

        .is-invalid {
            border: 1px solid #d9534f !important;
            background-color: #fff5f5;
        }

        .alert-success {
            margin-top: 15px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        function allowNumericOnly(evt) {
            const charCode = evt.which ? evt.which : evt.keyCode;
            if ((charCode >= 48 && charCode <= 57) || charCode === 46) return true;
            evt.preventDefault();
            return false;
        }

        function validateBeforeSubmit() {
            let invalid = false;
            document.querySelectorAll(".packet-input").forEach(function (tb) {
                if (tb.value.trim() === "") {
                    tb.classList.add("is-invalid");
                    invalid = true;
                } else {
                    tb.classList.remove("is-invalid");
                }
            });

            if (invalid) {
                alert("All packet weights are mandatory for every interval.");
                return false;
            }
            return true;
        }

        function recalculatePreview() {
            const stdWeightField = document.getElementById('<%= hdnStdWeight.ClientID %>');
            const tareWeightField = document.getElementById('<%= hdnTareWeight.ClientID %>'); // New

            if (!stdWeightField || stdWeightField.value === "") return;

            const standardWeight = parseFloat(stdWeightField.value);
            // Safely parse Tare Weight (default to 0 if empty)
            const tareWeight = (tareWeightField && tareWeightField.value !== "") ? parseFloat(tareWeightField.value) : 0; 

            if (isNaN(standardWeight) || standardWeight <= 0) return;

            const inputs = document.querySelectorAll('.packet-input');
            let values = [];

            inputs.forEach(tb => {
                const val = tb.value.trim();
                if (val !== "" && !val.includes(",")) {
                    const num = parseFloat(val);
                    if (!isNaN(num)) values.push(num);
                }
            });

            if (values.length === 0) {
                clearPreview();
                return;
            }

            const min = Math.min(...values);
            const max = Math.max(...values);
            const avg = values.reduce((a, b) => a + b, 0) / values.length;
    
            // NEW: Calculate Net Average, then Give-Away
            const netAvg = avg - tareWeight;
            const giveAway = ((netAvg - standardWeight) / standardWeight) * 100;

            document.getElementById('<%= lblMinWeight.ClientID %>').innerText = min.toFixed(2);
            document.getElementById('<%= lblMaxWeight.ClientID %>').innerText = max.toFixed(2);
            document.getElementById('<%= lblAverage.ClientID %>').innerText = avg.toFixed(2); // Displays Gross Average
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
                    <div class="x_panel" style="border-top: 3px solid #2A3F54; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                        <div class="x_title">
                            <h2 style="color: #2A3F54; font-weight: bold;"><i class="fa fa-info-circle"></i>Context Selection</h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                        <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="Input Required" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                        <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="Input Required" ForeColor="Red" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                        <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="Input Required" ForeColor="Red" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                        <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="Input Required" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="Input Required" ForeColor="Red" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_BrandSKU_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="row" id="divMetadata" runat="server" visible="false">
                                <div class="col-md-12">
                                    <hr style="margin: 10px 0; border-top: 1px solid #eee;" />
                                </div>
                                <div class="col-md-4 col-sm-6">
                                    <b style="color: #555;">Standard Weight (g):</b><br />
                                    <asp:Label ID="lblStandardWeightDisplay" runat="server" Font-Bold="true" ForeColor="#E74C3C" />
                                </div>
                                <div class="col-md-4 col-sm-6">
                                    <b style="color: #555;">Packet Window:</b><br />
                                    <asp:Label ID="lblWindow" runat="server" Font-Bold="true" />
                                </div>
                                <div class="col-md-4 col-sm-6">
                                    <b style="color: #555;">Shift:</b><br />
                                    <asp:Label ID="lblShift" runat="server" Font-Bold="true" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <asp:Panel ID="pnlPacketEntry" runat="server" Visible="false">
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
                                    All 5 packet weights are mandatory for each interval block.
                                    <asp:HiddenField ID="hdnStdWeight" runat="server" />
                                    <asp:HiddenField ID="hdnTareWeight" runat="server" />
                                </div>
                                <div class="alert alert-warning" style="padding: 10px; font-size: 13px;">
                                    <i class="fa fa-clock-o"></i>
                                    <b>Time-Gated Entry:</b> Intervals will automatically unlock as your shift progresses. You can only enter packet weights for 30-minute blocks that have already passed.
                                </div>
                                <asp:Repeater ID="rptIntervals" runat="server">
                                    <ItemTemplate>
                                        <div class="interval-header">
                                            Interval: <%# Eval("IntervalStartTime") %> – <%# Eval("IntervalEndTime") %>
                                        </div>
                                        <asp:HiddenField ID="hfStart" runat="server" Value='<%# Eval("IntervalStartTime") %>' />
                                        <asp:HiddenField ID="hfEnd" runat="server" Value='<%# Eval("IntervalEndTime") %>' />

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
                                                        <asp:TextBox ID="txtPacket1" runat="server" CssClass="form-control form-control-sm packet-input" placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
                                                    <td>
                                                        <asp:TextBox ID="txtPacket2" runat="server" CssClass="form-control form-control-sm packet-input" placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
                                                    <td>
                                                        <asp:TextBox ID="txtPacket3" runat="server" CssClass="form-control form-control-sm packet-input" placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
                                                    <td>
                                                        <asp:TextBox ID="txtPacket4" runat="server" CssClass="form-control form-control-sm packet-input" placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
                                                    <td>
                                                        <asp:TextBox ID="txtPacket5" runat="server" CssClass="form-control form-control-sm packet-input" placeholder="e.g. 36.05" onkeypress="return allowNumericOnly(event)" oninput="recalculatePreview()" /></td>
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
                                    <div class="col-md-3"><strong>Min Weight (g):</strong><br />
                                        <asp:Label ID="lblMinWeight" runat="server" Text="-" /></div>
                                    <div class="col-md-3"><strong>Max Weight (g):</strong><br />
                                        <asp:Label ID="lblMaxWeight" runat="server" Text="-" /></div>
                                    <div class="col-md-3"><strong>Average Weight (g):</strong><br />
                                        <asp:Label ID="lblAverage" runat="server" Text="-" /></div>
                                    <div class="col-md-3"><strong>Give-Away (%):</strong><br />
                                        <asp:Label ID="lblGiveAway" runat="server" Text="-" /></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-12">
                        <div class="x_panel">
                            <div class="x_content">
                                <asp:Button ID="btnSubmitPacket" runat="server" Text="Submit Packet Weight" CssClass="btn btn-primary btn-sm" OnClientClick="return validateBeforeSubmit();" OnClick="btnSubmitPacket_Click" />
                                <asp:Label ID="lblError" runat="server" CssClass="text-danger" />
                            </div>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <div class="row">
                <div class="col-md-12">
                    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success">
                        <strong>Packet Weight Capture completed successfully.</strong><br />
                        You may close this tab or return to the Dashboard.
                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
