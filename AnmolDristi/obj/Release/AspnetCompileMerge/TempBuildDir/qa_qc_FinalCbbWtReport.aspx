<%@ Page Title="AIL | Final CBB Weight Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qa_qc_FinalCbbWtReport.aspx.cs" Inherits="AnmolDristi.qa_qc_FinalCbbWtReport" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }

        .custom-page-title {
            width: 100%;
            background-color: #f0f0f0; /* Example: Change background color */
            margin-top: 20px;
            padding-top: 35px;
            padding-right: 20px;
            padding-left: 20px;
            /*padding-bottom:20px;*/
        }

            .custom-page-title .title_left h3 {
                font-size: 20px;
                color: #333333;
                font-weight: bold;
            }

        /*.nav-tabs .nav-link.active {
            background-color: #17a2b8;
            color: white;
            border: 2px solid #17a2b8;
            border-radius: 5px;
        }

        .nav-tabs .nav-link:hover {
            background-color: #e9ecef;
            color: #17a2b8;
        }*/

        .approver-photo {
            width: 50px;
            height: 50px;
            border-radius: 50%;
            object-fit: cover;
        }

        .approver-flow {
            display: flex;
            align-items: center;
            justify-content: space-around;
            padding: 1rem;
            background-color: #f8f9fa;
            border: 1px solid #ddd;
            border-radius: .25rem;
        }

        .flow-line {
            flex: 1;
            border-top: 2px solid #007bff;
            margin: 0 10px;
        }

        .approver-item {
            text-align: center;
        }
    </style>

    <script type="text/javascript">
        function validateGridViewOld() {
            var isValid = true;
            var gridView = document.getElementById('<%= GridView1.ClientID %>');
            for (var i = 1; i < gridView.rows.length; i++) {  // Start from 1 to skip header row
                var row = gridView.rows[i];

                var txtGrossWeight = row.querySelector("input[id*='txtGrossWeight']");
                //var txtDescription = row.querySelector("input[id*='txtDescription']");

                // Check if TextBoxes are filled
                if (txtGrossWeight && txtGrossWeight.value.trim() === "") {
                    isValid = false;
                    txtGrossWeight.style.borderColor = "red";
                } else {
                    txtGrossWeight.style.borderColor = "";
                }

                //if (txtDescription && txtDescription.value.trim() === "") {
                //    isValid = false;
                //    txtDescription.style.borderColor = "red";
                //} else {
                //    txtDescription.style.borderColor = "";
                //}
            }

            // If not valid, prevent form submission
            if (!isValid) {
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'Input Required',
                        text: 'Please fill all the required fields.',
                        type: 'warning',
                        styling: 'bootstrap3',
                        delay: 3000,        // Notification auto-dismiss delay in milliseconds (3 seconds)
                        buttons: {
                            closer: true,  // Show a close button in the notification
                            sticker: false // Hide the sticker button
                        }
                    });
                }, 200); // Adjust delay as necessary
            }

            return isValid;
        }


        function calculateAverage() {
            var gridView = document.getElementById('<%= GridView1.ClientID %>');
            var totalWeight = 0;
            var count = 0;
            var minWeight = Number.MAX_VALUE;
            var maxWeight = Number.MIN_VALUE;

            // Iterate over GridView rows (skip the header row)
            for (var i = 1; i < gridView.rows.length; i++) {
                var row = gridView.rows[i];
                var txtGrossWeight = row.querySelector("input[id*='txtGrossWeight']");

                // Check if the value is not empty and accumulate total weight
                if (txtGrossWeight && txtGrossWeight.value.trim() !== "") {
                    var weight = parseFloat(txtGrossWeight.value.trim());

                    if (!isNaN(weight) && weight > 0) {
                        totalWeight += weight;
                        count++;

                        // Track min and max weight
                        if (weight < minWeight) minWeight = weight;
                        if (weight > maxWeight) maxWeight = weight;
                    }
                }
            }

            // Calculate the average, min, max, and difference
            var averageWeight = count > 0 ? (totalWeight / count).toFixed(2) : "0.00";
            var difference = count > 0 ? (maxWeight - minWeight).toFixed(2) : "0.00";
            minWeight = (minWeight !== Number.MAX_VALUE) ? minWeight.toFixed(2) : "0.00";
            maxWeight = (maxWeight !== Number.MIN_VALUE) ? maxWeight.toFixed(2) : "0.00";

            // Update the values on the page
            document.getElementById('<%= lblAvgWeights.ClientID %>').innerHTML = averageWeight;
            document.getElementById('<%= lblMinValue.ClientID %>').innerHTML = minWeight + " kgs";
            document.getElementById('<%= lblMaxValue.ClientID %>').innerHTML = maxWeight + " kgs";
            document.getElementById('<%= lblDiffMinMax.ClientID %>').innerHTML = difference + " kgs";
        }


        function validateGridView() {
            var isValid = true;
            var filledRowsCount = 0; // Counter to track filled rows
            var minimumRequiredRows = 3; // Set the minimum number of required filled rows
            var gridView = document.getElementById('<%= GridView1.ClientID %>');
            var totalWeight = 0;
            var count = 0;

            // Iterate over GridView rows (skip the header row)
            for (var i = 1; i < gridView.rows.length; i++) {
                var row = gridView.rows[i];
                var txtGrossWeight = row.querySelector("input[id*='txtGrossWeight']");

                // Check if TextBox is filled and calculate total weight
                if (txtGrossWeight && txtGrossWeight.value.trim() !== "") {
                    totalWeight += parseFloat(txtGrossWeight.value.trim());
                    count++;
                    filledRowsCount++; // Count the row as filled
                    txtGrossWeight.style.borderColor = ""; // Clear any previous validation errors
                } else if (txtGrossWeight) {
                    txtGrossWeight.style.borderColor = "red"; // Mark as invalid if empty
                }
            }

            // Check if the minimum number of filled rows is met
            if (filledRowsCount < minimumRequiredRows) {
                isValid = false;
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'Input Required',
                        text: 'Please ensure at least ' + minimumRequiredRows + ' rows have valid non-zero numeric values',
                        type: 'warning',
                        styling: 'bootstrap3',
                        delay: 3000,        // Notification auto-dismiss delay in milliseconds (3 seconds)
                        buttons: {
                            closer: true,  // Show a close button in the notification
                            sticker: false // Hide the sticker button
                        }
                    });
                }, 200); // Adjust delay as necessary
            }

            // If there are valid entries, calculate the average
            if (isValid && count > 0) {
                var averageWeight = totalWeight / count;
                document.getElementById('<%= lblAvgWeights.ClientID %>').value = averageWeight.toFixed(2);
            } else {
                document.getElementById('<%= lblAvgWeights.ClientID %>').value = "0.00";
            }

            return isValid;
        }

        function resetGridViewTextboxes() {
            var gridView = document.getElementById('<%= GridView1.ClientID %>');
            var textboxes = gridView.getElementsByTagName('input');

            for (var i = 0; i < textboxes.length; i++) {
                if (textboxes[i].type == 'text') {
                    textboxes[i].value = '';
                }
            }

            return false; // Prevent postback
        }

        function activateTab(tabId) {
            var tabElement = document.getElementById(tabId);
            var tabLink = document.querySelector('a[href="#' + tabId + '"]');

            // Remove active class from all tabs
            document.querySelectorAll('#myTab .nav-link').forEach(function (link) {
                link.classList.remove('active');
            });

            // Remove 'show active' class from all tab panes
            document.querySelectorAll('.tab-pane').forEach(function (pane) {
                pane.classList.remove('show', 'active');
            });

            // Add active class to the selected tab and its content
            tabLink.classList.add('active');
            document.getElementById(tabId).classList.add('show', 'active');
        }

    </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
    <asp:HiddenField ID="hdn_formid" runat="server" />
    <asp:HiddenField ID="hdn_minwt" runat="server" />
    <asp:HiddenField ID="hdn_maxwt" runat="server" />

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title custom-page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="row">
                            <div class="col-12">
                                <div class="card">
                                    <div class="card-body">
                                        <div class="row">
                                            <div class="col-12">
                                                <ul class="nav nav-tabs mb-4" id="myTab" role="tablist">
                                                    <li class="nav-item">
                                                        <a class="nav-link active text-info" id="basicData-tab" data-toggle="tab" href="#basicData" role="tab"
                                                            aria-controls="basicData" aria-selected="true"><i class="fa fa-id-badge mr-2"></i>CBB Basic Data</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="GrossWeightData-tab" data-toggle="tab" href="#GrossWeightData" role="tab"
                                                            aria-controls="GrossWeightData" aria-selected="false"><i class="fa fa-id-badge mr-2"></i>Gross Weights</a>
                                                    </li>

                                                </ul>
                                                <div class="tab-content ml-1" id="myTabContent">
                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x_content">
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>

                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" InitialValue="0" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="lblBatchNo" runat="server" AssociatedControlID="TXT_BatchNo" Text="Batch No:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BatchNo" runat="server" ErrorMessage="Required" ValidationGroup="Submit" InitialValue="" ControlToValidate="TXT_BatchNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_BatchNo" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_BatchNo" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXT_BatchNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Batch No"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>



                                                            <div class="col-md-3" id="RowCount_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label7" runat="server" AssociatedControlID="TB_NoPacks" Text="No Of Packs in 1 CBB:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Required" InitialValue="" ValidationGroup="Submit" ControlToValidate="TB_NoPacks" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NoPacks" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RangeValidator1" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NoPacks" ErrorMessage="[10 - 500]" ForeColor="Red" MinimumValue="10" MaximumValue="500" Type="Double" Display="Static"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_NoPacks" runat="server" CssClass="form-control form-control-sm rounded" ReadOnly="false" Text=""></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="lblMRP" runat="server" AssociatedControlID="TXT_MRP" Text="Packet MRP:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MRP" runat="server" ErrorMessage="MRP Required" InitialValue="" ControlToValidate="TXT_MRP" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_MRP" runat="server" ControlToValidate="TXT_MRP" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_MRP" runat="server" ControlToValidate="TXT_MRP" ErrorMessage="[0.00 - 9999.99]" ForeColor="Red" MinimumValue="0.00" MaximumValue="9999.99" Type="Double" Display="Static"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXT_MRP" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="MRP [0.00 - 9999.99]"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-12 text-center">
                                                                <asp:Label ID="lblMessage" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                                                                <asp:Button ID="Btn_Save" runat="server" Text="Proceed Next" OnClick="Btn_Save_Click" CausesValidation="true" ValidationGroup="Submit" CssClass="btn btn-sm btn-primary" />
                                                                <asp:Button ID="Btn_Reset" runat="server" Text="Reset" OnClick="Btn_Reset_Click" CssClass="btn btn-sm btn-warning" />
                                                                <asp:Button ID="btn_home1" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" PostBackUrl="~/home.aspx" />
                                                            </div>



                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="GrossWeightData" role="tabpanel" aria-labelledby="GrossWeightData-tab">
                                                        <div class="x_content">
                                                            <asp:GridView ID="GridView1" runat="server" Visible="true" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="No of Packs">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                        <HeaderStyle CssClass="text-center" />
                                                                        <ItemStyle CssClass="text-center" />
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Enter Gross Weights">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtGrossWeight" runat="server" CssClass="form-control form-control-sm rounded" onkeyup="calculateAverage();"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                        <HeaderStyle CssClass="text-center" />
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>


                                                            <%--<div class="col-md-3" id="AvgWt_TB" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="lbl_txtAverageGrossWeight" runat="server" AssociatedControlID="txtAverageGrossWeight" Text="Avergae of all the Weights:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="txtAverageGrossWeight" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <asp:GridView ID="yourGridView" runat="server" AutoGenerateColumns="False" Visible="false" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:BoundField DataField="SlNo" HeaderText="Sl. No." />
                                                                    <asp:BoundField DataField="GrossWeight" HeaderText="Gross Weight" />
                                                                </Columns>
                                                            </asp:GridView>
                                                        </div>

                                                        <div class="col-md-6" id="Div1" runat="server" visible="true">
                                                            <!-- Table structure for Min, Max, Difference, and Average -->
                                                            <table class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" style="width: 100%;">
                                                                <thead>
                                                                    <tr>
                                                                        <th>Min Value</th>
                                                                        <th>Max Value</th>
                                                                        <th>Difference (Min-Max)</th>
                                                                        <th>Average Value</th>
                                                                    </tr>
                                                                </thead>
                                                                <tbody>
                                                                    <tr>
                                                                        <td>
                                                                            <asp:Label ID="lblMinValue" runat="server" Text="0"></asp:Label>
                                                                        </td>
                                                                        <td>
                                                                            <asp:Label ID="lblMaxValue" runat="server" Text="0"></asp:Label>
                                                                        </td>
                                                                        <td>
                                                                            <asp:Label ID="lblDiffMinMax" runat="server" Text="0"></asp:Label>
                                                                        </td>
                                                                        <td>
                                                                            <asp:Label ID="lblAvgWeights" runat="server" Text="0"></asp:Label>
                                                                        </td>
                                                                    </tr>
                                                                </tbody>
                                                            </table>
                                                        </div>

                                                        <div class="col-md-12 text-center">

                                                            <asp:Button ID="btnSubmit" runat="server" Text="Final Submit" OnClientClick="return validateGridView();" ValidationGroup="Submit2" OnClick="btnSubmit_Click" CssClass="btn btn-sm btn-primary" />
                                                            <asp:Button ID="btn_resetgrid" runat="server" Text="Reset Inputs" CssClass="btn btn-sm btn-warning" OnClientClick="return resetGridViewTextboxes();" />
                                                            <asp:Label ID="Label6" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                                                            <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" PostBackUrl="~/home.aspx" />
                                                        </div>

                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
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
                            <h2>
                                <asp:Label ID="Label8" runat="server" Text="Approval Matrix"></asp:Label></h2>
                            <div class="clearfix"></div>

                        </div>
                        <div class="x_content">
                            <!-- Approver Flow Diagram -->
                            <div class="approver-flow">
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label9" runat="server" Text="Approver 1" />
                                    </p>
                                    <asp:Image ID="Image3" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="Approver1NameLabel" runat="server" Text='<%# Eval("Approver1Name") %>' />
                                    </p>
                                    <p>
                                        <asp:Label ID="Approver1CodeLabel" runat="server" Text='<%# Eval("Approver1EmployeeCode") %>' />
                                    </p>
                                </div>
                                <div class="flow-line"></div>
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label10" runat="server" Text="Approver 2" />
                                    </p>
                                    <asp:Image ID="Image2" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="Approver2NameLabel" runat="server" Text='<%# Eval("Approver2Name") %>' />
                                    </p>
                                    <p>
                                        <asp:Label ID="Approver2CodeLabel" runat="server" Text='<%# Eval("Approver2EmployeeCode") %>' />
                                    </p>
                                </div>
                                <div class="flow-line"></div>
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label11" runat="server" Text="Approver 3" />
                                    </p>
                                    <asp:Image ID="Image1" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="DottedLineApproverNameLabel" runat="server" Text='<%# Eval("DottedLineApproverName") %>' />
                                    </p>
                                    <p>
                                        <asp:Label ID="DottedLineApproverCodeLabel" runat="server" Text='<%# Eval("DottedLineApproverEmployeeCode") %>' />
                                    </p>
                                </div>
                            </div>

                            <hr />

                            <!-- GridView for Detailed Information -->
                            <asp:GridView ID="GridViewApprovers" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" Visible="false">
                                <Columns>
                                    <asp:BoundField DataField="Approver1Name" HeaderText="Approver 1 Name" HtmlEncode="false" />
                                    <asp:BoundField DataField="Approver1EmployeeCode" HeaderText="Approver 1" HtmlEncode="false" />
                                    <asp:BoundField DataField="Approver2Name" HeaderText="Approver 2 Name" HtmlEncode="false" />
                                    <asp:BoundField DataField="Approver2EmployeeCode" HeaderText="Approver 2" HtmlEncode="false" />
                                    <asp:BoundField DataField="DottedLineApproverName" HeaderText="Dotted Line Approver Name" HtmlEncode="false" />
                                    <asp:BoundField DataField="DottedLineApproverEmployeeCode" HeaderText="Dotted Line Approver Code" HtmlEncode="false" />
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
