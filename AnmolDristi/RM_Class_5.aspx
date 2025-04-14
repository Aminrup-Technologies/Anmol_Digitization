<%@ Page Title="AIL | QC RM - Class - V" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RM_Class_5.aspx.cs" Inherits="AnmolDristi.RM_Class_5" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">


    <style type="text/css">
        .hidden {
            display: none;
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

        function validateForm1() {
            var fileUpload = document.getElementById('<%= FU_MaterialImage.ClientID %>');
            var lblErrorMessage1 = document.getElementById('<%= lblErrorMessage1.ClientID %>');
            if (fileUpload.files.length === 0) {
                lblErrorMessage1.innerHTML = "Please upload file.";
                return false;
            } else {
                lblErrorMessage1.innerHTML = "";
                return true;
            }
        }

        function validateSubmit() {
            var fileUpload = document.getElementById('<%= FU_MaterialImage.ClientID %>'); // Get the FileUpload control
            var errorMessageLabel = document.getElementById('<%= lblErrorMessage1.ClientID %>'); // Get the error message label

            if (fileUpload.value === "") {
                errorMessageLabel.innerHTML = "Please select a file before submitting."; // Display error message
                errorMessageLabel.style.color = "red"; // Change color to red
                return false; // Prevent form submission
            }

            // File is selected, return true to allow form submission
            return true;
        }


        function toggleSmellRemarksDiv(radioButtonList) {
            console.log("toggleSmellRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SmellRemarksDiv");
            console.log("DIV :" + remarksDiv);
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "Not Ok". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200); // Adjust delay as necessary
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleAppearanceRemarksDiv(radioButtonList) {
            console.log("toggleAppearanceRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("AppearanceRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "Not Ok". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200); // Adjust delay as necessary
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleTasteFlavorRemarksDiv(radioButtonList) {
            console.log("toggleTasteFlavorRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("TasteFlavorRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "Not Ok". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200); // Adjust delay as necessary
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleGradeRemarksDiv(radioButtonList) {
            console.log("toggleGradeRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("GradeRemarksDIV");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "Not Ok". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200); // Adjust delay as necessary
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateQuantityValue(textBox) {
            console.log("validateQuantityValue function called");
            var qtyValue = parseFloat(document.getElementById('<%=TB_Quantity.ClientID%>').value);
            var remarksDiv = document.getElementById("QuantityRemarksDIV");
            var minQtyValue = parseFloat(document.getElementById('<%= hdnMinQtyValue.ClientID %>').value);
            var maxQtyValue = parseFloat(document.getElementById('<%= hdnMaxQtyValue.ClientID %>').value);

            // Define the valid range
            //var minQtyValue = 1000.00;
            //var maxQtyValue = 2000.00;

            // Check if quantity value is within the valid range
            if (!isNaN(qtyValue) && qtyValue !== "" && qtyValue < minQtyValue || qtyValue > maxQtyValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validatePurityValue(textBox) {
            console.log("validatePurityValue function called");

            // Retrieve elements and values
            var PurityValue = parseFloat(document.getElementById('<%= TB_Purity.ClientID %>').value);
            var remarksDiv = document.getElementById("PurityRemarksDIV");
            var minPurityValue = parseFloat(document.getElementById('<%= hdnMinPurityValue.ClientID %>').value);
            var maxPurityValue = parseFloat(document.getElementById('<%= hdnMaxPurityValue.ClientID %>').value);

            // Define the valid range
            //var minPurityValue = 20.00;
            //var maxPurityValue = 30.00;

            // Check if ph value is within the valid range
            if (!isNaN(PurityValue) && PurityValue !== "" && PurityValue < minPurityValue || PurityValue > maxPurityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validatePhValue(textBox) {
            console.log("validatePhValue function called");

            // Retrieve elements and values
            var PHValue = parseFloat(document.getElementById('<%= TB_PH.ClientID %>').value);
            var remarksDiv = document.getElementById("PHRemarksDIV");
            var minPHValue = parseFloat(document.getElementById('<%= hdnMinPhValue.ClientID %>').value);
            var maxPHValue = parseFloat(document.getElementById('<%= hdnMaxPhValue.ClientID %>').value);

            // Define the valid range
            //var minPHValue = 20.00;
            //var maxPHValue = 30.00;

            // Check if ph value is within the valid range
            if (!isNaN(PHValue) && PHValue !== "" && PHValue < minPHValue || PHValue > maxPHValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateMoistureValue(textBox) {
            console.log("validateMoistureValue function called");

            // Retrieve elements and values
            var moistureValue = parseFloat(document.getElementById('<%= TB_Moisture.ClientID %>').value);
            var remarksDiv = document.getElementById("MoistureRemarksDIV");
            var minMoistureValue = parseFloat(document.getElementById('<%= hdnMinMoistureValue.ClientID %>').value);
            var maxMoistureValue = parseFloat(document.getElementById('<%= hdnMaxMoistureValue.ClientID %>').value);

            // Define the valid range
            //var minMoistureValue = 20.00;
            //var maxMoistureValue = 30.00;

            // Check if moisture value is within the valid range
            if (!isNaN(moistureValue) && moistureValue !== "" && moistureValue < minMoistureValue || moistureValue > maxMoistureValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateAcidValue(textbox) {
            console.log("validateAcidValue function called");

            // Retrieve elements and values
            var acidValue = parseFloat(document.getElementById('<%= TB_Acid.ClientID %>').value);
            var remarksDiv = document.getElementById("AcidRemarksDIV");
            var minAcidValue = parseFloat(document.getElementById('<%= hdnMinAcidValue.ClientID %>').value);
            var maxAcidValue = parseFloat(document.getElementById('<%= hdnMaxAcidValue.ClientID %>').value);

            // Define the valid range
            //var minAcidValue = 20.00;
            //var maxAcidValue = 30.00;

            // Check if AcidValue is within the valid range
            if (!isNaN(acidValue) && acidValue !== "" && acidValue < minAcidValue || acidValue > maxAcidValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateMpcpValue(textbox) {
            console.log("validateMpcpValue function called");

            // Retrieve elements and values
            var mpcpValue = parseFloat(document.getElementById('<%= TB_Mpcp.ClientID %>').value);
            var remarksDiv = document.getElementById("MpcpRemarksDIV");
            var minMpcpValue = parseFloat(document.getElementById('<%= hdnMinMpcpValue.ClientID %>').value);
            var maxMpcpValue = parseFloat(document.getElementById('<%= hdnMaxMpcpValue.ClientID %>').value);

            // Define the valid range
            //var minMpcpValue = 20.00;
            //var maxMpcpValue = 30.00;

            // Check if MpcpValue is within the valid range
            if (!isNaN(mpcpValue) && mpcpValue !== "" && mpcpValue < minMpcpValue || mpcpValue > maxMpcpValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateDispersabilityValue(textbox) {
            console.log("validateDispersabilityValue function called");

            // Retrieve elements and values
            var dispersabilityValue = parseFloat(document.getElementById('<%= TB_Dispersability.ClientID %>').value);
            var remarksDiv = document.getElementById("DispersabilityRemarksDIV");
            var minDispersabilityValue = parseFloat(document.getElementById('<%= hdnMinDispersabilityValue.ClientID %>').value);
            var maxDispersabilityValue = parseFloat(document.getElementById('<%= hdnMaxDispersabilityValue.ClientID %>').value);

            // Define the valid range
            //var minDispersabilityValue = 20.00;
            //var maxDispersabilityValue = 30.00;

            // Check if DispersabilityValue is within the valid range
            if (!isNaN(dispersabilityValue) && dispersabilityValue !== "" && dispersabilityValue < minDispersabilityValue || dispersabilityValue > maxDispersabilityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateDrcValue(textbox) {
            console.log("validateDrcValue function called");

            // Retrieve elements and values
            var drcValue = parseFloat(document.getElementById('<%= TB_Drc.ClientID %>').value);
            var remarksDiv = document.getElementById("DrcRemarksDIV");
            var minDrcValue = parseFloat(document.getElementById('<%= hdnMinDrcValue.ClientID %>').value);
            var maxDrcValue = parseFloat(document.getElementById('<%= hdnMaxDrcValue.ClientID %>').value);

            // Define the valid range
            //var minDrcValue = 20.00;
            //var maxDrcValue = 30.00;

            // Check if DrcValue is within the valid range
            if (!isNaN(drcValue) && drcValue !== "" && drcValue < minDrcValue || drcValue > maxDrcValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateMonoGlycerideContentValue(textbox) {
            console.log("validateMonoGlycerideContentValue function called");

            // Retrieve elements and values
            var monoGlycerideContentValue = parseFloat(document.getElementById('<%= TB_MonoGlycerideContent.ClientID %>').value);
            var remarksDiv = document.getElementById("MonoGlycerideContentRemarksDIV");
            var minMonoGlycerideContentValue = parseFloat(document.getElementById('<%= hdnMinMonoGlycerideContentValue.ClientID %>').value);
            var maxMonoGlycerideContentValue = parseFloat(document.getElementById('<%= hdnMaxMonoGlycerideContentValue.ClientID %>').value);

            // Define the valid range
            //var minMonoGlycerideContentValue = 20.00;
            //var maxMonoGlycerideContentValue = 30.00;

            // Check if MonoGlycerideContentValue is within the valid range
            if (!isNaN(monoGlycerideContentValue) && monoGlycerideContentValue !== "" && monoGlycerideContentValue < minMonoGlycerideContentValue || monoGlycerideContentValue > maxMonoGlycerideContentValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateNeutralizingValue(textbox) {
            console.log("validateNeutralizingValue function called");

            // Retrieve elements and values
            var neutralizingValue = parseFloat(document.getElementById('<%= TB_Neutralizing.ClientID %>').value);
            var remarksDiv = document.getElementById("NeutralizingRemarksDIV");
            var minNeutralizingValue = parseFloat(document.getElementById('<%= hdnMinNeutralizingValue.ClientID %>').value);
            var maxNeutralizingValue = parseFloat(document.getElementById('<%= hdnMaxNeutralizingValue.ClientID %>').value);

            // Define the valid range
            //var minNeutralizingValue = 20.00;
            //var maxNeutralizingValue = 30.00;

            // Check if NeutralizingValue is within the valid range
            if (!isNaN(neutralizingValue) && neutralizingValue !== "" && neutralizingValue < minNeutralizingValue || neutralizingValue > maxNeutralizingValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateBrixValue(textbox) {
            console.log("validateBrixValue function called");

            // Retrieve elements and values
            var brixValue = parseFloat(document.getElementById('<%= TB_Brix.ClientID %>').value);
            var remarksDiv = document.getElementById("BrixRemarksDIV");
            var minBrixValue = parseFloat(document.getElementById('<%= hdnMinBrixValue.ClientID %>').value);
            var maxBrixValue = parseFloat(document.getElementById('<%= hdnMaxBrixValue.ClientID %>').value);

            // Define the valid range
            //var minBrixValue = 20.00;
            //var maxBrixValue = 30.00;

            // Check if BrixValue is within the valid range
            if (!isNaN(brixValue) && brixValue !== "" && brixValue < minBrixValue || brixValue > maxBrixValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }


        function validateWIMValue(textbox) {
            console.log("validateWIMValue function called");

            // Retrieve elements and values
            var wimValue = parseFloat(document.getElementById('<%= TB_WIM.ClientID %>').value);
            var remarksDiv = document.getElementById("WIMRemarksDIV");
            var minWIMValue = parseFloat(document.getElementById('<%= hdnMinWIMValue.ClientID %>').value);
            var maxWIMValue = parseFloat(document.getElementById('<%= hdnMaxWIMValue.ClientID %>').value);

            // Define the valid range
            //var minWIMValue = 20.00;
            //var maxWIMValue = 30.00;

            // Check if WIM value is within the valid range
            if (!isNaN(wimValue) && wimValue !== "" && wimValue < minWIMValue || wimValue > maxWIMValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }


        function toggleAppStatusRemarksDiv(radioButtonList) {
            console.log("toggleAppStatusRemarksDiv function called");

            // Get the actual IDs of the controls rendered by ASP.NET
            var remarksInput = document.getElementById('<%= TXB_AppStatus_Remarks.ClientID %>');
             var remarksValidator = document.getElementById('<%= RFV_TXB_AppStatus_Remarks.ClientID %>');

             if (!remarksInput || !remarksValidator) {
                 console.error("Remarks input or validator not found in the DOM.");
                 return;
             }

             var selectedValue = radioButtonList.querySelector("input:checked").value;
             console.log("Selected value: " + selectedValue);

             if (selectedValue === "0") { // "Rejected" selected
                 remarksInput.required = true;
                 remarksValidator.style.display = "inline"; // Show validation error if not provided
                 setTimeout(function () {
                     // Display a PNotify notification
                     new PNotify({
                         title: 'Input Required',
                         text: 'You have selected "Rejected". Remarks are mandatory.',
                         type: 'warning',
                         styling: 'bootstrap3'
                     });
                 }, 200); // Adjust delay as necessary
             } else { // "Accepted" selected
                 remarksInput.required = false;
                 remarksValidator.style.display = "none"; // Hide validation error
             }
         }

        function showNotification(title, text, type) {
            new PNotify({
                title: title,
                text: text,
                type: type,
                styling: 'bootstrap3'
            });
        }

    </script>

    <asp:HiddenField ID="hdn_formid" runat="server" />

    <asp:HiddenField ID="hdnMinBrandValue" runat="server" />
    <asp:HiddenField ID="hdnMaxBrandValue" runat="server" />

    <asp:HiddenField ID="hdnMinSupplierValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSupplierValue" runat="server" />

    <asp:HiddenField ID="hdnMinQtyValue" runat="server" />
    <asp:HiddenField ID="hdnMaxQtyValue" runat="server" />

    <asp:HiddenField ID="hdnMinImpuritiesValue" runat="server" />
    <asp:HiddenField ID="hdnMaxImpuritiesValue" runat="server" />

    <asp:HiddenField ID="hdnMinPurityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxPurityValue" runat="server" />

    <asp:HiddenField ID="hdnMinPhValue" runat="server" />
    <asp:HiddenField ID="hdnMaxPhValue" runat="server" />

    <asp:HiddenField ID="hdnMinMoistureValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMoistureValue" runat="server" />

    <asp:HiddenField ID="hdnMinAcidValue" runat="server" />
    <asp:HiddenField ID="hdnMaxAcidValue" runat="server" />

    <asp:HiddenField ID="hdnMinMpcpValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMpcpValue" runat="server" />

    <asp:HiddenField ID="hdnMinDispersabilityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxDispersabilityValue" runat="server" />

    <asp:HiddenField ID="hdnMinDrcValue" runat="server" />
    <asp:HiddenField ID="hdnMaxDrcValue" runat="server" />

    <asp:HiddenField ID="hdnMinMonoGlycerideContentValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMonoGlycerideContentValue" runat="server" />

    <asp:HiddenField ID="hdnMinNeutralizingValue" runat="server" />
    <asp:HiddenField ID="hdnMaxNeutralizingValue" runat="server" />

    <asp:HiddenField ID="hdnMinBrixValue" runat="server" />
    <asp:HiddenField ID="hdnMaxBrixValue" runat="server" />

    <asp:HiddenField ID="hdnMinWIMValue" runat="server" />
    <asp:HiddenField ID="hdnMaxWIMValue" runat="server" />


    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label>
                    </h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label>
                            </h2>
                            <div class="clearfix"></div>
                        </div>


                        <div class="x-content">

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-1 : RM Basic Details</h4>
                                <hr>
                            </div>

                            <div class="col-md-3" id="MaterialDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_Material" runat="server" AssociatedControlID="DDL_Material" Text="Material Name" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Material_Value" runat="server" AssociatedControlID="DDL_Material" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Material" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_Material_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <%--<div id="MaterialDIV" class="col-md-3">
                                <div class="mb-3">
                                    <label for="DDL_Material" id="Label_DDL_Material" style="color: Brown; font-size: Small; font-weight: bold;">Material Name</label>
                                    <span id="RFV_DDL_Material" style="color: Red; display: none;">*</span>
                                    <div class="input-group-sm">
                                        <select name="DDL_Material" id="DDL_Material" class="form-control form-control-sm rounded">
                                            <option selected="selected" value="0">Select</option>
                                            <option value="22">GMS</option>
                                            <option value="23">Yeast</option>
                                            <option value="24">Salt</option>
                                            <option value="25">SBC</option>
                                            <option value="26">SMBS</option>
                                            <option value="27">ABC</option>
                                            <option value="28">MACP</option>
                                            <option value="29">Sorbital</option>
                                        </select>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="PlantDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SupplierDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Supplier" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Supplier" runat="server" ControlToValidate="TB_Supplier" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Text="" Placeholder="Supplier (3-30 characters)" MaxLength="30"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BrandDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TB_Brand" runat="server" AssociatedControlID="TB_Brand" Text="Brand Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Brand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_Brand"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Brand" runat="server" ControlToValidate="TB_Brand" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Brand" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Brand Name (3-30 characters)" MaxLength="30"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ChallanNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_ChallanNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" Text="" ValidationGroup="Submit" Placeholder="Challan No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ChallanDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="QuantityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Quantity" runat="server" AssociatedControlID="TB_Quantity" Text="Quantity Supplied :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" ValidationGroup="Submit" runat="server" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Quantity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [1000.00-2000.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Quantity Value(in pkts) " oninput="validateQuantityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="QuantityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Quantity_Remarks" runat="server" AssociatedControlID="TXB_Quantity_Remarks" Text="Quantity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Quantity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Quantity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Quantity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LotNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_LotNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Lot/Gate No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BatchNo" runat="server" AssociatedControlID="TB_BatchNo" Text="Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BatchNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_BatchNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BatchNo" runat="server" ControlToValidate="TB_BatchNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BatchNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Batch No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="VehicleNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_VehicleNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Vehicle No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PkdMfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_PkdMfg" runat="server" AssociatedControlID="TB_PkdMfg" Text="Pkd/Mfg Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PkdMfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_PkdMfg" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PkdMfg" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr>
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-2[A] : Parameters with Standards (Yes / No)</h4>
                                <hr>
                            </div>

                            <div class="col-md-3" id="GradeDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_Grade" runat="server" AssociatedControlID="RBL_Grade" Text="Grade :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Grade" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Grade" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Grade" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleGradeRemarksDiv(this);">
                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GradeRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Grade_Remarks" runat="server" AssociatedControlID="TXB_Grade_Remarks" Text="Grade Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Grade_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Grade_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Grade_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ColorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColor" runat="server" AssociatedControlID="DDL_Color" Text=" Color  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Color" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Color" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Color" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_Color_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ColorRemarksDiv" runat="server" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColorRemarksDiv" runat="server" AssociatedControlID="TXB_Color_Remarks" Text="Color (Others)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ColorRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Color_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Color_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SmellDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelSmell" runat="server" AssociatedControlID="RBL_Smell" Text="Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Smell" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="RBL_Smell" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Smell" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSmellRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SmellRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelSmellRemarks" runat="server" AssociatedControlID="TXB_Smell_Remarks" Text="Smell (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Smell_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Smell_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Smell_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppearanceDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppearance" runat="server" AssociatedControlID="RBL_Appearance" Text="Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Appearance" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Appearance" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Appearance" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleAppearanceRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppearanceRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppearancepRemarks" runat="server" AssociatedControlID="TXB_Appearance_Remarks" Text=" Appearance (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Appearance_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Appearance_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Appearance_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TasteFlavorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTasteFlavor" runat="server" AssociatedControlID="RBL_TasteFlavor" Text="Taste/Flavor :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_TasteFlavor" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_TasteFlavor" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_TasteFlavor" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleTasteFlavorRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TasteFlavorRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTasteFlavorRemarks" runat="server" AssociatedControlID="TXB_TasteFlavor_Remarks" Text=" Taste/Flavor  (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_TasteFlavor_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TasteFlavor_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_TasteFlavor_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr>
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-2[B]: Parameters with Standards (Lab Results)</h4>
                                <hr>
                            </div>

                            <div class="col-md-3" id="ImpuritiesDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Foreign_Impurities" runat="server" AssociatedControlID="TB_Foreign_Impurities" Text="ForeignMatter/Impurities :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Foreign_Impurities" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Foreign_Impurities" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Foreign_Impurities" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Foreign_Impurities" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Foreign_Impurities" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="ForeignMatter/Impurities (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PurityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Purity" runat="server" AssociatedControlID="TB_Purity" Text=" Purity Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Purity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_PH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Purity" runat="server" ControlToValidate="TB_Purity" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Purity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Purity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Purity Value" oninput="validatePurityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PurityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Purity_Remarks" runat="server" AssociatedControlID="TXB_Purity_Remarks" Text="Purity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Purity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Purity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Purity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PHDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_PH" runat="server" AssociatedControlID="TB_PH" Text=" PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PH" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_PH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PH" runat="server" ControlToValidate="TB_PH" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_PH" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PH" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="PH Value" oninput="validatePhValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PHRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_PH_Remarks" runat="server" AssociatedControlID="TXB_PH_Remarks" Text="PH Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_PH_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_PH_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_PH_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MoistureDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Moisture" runat="server" AssociatedControlID="TB_Moisture" Text="Moisture (%)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Moisture" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Moisture" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Moisture" runat="server" ControlToValidate="TB_Moisture" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Moisture" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Moisture" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Moisture Value" oninput="validateMoistureValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MoistureRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Moisture_Remarks" runat="server" AssociatedControlID="TXB_Moisture_Remarks" Text="Moisture Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Moisture_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Moisture_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Moisture_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AcidDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Acid" runat="server" AssociatedControlID="TB_Acid" Text="Acid Value:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Acid" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Acid" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Acid" runat="server" ControlToValidate="TB_Acid" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Acid" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Acid" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Acid Value " oninput="validateAcidValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AcidRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Acid_Remarks" runat="server" AssociatedControlID="TXB_Acid_Remarks" Text="Acid Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Acid_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Acid_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Acid_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MpcpDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mpcp" runat="server" AssociatedControlID="TB_Mpcp" Text="MP/CP:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Mpcp" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Mpcp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Mpcp" runat="server" ControlToValidate="TB_Mpcp" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Mpcp" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mpcp" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="MP/CP Value " oninput="validateMpcpValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MpcpRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Mpcp_Remarks" runat="server" AssociatedControlID="TXB_Mpcp_Remarks" Text="MP/CP Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Mpcp_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Mpcp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Mpcp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DispersabilityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Dispersability" runat="server" AssociatedControlID="TB_Dispersability" Text="Dispersability in water:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Dispersability" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Dispersability" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Dispersability" runat="server" ControlToValidate="TB_Dispersability" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Dispersability" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Dispersability" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Dispersability Value " oninput="validateDispersabilityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DispersabilityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Dispersability_Remarks" runat="server" AssociatedControlID="TXB_Dispersability_Remarks" Text="Dispersability Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Dispersability_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Dispersability_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Dispersability_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DrcDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Drc" runat="server" AssociatedControlID="TB_Drc" Text="Drc in 1 hr:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Drc" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Drc" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Drc" runat="server" ControlToValidate="TB_Drc" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Drc" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Drc" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Drc Value " oninput="validateDrcValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DrcRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Drc_Remarks" runat="server" AssociatedControlID="TXB_Drc_Remarks" Text="Drc Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Drc_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Drc_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Drc_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MonoGlycerideContentDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_MonoGlycerideContent" runat="server" AssociatedControlID="TB_MonoGlycerideContent" Text="Mono Glyceride Content:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_MonoGlycerideContent" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_MonoGlycerideContent" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_MonoGlycerideContent" runat="server" ControlToValidate="TB_MonoGlycerideContent" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_MonoGlycerideContent" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MonoGlycerideContent" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="MonoGlycerideContent Value " oninput="validateMonoGlycerideContentValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MonoGlycerideContentRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_MonoGlycerideContent_Remarks" runat="server" AssociatedControlID="TXB_MonoGlycerideContent_Remarks" Text="Mono Glyceride Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_MonoGlycerideContent_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_MonoGlycerideContent_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_MonoGlycerideContent_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="NeutralizingDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Neutralizing" runat="server" AssociatedControlID="TB_Neutralizing" Text="Neutralizing Value:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Neutralizing" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Neutralizing" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Neutralizing" runat="server" ControlToValidate="TB_Neutralizing" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Neutralizing" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Neutralizing" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Neutralizing Value " oninput="validateNeutralizingValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="NeutralizingRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Neutralizing_Remarks" runat="server" AssociatedControlID="TXB_Neutralizing_Remarks" Text="Neutralizing Value Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Neutralizing_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Neutralizing_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Neutralizing_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BrixDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Brix" runat="server" AssociatedControlID="TB_Brix" Text="Brix:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Brix" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Brix" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Brix" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Brix" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Brix" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Brix" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Brix Value " oninput="validateBrixValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BrixRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Brix_Remarks" runat="server" AssociatedControlID="TXB_Brix_Remarks" Text="Brix Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Brix_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Brix_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Brix_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="WIMDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_WIM" runat="server" AssociatedControlID="TB_WIM" Text="WIM (%) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_WIM" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_WIM" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_WIM" runat="server" ValidationGroup="Submit" ControlToValidate="TB_WIM" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_WIM" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_WIM" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="WIM (%) " oninput="validateWIMValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="WIMRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_WIM" runat="server" AssociatedControlID="TXB_WIM" Text="WIM Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_WIM" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_WIM" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_WIM" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-3: Decision Making</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="AppStatusDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppStatus" runat="server" AssociatedControlID="RBL_AppStatus" Text="Approval Status :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_AppStatus" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Save" ControlToValidate="RBL_AppStatus" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_AppStatus" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleAppStatusRemarksDiv(this);">
                                            <asp:ListItem Text="Accepted" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Rejected" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppStatusRemarksDiv">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppStatusRemarks" runat="server" AssociatedControlID="TXB_AppStatus_Remarks" Text="Approval Status Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_AppStatus_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_AppStatus_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_AppStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-4 : Phtotgraph Attachment</h4>
                                <hr>
                            </div>


                            <div class="col-md-3" id="FU_MaterialImage_Upldr" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_FU_MaterialImage" runat="server" AssociatedControlID="FU_MaterialImage" Text="Material Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_FU_MaterialImage" runat="server" ErrorMessage="*" ControlToValidate="FU_MaterialImage" Display="Dynamic" ValidationGroup="ValidationGroup1" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="CV_FU_MaterialImage" runat="server" ControlToValidate="FU_MaterialImage" Display="Dynamic" ValidationGroup="ValidationGroup1" ErrorMessage="Please upload file"></asp:CustomValidator>
                                    <asp:Label ID="lblErrorMessage1" runat="server" CssClass="text-danger"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:FileUpload ID="FU_MaterialImage" runat="server" CssClass="form-control rounded" onchange="displayImage(this);" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="BtnUploadFU_MaterialImage" runat="server" CssClass="btn btn-primary btn-sm" Text="Upload" OnClientClick="return validateForm1();" OnClick="BtnUploadFU_MaterialImage_Click" ValidationGroup="ValidationGroup1" CausesValidation="true" />
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FU_MaterialImage_img" runat="server" visible="false">
                                <asp:Image ID="uploadedImage1" runat="server" CssClass="img-fluid" />
                            </div>

                            <div class="col-md-12">
                                <hr>
                            </div>

                            <%--Button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="BtnSubmit_Click" />
                                        <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BtnReset_Click" />
                                        <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
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
                                <asp:Label ID="Label8" runat="server" Text="Approval Matrix"></asp:Label>
                            </h2>
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
                                        <asp:Label ID="Label7" runat="server" Text="Approver 2" />
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
