<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_process_rpt.aspx.cs" Inherits="AnmolDristi.qaqc_ProcessChecking_rpt" %>

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
            padding-bottom: 20px;
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

        /*.tab-disabled {
            color: #6c757d;
            pointer-events: none;
            cursor: not-allowed;
        }*/
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hdn_formid" runat="server" />
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />

    <script type="text/javascript">

        function validateForm() {
            // Ensure Page_ClientValidate exists (avoid JavaScript errors)
            if (typeof (Page_ClientValidate) == 'function') {
                if (!Page_ClientValidate('BasicDataSave')) {
                    new PNotify({
                        title: 'Missing Input',
                        text: 'Please complete before submitting.',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                    return false; // Stop form submission
                }
            }

            // Run additional image validation
            if (!validateImages()) {
                new PNotify({
                    title: 'Missing Input',
                    text: 'Invalid image selection. Please check your images.',
                    type: 'error',
                    styling: 'bootstrap3'
                });
                return false; // Stop form submission
            }

            return true; // Allow form submission
        }

        function validateSubmit() {
            var fileUpload = document.getElementById('<%= FU_BBImage.ClientID %>'); // Get the FileUpload control
            var errorMessageLabel = document.getElementById('<%= lblErrorMessage1.ClientID %>'); // Get the error message label

            if (fileUpload.value === "") {
                errorMessageLabel.innerHTML = "Please select a file before submitting."; // Display error message
                errorMessageLabel.style.color = "red"; // Change color to red
                return false; // Prevent form submission
            }
            return true;
        }

        function toggleMaidaColorAppRemarksDiv(radioButtonList) {
            //console.log("toggleMaidaColorAppRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MaidaColorAppRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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

        function toggleMaidaFlavorTasteRemarksDiv(radioButtonList) {
            //console.log("toggleMaidaFlavorTasteRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MaidaFlavorTasteRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleMaidaGrittinessRemarksDiv(radioButtonList) {
            //console.log("toggleMaidaGrittinessRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MaidaGrittinessRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleBBColorAppRemarksDiv(radioButtonList) {
            //console.log("toggleBBColorAppRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BBColorAppRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleBBFlavorTasteRemarksDiv(radioButtonList) {
            //console.log("toggleBBFlavorTasteRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BBFlavorTasteRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleBBMouthFeelRemarksDiv(radioButtonList) {
            //console.log("toggleBBMouthFeelRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BBMouthFeelRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleHvoSmellRemarksDiv(radioButtonList) {
            //console.log("toggleHvoSmellRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("HvoSmellRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleHvoTasteRemarksDiv(radioButtonList) {
            //console.log("toggleHvoTasteRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("HvoTasteRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSMPSmellRemarksDiv(radioButtonList) {
            //console.log("toggleSMPSmellRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SMPSmellRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSMPTasteRemarksDiv(radioButtonList) {
            //console.log("toggleSMPTasteRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SMPTasteRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSMPColorRemarksDiv(radioButtonList) {
            //console.log("toggleSMPColorRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SMPColorRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSyrupColorRemarksDiv(radioButtonList) {
            //console.log("toggleSyrupColorRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SyrupColorRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleInvertSyrupBucketRemarksDiv(radioButtonList) {
            //console.log("toggleInvertSyrupBucketRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("InvertSyrupBucketRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSugarSolBucketRemarksDiv(radioButtonList) {
            //console.log("toggleSugarSolBucketRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SugarSolBucketRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleCreamerBucketRemarksDiv(radioButtonList) {
            //console.log("toggleCreamerBucketRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("CreamerBucketRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSugarGrinderRemarksDiv(radioButtonList) {
            //console.log("toggleSugarGrinderRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SugarGrinderRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleOilSystemRemarksDiv(radioButtonList) {
            //console.log("toggleOilSystemRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("OilSystemRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleOilSprayRemarksDiv(radioButtonList) {
            //console.log("toggleOilSprayRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("OilSprayRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleMilkSprayRemarksDiv(radioButtonList) {
            //console.log("toggleMilkSprayRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MilkSprayRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleDrumCoveredRemarksDiv(radioButtonList) {
            //console.log("toggleDrumCoveredRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("DrumCoveredRemarksDiv");
            //console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "No". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleQualityRemarksDiv(radioButtonList) {
            //console.log("toggleQualityRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("QualityRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleMetalDetectorRemarksDiv(radioButtonList) {
            //console.log("toggleMetalDetectorRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MetalDetectorRemarksDiv");
            //console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'A Mail will be sent',
                        text: 'You have selected "No". Please provide additional remarks.',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleProcessSequenceRemarksDiv(radioButtonList) {
            //console.log("toggleProcessSequenceRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ProcessSequenceRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleDoughConditionRemarksDiv(radioButtonList) {
            //console.log("toggleDoughConditionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("DoughConditionRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleBalanceConditionRemarksDiv(radioButtonList) {
            //console.log("toggleBalanceConditionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BalanceConditionRemarksDiv");
            //console.log("Selected value: " + selectedValue);
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
                }, 200);
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleRoomTempRemarksDiv(textBox) {
            //console.log("toggleRoomTempRemarksDiv function called");
            var roomTemp = parseFloat(document.getElementById('<%=TB_RoomTemp.ClientID%>').value);
            var remarksDiv = document.getElementById("RoomTempRemarksDiv");
            if (!isNaN(roomTemp) && roomTemp !== "" && (roomTemp < 20 || roomTemp > 60)) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleTempRemarksDIV(textBox) {
            //console.log("toggleTempRemarksDIV function called");
            var Temp = parseFloat(document.getElementById('<%=TB_Temp.ClientID%>').value);
            var remarksDiv = document.getElementById("TempRemarksDIV");
            if (!isNaN(Temp) && Temp !== "" && Temp < 20 || Temp > 60) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleDoughTempRemarksDIV(textBox) {
            //console.log("toggleDoughTempRemarksDIV function called");
            var doughTemp = parseFloat(document.getElementById('<%=TB_DoughTemp.ClientID%>').value);
            var remarksDiv = document.getElementById("DoughTempRemarksDIV");
            if (!isNaN(doughTemp) && doughTemp !== "" && doughTemp < 20 || doughTemp > 60) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function togglePositionDiv(radioButtonList) {
            //console.log("togglePositionDiv function called");
            var selectedValue = radioButtonList.querySelector('input[type="radio"]:checked').value;
            var ovenControlsDiv = document.getElementById('OvenControlsDiv');
            var damperControlsDiv = document.getElementById('DamperControlsDiv');

            if (selectedValue === "Oven") {
                ovenControlsDiv.style.display = 'block'; // Show oven controls
                damperControlsDiv.style.display = 'none'; // Hide damper controls
            } else if (selectedValue === "Damper") {
                ovenControlsDiv.style.display = 'none'; // Hide oven controls
                damperControlsDiv.style.display = 'block'; // Show damper controls
            } else {
                ovenControlsDiv.style.display = 'none'; // Hide oven controls
                damperControlsDiv.style.display = 'none'; // Hide damper controls
            }
        }


        // active tab 
        function activateTab(tabId) {
            $('#' + tabId).tab('show');
        }

        function formatTimeInput(event) {
            let input = event.target.value;
            input = input.replace(/\D/g, ''); // Remove non-numeric characters
            if (input.length >= 6) {
                input = input.slice(0, 2) + ':' + input.slice(2, 4) + ':' + input.slice(4);
            } else if (input.length >= 4) {
                input = input.slice(0, 2) + ':' + input.slice(2);
            } else if (input.length >= 2) {
                input = input.slice(0, 2) + ':' + input.slice(2);
            }
            event.target.value = input;
        }


        //------ Weight data functions---------
        function validateGridView() {
            var isValid = true;
            var gridView = document.getElementById('<%= GridView1.ClientID %>');
            for (var i = 1; i < gridView.rows.length; i++) {  // Start from 1 to skip header row
                var row = gridView.rows[i];

                var txtActualWeight = row.querySelector("input[id*='txtActualWeight']");
                //var txtDescription = row.querySelector("input[id*='txtDescription']");

                // Check if TextBoxes are filled
                if (txtActualWeight && txtActualWeight.value.trim() === "") {
                    isValid = false;
                    txtActualWeight.style.borderColor = "red";
                } else {
                    txtActualWeight.style.borderColor = "";
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
                //alert("Please fill all the required fields.");
                new PNotify({
                    title: 'Missing Fields',
                    text: 'Please fill all the required fields.',
                    type: 'error', // Can be 'warning' or 'error' depending on the severity
                    styling: 'bootstrap3'
                });

            }

            return isValid;
        }

        var gridViewData = [];

        // Function to calculate deviation and update the grid data
        function calculateDeviation(inputElement) {
            //console.log("Function called: calculateDeviation");

            var row = inputElement.closest('tr');
            if (!row) {
                //console.error("Row not found! Exiting function.");
                return;
            }

            var slCell = row.querySelector('td');
            if (!slCell) {
                //console.error("SL cell not found! Exiting function.");
                return;
            }
            var slValue = slCell.textContent.trim();
            //console.log("Serial Number (Sl):", slValue);

            var standardWeightElement = row.querySelector('.standard-weight');
            var actualWeightElement = row.querySelector('.actual-weight');
            var deviationWeightElement = row.querySelector('.deviation-weight');
            var deviationPercentageElement = row.querySelector('.deviation-percentage');

            if (!standardWeightElement || !actualWeightElement || !deviationWeightElement || !deviationPercentageElement) {
                //console.error("One or more required input elements not found! Exiting function.");
                return;
            }

            var standardWeight = parseFloat(standardWeightElement.value) || 0;
            var actualWeight = parseFloat(actualWeightElement.value) || 0;

            var deviationWeight = actualWeight - standardWeight;
            deviationWeightElement.value = deviationWeight.toFixed(2);

            var deviationPercentage = standardWeight !== 0 ? (deviationWeight * 100) / standardWeight : 0;
            deviationPercentageElement.value = deviationPercentage.toFixed(2);

            if (Math.abs(deviationPercentage) > 5) {
                //console.warn("Deviation exceeds 5%! Triggering notification...");
                setTimeout(function () {
                    new PNotify({
                        title: 'Deviation Alert',
                        text: 'Weight deviation exceeds 5%',
                        type: 'warning',
                        styling: 'bootstrap3'
                    });
                }, 100);
            }

            var varietyElement = row.querySelector('.variety');
            var varietyName = varietyElement ? varietyElement.value.trim() : "Unknown";
            //console.log("Variety Name:", varietyName);

            var rowData = {
                Sl: slValue,
                Variety: varietyName,
                StandardWeight: standardWeight,
                ActualWeight: actualWeight,
                DeviationWeight: deviationWeight.toFixed(2),
                DeviationPercentage: deviationPercentage.toFixed(2)
            };
            //console.log("Row Data Object:", rowData);

            updateGridViewData(slValue, rowData);
            //console.log("Global GridView Data updated.");
        }


        // Function to update the global array with the current row's data
        function updateGridViewData(slValue, rowData) {
            // Find the existing row in gridViewData with the matching Sl value
            var existingRowIndex = gridViewData.findIndex(row => row.Sl === slValue);

            // If the row exists, update it; otherwise, add it
            if (existingRowIndex >= 0) {
                gridViewData[existingRowIndex] = rowData;
            } else {
                gridViewData.push(rowData);
            }

            // Log the updated data for debugging
            //console.log('GridView Data Updated:', gridViewData);
        }

        function collectAndSendData() {
            // Ensure all rows are processed
            document.querySelectorAll('#GridView1 tbody tr').forEach(row => {
                var actualWeightElement = row.querySelector('#txtActualWeight');
                if (actualWeightElement) {
                    calculateDeviation(actualWeightElement); // Process each row
                }
            });

            // Convert data to JSON
            var jsonData = JSON.stringify(gridViewData);
            //console.log('Data to be sent:', jsonData);

            // Send data to the server
            var xhr = new XMLHttpRequest();
            xhr.open('POST', 'qaqc_process_rpt.aspx/SaveData', true); // URL to your Web Method
            xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
            xhr.setRequestHeader('Accept', 'application/json');
            xhr.onreadystatechange = function () {
                if (xhr.readyState === XMLHttpRequest.DONE) {
                    if (xhr.status === 200) {
                        //console.log('Data successfully sent to server');
                    } else {
                        console.log('Error sending data:', xhr.statusText);
                    }
                }
            };
            xhr.send(JSON.stringify({ jsonData: jsonData }));
        }


        // --------Oven data functions ---------
        function validateGridView1() {
            var isValid = true;
            var gridView = document.getElementById('<%= GridView2.ClientID %>');
            for (var i = 1; i < gridView.rows.length; i++) {  // Start from 1 to skip header row
                var row = gridView.rows[i];

                var txtOvenTop = row.querySelector("input[id*='txtOvenTop']");
                var txtOvenBottom = row.querySelector("input[id*='txtOvenBottom']");
                var txtDamperTop = row.querySelector("input[id*='txtDamperTop']");

                // Check if TextBoxes are filled
                if (txtOvenTop && txtOvenTop.value.trim() && txtOvenBottom && txtOvenBottom.value.trim() && txtDamperTop && txtDamperTop.value.trim() === "") {
                    isValid = false;
                    txtOvenTop.style.borderColor = "red";
                    txtOvenBottom.style.borderColor = "red";
                    txtDamperTop.style.borderColor = "red";
                } else {
                    txtOvenTop.style.borderColor = "";
                    txtOvenBottom.borderColor = "";
                    txtDamperTop.borderColor = "";
                }

            }

            // If not valid, prevent form submission
            if (!isValid) {
                alert("Please fill all the required fields.");
            }

            return isValid;
        }

        var gridViewData1 = [];

        // Function to calculate damper bottom and update the grid data
        function calculateDamperBottom(inputElement) {
            // Find the row that contains the input element
            var row = inputElement.closest('tr');
            //console.log('rows:', row);

            var slValue = row.querySelector('td').textContent.trim();
            //console.log('slValue:', slValue);
            // Retrieve the Oven Top, Oven Bottom, Damper Top and Damper bottom  elements from the same row
            var ovenTopElement = row.querySelector('#txtOvenTop');
            var ovenTop = parseFloat(ovenTopElement.value) || 0;
            //console.log('Oven Top:', ovenTop);

            var ovenBottomElement = row.querySelector('#txtOvenBottom');
            var ovenBottom = parseFloat(ovenBottomElement.value) || 0;
            //console.log('Oven Bottom:', ovenBottom);

            var damperTopElement = row.querySelector('#txtDamperTop');
            var damperTop = parseFloat(damperTopElement.value) || 0;
            //console.log('Damper Top:', damperTop);

            // Calculate the damper bottom
            var damperBottom = 100 - damperTop;
            //console.log('Damper Bottom:', damperBottom);

            // Get the Damper bottom element from the same row
            var damperBottomElement = row.querySelector('#txtDamperBottom');

            // Format the damper bottom percentage to two decimal places
            var formattedDamperBottom = damperBottom.toFixed(2);
            //console.log('Formatted Damper Bottom Percentage:', formattedDamperBottom);

            // Update the Damper Bottom Percentage TextBox
            damperBottomElement.value = formattedDamperBottom;


            // Build JSON data for the current row
            var rowData = {
                Sl: slValue,
                Zone: row.querySelector('.zone').textContent.trim(),
                OvenTop: ovenTop,
                OvenBottom: ovenBottom,
                DamperTop: damperTop,
                DamperBottom: formattedDamperBottom
            };
            //console.log('rowData:', rowData);

            // Update global array with current row data
            updateGridViewData1(slValue, rowData);
        }

        // Function to update the global array with the current row's data
        function updateGridViewData1(slValue, rowData) {
            // Find the existing row in gridViewData with the matching Sl value
            var existingRowIndex = gridViewData1.findIndex(row => row.Sl === slValue);

            // If the row exists, update it; otherwise, add it
            if (existingRowIndex >= 0) {
                gridViewData1[existingRowIndex] = rowData;
            } else {
                gridViewData1.push(rowData);
            }

            // Log the updated data for debugging
            //console.log('GridView Data Updated:', gridViewData1);
        }

        function collectAndSendData1() {
            // Ensure all rows are processed
            document.querySelectorAll('#GridView2 tbody tr').forEach(row => {
                var ovenTopElement = row.querySelector('#txtOvenTop');
                var ovenBottomElement = row.querySelector('#txtOvenBottom');
                var damperTopElement = row.querySelector('#txtDamperTop');
                if (ovenTopElement && ovenBottomElement && damperTopElement) {
                    calculateDamperBottom(ovenTopElement && ovenBottomElement && damperTopElement); // Process each row
                }
            });

            // Convert data to JSON
            var jsonData = JSON.stringify(gridViewData1);
            //console.log('Data to be sent:', jsonData);

            // Send data to the server
            var xhr = new XMLHttpRequest();
            xhr.open('POST', 'qaqc_process_rpt.aspx/SaveData1', true); // URL to your Web Method
            xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
            xhr.setRequestHeader('Accept', 'application/json');
            xhr.onreadystatechange = function () {
                if (xhr.readyState === XMLHttpRequest.DONE) {
                    if (xhr.status === 200) {
                        //console.log('Data successfully sent to server');
                    } else {
                        //console.log('Error sending data:', xhr.statusText);
                    }
                }
            };
            xhr.send(JSON.stringify({ jsonData: jsonData }));
        }

        document.addEventListener('DOMContentLoaded', function () {
            const tabIds = ['basicData', 'weight', 'spongeData', 'doughData', 'ovenData', 'verificationData'];
            let enabledTabs = new Set(['basicData']); // Initially enable the first tab

            // Disable all tabs except the first one initially
            //for (let i = 0; i < tabIds.length; i++) {
            //    document.querySelector(`a[href="#${tabIds[i]}"]`).classList.add('disabled');
            //}

            // Function to check if the current tab is filled and enable the next tab
            function checkTabContent(index) {
                const currentTabContent = document.querySelector(`#${tabIds[index]}`);
                const inputs = currentTabContent.querySelectorAll('input, select, textarea');
                let isValid = true;

                inputs.forEach(input => {
                    if (!input.value.trim()) {
                        isValid = false;
                    }
                });

                if (isValid && index < tabIds.length - 1) {
                    // Enable the next tab and disable the current tab
                    const nextTab = document.querySelector(`a[href="#${tabIds[index + 1]}"]`);
                    const currentTab = document.querySelector(`a[href="#${tabIds[index]}"]`);

                    nextTab.classList.remove('disabled');
                    enabledTabs.add(tabIds[index + 1]); // Mark the next tab as enabled

                    // Disable the current tab once moving to the next one
                    currentTab.classList.add('disabled');
                    enabledTabs.delete(tabIds[index]); // Remove current tab from enabled set
                }
            }

            // Attach click event listeners to each tab
            tabIds.forEach((tabId, index) => {
                document.querySelector(`a[href="#${tabId}"]`).addEventListener('click', function (e) {
                    if (!enabledTabs.has(tabId)) {
                        e.preventDefault(); // Prevent navigation
                        //alert('Please complete the current section before proceeding.');
                        new PNotify({
                            title: 'Access Denied',
                            text: 'Please complete the previous section before proceeding.',
                            type: 'warning', // Can be 'success', 'error', 'info', 'warning'
                            styling: 'bootstrap3'
                        });
                    }
                });

                // Monitor input changes to check and enable the next tab
                const tabContent = document.querySelector(`#${tabId}`);
                const inputs = tabContent.querySelectorAll('input, select, textarea');

                inputs.forEach(input => {
                    input.addEventListener('input', function () {
                        checkTabContent(index);
                    });
                });
            });
        });


        function displayImage(input) {
            var file = input.files[0];
            if (!file) return;

            var prefix = input.getAttribute("data-prefix");
            //console.log('PrefixValue: ' + prefix + '');
            var img = document.createElement("img");
            var reader = new FileReader();

            reader.onload = function (e) {
                img.src = e.target.result;

                img.onload = function () {
                    var canvas = document.createElement("canvas");
                    var ctx = canvas.getContext("2d");

                    var maxWidth = 1200; // Increased resolution
                    var maxHeight = 1200; // Optional max height
                    var width = img.width;
                    var height = img.height;

                    // Resize based on maxWidth while maintaining aspect ratio
                    if (width > maxWidth || height > maxHeight) {
                        if (width > height) {
                            height = Math.floor((maxWidth / width) * height);
                            width = maxWidth;
                        } else {
                            width = Math.floor((maxHeight / height) * width);
                            height = maxHeight;
                        }
                    }

                    canvas.width = width;
                    canvas.height = height;
                    ctx.drawImage(img, 0, 0, width, height);

                    canvas.toBlob(function (blob) {
                        var formData = new FormData();

                        var fileExtension = file.name.split('.').pop(); // Get the file extension
                        var newFileName = prefix + "_" + new Date().getTime() + "." + fileExtension; // Create new filename with prefix

                        formData.append("image", blob, newFileName); // Append file with new filename
                        formData.append("prefix", prefix); // Append the prefix for folder selection

                        var xhr = new XMLHttpRequest();
                        xhr.open("POST", "/UploadImageHandler.ashx", true);

                        xhr.upload.onprogress = function (event) {
                            if (event.lengthComputable) {
                                var percentComplete = (event.loaded / event.total) * 100;
                                //console.log('Upload progress: ' + percentComplete + '%');
                            }
                        };

                        xhr.onload = function () {
                            if (xhr.status === 200) {
                                var response = JSON.parse(xhr.responseText);
                                var imageElement = document.querySelector('img[data-prefix="' + prefix + '"]');
                                //console.log("Image Display Element: ", imageElement);
                                var labelElement = document.querySelector('span[data-prefix="' + prefix + '"]');
                                //console.log("Hidden Element: ", labelElement);

                                if (imageElement) {

                                    imageElement.style.display = 'block';
                                    imageElement.src = response.imageUrl; // Update image source
                                    console.log("Image URL: " + response.imageUrl);
                                } else {
                                    //console.error("Image element not found for prefix: " + prefix);
                                }

                                if (labelElement) {
                                    labelElement.innerText = response.imageUrl;
                                    //labelElement.textContent = response.imageUrl;
                                }

                                // Now use if-else to bind to individual hidden fields
                                if (prefix === "QAPC/MaidaImage") {
                                    document.getElementById('<%= hdn_img1.ClientID %>').value = response.imageUrl;
                                } else if (prefix === "QAPC/BBImage") {
                                    document.getElementById('<%= hdn_img2.ClientID %>').value = response.imageUrl;
                                }

                                // Clear the file input uploader
                            input.value = '';

                            new PNotify({
                                title: 'Upload Success',
                                text: 'Image Saved!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        } else {
                            new PNotify({
                                title: 'Upload Failed',
                                text: 'An error occurred during the upload.',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        }
                        };

                        xhr.send(formData);
                    }, 'image/jpeg', 0.9); // Compress to 80% quality
                };
            };

        reader.readAsDataURL(file);
    }

    function validateImages() {
        var image1 = document.getElementById('<%= uploadedImage1.ClientID %>'); // First image
            var image2 = document.getElementById('<%= uploadedImage2.ClientID %>'); // Second image

            // Check if images are displayed
        if (image1.style.display === 'none' && image2.style.display === 'none') {
            console.log("Image Element Checking");
                new PNotify({
                    title: 'Missing Input',
                    text: 'Please upload at least one image before submitting.',
                    type: 'error',
                    styling: 'bootstrap3'
                });
                return false; // Prevent form submission
            }

            return true; // Allow form submission if all validations pass
        }

    </script>

    <asp:HiddenField ID="hdn_img1" runat="server" />
    <asp:HiddenField ID="hdn_img2" runat="server" />

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
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
                                                        <a class="nav-link active text-info" id="basicData-tab" data-toggle="tab" href="#basicData" role="tab" aria-controls="basicData" aria-selected="true"><%--<i class="fa fa-id-badge mr-2"></i>--%>Plant & Line</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="weight-tab" data-toggle="tab" href="#weight" role="tab" aria-controls="weight" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Raw Weight</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="spongeData-tab" data-toggle="tab" href="#spongeData" role="tab" aria-controls="spongeData" aria-selected="false">Sponge </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="doughData-tab" data-toggle="tab" href="#doughData" role="tab" aria-controls="doughData" aria-selected="false">Dough </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="ovenData-tab" data-toggle="tab" href="#ovenData" role="tab" aria-controls="ovenData" aria-selected="false">Oven </a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="verificationData-tab" data-toggle="tab" href="#verificationData" role="tab" aria-controls="verificationData" aria-selected="false">Verification</a>
                                                    </li>
                                                </ul>

                                                <div class="tab-content ml-1" id="myTabContent">

                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x-content">

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" InitialValue="0" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSaveNOT" ForeColor="Red" ControlToValidate="DDL_BrandSKU" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_ProcessWaterTemp" runat="server" AssociatedControlID="TB_ProcessWaterTemp" Text="Process Water Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_ProcessWaterTemp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_ProcessWaterTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_ProcessWaterTemp" runat="server" ControlToValidate="TB_ProcessWaterTemp" ForeColor="Red" ErrorMessage="Decimal only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_ProcessWaterTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Process Water Temp. "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="ProcessWaterTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelProcessWaterTempRemarks" runat="server" AssociatedControlID="TXB_ProcessWaterTemp_Remarks" Text="Process Water Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_ProcessWaterTemp_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ProcessWaterTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_ProcessWaterTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterPH" runat="server" AssociatedControlID="TB_WaterPH" Text="Water PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_WaterPH" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_WaterPH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_WaterPH" runat="server" ControlToValidate="TB_WaterPH" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterPH" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Water PH Value "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="WaterPHRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelWaterPHRemarks" runat="server" AssociatedControlID="TXB_WaterPH_Remarks" Text="Water PH Value Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_WaterPH_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_WaterPH_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_WaterPH_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterHardness" runat="server" AssociatedControlID="TB_WaterHardness" Text="Water Hardness" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_WaterHardness" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_WaterHardness" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_WaterHardness" runat="server" ControlToValidate="TB_WaterHardness" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterHardness" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Water Hardness Value "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="WaterHardnessRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelWaterHardnessRemarks" runat="server" AssociatedControlID="TXB_WaterHardness_Remarks" Text="Water Hardness Remarks:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_WaterHardness_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_WaterHardness_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_WaterHardness_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_WaterTest" runat="server" AssociatedControlID="TB_WaterTest" Text="Water Test :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_WaterTest" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_WaterTest" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_WaterTest" runat="server" ControlToValidate="TB_WaterTest" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_WaterTest" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Water Test (3-20 characters)" MaxLength="20"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_TDS" runat="server" AssociatedControlID="TB_TDS" Text="TDS" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_TDS" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_TDS" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_TDS" runat="server" ControlToValidate="TB_TDS" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_TDS" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="TDS Value "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="TDSRemarks" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelTDSRemarks" runat="server" AssociatedControlID="TXB_TDS_Remarks" Text="Tds Remarks:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TDS_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TDS_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_TDS_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaBrand" runat="server" AssociatedControlID="TB_MaidaBrand" Text="Maida Brand :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_MaidaBrand" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MaidaBrand" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_MaidaBrand" runat="server" ControlToValidate="TB_MaidaBrand" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaBrand" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Maida Brand (3-30 characters)" MaxLength="30"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaBatchNo" runat="server" AssociatedControlID="TB_MaidaBatchNo" Text="Maida Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_MaidaBatchNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MaidaBatchNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_MaidaBatchNo" runat="server" ControlToValidate="TB_MaidaBatchNo" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaBatchNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Maida Batch No"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_MaidaMfg" runat="server" AssociatedControlID="TB_MaidaMfg" Text="Maida Mfg :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaMfg" runat="server" ErrorMessage="Date is required " ControlToValidate="TB_MaidaMfg" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_MaidaMfg" runat="server" ControlToValidate="TB_MaidaMfg" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MaidaMfg" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaColorApp" runat="server" AssociatedControlID="RBL_MaidaColorApp" Text="Maida Color Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_RBL_MaidaColorApp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_MaidaColorApp" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaColorApp" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleMaidaColorAppRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaColorAppRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaColorAppRemarks" runat="server" AssociatedControlID="TXB_MaidaColorApp_Remarks" Text="Maida Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaColorAppRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_MaidaColorApp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaColorApp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaFlavorTaste" runat="server" AssociatedControlID="RBL_MaidaFlavorTaste" Text="Maida Flavor Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaFlavorTaste" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_MaidaFlavorTaste" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaFlavorTaste" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleMaidaFlavorTasteRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaFlavorTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaFlavorTasteRemarks" runat="server" AssociatedControlID="TXB_MaidaFlavorTaste_Remarks" Text="Maida Flavor Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaFlavorTasteRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_MaidaFlavorTaste_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaFlavorTaste_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaGrittiness" runat="server" AssociatedControlID="RBL_MaidaGrittiness" Text="Maida Grittiness :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaGrittiness" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_MaidaGrittiness" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MaidaGrittiness" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleMaidaGrittinessRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MaidaGrittinessRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMaidaGrittinessRemarks" runat="server" AssociatedControlID="TXB_MaidaGrittiness_Remarks" Text="Maida Grittiness (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MaidaGrittinessRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_MaidaGrittiness_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MaidaGrittiness_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBColorApp" runat="server" AssociatedControlID="RBL_BBColorApp" Text="Broken Biscuit Color Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBColorApp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_BBColorApp" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBColorApp" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleBBColorAppRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBColorAppRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBColorAppRemarks" runat="server" AssociatedControlID="TXB_BBColorApp_Remarks" Text="BB Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBColorAppRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_BBColorApp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBColorApp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBFlavorTaste" runat="server" AssociatedControlID="RBL_BBFlavorTaste" Text="Broken Biscuit Flavor Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBFlavorTaste" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_BBFlavorTaste" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBFlavorTaste" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleBBFlavorTasteRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBFlavorTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBFlavorTasteRemarks" runat="server" AssociatedControlID="TXB_BBFlavorTaste_Remarks" Text="BB Flavor Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBFlavorTasteRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_BBFlavorTaste_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBFlavorTaste_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBMouthFeel" runat="server" AssociatedControlID="RBL_BBMouthFeel" Text="Broken Biscuit Mouth Feel :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBMouthFeel" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_BBMouthFeel" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BBMouthFeel" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleBBMouthFeelRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BBMouthFeelRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBBMouthFeelRemarks" runat="server" AssociatedControlID="TXB_BBMouthFeel_Remarks" Text="BB Mouth Feel (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BBMouthFeelRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_BBMouthFeel_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BBMouthFeel_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoSmell" runat="server" AssociatedControlID="RBL_HvoSmell" Text="HVO Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoSmell" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_HvoSmell" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_HvoSmell" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleHvoSmellRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="HvoSmellRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoSmellRemarks" runat="server" AssociatedControlID="TXB_HvoSmell_Remarks" Text="HVO Smell (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoSmellRemakrs" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_HvoSmell_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_HvoSmell_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTaste" runat="server" AssociatedControlID="RBL_HvoTaste" Text="HVO Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoTaste" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_HvoTaste" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_HvoTaste" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleHvoTasteRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="HvoTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTasteRemarks" runat="server" AssociatedControlID="TXB_HvoTaste_Remarks" Text="HVO Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoTasteRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_HvoTaste_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_HvoTaste_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTemp" runat="server" AssociatedControlID="TB_HvoTemp" Text="HVO Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoTemp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_HvoTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_HvoTemp" runat="server" ControlToValidate="TB_HvoTemp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_HvoTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="HVO Temp. "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="HvoTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelHvoTempRemarks" runat="server" AssociatedControlID="TXB_HvoTemp_Remarks" Text="HVO Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_HvoTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_HvoTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_HvoTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPSmell" runat="server" AssociatedControlID="RBL_SMPSmell" Text="SMP Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SMPSmell" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SMPSmell" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPSmell" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSMPSmellRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPSmellRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPSmellRemarks" runat="server" AssociatedControlID="TXB_SMPSmell_Remarks" Text="SMP Smell (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SMPSmell_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SMPSmell_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPSmell_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPTaste" runat="server" AssociatedControlID="RBL_SMPTaste" Text="SMP Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SMPTaste" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SMPTaste" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPTaste" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSMPTasteRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPTasteRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPTasteRemarks" runat="server" AssociatedControlID="TXB_SMPTaste_Remarks" Text="SMP Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SMPTasteRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SMPTaste_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPTaste_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPColor" runat="server" AssociatedControlID="RBL_SMPColor" Text="SMP Color :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SMPColor" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SMPColor" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SMPColor" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSMPColorRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SMPColorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSMPColor_Remarks" runat="server" AssociatedControlID="TXB_SMPColor_Remarks" Text="SMP Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TXB_SMPColorRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SMPColor_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SMPColor_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupTemp" runat="server" AssociatedControlID="TB_HvoTemp" Text="Syrup Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SyrupTemp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_SyrupTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_SyrupTemp" runat="server" ControlToValidate="TB_SyrupTemp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_SyrupTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="HVO Temp. "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="SyrupTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupTempRemarks" runat="server" AssociatedControlID="TXB_SyrupTemp_Remarks" Text="Syrup Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SyrupTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SyrupTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SyrupTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupColor" runat="server" AssociatedControlID="RBL_SyrupColor" Text="Syrup Color :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SyrupColor" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SyrupColor" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SyrupColor" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSyrupColorRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SyrupColorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupColorRemarksDiv" runat="server" AssociatedControlID="TXB_SyrupColor_Remarks" Text="Syrup Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SyrupColorRemarksDiv" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SyrupColor_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SyrupColor_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_SyrupPH" runat="server" AssociatedControlID="TB_SyrupPH" Text="Syrup PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_SyrupPH" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_SyrupPH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_SyrupPH" runat="server" ControlToValidate="TB_SyrupPH" ForeColor="Red" ErrorMessage="Decimal" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_SyrupPH" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Syrup PH Value "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="SyrupPHRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSyrupPHRemarks" runat="server" AssociatedControlID="TXB_SyrupPH_Remarks" Text=" Syrup PH Value Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SyrupPHRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SyrupPH_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SyrupPH_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelInvertSyrupBucket" runat="server" AssociatedControlID="RBL_InvertSyrupBucket" Text="Invert Syrup Bucket Filter Sieve :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_InvertSyrupBucket" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_InvertSyrupBucket" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_InvertSyrupBucket" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleInvertSyrupBucketRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="InvertSyrupBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelInvertSyrupBucketRemarks" runat="server" AssociatedControlID="TXB_InvertSyrupBucket_Remarks" Text="Invert Syrup Bucket Filter Sieve Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_InvertSyrupBucketRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_InvertSyrupBucket_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_InvertSyrupBucket_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarSolBucket" runat="server" AssociatedControlID="RBL_SugarSolBucket" Text="Sugar Sol Bucket Filter Sieve :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SugarSolBucket" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SugarSolBucket" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SugarSolBucket" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSugarSolBucketRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SugarSolBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarSolBucketRemarks" runat="server" AssociatedControlID="TXB_SugarSolBucket_Remarks" Text="Sugar Sol Bucket Filter Sieve Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SugarSolBucketRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SugarSolBucket_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SugarSolBucket_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelCreamerBucketFilter" runat="server" AssociatedControlID="RBL_CreamerBucketFilter" Text="Creamer Bucket Filter Sheet :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_CreamerBucketFilter" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_CreamerBucketFilter" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_CreamerBucketFilter" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleCreamerBucketRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="CreamerBucketRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label12" runat="server" AssociatedControlID="TXB_CreamerBucket_Remarks" Text="Creamer Bucket Filter Sheet Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_CreamerBucketRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_CreamerBucket_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_CreamerBucket_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarGrinder" runat="server" AssociatedControlID="RBL_SugarGrinder" Text="Sugar Grinder Sheet :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SugarGrinder" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_SugarGrinder" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SugarGrinder" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleSugarGrinderRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="SugarGrinderRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelSugarGrinderRemarks" runat="server" AssociatedControlID="TXB_SugarGrinder_Remarks" Text="Sugar Grinder Sheet Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SugarGrinderRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_SugarGrinder_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_SugarGrinder_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSystem" runat="server" AssociatedControlID="RBL_OilSystem" Text="Oil System Bucket Filter :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_OilSystem" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_OilSystem" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_OilSystem" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleOilSystemRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="OilSystemRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSystemRemarks" runat="server" AssociatedControlID="TXB_OilSystem_Remarks" Text="Oil System Bucket Filter Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_OilSystemRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_OilSystem_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_OilSystem_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSpray" runat="server" AssociatedControlID="RBL_OilSpray" Text="Oil Spray Seive :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_OilSpray" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_OilSpray" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_OilSpray" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleOilSprayRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="OilSprayRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelOilSprayRemarks" runat="server" AssociatedControlID="TXB_OilSpray_Remarks" Text="Oil Spray Seive Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_OilSprayRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_OilSpray_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_OilSpray_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMilkSpray" runat="server" AssociatedControlID="RBL_MilkSpray" Text="Milk Spray Seive :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MilkSpray" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ForeColor="Red" ControlToValidate="RBL_MilkSpray" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MilkSpray" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleMilkSprayRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MilkSprayRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label13" runat="server" AssociatedControlID="TXB_MilkSpray_Remarks" Text="Milk Spray Seive Remarks (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MilkSprayRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_MilkSpray_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MilkSpray_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelColdRoomTemp" runat="server" AssociatedControlID="TB_ColdRoomTemp" Text="Temp. Of Cold Rooom:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_ColdRoomTemp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_ColdRoomTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_ColdRoomTemp" runat="server" ControlToValidate="TB_ColdRoomTemp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_ColdRoomTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=" Temp. Of Cold Room "></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="ColdRoomTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelColdRoomTempRemarks" runat="server" AssociatedControlID="TXB_ColdRoomTemp_Remarks" Text="Cold Room Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_ColdRoomTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ColdRoomTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_ColdRoomTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDeepFreezeTemp" runat="server" AssociatedControlID="TB_ColdRoomTemp" Text="Temp. Of Deep Freeze:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DeepFreezeTemp" runat="server" ErrorMessage="*" ValidationGroup="BasicDataSave" ControlToValidate="TB_DeepFreezeTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_DeepFreezeTemp" runat="server" ControlToValidate="TB_DeepFreezeTemp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DeepFreezeTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=" Temp. Of Deep Freeze"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                            <div class="col-md-3" id="DeepFreezeTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label14" runat="server" AssociatedControlID="TXB_DeepFreezeTemp_Remarks" Text="Deep Freeze Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DeepFreezeTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_DeepFreezeTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DeepFreezeTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="FU_MaidaImage_Upldr" runat="server" data-prefix="QAPC/MaidaImage">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_FU_MaidaImage" runat="server" AssociatedControlID="FU_MaidaImage" Text="Maida Appearance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_FU_MaidaImage" runat="server" ErrorMessage="*" ControlToValidate="FU_MaidaImage" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:CustomValidator ID="CV_FU_MaidaImage" runat="server" ControlToValidate="FU_BBImage" Display="Dynamic" ValidationGroup="BasicDataSave" ErrorMessage="Please upload file"></asp:CustomValidator>
                                                                    <asp:Label ID="lblErrorMessage2" runat="server" CssClass="text-danger"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:FileUpload ID="FU_MaidaImage" runat="server" CssClass="form-control rounded" data-prefix="QAPC/MaidaImage" onchange="displayImage(this);" />
                                                                        <asp:Label ID="lbl_QAPC_MaidaImg" runat="server" Text="" CssClass="image-label" data-prefix="QAPC/MaidaImage" Visible="true" ForeColor="Black"></asp:Label>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="FU_MaidaImage_img" runat="server" data-prefix="QAPC/MaidaImage">
                                                                <asp:Image ID="uploadedImage1" runat="server" CssClass="img-fluid" data-prefix="QAPC/MaidaImage" Style="display: none;" />
                                                            </div>

                                                            <%--<div class="col-md-3" id="FU_BBImage_Upldr" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_FU_BBImage" runat="server" AssociatedControlID="FU_BBImage" Text="Broken Biscuit Appearance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_FU_BBImage" runat="server" ErrorMessage="*" ControlToValidate="FU_BBImage" Display="Dynamic" ValidationGroup="ValidationGroup2" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:CustomValidator ID="CV_FU_BBImage" runat="server" ControlToValidate="FU_BBImage" Display="Dynamic" ValidationGroup="ValidationGroup2" ErrorMessage="Please upload file"></asp:CustomValidator>
                                                                    <asp:Label ID="lblErrorMessage1" runat="server" CssClass="text-danger"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:FileUpload ID="FU_BBImage" runat="server" CssClass="form-control rounded" />
                                                                        <span class="input-group-btn">
                                                                            <asp:Button ID="BtnUploadFU_BBImage" runat="server" CssClass="btn btn-primary btn-sm" Text="Upload" OnClientClick="return validateForm2();" OnClick="BtnUploadFU_BBImage_Click" ValidationGroup="ValidationGroup2" CausesValidation="true" />
                                                                        </span>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="FU_BBImage_Img" runat="server" visible="false">
                                                                <asp:Image ID="uploadedImage2" runat="server" CssClass="img-fluid" />
                                                            </div>--%>

                                                            <div class="col-md-3" id="FU_BBImage_Upldr" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_FU_BBImage" runat="server" AssociatedControlID="FU_BBImage" Text="Broken Biscuit Appearance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_FU_BBImage" runat="server" ErrorMessage="*" ControlToValidate="FU_BBImage" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:CustomValidator ID="CV_FU_BBImage" runat="server" ControlToValidate="FU_BBImage" Display="Dynamic" ValidationGroup="BasicDataSave" ErrorMessage="Please upload file"></asp:CustomValidator>
                                                                    <asp:Label ID="lblErrorMessage1" runat="server" CssClass="text-danger"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:FileUpload ID="FU_BBImage" runat="server" CssClass="form-control rounded" data-prefix="QAPC/BBImage" onchange="displayImage(this);" />
                                                                        <asp:Label ID="lbl_QAPC_BBImage" runat="server" Text="" CssClass="image-label" data-prefix="QAPC/BBImage" Visible="true" ForeColor="Black"></asp:Label>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="FU_BBImage_Img" runat="server" data-prefix="QAPC/BBImage">
                                                                <asp:Image ID="uploadedImage2" runat="server" CssClass="img-fluid" data-prefix="QAPC/BBImage" Style="display: none;" />
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_BasicbtnSubmit" runat="server" AssociatedControlID="BasicBtnSubmit" Text="Click to SAVE Basic Data" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="BasicBtnSubmit" runat="server" Text="Proceed Next" CssClass="btn btn-primary btn-sm" ValidationGroup="BasicDataSave" OnClientClick="validateImages();" CausesValidation="true" OnClick="BasicBtnSubmit_Click" />
                                                                        <asp:Button ID="BasicBtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BasicBtnReset_Click" />
                                                                        <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                    </div>
                                                                </div>
                                                            </div>


                                                        </div>
                                                    </div>
                                                    <%-- Basic Data ends here--%>

                                                    <%-- Weight Data Starts Here--%>
                                                    <div class="tab-pane fade" id="weight" role="tabpanel" aria-labelledby="weight-tab">
                                                        <div class="x-content">
                                                            <asp:GridView ID="GridView1" Width="100%" runat="server" AutoGenerateColumns="False"
                                                                CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>

                                                                    <asp:TemplateField HeaderText="Sl" HeaderStyle-Width="5%">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Variety" HeaderStyle-Width="35%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtVariety" runat="server" CssClass="variety form-control form-control-sm rounded" Text='<%# Eval("sap_IngredientName") %>' ReadOnly='<%# Convert.ToInt32(Eval("BOM_Qnty")) == 0 ? false : true %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Standard Weight" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtStandardWeight" runat="server" CssClass="standard-weight form-control form-control-sm rounded"
                                                                                ReadOnly='<%# Convert.ToInt32(Eval("BOM_Qnty")) == 0 ? false : true %>' ForeColor="DimGray" Text='<%# Eval("BOM_Qnty") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Actual Weight" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtActualWeight" runat="server" ClientIDMode="Static" CssClass="form-control form-control-sm rounded actual-weight" onkeyup="calculateDeviation(this)"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Deviation Weight" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDeviation" runat="server" CssClass="deviation-weight form-control form-control-sm rounded"
                                                                                ReadOnly="true"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Deviation Percentage" HeaderStyle-Width="15%">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDeviationPercentage" runat="server" CssClass="deviation-percentage form-control form-control-sm rounded"
                                                                                ReadOnly="true"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                </Columns>
                                                            </asp:GridView>
                                                            <asp:Button ID="WgtbtnSubmit" runat="server" CssClass="btn btn-primary btn-sm" ValidationGroup="SubmitRM" CausesValidation="false" Enabled="false" Text="Proceed Next" OnClientClick="return validateGridView() && collectAndSendData();" OnClick="WgtbtnSubmit_Click" />
                                                            <asp:Button ID="WgtbtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="WgtbtnReset_Click" />
                                                        </div>
                                                    </div>
                                                    <%-- Weight ends Starts Here--%>

                                                    <%--Sponge Data Start Here--%>
                                                    <div class="tab-pane fade" id="spongeData" role="tabpanel" aria-labelledby="spongeData-tab">
                                                        <div class="x-content">


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelRoomTemp" runat="server" AssociatedControlID="TB_RoomTemp" Text="Room Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_RoomTemp" runat="server" ErrorMessage="*" ValidationGroup="SpongeSubmit" ControlToValidate="TB_RoomTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_RoomTemp" runat="server" ValidationGroup="SpongeSubmit" ControlToValidate="TB_RoomTemp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RoomTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Room Temp." oninput="toggleRoomTempRemarksDiv(this);"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="RoomTempRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label15" runat="server" AssociatedControlID="TXB_RoomTemp_Remarks" Text="Room Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_RoomTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="SpongeSubmit" ControlToValidate="TXB_RoomTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_RoomTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDrumCovered" runat="server" AssociatedControlID="RBL_DrumCovered" Text="Sponge Drum Covered :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DrumCovered" runat="server" ValidationGroup="SpongeSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_DrumCovered" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_DrumCovered" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleDrumCoveredRemarksDiv(this);">
                                                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DrumCoveredRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label16" runat="server" AssociatedControlID="TXB_DrumCovered_Remarks" Text="Sponge Drum Covered (No)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DrumCoveredRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="SpongeSubmit" ControlToValidate="TXB_DrumCovered_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DrumCovered_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelQuality" runat="server" AssociatedControlID="RBL_Quality" Text="Quality :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_Quality" runat="server" ValidationGroup="SpongeSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_Quality" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_Quality" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleQualityRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="QualityRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label17" runat="server" AssociatedControlID="TXB_Quality_Remarks" Text="Quality (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_QualityRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="SpongeSubmit" ControlToValidate="TXB_Quality_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_Quality_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelStandingTime" runat="server" AssociatedControlID="TB_StandingTime" Text="Standing Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_Time" runat="server" ErrorMessage="Standing Time is required " ControlToValidate="TB_StandingTime" ValidationGroup="SpongeSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_StandingTime" runat="server" ControlToValidate="TB_StandingTime" ForeColor="Red" ValidationGroup="SpongeSubmit" Display="Dynamic" ErrorMessage="Please enter time in hhmmsss format" ValidationExpression="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StandingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="hh:mm:ss" MaxLength="8" oninput="formatTimeInput(event)"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="StandingTimeRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label77" runat="server" AssociatedControlID="TXB_StandingTime_Remarks" Text="Standing Time Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_StandingTimeRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="SpongeSubmit" ControlToValidate="TXB_StandingTime_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_StandingTime_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelTemp" runat="server" AssociatedControlID="TB_Temp" Text="Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_Temp" runat="server" ErrorMessage="*" ValidationGroup="SpongeSubmit" ControlToValidate="TB_Temp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_Temp" runat="server" ValidationGroup="SpongeSubmit" ControlToValidate="TB_Temp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Temp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Temp. " oninput="toggleTempRemarksDIV(this);"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="TempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label18" runat="server" AssociatedControlID="TXB_Temp_Remarks" Text="Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="SpongeSubmit" ControlToValidate="TXB_Temp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_Temp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_SpongeBtnSubmit" runat="server" AssociatedControlID="SpongeBtnSubmit" Text="Click to Proceed" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="SpongeBtnSubmit" runat="server" Text="Proceed Next" CssClass="btn btn-success btn-sm" Enabled="false" ValidationGroup="SpongeSubmit" CausesValidation="false" OnClick="SpongeBtnSubmit_Click" />
                                                                        <asp:Button ID="SpongeBtnNotApplicable" runat="server" Text="N/A or SKIP" CssClass="btn btn-primary btn-sm" CausesValidation="false" OnClick="SpongeBtnNotApplicable_Click" />
                                                                        <asp:Button ID="Spongebtn_Reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="Spongebtn_Reset_Click" />

                                                                    </div>
                                                                </div>
                                                            </div>

                                                        </div>
                                                    </div>
                                                    <%-- Sponge Data Ends Here--%>

                                                    <%--Dough Data Start Here--%>
                                                    <div class="tab-pane fade" id="doughData" role="tabpanel" aria-labelledby="doughData-tab">
                                                        <div class="x-content">


                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughTemp" runat="server" AssociatedControlID="TB_DoughTemp" Text="Dough Temp :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughTemp" runat="server" ErrorMessage="*" ValidationGroup="DoughSubmit" ControlToValidate="TB_DoughTemp" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_DoughTemp" runat="server" ValidationGroup="DoughSubmit" ControlToValidate="TB_Temp" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DoughTemp" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Temp." oninput="toggleDoughTempRemarksDIV(this);"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DoughTempRemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label19" runat="server" AssociatedControlID="TXB_DoughTemp_Remarks" Text="Dough Temp. Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughTempRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_DoughTemp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DoughTemp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughRestTime" runat="server" AssociatedControlID="TB_DoughRestTime" Text="Dough Rest Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughRestTime" runat="server" ErrorMessage="Dough Rest Time is required " ControlToValidate="TB_DoughRestTime" ValidationGroup="DoughSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_DoughRestTime" runat="server" ControlToValidate="TB_DoughRestTime" ForeColor="Red" ValidationGroup="DoughSubmit" ErrorMessage="Please enter time in hhmmsss format" ValidationExpression="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DoughRestTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="hh:mm:ss" MaxLength="8" oninput="formatTimeInput(event)"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DoughRestTimeRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label70" runat="server" AssociatedControlID="TXB_DoughRestTime_Remarks" Text="Dough Remarks Temp . Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughRestTimeRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_DoughRestTime_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DoughRestTime_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMetalDectector" runat="server" AssociatedControlID="RBL_MetalDectector" Text="Metal Detector Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MetalDectector" runat="server" ValidationGroup="DoughSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_MetalDectector" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_MetalDectector" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleMetalDetectorRemarksDiv(this);">
                                                                            <asp:ListItem Text="Yes" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="No" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MetalDetectorRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label20" runat="server" AssociatedControlID="TXB_MetalDetector_Remarks" Text="Condition (No)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MetalDetectorRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_MetalDetector_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MetalDetector_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelProcessSequence" runat="server" AssociatedControlID="RBL_ProcessSequence" Text="Process Sequence :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_ProcessSequence" runat="server" ValidationGroup="DoughSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_MetalDectector" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_ProcessSequence" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleProcessSequenceRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="ProcessSequenceRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label21" runat="server" AssociatedControlID="TXB_ProcessSequence_Remarks" Text="Process Sequence (NoT Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_ProcessSequenceRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_ProcessSequence_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_ProcessSequence_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelCreamingTime" runat="server" AssociatedControlID="TB_CreamingTime" Text="Creaming Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_CreamingTime" runat="server" ErrorMessage="Creaming Time is required " ControlToValidate="TB_CreamingTime" ValidationGroup="DoughSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_CreamingTime" runat="server" ControlToValidate="TB_CreamingTime" ForeColor="Red" ValidationGroup="DoughSubmit" ErrorMessage="Please enter time in hhmmsss format" ValidationExpression="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_CreamingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="hh:mm:ss" MaxLength="8" oninput="formatTimeInput(event)"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="CreamingTimeRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label71" runat="server" AssociatedControlID="TXB_CreamingTime_Remarks" Text="CreamingTime Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_CreamingTimeRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_CreamingTime_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_CreamingTime_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelMixingTime" runat="server" AssociatedControlID="TB_MixingTime" Text="Mixing Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MixingTime" runat="server" ErrorMessage="Mixing Time is required " ControlToValidate="TB_MixingTime" ValidationGroup="DoughSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_MixingTime" runat="server" ControlToValidate="TB_MixingTime" ForeColor="Red" ValidationGroup="DoughSubmit" ErrorMessage="Please enter time in hhmmsss format" ValidationExpression="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MixingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="hh:mm:ss" MaxLength="8" oninput="formatTimeInput(event)"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="MixingTimeRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label72" runat="server" AssociatedControlID="TXB_MixingTime_Remarks" Text="Mixing Time Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_MixingTimeRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_MixingTime_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_MixingTime_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelBakingTime" runat="server" AssociatedControlID="TB_BakingTime" Text="Baking Time :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BakingTime" runat="server" ErrorMessage="Baking Time is required " ControlToValidate="TB_DoughRestTime" ValidationGroup="DoughSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_BakingTime" runat="server" ControlToValidate="TB_BakingTime" ForeColor="Red" ValidationGroup="DoughSubmit" ErrorMessage="Please enter time in hhmmsss format" ValidationExpression="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BakingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="hh:mm:ss" MaxLength="8" oninput="formatTimeInput(event)"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BakingTimeRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label73" runat="server" AssociatedControlID="TXB_BakingTime_Remarks" Text="Baking time Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BakingTimeRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_BakingTime_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BakingTime_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDiceRpm" runat="server" AssociatedControlID="TB_DoughTemp" Text="Dice RPM :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DiceRpm" runat="server" ErrorMessage="*" ValidationGroup="DoughSubmit" ControlToValidate="TB_DiceRpm" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_DiceRpm" runat="server" ValidationGroup="DoughSubmit" ControlToValidate="TB_DiceRpm" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DiceRpm" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Dice Rmp"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DiceRpmRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label74" runat="server" AssociatedControlID="TXB_DiceRpm_Remarks" Text="Dice Rpm Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DiceRpmRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_DiceRpm_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DiceRpm_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelDoughCondition" runat="server" AssociatedControlID="RBL_DoughCondition" Text="Condition Of Dough :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughCondition" runat="server" ValidationGroup="DoughSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_DoughCondition" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_DoughCondition" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleDoughConditionRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DoughConditionRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label22" runat="server" AssociatedControlID="TXB_DoughCondition_Remarks" Text="Dough Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DoughConditionRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="DoughSubmit" ControlToValidate="TXB_DoughCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_DoughCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="lbl_DoughBtnSubmit" runat="server" AssociatedControlID="DoughBtnSubmit" Text="Click to SAVE Dough Data" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="DoughBtnSubmit" runat="server" Text="Proceed Next" CssClass="btn btn-primary btn-sm" Enabled="false" ValidationGroup="DoughSubmit" CausesValidation="false" OnClick="DoughBtnSubmit_Click" />
                                                                        <asp:Button ID="DoughBtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="DoughBtnReset_Click" />
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="ovenData" role="tabpanel" aria-labelledby="ovenData-tab">
                                                        <div class="x-content">
                                                            <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Zone">
                                                                        <ItemTemplate>
                                                                            <span class="zone"><%# Eval("Zone") %></span>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Oven Top">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtOvenTop" runat="server" ClientIDMode="Static" Width="70%" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Oven Bottom">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtOvenBottom" runat="server" ClientIDMode="Static" Width="70%" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Damper Top">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDamperTop" runat="server" ClientIDMode="Static" Width="70%" CssClass="form-control form-control-sm rounded" onkeyup="calculateDamperBottom(this)"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Damper Bottom">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="txtDamperBottom" runat="server" ClientIDMode="Static" Width="70%" CssClass="form-control form-control-sm rounded damperBottom-percentage" ReadOnly="true"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>

                                                        </div>

                                                        <asp:Button ID="OvenBtnSubmit" runat="server" CssClass="btn btn-primary btn-sm" Text="Proceed Next" CausesValidation="false" Enabled="false" ValidationGroup="SubmitOven" OnClientClick="return validateGridView1() && collectAndSendData1(); " OnClick="OvenBtnSubmit_Click" />
                                                        <asp:Button ID="OvenBtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="OvenBtnReset_Click" />

                                                    </div>

                                                    <div class="tab-pane fade" id="verificationData" role="tabpanel" aria-labelledby="verificationData-tab">
                                                        <div class="x-content">

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label10" runat="server" AssociatedControlID="RBL_BalanceCondition" Text="Weighing Balance Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BalanceCondition" runat="server" ValidationGroup="VerifiedSubmit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_BalanceCondition" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_BalanceCondition" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="3" Width="100%" onchange="toggleBalanceConditionRemarksDiv(this);">
                                                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            <asp:ListItem Text="N/A" Value="5"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="BalanceConditionRemarksDiv" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label46" runat="server" AssociatedControlID="TXB_BalanceCondition_Remarks" Text="Weighing Balance Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_BalanceCondition_Remarks" runat="server" ErrorMessage="*" ValidationGroup="VerifiedSubmit" ForeColor="Red" ControlToValidate="TXB_BalanceCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TXB_BalanceCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="LabelRawBiscuitWgt" runat="server" AssociatedControlID="TB_RawBiscuitWgt" Text="Raw Biscuit Weight:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_RawBiscuitWgt" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_RawBiscuitWgt" ValidationGroup="VerifiedSubmit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_RawBiscuitWgt" runat="server" ControlToValidate="TB_RawBiscuitWgt" ForeColor="Red" ValidationGroup="VerifiedSubmit" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_RawBiscuitWgt" runat="server" ValidationGroup="VerifiedSubmit" ControlToValidate="TB_RawBiscuitWgt" ErrorMessage="[01.00 - 1000.00 gm]" ForeColor="Red" MinimumValue="01.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RawBiscuitWgt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Raw Biscuit Wgt" MaxLength="10"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_FinalBtnSubmit" runat="server" AssociatedControlID="FinalBtnSubmit" Text="Click to Submit" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="FinalBtnSubmit" runat="server" Text="Final Submit" CssClass="btn btn-primary btn-sm" Enabled="false" ValidationGroup="VerifiedSubmit" CausesValidation="false" OnClick="FinalBtnSubmit_Click" />
                                                                        <asp:Button ID="FinalBtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="FinalBtnReset_Click" />
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
