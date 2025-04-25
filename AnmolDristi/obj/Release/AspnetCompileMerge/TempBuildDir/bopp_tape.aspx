<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="bopp_tape.aspx.cs" Inherits="AnmolDristi.bopp_tape" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }
        /* Custom CSS to ensure proper alignment and spacing */
        .d-flex {
            display: flex;
            align-items: center;
        }

        .remarks-container {
            display: flex;
            flex-direction: column;
            margin-left: 10px;
        }

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
            padding-bottom: 20px;
        }

            .custom-page-title .title_left h3 {
                font-size: 20px;
                color: #333333;
                font-weight: bold;
            }


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
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script type="text/javascript">

        <%--// JavaScript function to toggle remarks textbox
        function toggleRemarks() {
            var inputVal = parseFloat(document.getElementById('<%= TB_StandardDimension.ClientID %>').value);
            var remarksSection = document.getElementById('RemarksSection');

            // Check if the input is outside the range 99.5 to 101
            if (inputVal < 0.00 || inputVal > 1000.00) {
                remarksSection.style.display = 'block'; // Show remarks section
            } else {
                remarksSection.style.display = 'none';  // Hide remarks section
            }
        }

        // Ensure the DOM is loaded before adding the event listener
        document.addEventListener("DOMContentLoaded", function () {
            document.getElementById('<%= TB_StandardDimension.ClientID %>').addEventListener('keyup', toggleRemarks);
        });
        window.onload = function () {

            // Function to validate the Standard Dimension against the range 99.5 to 101
            function checkStandardDimension() {
                var dimensionValue = parseFloat(document.getElementById('<%= TB_StandardDimension.ClientID %>').value);
                var minValue = 0.00;
                var maxValue = 1000.00;

                // Show remarks if the dimension value is outside the valid range
                if (dimensionValue < minValue || dimensionValue > maxValue) {
                    document.getElementById('RemarksSection').style.display = 'block';
                } else {
                    document.getElementById('RemarksSection').style.display = 'none';
                }
            }

            // Function to validate the Standard GSM against the range 99.5 to 101
            function checkStandardGSM() {
                var gsmValue = parseFloat(document.getElementById('<%= TB_StandardGSM.ClientID %>').value);
                var minValue = 0.00;
                var maxValue = 1000.00;

                // Show remarks if GSM value is outside the valid range
                if (gsmValue < minValue || gsmValue > maxValue) {
                    document.getElementById('div_GSM_Remarks').style.display = 'block';
                } else {
                    document.getElementById('div_GSM_Remarks').style.display = 'none';
                }
            }

            // Attach event listener for Standard Dimension TextBox to validate input on keyup
            var standardDimensionTextBox = document.getElementById('<%= TB_StandardDimension.ClientID %>');
            if (standardDimensionTextBox) {
                standardDimensionTextBox.addEventListener('keyup', checkStandardDimension);
            }

            // Attach event listener for Standard GSM TextBox to validate input on change
            var standardGSMTextBox = document.getElementById('<%= TB_StandardGSM.ClientID %>');
            if (standardGSMTextBox) {
                standardGSMTextBox.addEventListener('change', checkStandardGSM);
            }
        };--%>

        
        function validateObservedGSM() {
            var stdGSMInput = document.getElementById('<%= TB_StandardGSM.ClientID %>').value.trim();
            var obsGSMValue = parseFloat(document.getElementById('<%= TB_GSMObservation.ClientID %>').value);
            var remarksDiv = document.getElementById("GSM_ObsremarkSection");
            var rangeText = document.getElementById("GSM_ValidRange");

            console.log("Raw STD GSM Input:", stdGSMInput);
            console.log("Raw Observed GSM Input:", obsGSMValue);

            var regex = /^([\d.]+)\s*±\s*([\d.]+)%?$/; // Regex to match "27.3 ± 5%" format

            var stdGSMValue, tolerancePercent;

            if (regex.test(stdGSMInput)) {
                var matches = stdGSMInput.match(regex);
                stdGSMValue = parseFloat(matches[1]); // Extract standard value (e.g., 27.3)
                tolerancePercent = parseFloat(matches[2]) / 100; // Convert 5% to 0.05
            } else {
                stdGSMValue = parseFloat(stdGSMInput); // If no ± tolerance, assume no percentage
                tolerancePercent = 0.05; // Default tolerance to 5%
            }

            console.log("Parsed STD GSM Value:", stdGSMValue);
            console.log("Parsed Tolerance Percentage:", tolerancePercent * 100, "%");

            if (isNaN(stdGSMValue)) {
                rangeText.innerHTML = "Expected Range: (Enter a valid STD GSM first)";
                rangeText.style.color = "red";
                console.warn("Invalid STD GSM Value. Cannot calculate range.");
                return;
            }

            // Calculate tolerance range dynamically
            var tolerance = stdGSMValue * tolerancePercent;
            var lowerLimit = (stdGSMValue - tolerance).toFixed(2);
            var upperLimit = (stdGSMValue + tolerance).toFixed(2);

            console.log("Calculated Tolerance Value:", tolerance);
            console.log("Expected Range:", lowerLimit, "-", upperLimit);

            // Show expected range
            rangeText.innerHTML = `Expected Range: ${lowerLimit} - ${upperLimit}`;
            rangeText.style.color = "green";

            // Validate Observed GSM
            if (!isNaN(obsGSMValue)) {
                console.log("Observed GSM Value:", obsGSMValue);
                if (obsGSMValue < lowerLimit || obsGSMValue > upperLimit) {
                    console.warn("Observed GSM is OUT of range. Showing remark section.");
                    remarksDiv.style.display = "block"; // Show remarks
                } else {
                    console.log("Observed GSM is WITHIN range. Hiding remark section.");
                    remarksDiv.style.display = "none"; // Hide remarks
                }
            } else {
                console.warn("Invalid Observed GSM Value.");
            }
        }



        function validateObservedDimension() {
            var stdDimInput = document.getElementById('<%= TB_StandardDimension.ClientID %>').value.trim();
            var obsDimValue = parseFloat(document.getElementById('<%= TB_DimensionObservation.ClientID %>').value);
            var remarksDiv = document.getElementById("Dimension_ObsremarkSection");
            var rangeText = document.getElementById("Dimension_ValidRange");

            console.log("Raw STD Dimension Input:", stdDimInput);
            console.log("Raw Observed Dimension Input:", obsDimValue);

            var regex = /^([\d.]+)\s*±\s*([\d.]+)?$/; // Match "30.5 ± 2" OR just "30.5"

            var stdDimValue, toleranceValue;

            if (regex.test(stdDimInput)) {
                var matches = stdDimInput.match(regex);
                stdDimValue = parseFloat(matches[1]); // Extract standard value (e.g., 30.5)
                toleranceValue = matches[2] ? parseFloat(matches[2]) : 0; // Extract tolerance if present, else 0
            } else {
                stdDimValue = parseFloat(stdDimInput); // If no ± tolerance, assume 0 mm tolerance
                toleranceValue = 0; // Default tolerance to 0 mm
            }

            console.log("Parsed STD Dimension Value:", stdDimValue);
            console.log("Parsed Tolerance Value:", toleranceValue, "mm");

            if (isNaN(stdDimValue)) {
                rangeText.innerHTML = "Expected Range: (Enter a valid STD Dimension first)";
                rangeText.style.color = "red";
                console.warn("Invalid STD Dimension Value. Cannot calculate range.");
                return;
            }

            // Calculate tolerance range dynamically
            var lowerLimit = (stdDimValue - toleranceValue).toFixed(2);
            var upperLimit = (stdDimValue + toleranceValue).toFixed(2);

            console.log("Expected Range:", lowerLimit, "-", upperLimit);

            // Show expected range
            rangeText.innerHTML = `Expected Range: ${lowerLimit} - ${upperLimit} mm`;
            rangeText.style.color = "green";

            // Validate Observed Dimension
            if (!isNaN(obsDimValue)) {
                console.log("Observed Dimension Value:", obsDimValue);
                if (obsDimValue < lowerLimit || obsDimValue > upperLimit) {
                    console.warn("Observed Dimension is OUT of range. Showing remark section.");
                    remarksDiv.style.display = "block"; // Show remarks
                } else {
                    console.log("Observed Dimension is WITHIN range. Hiding remark section.");
                    remarksDiv.style.display = "none"; // Hide remarks
                }
            } else {
                console.warn("Invalid Observed Dimension Value.");
            }
        }






    </script>
    <asp:HiddenField ID="hdn_formid" runat="server" />
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text=" Adhesive Tape Report"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QC/PKNG/02"></asp:Label>
                            </h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_MatVarietyName" runat="server" AssociatedControlID="TB_MatVarietyName" Text="Material / Variety" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_MatVarietyName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MatVarietyName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_MatVarietyName" runat="server" ControlToValidate="TB_MatVarietyName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_MatVarietyName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Material / Variety" MaxLength="50"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>
                                <%--<div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>--%>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_SupplierName" runat="server" AssociatedControlID="TB_SupplierName" Text="Supplier Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_SupplierName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_SupplierName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_SupplierName" runat="server" ControlToValidate="TB_SupplierName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_SupplierName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Supplier Name" MaxLength="50"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3" id="SizeDIV" runat="server">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_Size" runat="server" AssociatedControlID="TB_Size" Text="Sample Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_Size" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Size" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_Size" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Size" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <asp:RangeValidator ID="CV_TB_Size" runat="server" ControlToValidate="TB_Size" ErrorMessage="[10 - 40]" ForeColor="Red" MinimumValue="10" MaximumValue="40"></asp:RangeValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Size(in pkts)"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Challan No." MaxLength="20"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date Required" ControlToValidate="TB_ChallanDate" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" TextMode="Date" Placeholder="Select Challan Date"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot and Gate No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_LotNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Lot and Gate No." MaxLength="20"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_VehicleNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Vehicle No." MaxLength="15"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_PrintingColour" runat="server" AssociatedControlID="TB_PrintingColour" Text="Printing Colour:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_PrintingColour" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_PrintingColour" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_PrintingColour" runat="server" ControlToValidate="TB_PrintingColour" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_PrintingColour" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Printing Colour" MaxLength="20"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_AdhesiveProperty" runat="server" AssociatedControlID="TB_AdhesiveProperty" Text="Adhesive Property:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_AdhesiveProperty" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_AdhesiveProperty" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_TB_AdhesiveProperty" runat="server" ControlToValidate="TB_AdhesiveProperty" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_AdhesiveProperty" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Adhesive Property" MaxLength="25"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <%--                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_StandardDimension" runat="server" AssociatedControlID="TB_StandardDimension" Text="Standard Dimension (in mm):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_StandardDimension" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_StandardDimension" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RangeValidator ID="RV_TB_StandardDimension" runat="server" ControlToValidate="TB_StandardDimension" ValidationGroup="Submit" MinimumValue="0.00" MaximumValue="1000.0" Type="Double" ErrorMessage="Value must be between 0.00 and 1000.00 mm" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardDimension" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Standard Dimension" MaxLength="25"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <!-- Dimension Observation Field (No Validation) -->
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_DimensionObservation" runat="server" AssociatedControlID="TB_DimensionObservation" Text="Observation Dimension(in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <!-- No validation required for this input field -->
                                            <asp:TextBox ID="TB_DimensionObservation" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Dimension Observation"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                
                                <!-- Remarks Textbox for invalid input (initially hidden) -->
                                <div class="col-md-3" id="RemarksSection" style="display: none;">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_Remarks" runat="server" Text="Dimension Remarks:" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardDimensionRemarks" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Remarks" MaxLength="250"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>


                                <div class="col-md-3">
                                    <!-- Standard GSM Field with Validation (Range: 99.5 to 101) -->
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_StandardGSM" runat="server" AssociatedControlID="TB_StandardGSM" Text="Standard GSM:" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_StandardGSM" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_StandardGSM" ValidationGroup="Submit" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RangeValidator ID="RV_StandardGSM" runat="server" ControlToValidate="TB_StandardGSM" MinimumValue="0.00" MaximumValue="1000.0" Type="Double" ErrorMessage="Value must be between 0.00 and 1000.00 mm" ValidationGroup="Submit" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardGSM" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Standard GSM"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <!-- GSM Observation Field (No Validation Required) -->
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_GSMObservation" runat="server" AssociatedControlID="TB_GSMObservation" Text="GSM Observation:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_GSMObservation" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter GSM Observation"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                
                                <!-- Hidden Remarks Field for Standard GSM Validation Deviation (Initially Hidden) -->
                                <div class="col-md-3" id="div_GSM_Remarks" style="display: none;">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_GSMRemarks" runat="server" AssociatedControlID="TB_GSMRemarks" Text="Remarks (GSM Deviation):" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_GSMRemarks" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter reason for deviation"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>--%>

                                <%--Std--%>
                                <div class="col-md-3" id="Std_Dim_Div" runat="server" visible="true">
                                    <div class="mb-3">
                                        <asp:Label ID="LBL_TB_StandardDimension" runat="server" AssociatedControlID="TB_StandardDimension" Text="Dimension (STD):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:Literal ID="span_Dimension" runat="server" Text='<%# string.IsNullOrEmpty(Eval("GMS_Std")?.ToString()) ? "0.00" : Eval("GMS_Std") %>'></asp:Literal>
                                        <asp:RequiredFieldValidator ID="RFV_TB_StandardDimension" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_StandardDimension" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:CustomValidator ID="CV_TB_StandardDimension" runat="server" ControlToValidate="TB_StandardDimension" ValidationGroup="Submit" ErrorMessage="Invalid format! Use '27.3 ± 2 mm' or '420 mm'."  Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardDimension" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="e.g., 27.3 ± 2 mm" MaxLength="15" onkeyup="validateObservedDimension()" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <%-- Obs--%>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_DimensionObservation" runat="server" AssociatedControlID="TB_DimensionObservation" Text="Dimension (OBS):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_DimensionObservation" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionObservation" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_DimensionObservation" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Observed Dimension" MaxLength="10" onkeyup="validateObservedDimension()" ClientIDMode="Static"></asp:TextBox>
                                            <small id="Dimension_ValidRange" class="form-text text-muted" style="font-weight: bold; color: green;">Expected Range: (STD Value Missing, enter in mm)
                                            </small>
                                        </div>
                                    </div>
                                </div>

                                <%--Remarks--%>
                                <div class="col-md-3" id="Dimension_ObsremarkSection" style="display: none;">
                                    <div class="mb-3">
                                        <asp:Label ID="lbl_TB_StandardDimensionRemarks" runat="server" AssociatedControlID="TB_StandardDimensionRemarks" Text="Please provide a remark for deviation:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardDimensionRemarks" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter remark for deviation"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <%--std--%>
                                <div class="col-md-3" id="Std_gsmwt_Div" runat="server" visible="true">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_StandardGSM" runat="server" AssociatedControlID="TB_StandardGSM" Text="GSM/WT per 10 PS (STD):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:Literal ID="span_GMS" runat="server" Text='<%# string.IsNullOrEmpty(Eval("GMS_Std")?.ToString()) ? "0.00" : Eval("GMS_Std") %>'></asp:Literal>
                                        <asp:RequiredFieldValidator ID="RFV_TB_StandardGSM" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_StandardGSM" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:CustomValidator ID="CV_TB_StandardGSM" runat="server" ControlToValidate="TB_StandardGSM" ValidationGroup="Submit" ErrorMessage="Invalid input format! Use: '27.3 ± 5%', '8.2 to 10.2', or '420'"  Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_StandardGSM" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="e.g., 27.3 ± 2%" MaxLength="10" onkeyup="validateObservedGSM()" ClientIDMode="Static"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <%--obs--%>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_GSMObservation" runat="server" AssociatedControlID="TB_GSMObservation" Text="GSM/WT per 10 PS (OBS):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_GSMObservation" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_GSMObservation" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_GSMObservation" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Enter Observed GSM" MaxLength="10" onkeyup="validateObservedGSM()" ClientIDMode="Static"></asp:TextBox>
                                            <small id="GSM_ValidRange" class="form-text text-muted" style="font-weight: bold; color: green;">Expected Range: (STD Value Missing)
                                            </small>
                                        </div>
                                    </div>
                                </div>

                                <%--remarks--%>
                                <div class="col-md-3" id="GSM_ObsremarkSection" style="display: none;">
                                    <div class="mb-3">
                                        <asp:Label ID="lbl_TB_GSMRemarks" runat="server" AssociatedControlID="TB_GSMRemarks" Text="Please provide a remark for deviation:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_GSMRemarks" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter remark for deviation"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>


                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_Remarks" runat="server" AssociatedControlID="TB_Remarks" Text="Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" TextMode="MultiLine" Placeholder="Enter any additional remarks" Rows="4" MaxLength="500"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <%--Button--%>
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <div class="input-group input-group-sm">
                                            <asp:Button ID="BtnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="BtnSubmit_Click" />
                                            <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BtnReset_Click" />
                                            <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
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
                                <asp:Label ID="Label6" runat="server" Text="Approval Matrix"></asp:Label></h2>
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
