<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RM_Class_3.aspx.cs" Inherits="AnmolDristi.RM_Class_3" %>

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

        //image
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

        function validateSave() {
            var fileUpload = document.getElementById('<%= FU_MaterialImage.ClientID %>'); // Get the FileUpload control
            var errorMessageLabel = document.getElementById('<%= lblErrorMessage1.ClientID %>'); // Get the error message label

            if (fileUpload.value === "") {
                errorMessageLabel.innerHTML = "Please select a file before Saveting."; // Display error message
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

        function togglePackingConditionRemarksDiv(radioButtonList) {
            console.log("togglePackingConditionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("PackingConditionRemarksDiv");
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
            var remarksDiv = document.getElementById("GradeRemarksDiv");
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

        function validateBestBeforeDate() {
            const mfgDateInput = document.getElementById('<%= TB_Mfg.ClientID %>');
            const bestBeforeDateInput = document.getElementById('<%= TB_BeforeDate.ClientID %>');

            if (!mfgDateInput.value || !bestBeforeDateInput.value) {
                // If either date is not entered, skip further validation (required validators handle this)
                return;
            }

            const mfgDate = new Date(mfgDateInput.value);
            const bestBeforeDate = new Date(bestBeforeDateInput.value);

            if (isNaN(mfgDate.getTime()) || isNaN(bestBeforeDate.getTime())) {
                alert("Invalid date format. Please select valid dates.");
                bestBeforeDateInput.value = ""; // Reset invalid best before date
                return;
            }

            // Calculate difference in days
            const differenceInDays = (bestBeforeDate - mfgDate) / (1000 * 60 * 60 * 24);

            // Check if the difference is at least 2 years (730 days)
            if (differenceInDays < 730) {
                alert("Best Before Date must be at least 2 years (730 days) after the Manufacturing Date.");
                bestBeforeDateInput.value = ""; // Reset invalid best before date
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

            // Check if PH value is within the valid range
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

        function validateAshValue(textBox) {
            console.log("validateAshValue function called");

            // Retrieve elements and values
            var ashValue = parseFloat(document.getElementById('<%= TB_TotalAsh.ClientID %>').value);
            var remarksDiv = document.getElementById("AshRemarksDIV");
            var minAshValue = parseFloat(document.getElementById('<%= hdnMinTotalAshValue.ClientID %>').value);
            var maxAshValue = parseFloat(document.getElementById('<%= hdnMaxTotalAshValue.ClientID %>').value);

            // Define the valid range
            //var minAshValue = 20.00;
            //var maxAshValue = 30.00;

            // Check if Total Ash value is within the valid range
            if (!isNaN(ashValue) && ashValue !== "" && ashValue < minAshValue || ashValue > maxAshValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateInsolubleAshValue(textbox) {
            console.log("validateInsolubleAshValue function called");

            // Retrieve elements and values
            var insolubleAshValue = parseFloat(document.getElementById('<%= TB_InsolubleAsh.ClientID %>').value);
            var remarksDiv = document.getElementById("InsolubleAshRemarksDIV");
            var minInsolubleAshValue = parseFloat(document.getElementById('<%= hdnMinInsolubleAshValue.ClientID %>').value);
            var maxInsolubleAshValue = parseFloat(document.getElementById('<%= hdnMaxInsolubleAshValue.ClientID %>').value);

            // Define the valid range
            //var minInsolubleAshValue = 20.00;
            //var maxInsolubleAshValue = 30.00;

            // Check if Insoluble Ash value is within the valid range
            if (!isNaN(insolubleAshValue) && insolubleAshValue !== "" && insolubleAshValue < minInsolubleAshValue || insolubleAshValue > maxInsolubleAshValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSolidValue(textbox) {
            console.log("validateSolidValue function called");

            // Retrieve elements and values
            var totalSolidValue = parseFloat(document.getElementById('<%= TB_TotalSolid.ClientID %>').value);
            var remarksDiv = document.getElementById("SolidRemarksDIV");
            var minTotalSolidValue = parseFloat(document.getElementById('<%= hdnMinTotalSolidValue.ClientID %>').value);
            var maxTotalSolidValue = parseFloat(document.getElementById('<%= hdnMaxTotalSolidValue.ClientID %>').value);

            // Define the valid range
            //var minTotalSolidValue = 20.00;
            //var maxTotalSolidValue = 30.00;

            // Check if Solid value is within the valid range
            if (!isNaN(totalSolidValue) && totalSolidValue !== "" && totalSolidValue < minTotalSolidValue || totalSolidValue > maxTotalSolidValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateDextroseValue(textbox) {
            console.log("validateDextroseValue function called");

            // Retrieve elements and values
            var dextroseValue = parseFloat(document.getElementById('<%= TB_Dextrose.ClientID %>').value);
            var remarksDiv = document.getElementById("DextroseRemarksDIV");
            var minDextroseValue = parseFloat(document.getElementById('<%= hdnMinDextroseValue.ClientID %>').value);
            var maxDextroseValue = parseFloat(document.getElementById('<%= hdnMaxDextroseValue.ClientID %>').value);

            // Define the valid range
            //var minDextroseValue = 20.00;
            //var maxDextroseValue = 30.00;

            // Check if Dextrose value is within the valid range
            if (!isNaN(dextroseValue) && dextroseValue !== "" && dextroseValue < minDextroseValue || dextroseValue > maxDextroseValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateTitrableAcidityValue(textbox) {
            console.log("validateTitrableAcidityValue function called");

            // Retrieve elements and values
            var titrableAcidityValue = parseFloat(document.getElementById('<%= TB_TitrableAcidity.ClientID %>').value);
            var remarksDiv = document.getElementById("TitrableAcidityRemarksDIV");
            var minTitrableAcidityValue = parseFloat(document.getElementById('<%= hdnMinTitrableAcidityValue.ClientID %>').value);
            var maxTitrableAcidityValue = parseFloat(document.getElementById('<%= hdnMaxTitrableAcidityValue.ClientID %>').value);

            // Define the valid range
            //var minTitrableAcidityValue = 20.00;
            //var maxTitrableAcidityValue = 30.00;

            // Check if TitrableAcidity value is within the valid range
            if (!isNaN(titrableAcidityValue) && titrableAcidityValue !== "" && titrableAcidityValue < minTitrableAcidityValue || titrableAcidityValue > maxTitrableAcidityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }

        }

        function validateAlcoholicAcidityValue(textbox) {
            console.log("validateAlcoholicAcidityValue function called");

            // Retrieve elements and values
            var alcoholicAcidityValue = parseFloat(document.getElementById('<%= TB_AlcoholicAcidity.ClientID %>').value);
            var remarksDiv = document.getElementById("AlcoholicAcidityRemarksDIV");
            var minAlcoholicAcidityValue = parseFloat(document.getElementById('<%= hdnMinAlcoholicAcidityValue.ClientID %>').value);
            var maxAlcoholicAcidityValue = parseFloat(document.getElementById('<%= hdnMaxAlcoholicAcidityValue.ClientID %>').value);

            // Define the valid range
            //var minAlcoholicAcidityValue = 20.00;
            //var maxAlcoholicAcidityValue = 30.00;

            // Check if AlcoholicAcidity value is within the valid range
            if (!isNaN(alcoholicAcidityValue) && alcoholicAcidityValue !== "" && alcoholicAcidityValue < minAlcoholicAcidityValue || alcoholicAcidityValue > maxAlcoholicAcidityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSO2Value(textbox) {
            console.log("validateSO2Value function called");

            // Retrieve elements and values
            var sO2Value = parseFloat(document.getElementById('<%= TB_SO2.ClientID %>').value);
            var remarksDiv = document.getElementById("SO2RemarksDIV");
            var minSO2Value = parseFloat(document.getElementById('<%= hdnMinSO2Value.ClientID %>').value);
            var maxSO2Value = parseFloat(document.getElementById('<%= hdnMaxSO2Value.ClientID %>').value);


            // Define the valid range
            //var minSO2Value = 20.00;
            //var maxSO2Value = 30.00;

            // Check if SO2 value is within the valid range
            if (!isNaN(sO2Value) && sO2Value !== "" && sO2Value < minSO2Value || sO2Value > maxSO2Value) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateGlycerineContentValue(textbox) {
            console.log("validateGlycerineContentValue function called");

            // Retrieve elements and values
            var glycerineContentValue = parseFloat(document.getElementById('<%= TB_GlycerineContent.ClientID %>').value);
            var remarksDiv = document.getElementById("GlycerineContentRemarksDIV");
            var minGlycerineContentValue = parseFloat(document.getElementById('<%= hdnMinGlycerineContentValue.ClientID %>').value);
            var maxGlycerineContentValue = parseFloat(document.getElementById('<%= hdnMaxGlycerineContentValue.ClientID %>').value);

            // Define the valid range
            //var minGlycerineContentValue = 20.00;
            //var maxGlycerineContentValue = 30.00;

            // Check if GlycerineContent value is within the valid range
            if (!isNaN(glycerineContentValue) && glycerineContentValue !== "" && glycerineContentValue < minGlycerineContentValue || glycerineContentValue > maxGlycerineContentValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateGlucoseContentValue(textbox) {
            console.log("validateGlucoseContentValue function called");

            // Retrieve elements and values
            var glucoseContentValue = parseFloat(document.getElementById('<%= TB_GlucoseContent.ClientID %>').value);
            var remarksDiv = document.getElementById("GlucoseContentRemarksDIV");
            var minGlucoseContentValue = parseFloat(document.getElementById('<%= hdnMinGlucoseContentValue.ClientID %>').value);
            var maxGlucoseContentValue = parseFloat(document.getElementById('<%= hdnMaxGlucoseContentValue.ClientID %>').value);

            // Define the valid range
            //var minGlucoseContentValue = 20.00;
            //var maxGlucoseContentValue = 30.00;

            // Check if GlucoseContent value is within the valid range
            if (!isNaN(glucoseContentValue) && glucoseContentValue !== "" && glucoseContentValue < minGlucoseContentValue || glucoseContentValue > maxGlucoseContentValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateLossOnDryingValue(textbox) {
            console.log("validateLossOnDryingValue function called");

            // Retrieve elements and values
            var lossOnDryingValue = parseFloat(document.getElementById('<%= TB_LossOnDrying.ClientID %>').value);
            var remarksDiv = document.getElementById("LossOnDryingRemarksDIV");
            var minLossOnDryingValue = parseFloat(document.getElementById('<%= hdnMinLossonDryingValue.ClientID %>').value);
            var maxLossOnDryingValue = parseFloat(document.getElementById('<%= hdnMaxLossonDryingValue.ClientID %>').value);

            // Define the valid range
            //var minLossOnDryingValue = 20.00;
            //var maxLossOnDryingValue = 30.00;

            // Check if LossOnDrying value is within the valid range
            if (!isNaN(lossOnDryingValue) && lossOnDryingValue !== "" && lossOnDryingValue < minLossOnDryingValue || lossOnDryingValue > maxLossOnDryingValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSolubilityValue(textbox) {
            console.log("validateSolubilityValue function called");

            // Retrieve elements and values
            var solubilityValue = parseFloat(document.getElementById('<%= TB_Solubility.ClientID %>').value);
            var remarksDiv = document.getElementById("SolubilityRemarksDIV");
            var minSolubilityValue = parseFloat(document.getElementById('<%= hdnMinSolubilityValue.ClientID %>').value);
            var maxSolubilityValue = parseFloat(document.getElementById('<%= hdnMaxSolubilityValue.ClientID %>').value);

            // Define the valid range
            //var minSolubilityValue = 20.00;
            //var maxSolubilityValue = 30.00;

            // Check if Solubility value is within the valid range
            if (!isNaN(solubilityValue) && solubilityValue !== "" && solubilityValue < minSolubilityValue || solubilityValue > maxSolubilityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateShapeOrSizeValue(textbox) {
            console.log("validateShapeOrSizeValue function called");

            // Retrieve elements and values
            var shapeOrSizeValue = parseFloat(document.getElementById('<%= TB_ShapeOrSize.ClientID %>').value);
            var remarksDiv = document.getElementById("ShapeOrSizeRemarksDIV");
            var minShapeOrSizeValue = parseFloat(document.getElementById('<%= hdnMinShapeSizeValue.ClientID %>').value);
            var maxShapeOrSizeValue = parseFloat(document.getElementById('<%= hdnMaxShapeSizeValue.ClientID %>').value);

            // Define the valid range
            //var minShapeOrSizeValue = 20.00;
            //var maxShapeOrSizeValue = 30.00;

            // Check if Shape Or Size value is within the valid range
            if (!isNaN(shapeOrSizeValue) && shapeOrSizeValue !== "" && shapeOrSizeValue < minShapeOrSizeValue || shapeOrSizeValue > maxShapeOrSizeValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSucroseValue(textbox) {
            console.log("validateSucroseValue function called");

            // Retrieve elements and values
            var sucroseValue = parseFloat(document.getElementById('<%= TB_Sucrose.ClientID %>').value);
            var remarksDiv = document.getElementById("SucroseRemarksDIV");
            var minSucroseValue = parseFloat(document.getElementById('<%= hdnMinSucroseValue.ClientID %>').value);
            var maxSucroseValue = parseFloat(document.getElementById('<%= hdnMaxSucroseValue.ClientID %>').value);

            // Define the valid range
            //var minSucroseValue = 20.00;
            //var maxSucroseValue = 30.00;

            // Check if Sucrose value is within the valid range
            if (!isNaN(sucroseValue) && sucroseValue !== "" && sucroseValue < minSucroseValue || sucroseValue > maxSucroseValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleManufNameAddRemarksDiv(radioButtonList) {
            console.log("toggleManufNameAddRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ManufNameAddRemarksDiv");
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

        function toggleManufDatePkdRemarksDiv(radioButtonList) {
            console.log("toggleManufDatePkdRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ManufDatePkdRemarksDiv");
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

        function toggleBestb4dateRemarksDiv(radioButtonList) {
            console.log("toggleBestb4dateRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("Bestb4dateRemarksDiv");
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

        function toggleBatchLotNoRemarksDiv(radioButtonList) {
            console.log("toggleBatchLotNoRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BatchLotNoRemarksDiv");
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

        function toggleOdourAfterAcidificationRemarksDiv(radioButtonList) {
            console.log("toggleOdourAfterAcidificationRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("OdourAfterAcidificationRemarksDiv");
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

        function toggleColourRemarksDiv(radioButtonList) {
            console.log("toggleColourRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ColourRemarksDiv");
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


        function validateSulphideValue(textbox) {
            console.log("validateSulphideValue function called");

            // Retrieve elements and values
            var sulphideValue = parseFloat(document.getElementById('<%= TB_Sulphide.ClientID %>').value);
            var remarksDiv = document.getElementById("SulphideRemarksDIV");
            var minSulphideValue = parseFloat(document.getElementById('<%= hdnMinSulphideValue.ClientID %>').value);
            var maxSulphideValue = parseFloat(document.getElementById('<%= hdnMaxSulphideValue.ClientID %>').value);

            // Define the valid range
            //var minSulphideValue = 20.00;
            //var maxSulphideValue = 30.00;

            // Check if Sulphide value is within the valid range
            if (!isNaN(sulphideValue) && sulphideValue !== "" && sulphideValue < minSulphideValue || sulphideValue > maxSulphideValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSulphatedAshValue(textbox) {
            console.log("validateSulphatedAshValue function called");

            var sulphatedAshValue = parseFloat(document.getElementById('<%= TB_SulphatedAsh.ClientID %>').value);
            var remarksDiv = document.getElementById("SulphatedAshRemarksDIV");
            var minSulphatedAshValue = parseFloat(document.getElementById('<%= hdnMinSulphatedAshValue.ClientID %>').value);
            var maxSulphatedAshValue = parseFloat(document.getElementById('<%= hdnMaxSulphatedAshValue.ClientID %>').value);

            // Define the valid range
            //var minSulphatedAshValue = 20.00;
            //var maxSulphatedAshValue = 30.00;

            // Check if Sulphated Ash value is within the valid range
            if (!isNaN(sulphatedAshValue) && sulphatedAshValue !== "" && sulphatedAshValue < minSulphatedAshValue || sulphatedAshValue > maxSulphatedAshValue) {
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

        function toggleForeignMattersRemarksDiv(radioButtonList) {
            console.log("toggleForeignMattersRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ForeignMattersRemarksDiv");
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

    </script>

    <asp:HiddenField ID="hdn_formid" runat="server" />

    <asp:HiddenField ID="hdnMinSupplierValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSupplierValue" runat="server" />

    <asp:HiddenField ID="hdnMinBrandValue" runat="server" />
    <asp:HiddenField ID="hdnMaxBrandValue" runat="server" />

    <asp:HiddenField ID="hdnMinLicenseNoValue" runat="server" />
    <asp:HiddenField ID="hdnMaxLicenseNoValue" runat="server" />

    <asp:HiddenField ID="hdnMinMfgNameValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMfgNameValue" runat="server" />

    <asp:HiddenField ID="hdnMinQtyValue" runat="server" />
    <asp:HiddenField ID="hdnMaxQtyValue" runat="server" />

    <asp:HiddenField ID="hdnMinSizeValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSizeValue" runat="server" />

    <asp:HiddenField ID="hdnMinImpuritiesValue" runat="server" />
    <asp:HiddenField ID="hdnMaxImpuritiesValue" runat="server" />

    <asp:HiddenField ID="hdnMinPhValue" runat="server" />
    <asp:HiddenField ID="hdnMaxPhValue" runat="server" />

    <asp:HiddenField ID="hdnMinMoistureValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMoistureValue" runat="server" />

    <asp:HiddenField ID="hdnMinTotalAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTotalAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinInsolubleAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxInsolubleAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinTotalSolidValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTotalSolidValue" runat="server" />

    <asp:HiddenField ID="hdnMinDextroseValue" runat="server" />
    <asp:HiddenField ID="hdnMaxDextroseValue" runat="server" />

    <asp:HiddenField ID="hdnMinTitrableAcidityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTitrableAcidityValue" runat="server" />

    <asp:HiddenField ID="hdnMinAlcoholicAcidityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxAlcoholicAcidityValue" runat="server" />

    <asp:HiddenField ID="hdnMinSO2Value" runat="server" />
    <asp:HiddenField ID="hdnMaxSO2Value" runat="server" />

    <asp:HiddenField ID="hdnMinGlycerineContentValue" runat="server" />
    <asp:HiddenField ID="hdnMaxGlycerineContentValue" runat="server" />

    <asp:HiddenField ID="hdnMinGlucoseContentValue" runat="server" />
    <asp:HiddenField ID="hdnMaxGlucoseContentValue" runat="server" />

    <asp:HiddenField ID="hdnMinLossonDryingValue" runat="server" />
    <asp:HiddenField ID="hdnMaxLossonDryingValue" runat="server" />

    <asp:HiddenField ID="hdnMinSolubilityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSolubilityValue" runat="server" />

    <asp:HiddenField ID="hdnMinSucroseValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSucroseValue" runat="server" />

    <asp:HiddenField ID="hdnMinSulphideValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSulphideValue" runat="server" />

    <asp:HiddenField ID="hdnMinShapeSizeValue" runat="server" />
    <asp:HiddenField ID="hdnMaxShapeSizeValue" runat="server" />

    <asp:HiddenField ID="hdnMinSulphatedAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSulphatedAshValue" runat="server" />

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
                                <h4 class="text-left text-info">Step-1 : RM Selection</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="MaterialDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_Material" runat="server" AssociatedControlID="DDL_Material" Text="Material Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Save" ControlToValidate="DDL_Material" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" AutoPostBack="true" OnSelectedIndexChanged="DDL_Material_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PlantDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Save" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-2 : RM Basic Details</h4>
                                <hr />
                            </div>
                            <div class="col-md-3" id="ReceivingDate" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="lbl_TB_ReceivingDate" runat="server" AssociatedControlID="TB_ReceivingDate" Text=" Receiving Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ReceivingDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_ReceivingDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ReceivingDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="QuantityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Quantity" runat="server" AssociatedControlID="TB_Quantity" Text="Quantity Supplied :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" ValidationGroup="Save" runat="server" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Quantity" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [1000.00-2000.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Quantity Value(in kg)" oninput="validateQuantityValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="SizeDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Size" runat="server" AssociatedControlID="TB_Size" Text="Sample Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Size" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_Size" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Size" runat="server" ValidationGroup="Save" ControlToValidate="TB_Size" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Size" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [1.00-10.00]" Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Size(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SupplierDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_Supplier" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Supplier" runat="server" ControlToValidate="TB_Supplier" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Text="" Placeholder="Supplier (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BrandDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BrandName" runat="server" AssociatedControlID="TB_BrandName" Text="Brand Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BrandName" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_BrandName" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BrandName" ValidationGroup="Save" runat="server" ControlToValidate="TB_BrandName" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BrandName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Supplier Brand Name"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BatchNo" runat="server" AssociatedControlID="TB_BatchNo" Text="Mfg. Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BatchNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_BatchNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BatchNo" runat="server" ControlToValidate="TB_BatchNo" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BatchNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Batch No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ChallanNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_ChallanNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" Text="" ValidationGroup="Save" Placeholder="Challan No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="ChallanDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" ValidationGroup="Save"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="ChallanDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanDate" runat="server"
                                        ControlToValidate="TB_ChallanDate" ValidationGroup="Save" ForeColor="Red"
                                        ErrorMessage="Enter valid dates in DD-MM-YYYY format, separated by commas"
                                        ValidationExpression="^(\d{2}-\d{2}-\d{4})(,\s*\d{2}-\d{2}-\d{4})*$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LotNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_LotNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Lot/Gate No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="MfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mfg" runat="server" AssociatedControlID="TB_Mfg" Text="Pkd/Mfg Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Mfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_Mfg" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" ValidationGroup="Save"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="MfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mfg" runat="server" AssociatedControlID="TB_Mfg" Text=" Pkd/Mfg. Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Mfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_Mfg" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Mfg" runat="server"
                                        ControlToValidate="TB_Mfg" ValidationGroup="Save" ForeColor="Red"
                                        ErrorMessage="Enter valid dates in DD-MM-YYYY format, separated by commas"
                                        ValidationExpression="^(\d{2}-\d{2}-\d{4})(,\s*\d{2}-\d{2}-\d{4})*$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%-- <div class="col-md-3" id="BeforeDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BeforeDate" runat="server" AssociatedControlID="TB_BeforeDate" Text=" Best Before Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BeforeDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_BeforeDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BeforeDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" TextMode="Date" oninput="validateBestBeforeDate()"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="BeforeDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BeforeDate" runat="server" AssociatedControlID="TB_BeforeDate" Text="Best Before Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BeforeDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Save" ControlToValidate="TB_BeforeDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BeforeDate" runat="server"
                                        ControlToValidate="TB_BeforeDate" ValidationGroup="Save" ForeColor="Red"
                                        ErrorMessage="Enter valid dates in DD-MM-YYYY format, separated by commas"
                                        ValidationExpression="^(\d{2}-\d{2}-\d{4})(,\s*\d{2}-\d{2}-\d{4})*$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BeforeDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="VehicleNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_VehicleNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Vehicle No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-3[A] : Optional Inputs</h4>
                                <hr />
                            </div>

                            <div class="col-lg-9 col-sm-12 col-md-6" id="MfgNameDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_MfgName" runat="server" AssociatedControlID="TB_MfgName" Text="Manufacturer Name and Address :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_MfgName" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_MfgName" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_MfgName" runat="server" ControlToValidate="TB_MfgName" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Aphanumeric Only" ValidationExpression="^[A-Za-z0-9\s,.\-\/#]{3,100}$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MfgName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" TextMode="MultiLine" Placeholder="Name and address (3-100 characters)" Rows="1" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-lg-3 col-sm-12 col-md-6" id="FssaiNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_FssaiNo" runat="server" AssociatedControlID="TB_FssaiNo" Text="FSSAI License No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_FssaiNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Save" ControlToValidate="TB_FssaiNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_FssaiNo" runat="server" ControlToValidate="TB_FssaiNo" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Number Only 14 digit" Maxlength="14" ValidationExpression="^[1-9]\d*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FssaiNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="FSAAI License No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-3[B[ : Parameters with Standards (Yes / No)</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="ManufNameAddDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ManufNameAdd" runat="server" ValidationGroup="Save" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_ManufNameAdd" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_ManufNameAdd" runat="server" AssociatedControlID="RBL_ManufNameAdd" Text="Manufacturer Name/Address :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ManufNameAdd" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleManufNameAddRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ManufNameAddRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_ManufNameAdd_Remarks" runat="server" AssociatedControlID="TXB_ManufNameAdd_Remarks" Text="Manufacturer Name/Address (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_ManufNameAdd_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ManufNameAdd_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ManufNameAdd_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ManufDatePkdDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ManufDatePkd" runat="server" ValidationGroup="Save" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_ManufDatePkd" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_ManufDatePkd" runat="server" AssociatedControlID="RBL_ManufDatePkd" Text="Manufacturing Date/Packaging Date :(Clearly visible)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ManufDatePkd" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleManufDatePkdRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ManufDatePkdRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_ManufDatePkd_Remarks" runat="server" AssociatedControlID="TXB_ManufDatePkd_Remarks" Text="Manufacturing Date/Packaging Date (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_ManufDatePkd_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ManufDatePkd_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ManufDatePkd_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="Bestb4dateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Bestb4date" runat="server" ValidationGroup="Save"
                                        ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Bestb4date" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <asp:Label ID="Lbl_Bestb4date" runat="server" AssociatedControlID="RBL_Bestb4date"
                                        Text="Best Before Date :(Two years from manufacturing date & In clear readable form)" ForeColor="Blue"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Bestb4date" runat="server" CssClass="form-control form-control-sm rounded remove-border"
                                            RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3"
                                            Width="100%" onchange="toggleBestb4dateRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="Bestb4dateRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Bestb4date_Remarks" runat="server" AssociatedControlID="TXB_Bestb4date_Remarks"
                                        Text="Best Before Date (Not ok)" ForeColor="Red"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <asp:RequiredFieldValidator ID="RFV_TXB_Bestb4date_Remarks" runat="server" ErrorMessage="*"
                                        ForeColor="Red" ControlToValidate="TXB_Bestb4date_Remarks" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Bestb4date_Remarks" runat="server"
                                            CssClass="form-control form-control-sm rounded">
                                        </asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchLotNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_BatchLotNo" runat="server" ValidationGroup="Save"
                                        ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_BatchLotNo" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <asp:Label ID="Label_BatchLotNo" runat="server" AssociatedControlID="RBL_BatchLotNo"
                                        Text="Batch/Lot Number :(In clear readable form & must match with the material)" ForeColor="Blue"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_BatchLotNo" runat="server" CssClass="form-control form-control-sm rounded remove-border"
                                            RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3"
                                            Width="100%" onchange="toggleBatchLotNoRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchLotNoRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_BatchLotNo_Remarks" runat="server" AssociatedControlID="TXB_BatchLotNo_Remarks"
                                        Text="Batch/Lot Number (Not ok)" ForeColor="Red"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <asp:RequiredFieldValidator ID="RFV_TXB_BatchLotNo_Remarks" runat="server" ErrorMessage="*"
                                        ForeColor="Red" ControlToValidate="TXB_BatchLotNo_Remarks" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_BatchLotNo_Remarks" runat="server"
                                            CssClass="form-control form-control-sm rounded">
                                        </asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FssaiLogoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Fssai_Logo" runat="server" AssociatedControlID="RBL_Fssai_Logo" Text=" FSSAI License No. & Logo :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Fssai_Logo" runat="server" ErrorMessage="*" ValidationGroup="Save" ForeColor="Red" ControlToValidate="RBL_Fssai_Logo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Fssai_Logo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                            <asp:ListItem Text="Present" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Absent" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="VegLogoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Veg_Logo" runat="server" AssociatedControlID="RBL_Veg_Logo" Text=" Veg Logo :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Veg_Logo" runat="server" ErrorMessage="*" ValidationGroup="Save" ForeColor="Red" ControlToValidate="RBL_Fssai_Logo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Veg_Logo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                            <asp:ListItem Text="Present" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Absent" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SmellDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Smell" runat="server" AssociatedControlID="RBL_Smell" Text="Presence of Off-Odour/smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Smell" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Save" ControlToValidate="RBL_Smell" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Smell" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSmellRemarksDiv(this);">
                                            <asp:ListItem Text="Absent" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Present" Value="0"></asp:ListItem>
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

                            <div class="col-md-3" id="OdourAfterAcidificationDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_OdourAfterAcidification" runat="server" ValidationGroup="Save"
                                        ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_OdourAfterAcidification" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <asp:Label ID="Label_OdourAfterAcidification" runat="server" AssociatedControlID="RBL_OdourAfterAcidification"
                                        Text="Odour After Acidification :(Clearly noticeable)" ForeColor="Blue"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_OdourAfterAcidification" runat="server" CssClass="form-control form-control-sm rounded remove-border"
                                            RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3"
                                            Width="100%" onchange="toggleOdourAfterAcidificationRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="OdourAfterAcidificationRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_OdourAfterAcidification_Remarks" runat="server" AssociatedControlID="TXB_OdourAfterAcidification_Remarks"
                                        Text="Odour After Acidification (Not ok)" ForeColor="Red"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <asp:RequiredFieldValidator ID="RFV_TXB_OdourAfterAcidification_Remarks" runat="server" ErrorMessage="*"
                                        ForeColor="Red" ControlToValidate="TXB_OdourAfterAcidification_Remarks" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_OdourAfterAcidification_Remarks" runat="server"
                                            CssClass="form-control form-control-sm rounded">
                                        </asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PackingConditionDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Packing_Condition" runat="server" AssociatedControlID="RBL_Packing_Condition" Text="Packing Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Packing_Condition" runat="server" ValidationGroup="Save" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Packing_Condition" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Packing_Condition" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="togglePackingConditionRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PackingConditionRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_PackingCondition_Remarks" runat="server" AssociatedControlID="TXB_PackingCondition_Remarks" Text="Packing Condition (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_PackingCondition_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_PackingCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_PackingCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppearanceDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Appearance" runat="server" AssociatedControlID="RBL_Appearance" Text="Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Appearance" runat="server" ErrorMessage="*" ValidationGroup="Save" ForeColor="Red" ControlToValidate="RBL_Appearance" Display="Dynamic"></asp:RequiredFieldValidator>
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

                            <div class="col-md-3" id="ColourDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Colour" runat="server" ValidationGroup="Save"
                                        ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Colour" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <asp:Label ID="Label_Colour" runat="server" AssociatedControlID="RBL_Colour"
                                        Text="Colour :(White to Slight Dull White. No added colour.)" ForeColor="Blue"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Colour" runat="server" CssClass="form-control form-control-sm rounded remove-border"
                                            RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3"
                                            Width="100%" onchange="toggleColourRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ColourRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Colour_Remarks" runat="server" AssociatedControlID="TXB_Colour_Remarks"
                                        Text="Colour (Not ok)" ForeColor="Red"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <asp:RequiredFieldValidator ID="RFV_TXB_Colour_Remarks" runat="server" ErrorMessage="*"
                                        ForeColor="Red" ControlToValidate="TXB_Colour_Remarks" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Colour_Remarks" runat="server"
                                            CssClass="form-control form-control-sm rounded">
                                        </asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="ColorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColor" runat="server" AssociatedControlID="DDL_Color" Text=" Color :(White to Slight Dull White. No added colour.)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Color" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Save" ControlToValidate="DDL_Color" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Color" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" AutoPostBack="true" OnSelectedIndexChanged="DDL_Color_SelectedIndexChanged"></asp:DropDownList>
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
                            </div>--%>

                            <div class="col-md-3" id="TasteFlavorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTasteFlavor" runat="server" AssociatedControlID="RBL_TasteFlavor" Text="Taste/Flavor :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_TasteFlavor" runat="server" ErrorMessage="*" ValidationGroup="Save" ForeColor="Red" ControlToValidate="RBL_TasteFlavor" Display="Dynamic"></asp:RequiredFieldValidator>
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

                            <div class="col-md-3" id="GradeDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_Grade" runat="server" AssociatedControlID="RBL_Grade" Text="Food Grade :(Yes / No)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Grade" runat="server" ValidationGroup="Save" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Grade" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Grade" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleGradeRemarksDiv(this);">
                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GradeRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Grade_Remarks" runat="server" AssociatedControlID="TXB_Grade_Remarks" Text="Grade Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Grade_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Grade_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Grade_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-4: Parameters with Standards (Lab Results)</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="PHDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_PH" runat="server" AssociatedControlID="TB_PH" Text=" PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PH" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_PH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PH" runat="server" ControlToValidate="TB_PH" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_PH" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PH" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="PH Value" oninput="validatePhValue(this);"></asp:TextBox>
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
                                    <asp:RequiredFieldValidator ID="RFV_TB_Moisture" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_Moisture" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Moisture" runat="server" ControlToValidate="TB_Moisture" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Moisture" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Moisture" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Moisture Value" oninput="validateMoistureValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="AshDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TotalAsh" runat="server" AssociatedControlID="TB_TotalAsh" Text="Total Ash" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TotalAsh" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_TotalAsh" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TotalAsh" runat="server" ControlToValidate="TB_TotalAsh" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TotalAsh" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TotalAsh" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Total Ash Value " oninput="validateAshValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AshRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Ash_Remarks" runat="server" AssociatedControlID="TXB_Ash_Remarks" Text="Total Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Ash_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Ash_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Ash_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="InsolubleAshDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_InsolubleAsh" runat="server" AssociatedControlID="TB_InsolubleAsh" Text="Acid Insoluble Ash:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_InsolubleAsh" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_InsolubleAsh" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_InsolubleAsh" runat="server" ControlToValidate="TB_InsolubleAsh" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_InsolubleAsh" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_InsolubleAsh" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Insoluble Ash Value " oninput="validateInsolubleAshValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="InsolubleAshRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_InsolubleAsh_Remarks" runat="server" AssociatedControlID="TXB_InsolubleAsh_Remarks" Text="Acid Insoluble Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_InsolubleAsh_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_InsolubleAsh_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_InsolubleAsh_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SolidDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TotalSolid" runat="server" AssociatedControlID="TB_TotalSolid" Text="Total Solids:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TotalSolid" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_TotalSolid" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TotalSolid" runat="server" ControlToValidate="TB_TotalSolid" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TotalSolid" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TotalSolid" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Total Solids Value " oninput="validateSolidValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SolidRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Solid_Remarks" runat="server" AssociatedControlID="TXB_Solid_Remarks" Text="Total Solids Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Solid_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Solid_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Solid_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DextroseDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Dextrose" runat="server" AssociatedControlID="TB_Dextrose" Text="Dextrose Equivalent:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Dextrose" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_Dextrose" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Dextrose" runat="server" ControlToValidate="TB_Dextrose" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Dextrose" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Dextrose" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Dextrose Equivalent Value " oninput="validateDextroseValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DextroseRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Dextrose_Remarks" runat="server" AssociatedControlID="TXB_Dextrose_Remarks" Text="Dextrose Equivalent Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Dextrose_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Dextrose_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Dextrose_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TitrableAcidityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TitrableAcidity" runat="server" AssociatedControlID="TB_TitrableAcidity" Text=" Titrable Acidity :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TitrableAcidity" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_TitrableAcidity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TitrableAcidity" runat="server" ControlToValidate="TB_TitrableAcidity" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TitrableAcidity" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TitrableAcidity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Titrable Acidity Value " oninput="validateTitrableAcidityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TitrableAcidityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_TitrableAcidity_Remarks" runat="server" AssociatedControlID="TXB_TitrableAcidity_Remarks" Text="Titrable Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_TitrableAcidity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TitrableAcidity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_TitrableAcidity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AlcoholicAcidityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_AlcoholicAcidity" runat="server" AssociatedControlID="TB_TitrableAcidity" Text=" Alcoholic Acidity :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_AlcoholicAcidity" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_AlcoholicAcidity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_AlcoholicAcidity" runat="server" ControlToValidate="TB_AlcoholicAcidity" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_AlcoholicAcidity" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_AlcoholicAcidity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Alcoholic Acidity Value " oninput="validateAlcoholicAcidityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AlcoholicAcidityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_AlcoholicAcidity" runat="server" AssociatedControlID="TXB_AlcoholicAcidity" Text="Alcoholic Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_AlcoholicAcidity" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_AlcoholicAcidity" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_AlcoholicAcidity" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SO2DIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_SO2" runat="server" AssociatedControlID="TB_SO2" Text="SO2:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_SO2" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_SO2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_SO2" runat="server" ValidationGroup="Save" ControlToValidate="TB_SO2" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_SO2" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_SO2" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="So2 Value " oninput="validateSO2Value(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SO2RemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_SO2_Remarks" runat="server" AssociatedControlID="TXB_SO2_Remarks" Text="SO2 Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_SO2_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SO2_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_SO2_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlycerineContentDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GlycerineContent" runat="server" AssociatedControlID="TB_GlycerineContent" Text="Glycerine Content  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GlycerineContent" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_GlycerineContent" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GlycerineContent" runat="server" ValidationGroup="Save" ControlToValidate="TB_GlycerineContent" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_GlycerineContent" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GlycerineContent" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Glycerine Content(in mm)" oninput="validateGlycerineContentValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlycerineContentRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_GlycerineContent_Remarks" runat="server" AssociatedControlID="TXB_GlycerineContent_Remarks" Text="Glycerine Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GlycerineContent_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GlycerineContent_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GlycerineContent_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlucoseContentDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GlucoseContent" runat="server" AssociatedControlID="TB_GlucoseContent" Text="Glucose Content  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GlucoseContent" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_GlucoseContent" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GlucoseContent" runat="server" ValidationGroup="Save" ControlToValidate="TB_GlucoseContent" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_GlucoseContent" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GlucoseContent" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Glucose Content(in mm)" oninput="validateGlucoseContentValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlucoseContentRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_GlucoseContent_Remarks" runat="server" AssociatedControlID="TXB_GlucoseContent_Remarks" Text="Glucose Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GlucoseContent_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GlucoseContent_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GlucoseContent_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LossOnDryingDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LossOnDrying" runat="server" AssociatedControlID="TB_LossOnDrying" Text="Loss On Drying:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LossOnDrying" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_LossOnDrying" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LossOnDrying" runat="server" ControlToValidate="TB_LossOnDrying" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_LossOnDrying" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LossOnDrying" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Loss On Drying Value " oninput="validateLossOnDryingValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LossOnDryingRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_LossOnDrying_Remarks" runat="server" AssociatedControlID="TXB_LossOnDrying_Remarks" Text="Loss On Drying Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_LossOnDrying_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_LossOnDrying_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_LossOnDrying_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SolubilityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Solubility" runat="server" AssociatedControlID="TB_Solubility" Text="Solubility:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Solubility" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_Solubility" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Solubility" runat="server" ControlToValidate="TB_Solubility" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Solubility" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Solubility" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Solubility Value " oninput="validateSolubilityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SolubilityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Solubility_Remarks" runat="server" AssociatedControlID="TXB_Solubility_Remarks" Text="Solubility Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Solubility_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Solubility_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Solubility_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ShapeOrSizeDIV" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ShapeOrSize" runat="server" AssociatedControlID="TB_ShapeOrSize" Text="ShapeOrSize  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ShapeOrSize" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_ShapeOrSize" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ShapeOrSize" runat="server" ValidationGroup="Save" ControlToValidate="TB_ShapeOrSize" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_ShapeOrSize" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [2.00-3.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ShapeOrSize" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="ShapeOrSize(in mm)" oninput="validateShapeOrSizeValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ShapeOrSizeRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_ShapeOrSize_Remarks" runat="server" AssociatedControlID="TXB_ShapeOrSize_Remarks" Text="ShapeOrSize Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_ShapeOrSize_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ShapeOrSize_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ShapeOrSize_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SucroseDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Sucrose" runat="server" AssociatedControlID="TB_Sucrose" Text="Sucrose Content :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Sucrose" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_Sucrose" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Sucrose" runat="server" ValidationGroup="Save" ControlToValidate="TB_Sucrose" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Sucrose" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Sucrose" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Sucrose Content (%) " oninput="validateSucroseValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SucroseRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Sucrose" runat="server" AssociatedControlID="TXB_Sucrose" Text="Sucrose Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Sucrose" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Sucrose" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Sucrose" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SulphideDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Sulphide" runat="server" AssociatedControlID="TB_Sulphide" Text="Sulphide Content :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Sulphide" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_Sulphide" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Sulphide" runat="server" ValidationGroup="Save" ControlToValidate="TB_Sulphide" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Sulphide" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Sulphide" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Sucrose Content (%) " oninput="validateSulphideValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SulphideRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Sulphide" runat="server" AssociatedControlID="TXB_Sulphide" Text="Sulphide Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Sulphide" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Sulphide" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Sulphide" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SulphatedAshDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_SulphatedAsh" runat="server" AssociatedControlID="TB_SulphatedAsh" Text="Sulphated Ash" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_SulphatedAsh" runat="server" ErrorMessage="*" ValidationGroup="Save" ControlToValidate="TB_SulphatedAsh" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_SulphatedAsh" runat="server" ControlToValidate="TB_SulphatedAsh" ValidationGroup="Save" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_SulphatedAsh" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_SulphatedAsh" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="Sulphated Ash Value " oninput="validateSulphatedAshValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SulphatedAshRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_SulphatedAsh_Remarks" runat="server" AssociatedControlID="TXB_SulphatedAsh_Remarks" Text="Sulphated Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_SulphatedAsh_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SulphatedAsh_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_SulphatedAsh_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="WIMDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_WIM" runat="server" AssociatedControlID="TB_WIM" Text="Water Insoluble Matter (%) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_WIM" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_WIM" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_WIM" runat="server" ValidationGroup="Save" ControlToValidate="TB_WIM" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_WIM" runat="server" ValidationGroup="Save" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
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

                            <div class="col-md-3" id="BeverageDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_Beverage" runat="server" AssociatedControlID="RBL_Beverage" Text="Beverage Floc :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Beverage" runat="server" ErrorMessage="*" ValidationGroup="Save" ForeColor="Red" ControlToValidate="RBL_Beverage" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Beverage" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%">
                                            <asp:ListItem Text="Positive" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Negative" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ForeignMattersDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ForeignMatters" runat="server" ValidationGroup="Save"
                                        ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_ForeignMatters" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <asp:Label ID="Label_ForeignMatters" runat="server" AssociatedControlID="RBL_ForeignMatters"
                                        Text="Foreign Matters :(Absent)" ForeColor="Blue"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ForeignMatters" runat="server" CssClass="form-control form-control-sm rounded remove-border"
                                            RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3"
                                            Width="100%" onchange="toggleForeignMattersRemarksDiv(this);">
                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ForeignMattersRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_ForeignMatters_Remarks" runat="server" AssociatedControlID="TXB_ForeignMatters_Remarks"
                                        Text="Foreign Matters (Not ok)" ForeColor="Red"
                                        Font-Bold="true" Font-Size="Small">
                                    </asp:Label>

                                    <asp:RequiredFieldValidator ID="RFV_TXB_ForeignMatters_Remarks" runat="server" ErrorMessage="*"
                                        ForeColor="Red" ControlToValidate="TXB_ForeignMatters_Remarks" Display="Dynamic">
                                    </asp:RequiredFieldValidator>

                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ForeignMatters_Remarks" runat="server"
                                            CssClass="form-control form-control-sm rounded">
                                        </asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ImpuritiesDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Foreign_Impurities" runat="server" AssociatedControlID="TB_Foreign_Impurities" Text="ForeignMatter/Impurities :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Foreign_Impurities" runat="server" ValidationGroup="Save" ErrorMessage="Input Required" ControlToValidate="TB_Foreign_Impurities" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Foreign_Impurities" runat="server" ValidationGroup="Save" ControlToValidate="TB_Foreign_Impurities" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Foreign_Impurities" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Save" Placeholder="ForeignMatter/Impurities (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-5: Decision Making</h4>
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
                                <h4 class="text-left text-info">Step-6: RM Photograph</h4>
                                <hr />
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

                            <%--Button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSave" runat="server" AssociatedControlID="BtnSave" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSave" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" CausesValidation="true" ValidationGroup="Save" OnClick="BtnSave_Click" />
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
