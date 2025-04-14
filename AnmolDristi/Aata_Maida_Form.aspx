<%@ Page Title="AIL | QC RM - Atta/ Maida" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Aata_Maida_Form.aspx.cs" Inherits="AnmolDristi.Aata_Maida_Form" %>

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

        <%--function validateBestBeforeDate() {
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
            if (differenceInDays < 0 || differenceInDays > 90) {
                alert("Best Before Date must be within 90 days from the Manufacturing Date.");
                bestBeforeDateInput.value = ""; // Reset invalid best before date
            }

            //if (differenceInDays <= 30) {
            //    alert("Best Before Date must be at least 30 days after the Manufacturing Date.");
            //    bestBeforeDateInput.value = ""; // Reset invalid best before date
            //}
        }--%>

        function validateBestBeforeDate() {
            const mfgDateInput = document.getElementById('<%= TB_Mfg.ClientID %>');
            const bestBeforeDateInput = document.getElementById('<%= TB_BeforeDate.ClientID %>');

            if (!mfgDateInput.value || !bestBeforeDateInput.value) return;

            function parseDateDDMMYYYY(dateStr) {
                const parts = dateStr.trim().split("-");
                if (parts.length !== 3) return null;

                // Auto-detect YYYY-MM-DD (browser date input)
                if (parseInt(parts[0], 10) > 31) {
                    const year = parseInt(parts[0], 10);
                    const month = parseInt(parts[1], 10) - 1;
                    const day = parseInt(parts[2], 10);
                    return new Date(year, month, day);
                }

                // DD-MM-YYYY format
                const day = parseInt(parts[0], 10);
                const month = parseInt(parts[1], 10) - 1;
                const year = parseInt(parts[2], 10);
                return new Date(year, month, day);
            }

            const mfgValues = mfgDateInput.value.split(",").map(d => d.trim());
            const beforeValues = bestBeforeDateInput.value.split(",").map(d => d.trim());

            console.log("Raw Mfg Input:", mfgDateInput.value);
            console.log("Raw Best Before Input:", bestBeforeDateInput.value);
            console.log("Parsed Mfg Values:", mfgValues);
            console.log("Parsed Best Before Values:", beforeValues);

            const mfgDates = mfgValues.map(parseDateDDMMYYYY).filter(d => d);
            const bestBeforeDates = beforeValues.map(parseDateDDMMYYYY).filter(d => d);

            console.log("Parsed Mfg Dates (Date Objects):", mfgDates);
            console.log("Parsed Best Before Dates (Date Objects):", bestBeforeDates);

            if (mfgDates.length === 0 || bestBeforeDates.length === 0) {
                alert("Invalid date format. Please ensure all dates are in DD-MM-YYYY format.");
                bestBeforeDateInput.value = "";
                return;
            }

            if (mfgDates.length === 1 && bestBeforeDates.length > 1) {
                const mfgDate = mfgDates[0];
                for (let i = 0; i < bestBeforeDates.length; i++) {
                    const bbDate = bestBeforeDates[i];
                    const diff = (bbDate - mfgDate) / (1000 * 60 * 60 * 24);
                    console.log(`Checking BB[${i}]: ${beforeValues[i]} vs Mfg: ${mfgValues[0]} → Diff: ${diff}`);
                    if (diff < 0 || diff > 90 || isNaN(diff)) {
                        alert(`Invalid: Best Before Date (${beforeValues[i]}) must be within 90 days of Mfg. Date (${mfgValues[0]}).`);
                        bestBeforeDateInput.value = "";
                        return;
                    }
                }
            } else if (bestBeforeDates.length === 1 && mfgDates.length > 1) {
                const bbDate = bestBeforeDates[0];
                for (let i = 0; i < mfgDates.length; i++) {
                    const mfgDate = mfgDates[i];
                    const diff = (bbDate - mfgDate) / (1000 * 60 * 60 * 24);
                    console.log(`Checking Mfg[${i}]: ${mfgValues[i]} vs BB: ${beforeValues[0]} → Diff: ${diff}`);
                    if (diff < 0 || diff > 90 || isNaN(diff)) {
                        alert(`Invalid: Best Before Date (${beforeValues[0]}) must be within 90 days of Mfg. Date (${mfgValues[i]}).`);
                        bestBeforeDateInput.value = "";
                        return;
                    }
                }
            } else if (mfgDates.length === bestBeforeDates.length) {
                for (let i = 0; i < mfgDates.length; i++) {
                    const diff = (bestBeforeDates[i] - mfgDates[i]) / (1000 * 60 * 60 * 24);
                    console.log(`Checking Pair ${i}: Mfg ${mfgValues[i]} → BB ${beforeValues[i]} → Diff: ${diff}`);
                    if (diff < 0 || diff > 90 || isNaN(diff)) {
                        alert(`Invalid: Best Before Date (${beforeValues[i]}) must be within 90 days of Mfg. Date (${mfgValues[i]}).`);
                        bestBeforeDateInput.value = "";
                        return;
                    }
                }
            } else {
                console.warn("Mismatch in number of dates:", mfgDates.length, bestBeforeDates.length);
                alert("Mismatch in number of Manufacturing and Best Before Dates.");
                bestBeforeDateInput.value = "";
                return;
            }

            console.log("✅ All dates validated successfully.");
        }




        function toggleOdourRemarksDiv(radioButtonList) {
            console.log("toggleOdourRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("OdourRemarksDiv");
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

        function toggleColorAppRemarksDiv(radioButtonList) {
            console.log("toggleColorAppRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ColorAppRemarksDiv");
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

        function toggleFassaiNoLogoRemarksDiv(radioButtonList) {
            console.log("toggleFassaiNoLogoRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("FassaiNoLogoRemarksDiv");
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

        function toggleBBDateYesNoRemarksDiv(radioButtonList) {
            console.log("toggleBBDateYesNoRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BBDateYesNoRemarksDiv");
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

        function togglePackingConditionRemarksDiv(radioButtonList) {
            console.log("togglePackingConditionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("PackingConditionRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
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

        function toggleImpuritiesRemarksDiv(radioButtonList) {
            console.log("toggleImpuritiesRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ImpuritiesRemarksDiv");
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

        function validateGlutentContentValue(textbox) {
            console.log("validateGlutentContentValue function called");

            // Retrieve elements and values
            var glutentContentValue = parseFloat(document.getElementById('<%= TB_GlutentContent.ClientID %>').value);
            var remarksDiv = document.getElementById("GlutentContentRemarksDIV");
            var minGlutentContentValue = parseFloat(document.getElementById('<%= hdnMinGlutentContentValue.ClientID %>').value);
            var maxGlutentContentValue = parseFloat(document.getElementById('<%= hdnMaxGlutentContentValue.ClientID %>').value);

            // Define the valid range
            //var minGlutentContentValue = 20.00;
            //var maxGlutentContentValue = 30.00;

            // Check if GlutentContent value is within the valid range
            if (!isNaN(glutentContentValue) && glutentContentValue !== "" && glutentContentValue < minGlutentContentValue || glutentContentValue > maxGlutentContentValue) {
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

        function validateAbsorptionValue(textbox) {
            console.log("validateAbsorptionValue function called");

            // Retrieve elements and values
            var absorptionValue = parseFloat(document.getElementById('<%= TB_Absorption.ClientID %>').value);
            var remarksDiv = document.getElementById("AbsorptionRemarksDIV");
            var minAbsorptionValue = parseFloat(document.getElementById('<%= hdnMinAbsorptionValue.ClientID %>').value);
            var maxAbsorptionValue = parseFloat(document.getElementById('<%= hdnMaxAbsorptionValue.ClientID %>').value);

            // Define the valid range
            //var minAbsorptionValue = 20.00;
            //var maxAbsorptionValue = 30.00;

            // Check if Absorption value is within the valid range
            if (!isNaN(absorptionValue) && absorptionValue !== "" && absorptionValue < minAbsorptionValue || absorptionValue > maxAbsorptionValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSedimentationValue(textbox) {
            console.log("validateSedimentationValue function called");

            // Retrieve elements and values
            var sedimentationValue = parseFloat(document.getElementById('<%= TB_Sedimentation.ClientID %>').value);
            var remarksDiv = document.getElementById("SedimentationRemarksDIV");
            var minSedimentationValue = parseFloat(document.getElementById('<%= hdnMinSedimentationValue.ClientID %>').value);
            var maxSedimentationValue = parseFloat(document.getElementById('<%= hdnMaxSedimentationValue.ClientID %>').value);

            // Define the valid range
            //var minSedimentationValue = 20.00;
            //var maxSedimentationValue = 30.00;

            // Check if Sedimentation value is within the valid range
            if (!isNaN(sedimentationValue) && sedimentationValue !== "" && sedimentationValue < minSedimentationValue || sedimentationValue > maxSedimentationValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleGrittinessRemarksDiv(radioButtonList) {
            console.log("toggleGrittinessRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("GrittinessRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "1") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "Not Ok". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateAcidityValue(textbox) {
            console.log("validateAcidityValue function called");

            // Retrieve elements and values
            var AcidityValue = parseFloat(document.getElementById('<%= TB_Acidity.ClientID %>').value);
            var remarksDiv = document.getElementById("AcidityRemarksDIV");
            var minAcidityValue = parseFloat(document.getElementById('<%= hdnMinAcidityValue.ClientID %>').value);
            var maxAcidityValue = parseFloat(document.getElementById('<%= hdnMaxAcidityValue.ClientID %>').value);

            // Define the valid range
            //var minAcidityValue = 20.00;
            //var maxAcidityValue = 30.00;

            // Check if Acidity value is within the valid range
            if (!isNaN(AcidityValue) && AcidityValue !== "" && AcidityValue < minAcidityValue || AcidityValue > AcidityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateGranularityValue(textbox) {
            console.log("validateGranularityValue function called");

            // Retrieve elements and values
            var GranularityValue = parseFloat(document.getElementById('<%= TB_Granularity.ClientID %>').value);
            var remarksDiv = document.getElementById("GranularityRemarksDIV");
            var minGranularityValue = parseFloat(document.getElementById('<%= hdnMinGranularityValue.ClientID %>').value);
            var maxGranularityValue = parseFloat(document.getElementById('<%= hdnMaxGranularityValue.ClientID %>').value);

            // Define the valid range
            //var minGranularityValue = 20.00;
            //var maxGranularityValue = 30.00;

            // Check if Granularity value is within the valid range
            if (!isNaN(GranularityValue) && GranularityValue !== "" && GranularityValue < minGranularityValue || GranularityValue > GranularityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateGranularityRetentionValue(textbox) {
            console.log("validateGranularityRetentionValue function called");

            // Retrieve elements and values
            var GranularityRetentionValue = parseFloat(document.getElementById('<%= TB_GranularityRetention.ClientID %>').value);
            var remarksDiv = document.getElementById("GranularityRetentionRemarksDIV");
            var minGranularityRetentionValue = parseFloat(document.getElementById('<%= hdnMinGranularityRetentionValue.ClientID %>').value);
            var maxGranularityRetentionValue = parseFloat(document.getElementById('<%= hdnMaxGranularityRetentionValue.ClientID %>').value);

            // Define the valid range
            //var minGranularityRetentionValue = 20.00;
            //var maxGranularityRetentionValue = 30.00;

            // Check if Granularity Retention value is within the valid range
            if (!isNaN(GranularityRetentionValue) && GranularityRetentionValue !== "" && GranularityRetentionValue < minGranularityRetentionValue || GranularityRetentionValue > GranularityRetentionValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateRetentionValue(textbox) {
            console.log("validateRetentionValue function called");

            // Retrieve elements and values
            var RetentionValue = parseFloat(document.getElementById('<%= TB_Retention.ClientID %>').value);
            var remarksDiv = document.getElementById("RetentionRemarksDIV");
            var minRetentionValue = parseFloat(document.getElementById('<%= hdnMinRetentionValue.ClientID %>').value);
            var maxRetentionValue = parseFloat(document.getElementById('<%= hdnMaxRetentionValue.ClientID %>').value);

            // Define the valid range
            //var minRetentionValue = 20.00;
            //var maxRetentionValue = 30.00;

            // Check if Granularity Retention value is within the valid range
            if (!isNaN(RetentionValue) && RetentionValue !== "" && RetentionValue < minRetentionValue || RetentionValue > RetentionValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

    </script>

    <asp:HiddenField ID="hdn_formid" runat="server" />

    <asp:HiddenField ID="hdnMinMoistureValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMoistureValue" runat="server" />

    <asp:HiddenField ID="hdnMinTotalAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTotalAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinInsolubleAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxInsolubleAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinGlutentContentValue" runat="server" />
    <asp:HiddenField ID="hdnMaxGlutentContentValue" runat="server" />

    <asp:HiddenField ID="hdnMinAlcoholicAcidityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxAlcoholicAcidityValue" runat="server" />

    <asp:HiddenField ID="hdnMinAbsorptionValue" runat="server" />
    <asp:HiddenField ID="hdnMaxAbsorptionValue" runat="server" />

    <asp:HiddenField ID="hdnMinSedimentationValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSedimentationValue" runat="server" />

    <asp:HiddenField ID="hdnMinAcidityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxAcidityValue" runat="server" />

    <asp:HiddenField ID="hdnMinGranularityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxGranularityValue" runat="server" />

    <asp:HiddenField ID="hdnMinGranularityRetentionValue" runat="server" />
    <asp:HiddenField ID="hdnMaxGranularityRetentionValue" runat="server" />

    <asp:HiddenField ID="hdnMinRetentionValue" runat="server" />
    <asp:HiddenField ID="hdnMaxRetentionValue" runat="server" />

    <asp:HiddenField ID="hdnMinBromateValue" runat="server" />
    <asp:HiddenField ID="hdnMaxBromateValue" runat="server" />

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
                                <hr />
                            </div>
                            <div class="col-md-3" id="MaterialDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_Material" runat="server" AssociatedControlID="DDL_Material" Text="Material Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Material" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_Material_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PlantDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="PlantLineDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ProductBrand" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_ProductBrand" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="BrandDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BrandName" runat="server" AssociatedControlID="TB_BrandName" Text="Brand Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BrandName" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BrandName" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BrandName" ValidationGroup="Submit" runat="server" ControlToValidate="TB_BrandName" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BrandName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Supplier Brand Name"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SupplierDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Supplier" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Supplier" runat="server" ControlToValidate="TB_Supplier" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Text="" Placeholder="Supplier (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="QuantityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Quantity" runat="server" AssociatedControlID="TB_Quantity" Text="Quantity Supplied:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" ValidationGroup="Submit" runat="server" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Quantity Value(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SizeDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Size" runat="server" AssociatedControlID="TB_Size" Text="Sample Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Size" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Size" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Size" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Size" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Size" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [1.00-10.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Size(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="SizeRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Size_Remarks" runat="server" AssociatedControlID="TXB_Size_Remarks" Text="Size Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Size_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Size_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Size_Remarks" runat="server" CssClass="form-control form-control-sm rounded" ></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="ChallanNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_ChallanNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Single quote (') is not allowed" ValidationExpression="^[^']*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" MaxLength="50" Placeholder="Challan No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="ChallanDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="ChallanDateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanDate" runat="server"
                                        ControlToValidate="TB_ChallanDate" ValidationGroup="Submit" ForeColor="Red"
                                        ErrorMessage="Enter valid dates in DD-MM-YYYY format, separated by commas"
                                        ValidationExpression="^(\d{2}-\d{2}-\d{4})(,\s*\d{2}-\d{2}-\d{4})*$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="MfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mfg" runat="server" AssociatedControlID="TB_Mfg" Text=" Mfg. Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Mfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_Mfg" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="MfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mfg" runat="server" AssociatedControlID="TB_Mfg" Text=" Mfg. Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Mfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_Mfg" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Dates" runat="server"
                                        ControlToValidate="TB_Mfg" ValidationGroup="Submit" ForeColor="Red"
                                        ErrorMessage="Enter valid dates in DD-MM-YYYY format, separated by commas"
                                        ValidationExpression="^(\d{2}-\d{2}-\d{4})(,\s*\d{2}-\d{2}-\d{4})*$"
                                        Display="Dynamic">
                                    </asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BatchNo" runat="server" AssociatedControlID="TB_BatchNo" Text="Mfg. Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BatchNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_BatchNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BatchNo" runat="server" ControlToValidate="TB_BatchNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[^']*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BatchNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Batch No"></asp:TextBox>
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
                            <div class="col-md-12">
                                <hr />
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-2 : Parameters with Standards (Yes / No)</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="ManufNameAddDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_ManufNameAdd" runat="server" AssociatedControlID="RBL_ManufNameAdd" Text="Manufacturer Name/Address : (In clear readable form)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ManufNameAdd" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_ManufNameAdd" Display="Dynamic"></asp:RequiredFieldValidator>
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

                            <div class="col-md-3" id="FassaiNoLogoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_FassaiNoLogo" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_FassaiNoLogo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_FassaiNoLogo" runat="server" AssociatedControlID="RBL_FassaiNoLogo" Text="FSSAI No/Logo :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_FassaiNoLogo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleFassaiNoLogoRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FassaiNoLogoRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_FassaiNoLogo_Remarks" runat="server" AssociatedControlID="TXB_FassaiNoLogo_Remarks" Text="FSSAI No/Logo (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_FassaiNoLogo_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_FassaiNoLogo_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_FassaiNoLogo_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3" id="BBDateYesNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_BBDateYesNo" runat="server" AssociatedControlID="RBL_BBDateYesNo" Text="Best Before Date:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BBDateYesNo" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_BBDateYesNo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_BBDateYesNo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleBBDateYesNoRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BBDateYesNoRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_BBDateYesNoRemarks" runat="server" AssociatedControlID="TXB_BBDateYesNoRemarks" Text="Best Before Date (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BBDateYesNoRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_BBDateYesNoRemarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_BBDateYesNoRemarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12" id="MfgNameDIV" runat="server">
                                <div class="mb-12">
                                    <asp:Label ID="Lbl_TB_MfgName" runat="server" AssociatedControlID="TB_MfgName" Text="Manufacturer Name and Address : In clear readable form" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_MfgName" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_MfgName" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <asp:RegularExpressionValidator ID="REV_TB_MfgName" runat="server" ControlToValidate="TB_MfgName" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z0-9\s,.\-\/#]{3,100}$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MfgName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" TextMode="MultiLine" Placeholder="Name and address (3-100 characters)" Rows="2" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-6" id="BeforeDateDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_TB_BeforeDate" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BeforeDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Lbl_TB_BeforeDate" runat="server" AssociatedControlID="TB_BeforeDate" Text="Best Before Date : 30 days from manufacturing date & In clear readable form" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BeforeDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" TextMode="Date" oninput="setTimeout(validateBestBeforeDate, 200);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="FssaiNoDIV" runat="server">
                                <div class="mb-6">
                                    <asp:Label ID="Lbl_TB_FssaiNo" runat="server" AssociatedControlID="TB_FssaiNo" Text="FSSAI License No. : In clear readable form & must match with the material" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_FssaiNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_FssaiNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <asp:RegularExpressionValidator ID="REV_TB_FssaiNo" runat="server" ControlToValidate="TB_FssaiNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Number Only 14 digit" Maxlength="14" ValidationExpression="^[1-9]\d*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FssaiNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="FSAAI License No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="FssaiLogoDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Fssai_Logo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Fssai_Logo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_Fssai_Logo" runat="server" AssociatedControlID="RBL_Fssai_Logo" Text=" FSSAI Licence Logo : In clear readable form & must match with the material" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Fssai_Logo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                            <asp:ListItem Text="Present" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Absent" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="VegLogoDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Veg_Logo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Veg_Logo" Display="Static"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_Veg_Logo" runat="server" AssociatedControlID="RBL_Veg_Logo" Text=" Veg Logo : Must be present" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Veg_Logo" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%">
                                            <asp:ListItem Text="Present" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Absent" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="PackingConditionDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Packing_Condition" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Packing_Condition" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_Packing_Condition" runat="server" AssociatedControlID="RBL_Packing_Condition" Text="Packing Condition : Sealed & intact condition" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Packing_Condition" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="togglePackingConditionRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="PackingConditionRemarksDiv" style="display: none;">
                                <div class="mb-6">
                                    <asp:Label ID="Label_TXB_PackingCondition_Remarks" runat="server" AssociatedControlID="TXB_PackingCondition_Remarks" Text="Packing Condition (Not ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_PackingCondition_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_PackingCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_PackingCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="ColorAppDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ColorApp" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_ColorApp" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="LabelColorApp" runat="server" AssociatedControlID="RBL_ColorApp" Text=" Colour And Appearance : Creamy white & free flowing having no lump or wet state development" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ColorApp" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleColorAppRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="ColorAppRemarksDiv" style="display: none;">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_TXB_ColorApp_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ColorApp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_TXB_ColorApp_Remarks" runat="server" AssociatedControlID="TXB_ColorApp_Remarks" Text=" Colour And Appearance (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ColorApp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="OdourDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Odour" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" InitialValue="" ControlToValidate="RBL_Odour" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="LabelOdour" runat="server" AssociatedControlID="RBL_Odour" Text=" Odour : Should be free from any off Odour, rancid /fermented smell" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Odour" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleOdourRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="OdourRemarksDiv" style="display: none;">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Odour_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Odour_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Label_TXB_Odour_Remarks" runat="server" AssociatedControlID="TXB_Odour_Remarks" Text="Odour (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Odour_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="TasteFlavorDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_TasteFlavor" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_TasteFlavor" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="LabelTasteFlavor" runat="server" AssociatedControlID="RBL_TasteFlavor" Text="Taste : Clear & must free from bitter & rancid taste" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_TasteFlavor" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleTasteFlavorRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="TasteFlavorRemarksDiv" style="display: none;">
                                <div class="mb-6">
                                    <asp:Label ID="LabelTasteFlavorRemarks" runat="server" AssociatedControlID="TXB_TasteFlavor_Remarks" Text=" Taste  (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_TasteFlavor_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TasteFlavor_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_TasteFlavor_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="ImpuritiesDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Impurities" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Impurities" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:Label ID="LabelImpurities" runat="server" AssociatedControlID="RBL_Impurities" Text="Impurities : Should be free from rodent hair, excreta, weevils, larvae, & other visible foreign matter" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Impurities" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleImpuritiesRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="ImpuritiesRemarksDiv" style="display: none;">
                                <div class="mb-6">
                                    <asp:Label ID="LabelImpuritiesRemarks" runat="server" AssociatedControlID="TXB_Impurities_Remarks" Text="Impurities (Not Ok)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ImpuritiesRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Impurities_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Impurities_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-12">
                                <hr />
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-3[A]: Parameters with Standards (Lab Results)</h4>
                                <hr />
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

                            <div class="col-md-3" id="AbsorptionDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Absorption" runat="server" AssociatedControlID="TB_Absorption" Text="Water Absorption Property :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Absorption" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Absorption" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Absorption" runat="server" ControlToValidate="TB_Absorption" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Absorption" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Absorption" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Absorption Value" oninput="validateAbsorptionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AbsorptionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Absorption_Remarks" runat="server" AssociatedControlID="TXB_Absorption_Remarks" Text="Absorption Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Absorption_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Absorption_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Absorption_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SedimentationDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Sedimentation" runat="server" AssociatedControlID="TB_Sedimentation" Text="Sedimentation Value:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Sedimentation" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Sedimentation" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Sedimentation" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Sedimentation" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Sedimentation" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Sedimentation" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Sedimentation Value " oninput="validateSedimentationValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SedimentationRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Sedimentation_Remarks" runat="server" AssociatedControlID="TXB_Sedimentation_Remarks" Text="Sedimentation Value Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Sedimentation_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Sedimentation_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Sedimentation_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GrittinessDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelGrittiness" runat="server" AssociatedControlID="RBL_Grittiness" Text="Grittiness in Flour (CCL4 Test) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Grittiness" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Grittiness" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Grittiness" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleGrittinessRemarksDiv(this);">
                                            <asp:ListItem Text="Positive" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Negative" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GrittinessRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelGrittinessRemarks" runat="server" AssociatedControlID="TXB_Grittiness_Remarks" Text="Grittiness (Positive)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_GrittinessRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Grittiness_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Grittiness_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AcidityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Acidity" runat="server" AssociatedControlID="TB_Acidity" Text="Germ Oil Acidity Index :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Acidity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Acidity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Acidity" runat="server" ControlToValidate="TB_Acidity" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Acidity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Acidity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder=" Acidity Value " oninput="validateAcidityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AcidityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Acidity_Remarks" runat="server" AssociatedControlID="TXB_Acidity_Remarks" Text="Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Acidity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Acidity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Acidity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Granularity" runat="server" AssociatedControlID="TB_Granularity" Text="Granularity on 70 mesh:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Granularity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Granularity" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Granularity" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Granularity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Granularity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Granularity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Granularity on 70 mesh " oninput="validateGranularityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Granularity_Remarks" runat="server" AssociatedControlID="TXB_Granularity_Remarks" Text="Granularity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Granularity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Granularity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Granularity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityRetentionDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GranularityRetention" runat="server" AssociatedControlID="TB_GranularityRetention" Text="Granularity/Retention on 85 mesh:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GranularityRetention" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_GranularityRetention" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GranularityRetention" runat="server" ValidationGroup="Submit" ControlToValidate="TB_GranularityRetention" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_GranularityRetention" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GranularityRetention" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Granularity " oninput="validateGranularityRetentionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityRetentionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_GranularityRetention_Remarks" runat="server" AssociatedControlID="TXB_GranularityRetention_Remarks" Text="Granularity 85 Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GranularityRetention_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GranularityRetention_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GranularityRetention_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="RetentionDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Retention" runat="server" AssociatedControlID="TB_Retention" Text="Granularity/Retention on 36 mesh:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Retention" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Retention" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Retention" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Retention" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Retention" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Retention" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Granularity " oninput="validateRetentionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="RetentionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Retention_Remarks" runat="server" AssociatedControlID="TXB_Retention_Remarks" Text="Granularity 36 Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Retention_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Retention_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Retention_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BromateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Bromate" runat="server" AssociatedControlID="TB_Bromate" Text="Bromate and Iodate :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Bromate" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Bromate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Bromate" runat="server" ControlToValidate="TB_Bromate" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Bromate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Bromate and Iodate (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-3[B] : Input Later Parameters</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="TotalAshDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TotalAsh" runat="server" AssociatedControlID="TB_TotalAsh" Text="Total Ash" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TotalAsh" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_TotalAsh" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TotalAsh" runat="server" ControlToValidate="TB_TotalAsh" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TotalAsh" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TotalAsh" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Total Ash Value " oninput="validateAshValue(this);"></asp:TextBox>
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
                                    <asp:RequiredFieldValidator ID="RFV_TB_InsolubleAsh" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_InsolubleAsh" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_InsolubleAsh" runat="server" ControlToValidate="TB_InsolubleAsh" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_InsolubleAsh" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_InsolubleAsh" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Insoluble Ash Value " oninput="validateInsolubleAshValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="GlutentContentDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GlutentContent" runat="server" AssociatedControlID="TB_GlutentContent" Text="Glutent Content  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GlutentContent" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_GlutentContent" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GlutentContent" runat="server" ValidationGroup="Submit" ControlToValidate="TB_GlutentContent" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_GlutentContent" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GlutentContent" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Glutent Content(in mm)" oninput="validateGlutentContentValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlutentContentRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_GlutentContent_Remarks" runat="server" AssociatedControlID="TXB_GlutentContent_Remarks" Text="Glutent Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GlutentContent_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GlutentContent_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GlutentContent_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AlcoholicAcidityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_AlcoholicAcidity" runat="server" AssociatedControlID="TB_AlcoholicAcidity" Text=" Alcoholic Acidity :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_AlcoholicAcidity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_AlcoholicAcidity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_AlcoholicAcidity" runat="server" ControlToValidate="TB_AlcoholicAcidity" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_AlcoholicAcidity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_AlcoholicAcidity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Alcoholic Acidity Value " oninput="validateAlcoholicAcidityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AlcoholicAcidityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_AlcoholicAcidity_Remarks" runat="server" AssociatedControlID="TXB_AlcoholicAcidity_Remarks" Text="Alcoholic Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_AlcoholicAcidity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_AlcoholicAcidity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_AlcoholicAcidity_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <hr />
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-left text-info">Step-4 : Phtotgraph Attachment</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="FU_MaterialImage_Upldr" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_FU_MaterialImage" runat="server" AssociatedControlID="FU_MaterialImage" Text="Material Bag Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
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
                                <hr />
                            </div>
                            <div class="col-md-12">
                                <h4 class="text-center text-success">Final Step : Data Submission</h4>
                                <hr />
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
