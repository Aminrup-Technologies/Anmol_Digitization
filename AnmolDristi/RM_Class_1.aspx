<%@ Page Title="AIL | QC RM - Class - I" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RM_Class_1.aspx.cs" Inherits="AnmolDristi.RM_Class_1" %>

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

            // Check if grade value is within the valid range
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

        function validateProteinValue(textbox) {
            console.log("validateProteinValue function called");

            // Retrieve elements and values
            var proteinValue = parseFloat(document.getElementById('<%= TB_Protein.ClientID %>').value);
            var remarksDiv = document.getElementById("ProteinRemarksDIV");
            var minProteinValue = parseFloat(document.getElementById('<%= hdnMinProteinValue.ClientID %>').value);
            var maxProteinValue = parseFloat(document.getElementById('<%= hdnMaxProteinValue.ClientID %>').value);

            // Define the valid range
            //var minProteinValue = 20.00;
            //var maxProteinValue = 30.00;

            // Check if Protein value is within the valid range
            if (!isNaN(proteinValue) && proteinValue !== "" && proteinValue < minProteinValue || proteinValue > maxProteinValue) {
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

        function validateDensityValue(textbox) {
            console.log("validateDensityValue function called");

            // Retrieve elements and values
            var densityValue = parseFloat(document.getElementById('<%= TB_Density.ClientID %>').value);
            var remarksDiv = document.getElementById("DensityRemarksDIV");
            var minDensityValue = parseFloat(document.getElementById('<%= hdnMinDensityValue.ClientID %>').value);
            var maxDensityValue = parseFloat(document.getElementById('<%= hdnMaxDensityValue.ClientID %>').value);

            // Define the valid range
            //var minDensityValue = 20.00;
            //var maxDensityValue = 30.00;

            // Check if Densityvalue is within the valid range
            if (!isNaN(densityValue) && densityValue !== "" && densityValue < minDensityValue || densityValue > maxDensityValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateFatValue(textbox) {
            console.log("validateDensityValue function called");

            // Retrieve elements and values
            var fatContentValue = parseFloat(document.getElementById('<%= TB_FatContent.ClientID %>').value);
            var remarksDiv = document.getElementById("FatContentRemarksDIV");
            var minFatContentValue = parseFloat(document.getElementById('<%= hdnMinFatValue.ClientID %>').value);
            var maxFatContentValue = parseFloat(document.getElementById('<%= hdnMaxFatValue.ClientID %>').value);

            // Define the valid range
            //var minFatContentValue = 20.00;
            //var maxFatContentValue = 30.00;

            // Check if fatContentValue is within the valid range
            if (!isNaN(fatContentValue) && fatContentValue !== "" && fatContentValue < minFatContentValue || fatContentValue > maxFatContentValue) {
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

        function validateSugarValue(textbox) {
            console.log("validateSugarValue function called");

            // Retrieve elements and values
            var sugarValue = parseFloat(document.getElementById('<%= TB_ReducingSugar.ClientID %>').value);
            var remarksDiv = document.getElementById("SugarRemarksDIV");
            var minSugarValue = parseFloat(document.getElementById('<%= hdnMinSugarValue.ClientID %>').value);
            var maxSugarValue = parseFloat(document.getElementById('<%= hdnMaxSugarValue.ClientID %>').value);

            // Define the valid range
            //var minSugarValue = 20.00;
            //var maxSugarValue = 30.00;

            // Check if SugarValue is within the valid range
            if (!isNaN(sugarValue) && sugarValue !== "" && sugarValue < minSugarValue || sugarValue > maxSugarValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSyrupValue(textbox) {
            console.log("validateSyrupValue function called");
            // Retrieve elements and values
            var syrupValue = parseFloat(document.getElementById('<%= TB_DrainableSyrup.ClientID %>').value);
            var remarksDiv = document.getElementById("SyrupRemarksDIV");
            var minSyrupValue = parseFloat(document.getElementById('<%= hdnMinSyrupValue.ClientID %>').value);
            var maxSyrupValue = parseFloat(document.getElementById('<%= hdnMaxSyrupValue.ClientID %>').value);

            // Define the valid range
            //var minSyrupValue = 20.00;
            //var maxSyrupValue = 30.00;

            // Check if SyrupValue is within the valid range
            if (!isNaN(syrupValue) && syrupValue !== "" && syrupValue < minSyrupValue || syrupValue > maxSyrupValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateSeedsValue(sender, args) {
            console.log("validateSeedsValue function called");

            // Retrieve elements and values
            var seedsValue = parseFloat(document.getElementById('<%= TB_Matured_Immatured_Seeds.ClientID %>').value);
            var remarksDiv = document.getElementById("SeedsRemarksDIV");
            var minSeedsValue = parseFloat(document.getElementById('<%= hdnMinSeedValue.ClientID %>').value);
            var maxSeedsValue = parseFloat(document.getElementById('<%= hdnMaxSeedValue.ClientID %>').value);

            // Define the valid range
            //var minSeedsValue = 20.00;
            //var maxSeedsValue = 30.00;

            // Check if SeedsValue is within the valid range
            if (!isNaN(seedsValue) && seedsValue !== "" && seedsValue < minSeedsValue || seedsValue > maxSeedsValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateBrixValue(sender, args) {
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

        function validateTSValue(sender, args) {
            console.log("validateTSValue function called");

            // Retrieve elements and values
            var tsValue = parseFloat(document.getElementById('<%= TB_TS.ClientID %>').value);
            var remarksDiv = document.getElementById("BrixRemarksDIV");
            var minTSValue = parseFloat(document.getElementById('<%= hdnMinTSValue.ClientID %>').value);
            var maxTSValue = parseFloat(document.getElementById('<%= hdnMaxTSValue.ClientID %>').value);

            // Define the valid range
            //var minTSValue = 20.00;
            //var maxTSValue = 30.00;

            // Check if TSValue is within the valid range
            if (!isNaN(tsValue) && tsValue !== "" && tsValue < minTSValue || tsValue > maxTSValue) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
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

        //function toggleAppStatusRemarksDiv(radioButtonList) {
        //    console.log("toggleAppStatusRemarksDiv function called");
        //    var selectedValue = radioButtonList.querySelector("input:checked").value;
        //    var remarksDiv = document.getElementById("AppStatusRemarksDiv");
        //    console.log("Selected value: " + selectedValue);
        //    if (selectedValue === "0") { // "Rejected" selected
        //        remarksDiv.style.display = "block";
        //        setTimeout(function () {
        //            // Display a PNotify notification
        //            new PNotify({
        //                title: 'A Mail will be sent',
        //                text: 'You have selected "Rejected". Please provide additional remarks.',
        //                type: 'warning',
        //                styling: 'bootstrap3'
        //            });
        //        }, 200); // Adjust delay as necessary
        //    } else {
        //        remarksDiv.style.display = "none";
        //    }
        //}

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

    </script>

    <asp:HiddenField ID="hdn_formid" runat="server" />

    <asp:HiddenField ID="hdnMinSupplierValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSupplierValue" runat="server" />

    <asp:HiddenField ID="hdnMinQtyValue" runat="server" />
    <asp:HiddenField ID="hdnMaxQtyValue" runat="server" />

    <asp:HiddenField ID="hdnMinImpuritiesValue" runat="server" />
    <asp:HiddenField ID="hdnMaxImpuritiesValue" runat="server" />

    <asp:HiddenField ID="hdnMinPhValue" runat="server" />
    <asp:HiddenField ID="hdnMaxPhValue" runat="server" />

    <asp:HiddenField ID="hdnMinMoistureValue" runat="server" />
    <asp:HiddenField ID="hdnMaxMoistureValue" runat="server" />

    <asp:HiddenField ID="hdnMinProteinValue" runat="server" />
    <asp:HiddenField ID="hdnMaxProteinValue" runat="server" />

    <asp:HiddenField ID="hdnMinTotalAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTotalAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinInsolubleAshValue" runat="server" />
    <asp:HiddenField ID="hdnMaxInsolubleAshValue" runat="server" />

    <asp:HiddenField ID="hdnMinDensityValue" runat="server" />
    <asp:HiddenField ID="hdnMaxDensityValue" runat="server" />

    <asp:HiddenField ID="hdnMinFatValue" runat="server" />
    <asp:HiddenField ID="hdnMaxFatValue" runat="server" />

    <asp:HiddenField ID="hdnMinTotalSolidValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTotalSolidValue" runat="server" />

    <asp:HiddenField ID="hdnMinSyrupValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSyrupValue" runat="server" />

    <asp:HiddenField ID="hdnMinSugarValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSugarValue" runat="server" />

    <asp:HiddenField ID="hdnMinSeedValue" runat="server" />
    <asp:HiddenField ID="hdnMaxSeedValue" runat="server" />

    <asp:HiddenField ID="hdnMinBrixValue" runat="server" />
    <asp:HiddenField ID="hdnMaxBrixValue" runat="server" />

    <asp:HiddenField ID="hdnMinShapeSizeValue" runat="server" />
    <asp:HiddenField ID="hdnMaxShapeSizeValue" runat="server" />

    <asp:HiddenField ID="hdnMinTSValue" runat="server" />
    <asp:HiddenField ID="hdnMaxTSValue" runat="server" />

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
                            </div>--%>

                            <%--<div class="col-md-3" id="CategoryDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_ProductCategory" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" InitialValue="0" ValidationGroup="Submit" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>--%>

                            <%--<div class="col-md-3" id="BrandDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_ProductBrand" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>--%>

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
                                    <asp:Label ID="Lbl_TB_BrandName" runat="server" AssociatedControlID="TB_BrandName" Text="Brand Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BrandName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_BrandName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BrandName" runat="server" ControlToValidate="TB_BrandName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BrandName" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Text="" Placeholder="Brand Name (3-30 characters)" MaxLength="50"></asp:TextBox>
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

                            <div class="col-md-3" id="QuantityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Quantity" runat="server" AssociatedControlID="TB_Quantity" Text="Quantity :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" ValidationGroup="Submit" runat="server" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Quantity" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [1000.00-2000.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Quantity Value(in kg) " oninput="validateQuantityValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="ImpuritiesDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Foreign_Impurities" runat="server" AssociatedControlID="TB_Foreign_Impurities" Text="Foreign Matter/Impurities :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Foreign_Impurities" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Foreign_Impurities" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Foreign_Impurities" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Foreign_Impurities" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Foreign_Impurities" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="ForeignMatter/Impurities (3-20 characters)" MaxLength="20"></asp:TextBox>
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

                            <div class="col-md-3" id="ProteinDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Protein" runat="server" AssociatedControlID="TB_Protein" Text="Protein(%):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Protein" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Protein" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Protein" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Protein" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Protein" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Protein" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Protein Value " oninput="validateProteinValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ProteinRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Protein_Remarks" runat="server" AssociatedControlID="TXB_Protein_Remarks" Text="Protein Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Protein_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Protein_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Protein_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AshDIV" runat="server">
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
                                    <asp:Label ID="Lbl_TB_InsolubleAsh" runat="server" AssociatedControlID="TB_InsolubleAsh" Text="Insoluble Ash:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
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
                                    <asp:Label ID="Label_TXB_InsolubleAsh_Remarks" runat="server" AssociatedControlID="TXB_InsolubleAsh_Remarks" Text="Insoluble Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_InsolubleAsh_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_InsolubleAsh_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_InsolubleAsh_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DensityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Density" runat="server" AssociatedControlID="TB_Density" Text=" Density Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Density" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Density" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Density" runat="server" ControlToValidate="TB_Density" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Density" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Density" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Density Value " oninput="validateDensityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DensityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Density_Remarks" runat="server" AssociatedControlID="TXB_Density_Remarks" Text="Density Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Density_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Density_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Density_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FatContentDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_FatContent" runat="server" AssociatedControlID="TB_FatContent" Text="Fat Content:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_FatContent" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_FatContent" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_FatContent" runat="server" ControlToValidate="TB_FatContent" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_FatContent" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FatContent" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Fat Content Value " oninput="validateFatValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FatContentRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Fat_Remarks" runat="server" AssociatedControlID="TXB_Fat_Remarks" Text="Fat Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Fat_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Fat_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Fat_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SolidDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TotalSolid" runat="server" AssociatedControlID="TB_TotalSolid" Text="Total Solids:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TotalSolid" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_TotalSolid" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TotalSolid" runat="server" ControlToValidate="TB_TotalSolid" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TotalSolid" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TotalSolid" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Total Solids Value " oninput="validateSolidValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="SugarDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ReducingSugar" runat="server" AssociatedControlID="TB_ReducingSugar" Text="Reducing Sugar As Maltose:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ReducingSugar" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_ReducingSugar" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ReducingSugar" runat="server" ControlToValidate="TB_ReducingSugar" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_ReducingSugar" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ReducingSugar" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Reducing Sugar Value " oninput="validateSugarValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SugarRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Sugar_Remarks" runat="server" AssociatedControlID="TXB_Sugar_Remarks" Text="Reducing Sugar Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Sugar_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Sugar_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Sugar_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SyrupDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DrainableSyrup" runat="server" AssociatedControlID="TB_DrainableSyrup" Text=" Drainable Syrup :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DrainableSyrup" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DrainableSyrup" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DrainableSyrup" runat="server" ControlToValidate="TB_DrainableSyrup" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_DrainableSyrup" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DrainableSyrup" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Drainable Syrup Value " oninput="validateSyrupValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SyrupRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Syrup_Remarks" runat="server" AssociatedControlID="TXB_Syrup_Remarks" Text="Drainable Syrup Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Syrup_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Syrup_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Syrup_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SeedsDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Matured_Immatured_Seeds" runat="server" AssociatedControlID="TB_Matured_Immatured_Seeds" Text="No. Of Matured Immatured Seeds" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Matured_Immatured_Seeds" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Matured_Immatured_Seeds" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Matured_Immatured_Seeds" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Matured_Immatured_Seeds" ForeColor="Red" ErrorMessage="Integer Only" ValidationExpression="^[1-9]\d*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_Matured_Immatured_Seeds" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [2 -4]kg" Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Matured_Immatured_Seeds" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="No. Of Seeds Value(in kg) " oninput="validateSeedsValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SeedsRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTXB_Seeds_Remarks" runat="server" AssociatedControlID="TXB_Seeds_Remarks" Text="No. Of Matured Immatured Seeds Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Seeds_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Seeds_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Seeds_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
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

                            <div class="col-md-3" id="ShapeOrSizeDIV" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ShapeOrSize" runat="server" AssociatedControlID="TB_ShapeOrSize" Text="ShapeOrSize  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ShapeOrSize" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_ShapeOrSize" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ShapeOrSize" runat="server" ValidationGroup="Submit" ControlToValidate="TB_ShapeOrSize" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_ShapeOrSize" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [2.00-3.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ShapeOrSize" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="ShapeOrSize(in mm)" oninput="validateShapeOrSizeValue(this);"></asp:TextBox>
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

                            <div class="col-md-3" id="TSDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TS" runat="server" AssociatedControlID="TB_TS" Text="TS:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TS" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_TS" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TS" runat="server" ControlToValidate="TB_TS" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_TS" runat="server" ValidationGroup="Submit" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TS" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="TS Value " oninput="validateTsValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TSRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_TS_Remarks" runat="server" AssociatedControlID="TXB_TS_Remarks" Text="TS Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_TS_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TS_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_TS_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppStatusDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppStatus" runat="server" AssociatedControlID="RBL_AppStatus" Text="Approval Status :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_AppStatus" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="RBL_AppStatus" Display="Dynamic"></asp:RequiredFieldValidator>
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


                            <%--<div class="col-md-3" id="AppStatusRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelAppStatusRemarks" runat="server" AssociatedControlID="TXB_AppStatus_Remarks" Text="Application Status (Rejected)" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_AppStatus_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_AppStatus_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_AppStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>


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
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="BtnSubmit_Click1" />
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
