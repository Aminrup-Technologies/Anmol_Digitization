<%@ Page Title="QAQC | Leak Test Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_leaktest_rev1.aspx.cs" Inherits="AnmolDristi.qaqc_leaktest_rev1" %>

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

    <script type="text/javascript">
        function togglePassFailRemarksDiv(radioButtonList) {
            console.log("togglePassFailRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("PassFailRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function GridtogglePassFailRemarks(radioButtonList) {
            console.log("GridtogglePassFailRemarks function called");
            // Check the type of radioButtonList
            console.log("radioButtonList:", radioButtonList);

            // Get the value of the selected radio button
            var rbtnvalue = radioButtonList.querySelector("input:checked").value;
            console.log("Value of radioButtonList:", rbtnvalue);

            // Get the row containing the radio button list
            var row = radioButtonList.closest('tr');
            console.log("Row element:", row);

            // Ensure the row is found
            if (row) {
                // Find the remarks textbox within the same row using a specific class
                var remarksTextBox = row.querySelector('.TB_Remarks');
                console.log("Remarks TextBox element:", remarksTextBox);

                if (remarksTextBox) {
                    // Toggle visibility based on the value
                    if (rbtnvalue == "0") { // 'Not Ok' selected
                        console.log("Displaying the remarks TextBox");
                        remarksTextBox.style.display = 'block';
                        remarksTextBox.setAttribute("required", "true"); // Make it mandatory
                    } else { // 'Ok' selected
                        console.log("Hiding the remarks TextBox");
                        remarksTextBox.style.display = 'none';
                        remarksTextBox.removeAttribute("required"); // Remove mandatory requirement
                    }
                } else {
                    console.log("Remarks TextBox not found within the row");
                }
            } else {
                console.log("Row element not found");
            }
        }

        // JavaScript function to lock the fields
        <%--function lockFields() {
            // Disable DropDownList (DDL_BrandSKU)
            document.getElementById('<%= DDL_BrandSKU.ClientID %>').disabled = true;

            // Disable TextBox (TB_PackingMCNo)
            document.getElementById('<%= TB_PackingMCNo.ClientID %>').disabled = true;

            // Disable RadioButtonList (RBL_PassFail)
            var rbl = document.getElementById('<%= RBL_PassFail.ClientID %>');
            for (var i = 0; i < rbl.getElementsByTagName('input').length; i++) {
                rbl.getElementsByTagName('input')[i].disabled = true;
            }

            // Disable TextBox for Pass/Fail Remarks (TXB_PassFail_Remarks)
            document.getElementById('<%= TXB_PassFail_Remarks.ClientID %>').disabled = true;

            // Disable TextBox (TB_PercentageSlanted)
            document.getElementById('<%= TB_PercentageSlanted.ClientID %>').disabled = true;
        }--%>

        // JavaScript function to toggle the lock/unlock of the fields
        function toggleFields() {
            // Get the elements by their client-side IDs
            <%--var ddlPlant = document.getElementById('<%= DDL_Plant.ClientID %>');
            var ddlPlantLine = document.getElementById('<%= DDL_PlantLine.ClientID %>');
            var ddlProductCategory = document.getElementById('<%= DDL_ProductCategory.ClientID %>');
            var ddlProductBrand = document.getElementById('<%= DDL_ProductBrand.ClientID %>');
    
            var fields = [ddlPlant, ddlPlantLine, ddlProductCategory, ddlProductBrand];

            // Check if the fields are already disabled, if so, unlock them; otherwise, lock them
            var areFieldsDisabled = ddlPlant.disabled;

            for (var i = 0; i < fields.length; i++) {
                fields[i].disabled = !areFieldsDisabled; // Toggle disabled state
            }--%>
            // Get the elements by their client-side IDs
            var ddlPlant = document.getElementById('<%= DDL_Plant.ClientID %>');
            var ddlPlantLine = document.getElementById('<%= DDL_PlantLine.ClientID %>');
            var ddlProductCategory = document.getElementById('<%= DDL_ProductCategory.ClientID %>');
            var ddlProductBrand = document.getElementById('<%= DDL_ProductBrand.ClientID %>');

            var fields = [ddlPlant, ddlPlantLine, ddlProductCategory, ddlProductBrand];
            var areFieldsDisabled = ddlPlant.disabled;

            // Toggle the disabled state of the fields
            for (var i = 0; i < fields.length; i++) {
                fields[i].disabled = !areFieldsDisabled;
            }
            // Store the lock state in the hidden field
            var lockState = !areFieldsDisabled;
            document.getElementById('<%= hfLockState.ClientID %>').value = lockState;
        }

        function checkDropdownsAndToggleFields() {
            // Get all dropdowns that need to be checked
            var ddlPlant = document.getElementById('<%= DDL_Plant.ClientID %>');
            var ddlLine = document.getElementById('<%= DDL_PlantLine.ClientID %>');
            var ddlProductCategory = document.getElementById('<%= DDL_ProductCategory.ClientID %>');
            var ddlProductBrand = document.getElementById('<%= DDL_ProductBrand.ClientID %>');
            var ddlBrandSKU = document.getElementById('<%= DDL_BrandSKU.ClientID %>'); // Get DDL_BrandSKU
            var btnAddList = document.getElementById('<%= btn_AddList.ClientID %>'); // Get btn_AddList
            var btn_resetgrid = document.getElementById('<%= btn_resetgrid.ClientID %>'); // Get btn_AddList

            // Check if any dropdown has the default value (assuming value="0" for unselected)
            if (ddlPlant.value == "0" || ddlLine.value == "0" || ddlProductCategory.value == "0" || ddlProductBrand.value == "0") {
                // Show a message if any dropdown is not selected
                alert("Please select all required fields before locking/unlocking.");
                return false; // Prevent the action
            }

            // If all dropdowns are selected, toggle the fields
            toggleFields();  // Call your toggleFields function

            // Enable the DDL_BrandSKU and btn_AddList
            ddlBrandSKU.disabled = false; // Enable DDL_BrandSKU
            btnAddList.disabled = false; // Enable btn_AddList
            btn_resetgrid.disabled = false;

            return false; // Prevent postback
        }


    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hfLockState" runat="server" />
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
                <div class="col-md-12 col-sm-12">
                    <!-- Static Section -->
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="NewRow" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="NewRow" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="NewRow" InitialValue="0" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="btn_lock" Text="Lock Selection" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:Button ID="btn_lock" runat="server" Text="Lock/Unlock" CssClass="btn btn-sm btn-primary" CausesValidation="true" ValidationGroup="NewRow" OnClientClick="return checkDropdownsAndToggleFields();" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Dynamic Section</h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="lbl_DDL_BrandSKU" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Lbl_PackingMCNo" runat="server" AssociatedControlID="TB_PackingMCNo" Text="Packing M/C No." ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PackingMCNo" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_PackingMCNo" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PackingMCNo" runat="server" ControlToValidate="TB_PackingMCNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PackingMCNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Packing M/C No." Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelColorApp" runat="server" AssociatedControlID="RBL_PassFail" Text="LEAK TEST / SEAL INTEGRITY" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_PassFail" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_PassFail" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_PassFail" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="togglePassFailRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2" id="PassFailRemarksDiv" style="display: none;">
                                <div class="mb-2">
                                    <asp:Label ID="LabelPassFailRemarks" runat="server" AssociatedControlID="TXB_PassFail_Remarks" Text="Pass / Fail Comments (Fail)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PassFailRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_PassFail_Remarks" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_PassFail_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="lbl_TB_PercentageSlanted" runat="server" AssociatedControlID="TB_PercentageSlanted" Text="SLANTED / LOOSE PACK (%)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_PercentageSlanted" runat="server" ValidationGroup="Submit" ErrorMessage="Required" InitialValue="" ForeColor="Red" ControlToValidate="TB_PercentageSlanted" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PercentageSlanted" runat="server" ValidationGroup="Submit" ControlToValidate="TB_PercentageSlanted" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_PercentageSlanted" runat="server" ControlToValidate="TB_PercentageSlanted" ValidationGroup="Submit" ErrorMessage="[00.00 - 100.00]" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PercentageSlanted" runat="server" CssClass="form-control form-control-sm rounded" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12 text-center" id="AddMoreDiv" runat="server">
                                <asp:Button ID="btn_AddList" runat="server" Text="Submit" CausesValidation="true" ValidationGroup="Submit" CssClass="btn btn-sm btn-primary" OnClick="btn_AddList_Click" />
                                <asp:Button ID="btn_resetgrid" runat="server" Text="Reset Inputs" CssClass="btn btn-sm btn-warning" CausesValidation="false" OnClick="btn_resetgrid_Click1" />
                                <asp:Label ID="lbl_message" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                            </div>

                            <div class="col-md-12 text-center">
                                <asp:GridView ID="gvData" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" AutoGenerateColumns="false" ShowFooter="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowDeleting="gvData_RowDeleting">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Sl No.">
                                            <ItemTemplate>
                                                <%# Container.DataItemIndex + 1 %>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="Plant" HeaderText="Plant" SortExpression="Plant" Visible="false" />
                                        <asp:BoundField DataField="PlantId" HeaderText="Plant Id" SortExpression="PlantId" Visible="false" />
                                        <asp:BoundField DataField="PlantLine" HeaderText="Plant Line" SortExpression="PlantLine" Visible="false" />
                                        <asp:BoundField DataField="PlantLineId" HeaderText="Plant Line Id" SortExpression="PlantLineId" Visible="false" />
                                        <asp:BoundField DataField="ProductCategory" HeaderText="Product Category" SortExpression="ProductCategory" Visible="false"/>
                                        <asp:BoundField DataField="ProductCategoryId" HeaderText="Product Category Id" SortExpression="ProductCategoryId" Visible="false" />
                                        <asp:BoundField DataField="ProductBrand" HeaderText="Product Brand" SortExpression="ProductBrand" />
                                        <asp:BoundField DataField="ProductBrandId" HeaderText="Product Brand Id" SortExpression="ProductBrandId" Visible="false" />
                                        <asp:BoundField DataField="BrandSKU" HeaderText="Brand SKU" SortExpression="BrandSKU" />
                                        <asp:BoundField DataField="BrandSKUId" HeaderText="Brand SKU Id" SortExpression="BrandSKUId" Visible="false" />
                                        <asp:BoundField DataField="PackingMCNo" HeaderText="Packing M/C No." SortExpression="PackingMCNo" />
                                        <asp:BoundField DataField="LeakTestSealIntegrity" HeaderText="Leak Test / Seal Integrity" SortExpression="LeakTestSealIntegrity" />
                                        <asp:BoundField DataField="PassFailRemarks" HeaderText="Pass / Fail Remarks" SortExpression="PassFailRemarks" />
                                        <asp:BoundField DataField="PercentageSlanted" HeaderText="Slanted / Loose Pack (%)" SortExpression="PercentageSlanted" />
                                        <asp:TemplateField HeaderText="Action" HeaderStyle-Width="5%">
                                            <ItemTemplate>
                                                <asp:ImageButton ID="btndelete" runat="server" CommandName="Delete" Height="15px" ImageUrl="~/WebData/Internal/delete_icon.png" Width="15px" ToolTip="Delete" OnClientClick="return confirm('Do you want to DELETE...?')" ImageAlign="Middle" />
                                            </ItemTemplate>
                                            <ItemStyle CssClass="text text-center" />
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <div class="grid">No Data Found</div>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </div>

                        </div>
                    </div>

                    <%--button start--%>
                    <div class="col-md-6 center-margin" id="final_save_btns" runat="server" visible="false">
                        <div class="ln_solid"></div>
                        <div class="col-md-12 col-sm-12 center" style="text-align: center;">
                            <asp:Button ID="btn_svall" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" Visible="true" CausesValidation="false" OnClick="btn_svall_Click" />
                            <asp:Button ID="btn_resetpage" runat="server" Text="Reset Inputs" CssClass="btn btn-sm btn-warning" CausesValidation="false" PostBackUrl="~/qaqc_leaktest_rev1.aspx" />
                            <asp:Button ID="btn_home2" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" PostBackUrl="~/home.aspx" CausesValidation="false" />
                            <asp:Label ID="lbl_svall_msg" runat="server" Font-Bold="true"></asp:Label>
                        </div>
                    </div>
                    <%--button end--%>
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
