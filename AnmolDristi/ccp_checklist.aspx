<%@ Page Title="AIL | CCP Checklist" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ccp_checklist.aspx.cs" Inherits="AnmolDristi.ccp_checklist" %>

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

        function GridtoggleCleanedStatusRemarks(radioButtonList) {
            console.log("GridtoggleCleanedStatusRemarks function called");

            // Check the type of radioButtonList
            console.log("radioButtonList:", radioButtonList);

            var rbtnvalue = radioButtonList.querySelector("input:checked").value;
            // Check if the value is being accessed correctly
            console.log("Value of radioButtonList:", rbtnvalue);

            // Get the row containing the radio button list
            var row = radioButtonList.closest('tr');
            console.log("Row element:", row);

            // Ensure the row is found
            if (row) {
                // Find the remarks textbox within the same row using class
                var remarksTextBox = row.querySelector('.TXB_CleanedStatusRemarks');
                console.log("Remarks TextBox element:", remarksTextBox);

                if (remarksTextBox) {
                    // Toggle visibility based on the value
                    if (rbtnvalue == "0") {
                        console.log("Displaying the remarks TextBox");
                        remarksTextBox.style.display = 'block';
                    } else {
                        console.log("Hiding the remarks TextBox");
                        remarksTextBox.style.display = 'none';
                    }
                } else {
                    console.log("Remarks TextBox not found within the row");
                }
            } else {
                console.log("Row element not found");
            }
        }

        function validateGridView() {
            var grid = document.getElementById('<%= GridView_MetalCheck.ClientID %>');
            var rows = grid.getElementsByTagName("tr");

            var filledRowsCount = 0;

            // Loop through GridView rows, starting from 1 to skip the header row
            for (var i = 1; i < rows.length; i++) {
                var row = rows[i];

                // Get the TextBox for "Qty of Metal Found (gm)" and RadioButtonList for "Cleaned Status"
                var qtyTextBox = row.querySelector("[id*='TB_QtyMetalFound']");
                var radioList = row.querySelector("[id*='RBL_CleanedStatus']");

                // Check if the Qty of Metal Found is filled and at least one RadioButton is selected
                if (qtyTextBox && qtyTextBox.value.trim() !== "" && radioList && radioList.querySelector("input[type='radio']:checked")) {
                    filledRowsCount++;
                }
            }

            // Ensure at least 3 rows are filled
            if (filledRowsCount < 2) {
                setTimeout(function () {
                    // Display a PNotify notification
                    new PNotify({
                        title: 'Input Required',
                        text: 'Please fill in at least 2 records before proceeding.',
                        type: 'warning',
                        styling: 'bootstrap3',
                        delay: 3000,        // Notification auto-dismiss delay in milliseconds (3 seconds)
                        buttons: {
                            closer: true,  // Show a close button in the notification
                            sticker: false // Hide the sticker button
                        }
                    });
                }, 200); // Adjust delay as necessary
                return false; // Prevent the button click
            }

            return true; // Allow the button click if validation passes
        }


        function toggleFFRemarks(radioButtonList) {
            console.log("toggleFFRemarks function called");

            // Get the selected value from the RadioButtonList
            var selectedValue = radioButtonList.querySelector("input:checked").value;

            // Get the remarks DIV and validator elements
            var remarksDiv = document.getElementById("FF_RemarksDIV");
            var remarksValidator = document.getElementById('<%= RFV_FF_Remarks.ClientID %>');

            console.log("Selected value: " + selectedValue);

            // Show the remarks textbox and validator if "Not Sensing" is selected, otherwise hide them
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                remarksValidator.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
                remarksValidator.style.display = "none";
            }
        }
        function toggleNFERemarks(radioButtonList) {
            console.log("toggleNFERemarks function called");

            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("NFE_RemarksDIV");
            var remarksValidator = document.getElementById('<%= RFV_NFE_Remarks.ClientID %>');

            console.log("Selected value: " + selectedValue);

            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                remarksValidator.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
                remarksValidator.style.display = "none";
            }
        }

        function toggleSSRemarks(radioButtonList) {
            console.log("toggleSSRemarks function called");

            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SS_RemarksDIV");
            var remarksValidator = document.getElementById('<%= RFV_SS_Remarks.ClientID %>');

            console.log("Selected value: " + selectedValue);

            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
                remarksValidator.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
                remarksValidator.style.display = "none";
            }
        }

        function toggleCleanedStatusRemarks(radioButtonList) {
            console.log("toggleCleanedStatusRemarks function called");

            // Find the selected value in the RadioButtonList
            var selectedValue = radioButtonList.querySelector("input:checked").value;

            // Locate the closest GridView row (tr)
            var gridViewRow = radioButtonList.closest("tr");

            // Find the Remarks TextBox and Validator within the same row
            var remarksTextBox = gridViewRow.querySelector("input[id$='TXB_CleanedStatusRemarks']");
            var remarksValidator = gridViewRow.querySelector("span[id$='RFV_CleanedStatusRemarks']");

            console.log("Selected value: " + selectedValue);

            if (selectedValue === "0") { // If "Not Ok" is selected
                remarksTextBox.style.display = "block";
                if (remarksValidator) {
                    remarksValidator.style.display = "block";
                }
            } else { // If "Ok" is selected
                remarksTextBox.style.display = "none";
                if (remarksValidator) {
                    remarksValidator.style.display = "none";
                }
            }
        }

        function calculateRetention(element) {
            // Get the closest table row
            var row = element.closest('tr');

            // Select the input fields for initial sample, final retention, and label for percentage retention
            var initialSampleInput = row.querySelector('.TB_InitialSample');
            var finalRetentionInput = row.querySelector('.TB_FinalRetention');
            var retentionLabel = row.querySelector('.lbl_PercentageRetention');

            // Ensure the elements exist
            if (initialSampleInput && finalRetentionInput && retentionLabel) {
                // Parse the input values, allowing negative values for initialSample, defaulting to 0 if invalid
                var initialSample = parseFloat(initialSampleInput.value);
                var finalRetention = parseFloat(finalRetentionInput.value) || 0;

                // Check if initialSample is a valid number (not NaN)
                if (!isNaN(initialSample)) {
                    // Calculate the percentage retention; divide finalRetention by initialSample, and multiply by 100
                    var percentageRetention = (initialSample !== 0)
                        ? (finalRetention / initialSample) * 100 : 0;

                    // Update the label with the calculated retention percentage, rounded to 2 decimal places
                    retentionLabel.value = percentageRetention.toFixed(2) + " %";
                } else {
                    // If the initial sample is invalid, set retention label to 'Invalid input'
                    retentionLabel.value = "Invalid input";
                }
            } else {
                // Log an error message if any of the required elements are missing
                console.error('One or more required elements (initial sample, final retention, or label) are not found.');
            }
        }

        function validateSieveGridView() {
            var grid = document.getElementById('<%= GridView_Shieve.ClientID %>');
            if (!grid) {
                console.error('GridView not found.');
                return false;
            }

            var rows = grid.getElementsByTagName("tr");
            var filledRowsCount = 0; // Counter to track valid rows
            var rowsToValidate = 2; // Number of rows to validate

            // Loop through rows, starting from 1 to skip the header row
            for (var i = 1; i < rows.length; i++) {
                var row = rows[i];

                // Get TextBoxes for "Initial Sample", "Final Retention", and "Percentage Retention"
                var txtInitialSample = row.querySelector("[id*='TB_InitialSample']");
                var txtFinalRetention = row.querySelector("[id*='TB_FinalRetention']");
                var txtPercentageRetention = row.querySelector("[id*='lbl_PercentageRetention']");

                // Check for valid values
                var isRowValid = true;

                if (txtInitialSample) {
                    var initialSampleValue = parseFloat(txtInitialSample.value.trim());
                    if (isNaN(initialSampleValue) || initialSampleValue === 0) {
                        isRowValid = false;
                        txtInitialSample.style.borderColor = "red";
                    } else {
                        txtInitialSample.style.borderColor = ""; // Reset for valid input
                    }
                }

                if (txtFinalRetention) {
                    var finalRetentionValue = parseFloat(txtFinalRetention.value.trim());
                    if (isNaN(finalRetentionValue) || finalRetentionValue === 0) {
                        isRowValid = false;
                        txtFinalRetention.style.borderColor = "red";
                    } else {
                        txtFinalRetention.style.borderColor = ""; // Reset for valid input
                    }
                }

                if (txtPercentageRetention) {
                    var percentageRetentionValue = txtPercentageRetention.value.trim();
                    if (percentageRetentionValue === "" || percentageRetentionValue === "Invalid input") {
                        isRowValid = false;
                        txtPercentageRetention.style.borderColor = "red";
                    } else {
                        txtPercentageRetention.style.borderColor = ""; // Reset for valid input
                    }
                }

                // If the row is valid, increment the filledRowsCount
                if (isRowValid) {
                    filledRowsCount++;
                }

                // Stop validation if required number of rows are filled
                if (filledRowsCount >= rowsToValidate) {
                    break;
                }
            }

            // Ensure required number of valid rows
            if (filledRowsCount < rowsToValidate) {
                setTimeout(function () {
                    new PNotify({
                        title: 'Input Required',
                        text: 'Please ensure at least ' + rowsToValidate + ' rows have valid non-zero numeric values and valid percentage retention.',
                        type: 'warning',
                        styling: 'bootstrap3',
                        delay: 3000,
                        buttons: {
                            closer: true,
                            sticker: false,
                        },
                    });
                }, 200);
                return false; // Prevent form submission
            }

            return true; // Allow form submission
        }

    </script>

    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
    <asp:HiddenField ID="hdn_formid" runat="server" />

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
                                                            aria-controls="basicData" aria-selected="true"><%--<i class="fa fa-id-badge mr-2"></i>--%>Plant</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Metal_Check-tab" data-toggle="tab" href="#Metal_Check" role="tab"
                                                            aria-controls="Metal_Check" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Magnet Check</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Standard_Sieve-tab" data-toggle="tab" href="#Standard_Sieve" role="tab"
                                                            aria-controls="Standard_Sieve" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Sieve Condition</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Metal_Dctector_Area-tab" data-toggle="tab" href="#Metal_Dctector_Area" role="tab"
                                                            aria-controls="Metal_Dctector_Area" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Metal Detector</a>
                                                    </li>
                                                </ul>

                                                <div class="tab-content ml-1" id="myTabContent">
                                                    <%---Basic User Info Starts--%>
                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x_content">
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>



                                                            <div class="col-md-3" id="DDL_ProductCategory_DIV" runat="server" visible="false">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ForeColor="Red" InitialValue="0" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="DDL_ProductBrand_DIV" runat="server" visible="false">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <!-- Basic Data Save and ReSet Button-->
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_basic_btnSave" runat="server" AssociatedControlID="btnBDSave" Text="Click to Save" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="btnBDSave" runat="server" Text="Proceed Next" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="btnBDSave_Click" />
                                                                        <asp:Button ID="btnBDReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnBDReset_Click" />
                                                                        <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                    </div>
                                                                    <div class="mt-3">
                                                                        <!-- Add this label to display messages -->
                                                                        <asp:Label ID="lblMessage" runat="server" Text="" ForeColor="Red"></asp:Label>
                                                                    </div>
                                                                </div>
                                                            </div>


                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="Metal_Check" role="tabpanel" aria-labelledby="Metal_Check-tab">
                                                        <div class="x_content">
                                                            <asp:GridView ID="GridView_MetalCheck" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Location">
                                                                        <ItemTemplate>
                                                                            <asp:Label ID="lbl_Location" runat="server" Text='<%# Eval("Location") %>'></asp:Label>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Qty of Metal Found (gm)">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_QtyMetalFound" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Cleaned Status">
                                                                        <ItemTemplate>
                                                                            <asp:RadioButtonList ID="RBL_CleanedStatus" runat="server" RepeatDirection="Horizontal" onchange="GridtoggleCleanedStatusRemarks(this);">
                                                                                <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                                <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            </asp:RadioButtonList>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Remarks">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TXB_CleanedStatusRemarks" runat="server" CssClass="form-control form-control-sm rounded TXB_CleanedStatusRemarks" Placeholder="Remarks" Style="display: none;"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>


                                                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" OnRowDataBound="GridView1_MetalCheck_RowDataBound">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Location">
                                                                        <ItemTemplate>
                                                                            <asp:Label ID="lbl_Location" runat="server" Text='<%# Eval("Location") %>'></asp:Label>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Qty of Metal Found (gm)">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_QtyMetalFound" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Cleaned Status">
                                                                        <ItemTemplate>
                                                                            <asp:RadioButtonList ID="RBL_CleanedStatus" runat="server" RepeatDirection="Horizontal" CssClass="form-control form-control-sm" onchange="toggleCleanedStatusRemarks(this);">
                                                                                <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                                <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                            </asp:RadioButtonList>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:TemplateField HeaderText="Remarks for Not Ok">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TXB_CleanedStatusRemarks" runat="server" CssClass="form-control form-control-sm rounded TXB_CleanedStatusRemarks" Placeholder="Remarks"></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label11" runat="server" AssociatedControlID="btnBDSave" Text="Click to Save" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="btnSubmit" runat="server" Text="Proceed Next" CssClass="btn btn-sm btn-primary" OnClientClick="return validateGridView();" CausesValidation="false" OnClick="btnSubmit_Click" />
                                                                        <asp:Button ID="Button2" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnBDReset_Click" />
                                                                        <asp:Button ID="Button3" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                    </div>
                                                                    <div class="mt-3">
                                                                        <!-- Add this label to display messages -->
                                                                        <asp:Label ID="lbl_MagnetCheck" runat="server" Text="" ForeColor="Red"></asp:Label>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                        </div>
                                                    </div>


                                                    <!-- STANDARD SIEVE-->
                                                    <div class="tab-pane fade" id="Standard_Sieve" role="tabpanel" aria-labelledby="Standard_Sieve-tab">
                                                        <div class="x_content">

                                                            <asp:GridView ID="GridView_Shieve" runat="server" AutoGenerateColumns="False" Visible="true"
                                                                CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Sieve No">
                                                                        <ItemTemplate>
                                                                            <asp:Label ID="lbl_SieveNo" runat="server" CssClass="lbl_SieveNo" Text='<%# Eval("SieveNo") %>'></asp:Label>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="INITIAL SAMPLE">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_InitialSample" runat="server" CssClass="form-control form-control-sm rounded TB_InitialSample"
                                                                                oninput="calculateRetention(this);" Text='<%# Eval("InitialSample") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="FINAL RETENTION">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_FinalRetention" runat="server" CssClass="form-control form-control-sm rounded TB_FinalRetention"
                                                                                oninput="calculateRetention(this);" Text='<%# Eval("FinalRetention") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="% OF RETENTION">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="lbl_PercentageRetention" runat="server" CssClass="form-control form-control-sm rounded lbl_PercentageRetention" Text='<%# Eval("PercentageRetention") %>'></asp:TextBox>
                                                                            <%--<asp:Label ID="lbl_PercentageRetention" runat="server" CssClass="lbl_PercentageRetention"></asp:Label>--%>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>


                                                            <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" Visible="false"
                                                                CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:TemplateField HeaderText="Sl">
                                                                        <ItemTemplate>
                                                                            <%# Container.DataItemIndex + 1 %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="Sieve No">
                                                                        <ItemTemplate>
                                                                            <asp:Label ID="lbl_SieveNo" runat="server" CssClass="lbl_SieveNo" Text='<%# Eval("SieveNo") %>'></asp:Label>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="INITIAL SAMPLE">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_InitialSample" runat="server" CssClass="form-control form-control-sm rounded TB_InitialSample"
                                                                                Text='<%# Eval("InitialSample") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="FINAL RETENTION">
                                                                        <ItemTemplate>
                                                                            <asp:TextBox ID="TB_FinalRetention" runat="server" CssClass="form-control form-control-sm rounded TB_FinalRetention"
                                                                                Text='<%# Eval("FinalRetention") %>'></asp:TextBox>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>

                                                                    <asp:TemplateField HeaderText="% OF RETENTION">
                                                                        <ItemTemplate>

                                                                            <asp:Label ID="lbl_PercentageRetention" runat="server" CssClass="form-control form-control-sm rounded lbl_PercentageRetention" Text='<%# Eval("PercentageRetention") %>'></asp:Label>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>

                                                            <%--<div class="col-md-3">
                                                                    <div class="mb-3">
                                                                        <asp:Label ID="Label_InitialSieveSampleWt" runat="server" Text="Initial Sieve Sample Wt.(gm)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                        <asp:RequiredFieldValidator ID="RFV_InitialSieveSampleWt" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit3" ControlToValidate="TB_InitialSieveSampleWt" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                        <div class="input-group-sm">
                                                                            <asp:TextBox ID="TB_InitialSieveSampleWt" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </div>
                                                                    </div>
                                                                </div>

                                                                <div class="col-md-3">
                                                                    <div class="mb-3">
                                                                        <asp:Label ID="Label_FinalSieveRetentionWt" runat="server" Text="Final Sieve Retention Wt.(gm)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                        <asp:RequiredFieldValidator ID="RFV_FinalSieveRetentionWt" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit3" ControlToValidate="TB_FinalSieveRetentionWt" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                        <div class="input-group-sm">
                                                                            <asp:TextBox ID="TB_FinalSieveRetentionWt" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </div>
                                                                    </div>
                                                                </div>

                                                                <div class="col-md-3">
                                                                    <div class="mb-3">
                                                                        <asp:Label ID="Label_PercentageOfSieveRetention" runat="server" Text="Percentage of Sieve Retention" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                        <asp:RequiredFieldValidator ID="RFV_PercentageOfSieveRetention" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit3" ControlToValidate="TB_PercentageOfSieveRetention" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                        <div class="input-group-sm">
                                                                            <asp:TextBox ID="TB_PercentageOfSieveRetention" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                                                        </div>
                                                                    </div>
                                                                </div>

                                                                <div class="col-md-3">
                                                                    <div class="mb-3">
                                                                        <asp:Label ID="Label_Remarks" runat="server" Text="Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                        <asp:RequiredFieldValidator ID="RFV_Remarks" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit3" ControlToValidate="TB_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                        <div class="input-group-sm">
                                                                            <asp:TextBox ID="TB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" TextMode="MultiLine" Rows="3"></asp:TextBox>
                                                                        </div>
                                                                    </div>
                                                                </div>--%>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label14" runat="server" AssociatedControlID="btnBDSave" Text="Click to Save" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group input-group-sm">
                                                                        <asp:Button ID="btn_svSieve" runat="server" Text="Proceed Next" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit3" CausesValidation="true" OnClientClick="return validateSieveGridView();" OnClick="btn_svSieve_Click" />
                                                                        <asp:Button ID="btn_rstSieve" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" />
                                                                        <asp:Button ID="Button8" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                    </div>
                                                                    <div class="mt-3">
                                                                        <asp:Label ID="lbl_sivecheckmsg" runat="server" Text="" ForeColor="Red"></asp:Label>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="Metal_Dctector_Area" role="tabpanel" aria-labelledby="Metal_Dctector_Area-tab">
                                                        <div class="x_content">
                                                            <div class="col-md-3" id="DDL_PlantLine_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="lbl_DDL_PlantLine" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ForeColor="Red" ControlToValidate="DDL_PlantLine" ValidationGroup="AddList" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_FF_Status" runat="server" AssociatedControlID="RBL_FF_Status" Text="FF Status:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_RBL_FF_Status" runat="server" ValidationGroup="AddList" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_FF_Status" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_FF_Status" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleFFRemarks(this);">
                                                                            <asp:ListItem Text="Sensing" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Sensing" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="FF_RemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_FF_Remarks" runat="server" AssociatedControlID="TB_FF_Remarks" Text="FF (Not Sensing) Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_FF_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_FF_Remarks" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_FF_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_NFE_Status" runat="server" AssociatedControlID="RBL_NFE_Status" Text="NFE Status:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_NFE_Status" runat="server" ValidationGroup="AddList" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_NFE_Status" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_NFE_Status" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleNFERemarks(this);">
                                                                            <asp:ListItem Text="Sensing" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Sensing" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <!-- Remarks for NFE Status -->
                                                            <div class="col-md-3" id="NFE_RemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_NFE_Remarks" runat="server" AssociatedControlID="TB_NFE_Remarks" Text="NFE (Not Sensing) Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_NFE_Remarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TB_NFE_Remarks" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_NFE_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_SS_Status" runat="server" AssociatedControlID="RBL_SS_Status" Text="SS Status:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SS_Status" runat="server" ValidationGroup="AddList" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_SS_Status" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:RadioButtonList ID="RBL_SS_Status" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSSRemarks(this);">
                                                                            <asp:ListItem Text="Sensing" Value="1"></asp:ListItem>
                                                                            <asp:ListItem Text="Not Sensing" Value="0"></asp:ListItem>
                                                                        </asp:RadioButtonList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <!-- Remarks for SS Status -->
                                                            <div class="col-md-3" id="SS_RemarksDIV" style="display: none;">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label_SS_Remarks" runat="server" AssociatedControlID="TB_SS_Remarks" Text="SS Status (Not Sensing) Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_SS_Remarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TB_SS_Remarks" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_SS_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            

                                                            <div class="col-md-12" id="Div1" runat="server">
                                                                <asp:Button ID="btn_AddList" runat="server" Text="Add To List" CausesValidation="true" ValidationGroup="AddList" CssClass="btn btn-sm btn-primary" OnClick="btn_addtolist_Click" />
                                                                <asp:Button ID="btn_resetgrid" runat="server" Text="Reset Inputs" CssClass="btn btn-sm btn-warning" CausesValidation="false" OnClick="btn_resetgrid_Click" />
                                                                <asp:Label ID="lbl_addmoremessage" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                                                            </div>

                                                            <asp:GridView ID="Magnetgrid" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" AutoGenerateColumns="false" ShowFooter="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowDeleting="Magnetgrid_RowDeleting">
                                                                <Columns>
                                                                    <asp:BoundField DataField="PlantLine" HeaderText="Plant Line" />
                                                                    <asp:BoundField DataField="FF_Status" HeaderText="FF Status" />
                                                                    <asp:BoundField DataField="FF_Remarks" HeaderText="FF Remarks" Visible="false" />
                                                                    <asp:BoundField DataField="NFE_Status" HeaderText="NFE Status" />
                                                                    <asp:BoundField DataField="NFE_Remarks" HeaderText="NFE Remarks" Visible="false" />
                                                                    <asp:BoundField DataField="SS_Status" HeaderText="SS Status" />
                                                                    <asp:BoundField DataField="SS_Remarks" HeaderText="SS Remarks" Visible="false" />
                                                                    <asp:TemplateField HeaderText="Action" HeaderStyle-Width="5%">
                                                                        <ItemTemplate>
                                                                            <asp:ImageButton ID="btndelete" runat="server" CommandName="Delete" Height="15px" ImageUrl="~/WebData/Internal/delete_icon.png" Width="15px" ToolTip="Delete" OnClientClick="return confirm('Do you want to DELETE...?')" ImageAlign="Middle" />
                                                                        </ItemTemplate>
                                                                        <ItemStyle CssClass="text text-center" />
                                                                    </asp:TemplateField>
                                                                </Columns>
                                                            </asp:GridView>

                                                        </div>

                                                        <div class="col-md-12">&nbsp;</div>

                                                        <!-- Remarks -->
                                                            <div class="col-md-12">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label8" runat="server" Text="Overall Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="FinalSubmit" ControlToValidate="TB_MDRemarks" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_MDRemarks" runat="server" CssClass="form-control form-control-sm rounded" TextMode="MultiLine" Rows="2" Text="" PlaceHolder="Additional Comments / Remarks"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                        <!-- Basic Data Save and ReSet Button-->
                                                        <div class="col-md-12 pull-left" id="AddMoreDiv" runat="server">
                                                            <div class="mb-3">
                                                                <asp:Label ID="Label12" runat="server" AssociatedControlID="btnBDSave" Text="Final DATA Submit" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                <div class="input-group input-group-sm">
                                                                    <asp:Button ID="btn_finalsbmt" runat="server" Text="Final Submit" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="FinalSubmit" OnClick="btn_finalsbmt_Click" />
                                                                    <asp:Button ID="btn_rst_metaldet" runat="server" Text="Cancel" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btn_rst_metaldet_Click" />
                                                                    <asp:Button ID="Button5" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                </div>
                                                                <div class="mt-3">
                                                                    <!-- Add this label to display messages -->
                                                                    <asp:Label ID="lbl_mtldetmsg" runat="server" Text="" ForeColor="Red"></asp:Label>
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
                                <asp:Label ID="Label6" runat="server" Text="Approval Matrix"></asp:Label></h2>
                            <div class="clearfix"></div>

                        </div>
                        <div class="x_content">
                            <!-- Approver Flow Diagram -->
                            <div class="approver-flow">
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label7" runat="server" Text="Approver 1" />
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
                                        <asp:Label ID="Label9" runat="server" Text="Approver 2" />
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
                                        <asp:Label ID="Label10" runat="server" Text="Approver 3" />
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
