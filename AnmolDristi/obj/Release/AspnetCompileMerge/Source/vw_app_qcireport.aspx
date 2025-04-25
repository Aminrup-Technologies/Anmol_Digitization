<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="vw_app_qcireport.aspx.cs" Inherits="AnmolDristi.vw_app_qcireport" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
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
        function toggleRemarksDiv1(radioButtonList) {
            console.log("showTextbox function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("FlavTxt_RemarksDIV");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateForm1() {
            var fileUpload = document.getElementById('<%= FU_DesgImp.ClientID %>');
            var lblErrorMessage1 = document.getElementById('<%= lblErrorMessage1.ClientID %>');
            if (fileUpload.files.length === 0) {
                lblErrorMessage1.innerHTML = "Please upload file.";
                return false;
            } else {
                lblErrorMessage1.innerHTML = "";
                return true;
            }
        }

        <%--function validateForm2() {
            var fileUpload = document.getElementById('<%= FU_DesgImp.ClientID %>');
            var lblErrorMessage2 = document.getElementById('<%= lblErrorMessage2.ClientID %>');
            if (fileUpload.files.length === 0) {
                lblErrorMessage2.innerHTML = "Please upload file.";
                return false;
            } else {
                lblErrorMessage2.innerHTML = "";
                return true;
            }
        }--%>

        function displayImage(input) {
            if (input.files && input.files[0]) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    document.getElementById('<%= uploadedImage1.ClientID %>').src = e.target.result;
                    document.getElementById('FU_DesgImp_Img').style.display = 'block';
                };
                reader.readAsDataURL(input.files[0]);
                }
            }

            function validateForm2() {
                var fileInput = document.getElementById('<%= FU_DesgImp.ClientID %>');
                if (fileInput.value === "") {
                    document.getElementById('<%= lblErrorMessage2.ClientID %>').innerText = "Please upload file.";
                    return false;
                }
                document.getElementById('<%= lblErrorMessage2.ClientID %>').innerText = "";
                return true;
            }

            function toggleSpSzRemarksDiv1(radioButtonList) {
                console.log("toggleSpSzRemarksDiv function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                var remarksDiv = document.getElementById("SpSz_RemarksDIV");
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }
            }

            function toggleTextureBiteRemarksDiv(radioButtonList) {
                console.log("toggleTextureBiteRemarksDiv function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                var remarksDiv = document.getElementById("TextureBiteRemarksDiv");
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }
            }


            function toggleRemarksDiv(radioButtonList) {
                console.log("showTextbox function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                var remarksValidator = document.getElementById('<%= RFV_RBL_FlavTst_Rmrks.ClientID %>');
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    FlavTxt_RemarksDIV.style.display = "block";
                    remarksValidator.validationGroup = "Submit";
                } else {
                    FlavTxt_RemarksDIV.style.display = "none";
                    remarksValidator.validationGroup = "";
                }
            }

            function toggleSpSzRemarksDiv(radioButtonList) {
                console.log("toggleSpSzRemarksDiv function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    SpSz_RemarksDIV.style.display = "block";
                } else {
                    SpSz_RemarksDIV.style.display = "none";
                }
            }

            function toggleColorAppRemarksDiv(radioButtonList) {
                console.log("toggleColorAppRemarksDiv function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                var remarksDiv = document.getElementById("ColorAppRemarksDiv");
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }
            }


            function toggleTextureBiteRemarksDiv(radioButtonList) {
                console.log("toggleTextureBiteRemarksDiv function called");
                var selectedValue = radioButtonList.querySelector("input:checked").value;
                var remarksDiv = document.getElementById("TextureBiteRemarksDiv");
                console.log("Selected value: " + selectedValue);
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }
            }



            function validateSubmit() {
                var fileUpload = document.getElementById('<%= FU_ClrApp.ClientID %>'); // Get the FileUpload control
                var errorMessageLabel = document.getElementById('<%= lblErrorMessage1.ClientID %>'); // Get the error message label

                if (fileUpload.value === "") {
                    errorMessageLabel.innerHTML = "Please select a file before submitting."; // Display error message
                    errorMessageLabel.style.color = "red"; // Change color to red
                    return false; // Prevent form submission
                }

                // File is selected, return true to allow form submission
                return true;
            }

            // Function to calculate oil percentage
            function calculateOilPercentage() {
                var weightWithOilInput = parseFloat(document.getElementById('<%= TB_wgtwoil.ClientID %>').value) || 0;
                var weightWithoutOilInput = parseFloat(document.getElementById('<%= TB_wgtwtoil.ClientID %>').value) || 0;
                if (weightWithOilInput && weightWithoutOilInput) {
                    var oilPercentage = ((weightWithOilInput - weightWithoutOilInput) / weightWithOilInput) * 100;
                    console.log("Weight with oil input:", oilPercentage);
                    document.getElementById('<%= TB_oilpercent.ClientID %>').value = oilPercentage.toFixed(2);
                } else {
                    console.error("One or more input elements not found.");
                }
            }


        <%--function validateDryWeight(sender, args) {
            console.log("validateDryWeight function called");
            var remarks = document.getElementById('<%= TXB_DryWeight_Remarks.ClientID %>').value;
            var dryWeight = parseFloat(document.getElementById('<%= TB_DryWeight.ClientID %>').value);
            if (isNaN(dryWeight) || dryWeight < 42.00 || dryWeight > 43.00) {
                args.IsValid = false;
                document.getElementById('DryWeightRemarksDIV').style.display = 'block'; // Show the remarks div
                
                //showNotification('Error', 'Dry weight must be between ' + minDryWeight + ' and ' + maxDryWeight + ' or remarks must be provided!', 'error');
                if (remarks.trim() === '') {
                    // Remarks are not provided
                    remarks.validationGroup = "Submit";
                    showNotification('Error', 'Dry weight must be between [42.00 to 43.00]!', 'error');
                    showNotification('Error', 'Remarks must be provided!', 'error');
                    return false;
                } else {
                    // Remarks are provided
                    return true;
                }
                
            } else {
                args.IsValid = true;
                document.getElementById('DryWeightRemarksDIV').style.display = 'none'; // Hide the remarks div
            }
        }--%>

        <%--function validateDryWeight(sender, args) {
            console.log("validateDryWeight function called");

            // Retrieve elements and values
            var remarks = document.getElementById('<%= TXB_DryWeight_Remarks.ClientID %>').value;
            var dryWeight = parseFloat(document.getElementById('<%= TB_DryWeight.ClientID %>').value);

            // Check if dry weight field is empty
            if (dryWeight === '' || isNaN(dryWeight)) {
                args.IsValid = false;
                showNotification('Error', 'Dry weight is required.', 'error');
                return false;
            } else {
                // Define the valid range
                var minDryWeight = 42.00;
                var maxDryWeight = 43.00;

                // Check if dry weight is within the valid range
                if (dryWeight < minDryWeight || dryWeight > maxDryWeight) {
                    args.IsValid = false;
                    document.getElementById('DryWeightRemarksDIV').style.display = 'block'; // Show the remarks div

                    // Check if remarks are provided
                    if (remarks.trim() === '') {
                        // Remarks are not provided, show error
                        showNotification('Error', 'Dry weight must be between ' + minDryWeight + ' and ' + maxDryWeight + ' and reason must be provided!', 'error');
                        return false;
                    } else {
                        // Remarks are provided, allow submission
                        args.IsValid = true;
                        return true;
                    }
                } else {
                    // Dry weight is within the valid range, hide remarks div
                    args.IsValid = true;
                    document.getElementById('DryWeightRemarksDIV').style.display = 'none';
                    return true;
                }
            }
        }--%>


        function validateShapeSize(sender, args) {
            console.log("validateShapeSize function called");

            // Retrieve elements and values
            var remarks = document.getElementById('<%= TXB_ShapeSize_Remarks.ClientID %>').value;
            var shapeSize = parseFloat(document.getElementById('<%= TB_ShapeSize.ClientID %>').value);
            var minShapeSize = parseFloat(document.getElementById('<%= hdnMinShapeSize.ClientID %>').value);
            var maxShapeSize = parseFloat(document.getElementById('<%= hdnMaxShapeSize.ClientID %>').value);

            // Check if shape size field is empty
            if (shapeSize === '' || isNaN(shapeSize)) {
                args.IsValid = false;
                showNotification('Error', 'Shape size is required.', 'error');
                return false;
            } else {
                // Define the valid range
                //var minShapeSize = 5.00;
                //var maxShapeSize = 10.00;

                // Check if shape size is within the valid range
                if (shapeSize <= minShapeSize || shapeSize >= maxShapeSize) {
                    args.IsValid = false;
                    document.getElementById('ShapeSizeRemarksDIV').style.display = 'block'; // Show the remarks div

                    // Check if remarks are provided
                    if (remarks.trim() === '') {
                        // Remarks are not provided, show error
                        showNotification('Error', 'Shape size must be between ' + minShapeSize + ' and ' + maxShapeSize + ' and reason must be provided!', 'error');
                        return false;
                    } else {
                        // Remarks are provided, allow submission
                        args.IsValid = true;
                        return true;
                    }
                } else {
                    // Shape size is within the valid range, hide remarks div
                    args.IsValid = true;
                    document.getElementById('ShapeSizeRemarksDIV').style.display = 'none';
                    return true;
                }
            }
        }


        //function toggleDesignImpRemarksDiv(radioButtonList) {
        //    console.log("toggleDesignImpRemarksDiv function called");
        //    var selectedValue = radioButtonList.querySelector("input:checked").value;
        //    var remarksDiv = document.getElementById("DesignImpRemarksDiv");
        //    console.log("Selected value: " + selectedValue);
        //    if (selectedValue === "0") {
        //        remarksDiv.style.display = "block";
        //    } else {
        //        remarksDiv.style.display = "none";
        //    }
        //}

        var remarksVisible = false;

        function toggleDesignImpRemarksDiv(radioButtonList) {
            console.log("toggleDesignImpRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("DesignImpRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                remarksVisible = true;
            } else {
                remarksDiv.style.display = "none";
                remarksVisible = false;
            }
        }

        function validateDesignImpRemarks(sender, args) {
            var remarksInput = document.getElementById("TXB_DesignImp_Remarks");
            args.IsValid = !remarksVisible || (remarksVisible && remarksInput.value.trim() !== "");
        }


        function validateGaugeLen(sender, args) {
            console.log("validateGaugeLen function called");

            // Retrieve elements and values
            var remarks = document.getElementById('<%= TXB_GaugeLen_Remarks.ClientID %>').value;
            var gaugeLen = parseFloat(document.getElementById('<%= TB_GaugeLen.ClientID %>').value);
            var minGaugeLen = parseFloat(document.getElementById('<%= hdnMinGaugelen.ClientID %>').value);
            var maxGaugeLen = parseFloat(document.getElementById('<%= hdnMaxGaugelen.ClientID %>').value);

            // Check if gauge length field is empty
            if (gaugeLen === '' || isNaN(gaugeLen)) {
                args.IsValid = false;
                showNotification('Error', 'Gauge length is required.', 'error');
                return false;
            } else {
                // Define the valid range
                //var minGaugeLen = 20.00;
                //var maxGaugeLen = 30.00;

                // Check if gauge length is within the valid range
                if (gaugeLen <= minGaugeLen || gaugeLen >= maxGaugeLen) {
                    args.IsValid = false;
                    document.getElementById('GaugeLenRemarksDIV').style.display = 'block'; // Show the remarks div

                    // Check if remarks are provided
                    if (remarks.trim() === '') {
                        // Remarks are not provided, show error
                        showNotification('Error', 'Gauge length must be between ' + minGaugeLen + ' and ' + maxGaugeLen + ' and reason must be provided!', 'error');
                        return false;
                    } else {
                        // Remarks are provided, allow submission
                        args.IsValid = true;
                        return true;
                    }
                } else {
                    // Gauge length is within the valid range, hide remarks div
                    args.IsValid = true;
                    document.getElementById('GaugeLenRemarksDIV').style.display = 'none';
                    return true;
                }
            }
        }




        <%--function validateDryWeightOnInput() {
            var dryWeightInput = document.getElementById('<%= TB_DryWeight.ClientID %>').value;
            var isValid = validateDryWeightValue(dryWeightInput);
            if (!isValid) {
                document.getElementById('DryWeightRemarksDIV').style.display = 'block'; // Show the remarks div
            } else {
                document.getElementById('DryWeightRemarksDIV').style.display = 'none'; // Hide the remarks div
            }
        }

        function validateDryWeightValue(inputValue) {
            var dryWeight = parseFloat(inputValue);
            if (isNaN(dryWeight) || dryWeight < 0.00 || dryWeight > 100.00) {
                return false;
            } else {
                return true;
            }
        }--%>


        function showNotification(title, text, type) {
            new PNotify({
                title: title,
                text: text,
                type: type,
                styling: 'bootstrap3'
            });
        }

    </script>

    <asp:HiddenField ID="hdn_img1" runat="server" />
    <asp:HiddenField ID="hdn_img2" runat="server" />

    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
    <asp:HiddenField ID="hdn_fromid" runat="server" />
    <asp:HiddenField ID="hdn_formname" runat="server" />
    <asp:HiddenField ID="hdnMinNoOfPcs" runat="server" />
    <asp:HiddenField ID="hdnMaxNoOfPcs" runat="server" />

    <asp:HiddenField ID="hdnMinShapeSize" runat="server" />
    <asp:HiddenField ID="hdnMaxShapeSize" runat="server" />

    <asp:HiddenField ID="hdnMinBakingTime" runat="server" />
    <asp:HiddenField ID="hdnMaxBakingTime" runat="server" />

    <asp:HiddenField ID="hdnMinBakingTime2" runat="server" />
    <asp:HiddenField ID="hdnMaxBakingTime2" runat="server" />

    <asp:HiddenField ID="hdnMinGaugelen" runat="server" />
    <asp:HiddenField ID="hdnMaxGaugelen" runat="server" />

    <asp:HiddenField ID="hdnMin" runat="server" />
    <asp:HiddenField ID="hdnMax" runat="server" />

    <asp:HiddenField ID="HiddenField1" runat="server" />
    <asp:HiddenField ID="HiddenField2" runat="server" />

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>

                                    </div>

                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_NoOfPcs_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_NoOfPcs" runat="server" AssociatedControlID="TB_NoOfPcs" Text="No of Pcs :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_NoOfPcs" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_NoOfPcs" Display="Dynamic" ForeColor="Red" InitialValue="0"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_NoOfPcs" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NoOfPcs" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_NoOfPcs" runat="server" ControlToValidate="TB_NoOfPcs" ErrorMessage="[10 - 40]" ForeColor="Red" MinimumValue="15" MaximumValue="40" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_NoOfPcs" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Number of Pieces [10 - 40]" Text="0"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DryWeight" runat="server" AssociatedControlID="TB_DryWeight" Text="Dry Weight :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DryWeight" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DryWeight" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DryWeight" runat="server" ValidationGroup="Submit" ControlToValidate="TB_DryWeight" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_DryWeight" runat="server" ClientValidationFunction="validateDryWeight" ErrorMessage="Input Range [42.00-43.00]." Display="Dynamic" ValidationGroup="Submit" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DryWeight" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Dry Weight [42.00-43.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3" id="DryWeightRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TB_DryWeight_Remarks" runat="server" AssociatedControlID="TXB_DryWeight_Remarks" Text="Dry Weight Deviation Remarks:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TBX_DryWeight_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_DryWeight_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_DryWeight_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DippedWeight" runat="server" AssociatedControlID="TB_DippedWeight" Text="Dipped Weight :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DippedWeight" runat="server" ErrorMessage="*" InitialValue="" ValidationGroup="Submit" ControlToValidate="TB_DippedWeight" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DippedWeight" runat="server" ControlToValidate="TB_DippedWeight" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_DippedWeight" runat="server" ControlToValidate="TB_DippedWeight" ErrorMessage="[0.00 - 1000.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DippedWeight" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Dipped Weight [0.00 - 1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="TB_VartyPkt_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VartyPkt" runat="server" AssociatedControlID="TB_VartyPkt" Text="Variety Packet / LOT No :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VartyPkt" runat="server" ErrorMessage="*" ControlToValidate="TB_VartyPkt" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VartyPkt" runat="server" ControlToValidate="TB_VartyPkt" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VartyPkt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Variety Packet (3-20 characters)" MaxLength="20" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_BakingTime_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BakingTime" runat="server" AssociatedControlID="TB_BakingTime" Text="Baking Time (mm:ss) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BakingTime" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BakingTime" Display="Dynamic" ForeColor="Red" InitialValue="00:00"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BakingTime" runat="server" ValidationGroup="Submit" ControlToValidate="TB_BakingTime" ForeColor="Red" ErrorMessage="Please enter time in mm:ss format" ValidationExpression="^([0-5][0-9]):([0-5][0-9])$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_BakingTime" runat="server" ControlToValidate="TB_BakingTime" ValidationGroup="Submit" ErrorMessage="[00:00 - 15:00]" ForeColor="Red" MinimumValue="00:00" MaximumValue="15:00" Type="String" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BakingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Baking Time (mm:ss)" Text="00:00"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_BakingTime2_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BakingTime2" runat="server" AssociatedControlID="TB_BakingTime2" Text="Baking Time 2 (mm:ss) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BakingTime2" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BakingTime2" Display="Dynamic" ForeColor="Red" InitialValue=""></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BakingTime2" runat="server" ValidationGroup="Submit" ControlToValidate="TB_BakingTime2" ForeColor="Red" ErrorMessage="Please enter time in mm:ss format" ValidationExpression="^([0-5][0-9]):([0-5][0-9])$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_BakingTime2" runat="server" ControlToValidate="TB_BakingTime2" ValidationGroup="Submit" ErrorMessage="[00:00 - 15:00]" ForeColor="Red" MinimumValue="00:00" MaximumValue="15:00" Type="String" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BakingTime2" runat="server" CssClass="form-control form-control-sm rounded" Text="00:00" Placeholder="Baking Time (mm:ss)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColorApp" runat="server" AssociatedControlID="RBL_ColorApp" Text="Color Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ColorApp" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_ColorApp" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ColorApp" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleColorAppRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ColorAppRemarksDiv" style="display: none;" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColorAppRemarks" runat="server" AssociatedControlID="TXB_ColorApp_Remarks" Text="Color Appearance (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ColorAppRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ColorApp_Remarks" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ColorApp_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_FlavTst" runat="server" AssociatedControlID="RBL_FlavTst" Text="Flavour & Taste :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_FlavTst" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_FlavTst" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_FlavTst" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FlavTxt_RemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label12" runat="server" AssociatedControlID="TXB_RBL_FlavTst_Rmrks" Text="Flavour & Taste (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_FlavTst_Rmrks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_RBL_FlavTst_Rmrks" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_RBL_FlavTst_Rmrks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label13" runat="server" AssociatedControlID="RBL_SpSz" Text="Shape & Size" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_SpSz" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_SpSz" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_SpSz" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSpSzRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SpSz_RemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label14" runat="server" AssociatedControlID="TB_RBL_SpSz_Rmrks" Text="Shape & Size (Not OK)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_RBL_SpSz_Rmrks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_RBL_SpSz_Rmrks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_RBL_SpSz_Rmrks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>

                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelDesignImp" runat="server" AssociatedControlID="RBL_DesignImp" Text="Design Implementation :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_DesignImp" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_DesignImp" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_DesignImp" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleDesignImpRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DesignImpRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelDesignImpRemarks" runat="server" AssociatedControlID="TXB_DesignImp_Remarks" Text="Design Implementation (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DesignImpRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_DesignImp_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="CV_DesignImpRemarks" runat="server" ErrorMessage="Remarks are required when 'Not Ok' is selected" ClientValidationFunction="validateDesignImpRemarks" Display="Dynamic"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_DesignImp_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_TextureBite" runat="server" AssociatedControlID="TB_TextureBite" Text="Texture Bite :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_TextureBite" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_TextureBite" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_TextureBite" runat="server" ValidationGroup="Submit" ControlToValidate="TB_TextureBite" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_TextureBite" runat="server" ValidationGroup="Submit" ControlToValidate="TB_TextureBite" ErrorMessage="Texture bite should be between 0.00 and 10.00" ForeColor="Red" MinimumValue="0.00" MaximumValue="10.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TextureBite" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Texture Bite (0.00-10.00)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_RBL_TextureBite" runat="server" AssociatedControlID="RBL_TextureBite" Text="Texture Bite :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_TextureBite" runat="server" ValidationGroup="Submit" ErrorMessage="*" ForeColor="Red" ControlToValidate="RBL_TextureBite" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_TextureBite" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleTextureBiteRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TextureBiteRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTextureBiteRemarks" runat="server" AssociatedControlID="TXB_TextureBite_Remarks" Text="Texture & Bite (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TextureBiteRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_TextureBite_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_TextureBite_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_ShapeSize_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ShapeSize" runat="server" AssociatedControlID="TB_ShapeSize" Text="Shape & Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ShapeSize" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_ShapeSize" Display="Dynamic" ForeColor="Red" InitialValue=""></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ShapeSize" runat="server" ValidationGroup="Submit" ControlToValidate="TB_ShapeSize" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_ShapeSize" runat="server" ErrorMessage="*" Display="Dynamic" ValidationGroup="Submit" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ShapeSize" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Shape Size [5.00-10.00]" Text="0.00"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ShapeSizeRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TB_ShapeSize_Remarks" runat="server" AssociatedControlID="TXB_ShapeSize_Remarks" Text="Shape Size Deviation Remarks:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TBX_ShapeSize_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_ShapeSize_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ShapeSize_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>



                            <div class="col-md-3" id="TB_Moisture_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Moisture" runat="server" AssociatedControlID="TB_Moisture" Text="Moisture (%):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Moisture" runat="server" ValidationGroup="Submit" ErrorMessage="*" ControlToValidate="TB_Moisture" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Moisture" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Moisture" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Moisture" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Moisture" ErrorMessage="Moisture should be between 0.00% and 100.00%" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Moisture" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Moisture (0.00% - 100.00%)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_aWMAX_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_aWMAX" runat="server" AssociatedControlID="TB_aWMAX" Text="aW(MAX) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_aWMAX" runat="server" ValidationGroup="Submit" ErrorMessage="*" ControlToValidate="TB_aWMAX" ForeColor="Red" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_aWMAX" ValidationGroup="Submit" runat="server" ControlToValidate="TB_aWMAX" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_aWMAX" runat="server" ValidationGroup="Submit" ControlToValidate="TB_aWMAX" ErrorMessage="[0.00 - 100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_aWMAX" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="aW (MAX) Value [0.00 - 100.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_pHvalue_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_pHvalue" runat="server" AssociatedControlID="TB_pHvalue" Text="pH Value:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_pHvalue" runat="server" ValidationGroup="Submit" ErrorMessage="*" ControlToValidate="TB_pHvalue" ForeColor="Red" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_pHvalue" ValidationGroup="Submit" runat="server" ControlToValidate="TB_pHvalue" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_pHvalue" runat="server" ValidationGroup="Submit" ControlToValidate="TB_pHvalue" ErrorMessage="[0.00 - 100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_pHvalue" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="aW (MAX) Value [0.00 - 100.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_Length_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Length" runat="server" AssociatedControlID="TB_Length" Text="Length (L) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Length" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Length" ForeColor="Red" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Length" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Length" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Length" runat="server" ControlToValidate="TB_Length" ErrorMessage="[100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Length" runat="server" CssClass="form-control form-control-sm rounded" Text="" Placeholder="Length of the Product"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_Breadth_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Breadth" runat="server" AssociatedControlID="TB_Breadth" Text="Breadth (B) / Width (W) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Breadth" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Breadth" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Breadth" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Breadth" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Breadth" runat="server" ControlToValidate="TB_Breadth" ErrorMessage="[100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Breadth" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="[100.00]"></asp:TextBox>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_Height_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Height" runat="server" AssociatedControlID="TB_Height" Text="Height (H) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Height" runat="server" ErrorMessage="*" InitialValue="" ValidationGroup="Submit" ControlToValidate="TB_Height" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Height" runat="server" ControlToValidate="TB_Height" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Height" runat="server" ControlToValidate="TB_Height" ErrorMessage="[100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Height" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="[100.00]" Text="0.00"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TB_GaugeLen_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GaugeLen" runat="server" AssociatedControlID="TB_GaugeLen" Text="Gauge Length :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GaugeLen" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_GaugeLen" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GaugeLen" runat="server" ValidationGroup="Submit" ControlToValidate="TB_GaugeLen" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_GaugeLen" runat="server" ClientValidationFunction="validateGaugeLen" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ValidationGroup="Submit" ForeColor="Red"></asp:CustomValidator>
                                    <%--<asp:CustomValidator ID="CV_TB_GaugeLen" runat="server" ErrorMessage="Input Range [20.00-30.00]." Display="Dynamic" ValidationGroup="Submit" ForeColor="Red"></asp:CustomValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GaugeLen" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Gauge Length [20.00-30.00]" Text="0.00"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GaugeLenRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TB_GaugeLen_Remarks" runat="server" AssociatedControlID="TXB_GaugeLen_Remarks" Text="Gauge Length Deviation Remarks:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TBX_GaugeLen_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GaugeLen_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GaugeLen_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3" id="TB_wgtwtoil_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_wgtwtoil" runat="server" AssociatedControlID="TB_wgtwtoil" Text="Weight without oil /Dry Weight (g):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_wgtwtoil" ValidationGroup="Submit" runat="server" ErrorMessage="*" ControlToValidate="TB_wgtwtoil" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_wgtwtoil" ValidationGroup="Submit" runat="server" ControlToValidate="TB_wgtwtoil" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_wgtwtoil" runat="server" ValidationGroup="Submit" ControlToValidate="TB_wgtwtoil" ErrorMessage="Weight with oil should be between 0.00 and 100.00 kg" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_wgtwtoil" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Weight with oil (0.00 - 100.00 g)" Text="0.00" oninput="calculateOilPercentage()"></asp:TextBox>
                                    </div>
                                </div>
                            </div>



                            <div class="col-md-3" id="TB_wgtwoil_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_wgtwoil" runat="server" AssociatedControlID="TB_wgtwoil" Text="Weight with oil / Dipped Weight (g):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_wgtwoil" runat="server" ValidationGroup="Submit" ErrorMessage="*" ControlToValidate="TB_wgtwoil" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_wgtwoil" runat="server" ValidationGroup="Submit" ControlToValidate="TB_wgtwoil" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_wgtwoil" runat="server" ValidationGroup="Submit" ControlToValidate="TB_wgtwoil" ErrorMessage="Weight without oil should be between 0.00 and 1000.00 g" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_wgtwoil" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Weight without oil (0.00 - 1000.00 g)" Text="0.00" oninput="calculateOilPercentage()"></asp:TextBox>
                                    </div>
                                </div>
                            </div>




                            <div class="col-md-3" id="TB_oilpercent_DIV" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_oilpercent" runat="server" AssociatedControlID="TB_oilpercent" Text="Oil Percentage (%):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_oilpercent" runat="server" ValidationGroup="Submit" ErrorMessage="*" ControlToValidate="TB_oilpercent" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_oilpercent" runat="server" ValidationGroup="Submit" ControlToValidate="TB_oilpercent" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_oilpercent" runat="server" ValidationGroup="Submit" ControlToValidate="TB_oilpercent" ErrorMessage="0.00% and 50.00%" ForeColor="Red" MinimumValue="0.00" MaximumValue="50.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_oilpercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Oil Percentage (0.00% - 100.00%)" Text="0.00" ReadOnly="false"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_PktWgt" runat="server" AssociatedControlID="TB_PktWgt" Text="Packet Weight (g):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PktWgt" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_PktWgt" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PktWgt" runat="server" ValidationGroup="Submit" ControlToValidate="TB_PktWgt" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_PktWgt" runat="server" ValidationGroup="Submit" ControlToValidate="TB_PktWgt" ErrorMessage="Packet weight should be between 0.00 and 1000.00 g" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PktWgt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Packet Weight (0.00 - 1000.00 g)" Text="0.00"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <%--<div class="col-md-3" id="FU_DesgImp_Upldr" runat="server" visible="true">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_FU_DesgImp" runat="server" AssociatedControlID="FU_DesgImp" Text="Product Apperance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_FU_DesgImp" runat="server" ErrorMessage="*" ControlToValidate="FU_DesgImp" Display="Dynamic" ValidationGroup="ValidationGroup1" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="CV_FU_DesgImp" runat="server" ControlToValidate="FU_ClrApp" Display="Dynamic" ValidationGroup="ValidationGroup1" ErrorMessage="Please upload file"></asp:CustomValidator>
                                    <asp:Label ID="lblErrorMessage2" runat="server" CssClass="text-danger"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:FileUpload ID="FU_DesgImp" runat="server" CssClass="form-control rounded" onchange="displayImage(this);" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="BtnUploadFU_DesgImp" runat="server" CssClass="btn btn-primary btn-sm" Text="Upload" OnClientClick="return validateForm2();" OnClick="BtnUploadFU_DesgImp_Click" ValidationGroup="ValidationGroup1" CausesValidation="true" />
                                        </span>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3" id="FU_DesgImp_Upldr" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_FU_DesgImp" runat="server" AssociatedControlID="FU_DesgImp" Text="Product Appearance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_FU_DesgImp" runat="server" ErrorMessage="Product Photograph Required" ControlToValidate="FU_DesgImp" Display="Dynamic" ValidationGroup="Submit" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="CV_FU_DesgImp" runat="server" ControlToValidate="FU_DesgImp" Display="Dynamic" ValidationGroup="Submit" ErrorMessage="Please upload file"></asp:CustomValidator>
                                    <asp:Label ID="lblErrorMessage2" runat="server" CssClass="text-danger"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:FileUpload ID="FU_DesgImp" runat="server" CssClass="form-control rounded" onchange="displayImage(this);" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="BtnUploadFU_DesgImp" runat="server" CssClass="btn btn-primary btn-sm" Text="Upload" OnClientClick="return validateForm2();" ValidationGroup="ValidationGroup1" CausesValidation="true" />
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="FU_DesgImp_Img" runat="server" visible="false">
                                <div class="mb-6">
                                    <asp:Label ID="Label11" runat="server" AssociatedControlID="uploadedImage1" Text="Product Appearance" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Image ID="uploadedImage1" runat="server" CssClass="img-fluid" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="FU_ClrApp_Upldr" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_FU_ClrApp" runat="server" AssociatedControlID="FU_ClrApp" Text="Final Packet (Coding Zone)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_FU_ClrApp" runat="server" ErrorMessage="Product Photograph Required" ControlToValidate="FU_ClrApp" Display="Dynamic" ForeColor="Red" ValidationGroup="Submit"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="CV_FU_ClrApp" runat="server" ControlToValidate="FU_ClrApp" Display="Dynamic" ValidationGroup="Submit" ErrorMessage="Please upload at least one file"></asp:CustomValidator>
                                    <asp:Label ID="lblErrorMessage1" runat="server" CssClass="text-danger"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:FileUpload ID="FU_ClrApp" runat="server" CssClass="form-control rounded" />
                                        <span class="input-group-btn">
                                            <asp:Button ID="BtnUploadClrApp" runat="server" CssClass="btn btn-primary btn-sm" Text="Upload" OnClientClick="return validateForm1();" ValidationGroup="ValidationGroup2" />
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="FU_ClrApp_Img" runat="server" visible="false">
                                <div class="mb-6">
                                    <asp:Label ID="Label10" runat="server" AssociatedControlID="uploadedImage2" Text="Final Packet (Coding Zone)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Image ID="uploadedImage2" runat="server" CssClass="img-fluid" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="mb-12">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Re-Validate Inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="Submit" CausesValidation="true" />
                                        <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btnApprove_Click" />
                                        <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btnReject_Click" />
                                        <asp:Button ID="btnBack" runat="server" Text="Back" CssClass="btn btn-primary btn-sm" CausesValidation="false" OnClick="btnBack_Click"/>
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
                            <div class="approver-flow">
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label7" runat="server" Text="Approver 1" /></p>
                                    <asp:Image ID="Image3" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="Approver1NameLabel" runat="server" Font-Bold="true" /></p>
                                    <p>
                                        <asp:Label ID="Approver1CodeLabel" runat="server" Font-Bold="true" /></p>
                                </div>
                                <div class="flow-line"></div>
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label8" runat="server" Text="Approver 2" /></p>
                                    <asp:Image ID="Image2" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="Approver2NameLabel" runat="server" Font-Bold="true" /></p>
                                    <p>
                                        <asp:Label ID="Approver2CodeLabel" runat="server" Font-Bold="true" /></p>
                                </div>
                                <div class="flow-line"></div>
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label9" runat="server" Text="Approver 3" /></p>
                                    <asp:Image ID="Image1" runat="server" ImageUrl="~/WebData/No_Image.jpg" class="approver-photo" />
                                    <p>
                                        <asp:Label ID="DottedLineApproverNameLabel" runat="server" Font-Bold="true" /></p>
                                    <p>
                                        <asp:Label ID="DottedLineApproverCodeLabel" runat="server" Font-Bold="true" /></p>
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
