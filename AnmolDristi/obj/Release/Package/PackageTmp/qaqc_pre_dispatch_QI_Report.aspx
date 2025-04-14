<%@ Page Title="QC | Pre-Dispatch Clearance Report Form" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_pre_dispatch_QI_Report.aspx.cs" Inherits="AnmolDristi.qaqc_pre_dispatch_QI_Report" %>

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


        function toggleBoxConditionRemarksDiv(radioButtonList) {
            console.log("toggleBoxConditionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BoxConditionRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleTappingRemarksDiv(radioButtonList) {
            console.log("toggleTappingRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("TappingRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleLongSealRemarksDiv(radioButtonList) {
            console.log("toggleLongSealRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("LongSealRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleEndSealRemarksDiv(radioButtonList) {
            console.log("toggleEndSealRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("EndSealRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleMainPanelRemarksDiv(radioButtonList) {
            console.log("toggleMainPanelRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("MainPanelRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleCutsPacketsRemarksDiv(radioButtonList) {
            console.log("toggleCutsPacketsRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("CutsPacketsRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleBackingStatusRemarksDiv(radioButtonList) {
            console.log("toggleBackingStatusRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("BackingStatusRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleElongOvalRemarksDiv(radioButtonList) {
            console.log("toggleElongOvalRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ElongOvalRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleCuppingRemarksDiv(radioButtonList) {
            console.log("toggleCuppingRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("CuppingRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleImpressionRemarksDiv(radioButtonList) {
            console.log("toggleImpressionRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ImpressionRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleSoggyStatusRemarksDiv(radioButtonList) {
            console.log("toggleSoggyStatusRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SoggyStatusRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleForeignBodyRemarksDiv(radioButtonList) {
            console.log("toggleForeignBodyRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ForeignBodyRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleOffOdourRemarksDiv(radioButtonList) {
            console.log("toggleOffOdourRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("OffOdourRemarksDiv");
            console.log("Selected value: " + selectedValue);
            if (selectedValue === "0") {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }



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



        function showNotification(title, text, type) {
            new PNotify({
                title: title,
                text: text,
                type: type,
                styling: 'bootstrap3'
            });
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
                                    <asp:Label ID="lblInspectionLot" runat="server" AssociatedControlID="TXT_InspectionLot" Text="Inspection Lot :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_InspectionLot" runat="server" ErrorMessage="Required" ValidationGroup="NotSubmit" InitialValue="" ControlToValidate="TXT_InspectionLot" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_InspectionLot" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_InspectionLot" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_InspectionLot" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Inspection Lot"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblMaterialCode" runat="server" AssociatedControlID="TXT_MaterialCode" Text="Material Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_MaterialCode" runat="server" ErrorMessage="Required" ValidationGroup="NotSubmit" InitialValue="" ControlToValidate="TXT_MaterialCode" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_MaterialCode" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_MaterialCode" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_MaterialCode" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Inspection Lot"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblCBB_Produced" runat="server" AssociatedControlID="TXT_CBB_Produced" Text="No of CBB Produced :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_CBB_Produced" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_CBB_Produced" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_CBB_Produced" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_CBB_Produced" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_CBB_Produced" runat="server" ControlToValidate="TXT_CBB_Produced" ErrorMessage="[5 - 10000]" ForeColor="Red" MinimumValue="5" MaximumValue="10000" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_CBB_Produced" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Number of CBB"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblCBB_Checked" runat="server" AssociatedControlID="TXT_CBB_Checked" Text="No of CBB Checked :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_CBB_Checked" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_CBB_Checked" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_CBB_Checked" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_CBB_Checked" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_CBB_Checked" runat="server" ControlToValidate="TXT_CBB_Checked" ErrorMessage="[5 - 10000]" ForeColor="Red" MinimumValue="5" MaximumValue="10000" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_CBB_Checked" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Number of CBB Checked"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelBoxCondition" runat="server" AssociatedControlID="RBL_BoxCondition" Text="Box Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_BoxCondition" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_BoxCondition" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_BoxCondition" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleBoxConditionRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BoxConditionRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelBoxConditionRemarks" runat="server" AssociatedControlID="TXB_BoxCondition_Remarks" Text="Box Condition (Not Ok)" ForeColor="IndianRed" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BoxConditionRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_BoxCondition_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_BoxCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblPackets_CBB" runat="server" AssociatedControlID="TXT_Packets_CBB" Text="No of Packets in CBB :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Packets_CBB" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_Packets_CBB" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_Packets_CBB" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_Packets_CBB" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_Packets_CBB" runat="server" ControlToValidate="TXT_Packets_CBB" ErrorMessage="[5 - 10000]" ForeColor="Red" MinimumValue="5" MaximumValue="10000" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_Packets_CBB" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Packets in CBB"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTapping" runat="server" AssociatedControlID="RBL_Tapping" Text="Tapping of CBB :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Tapping" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_Tapping" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Tapping" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleTappingRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TappingRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelTappingRemarks" runat="server" AssociatedControlID="TXB_Tapping_Remarks" Text="Tapping of CBB (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TappingRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_Tapping_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Tapping_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblPkts_Checked_Per_CBB" runat="server" AssociatedControlID="TXT_Pkts_Checked_Per_CBB" Text="PKTS Checked/ CBB Box:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Pkts_Checked_Per_CBB" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_Pkts_Checked_Per_CBB" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_Pkts_Checked_Per_CBB" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_Pkts_Checked_Per_CBB" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_Pkts_Checked_Per_CBB" runat="server" ControlToValidate="TXT_Pkts_Checked_Per_CBB" ErrorMessage="[5 - 10000]" ForeColor="Red" MinimumValue="5" MaximumValue="10000" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_Pkts_Checked_Per_CBB" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="PKTS Checked / CBB Box"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblWt_of_Pkts" runat="server" AssociatedControlID="TXT_Wt_of_Pkts" Text="Wt. of Pkts (in gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Wt_of_Pkts" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_Wt_of_Pkts" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_Wt_of_Pkts" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_Wt_of_Pkts" ForeColor="Red" ErrorMessage="Decimal Only"
                                        ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_Wt_of_Pkts" runat="server" ControlToValidate="TXT_Wt_of_Pkts" ErrorMessage="[0.01 - 2000.00 gm]" ForeColor="Red" MinimumValue="0.01" MaximumValue="2000.00" Type="Double" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_Wt_of_Pkts" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Weight of Pkts"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblPackageDate" runat="server" AssociatedControlID="TXT_PackageDate" Text="Pkg Date:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PackageDate" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_PackageDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_PackageDate" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Packaging Date" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblBatchNo" runat="server" AssociatedControlID="TXT_BatchNo" Text="Batch/Lot No:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BatchNo" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_BatchNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_BatchNo" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_BatchNo" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_BatchNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Batch/Lot No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblPacketsMRP" runat="server" AssociatedControlID="TXT_PacketsMRP" Text="MRP of Pkts (in ₹):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PacketsMRP" runat="server" ErrorMessage="Required" ValidationGroup="Submit" ControlToValidate="TXT_PacketsMRP" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REF_PacketsMRP" runat="server" ValidationGroup="Submit" ControlToValidate="TXT_PacketsMRP" ForeColor="Red" ErrorMessage="Decimal Only"
                                        ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_PacketsMRP" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter MRP in ₹"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelLongSeal" runat="server" AssociatedControlID="RBL_LongSeal" Text="Long Seal Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_LongSeal" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_LongSeal" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_LongSeal" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleLongSealRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LongSealRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelLongSealRemarks" runat="server" AssociatedControlID="TXB_LongSeal_Remarks" Text="Long Seal Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_LongSealRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_LongSeal_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_LongSeal_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelEndSeal" runat="server" AssociatedControlID="RBL_EndSeal" Text="End Seal Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_EndSeal" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_EndSeal" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_EndSeal" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleEndSealRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="EndSealRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelEndSealRemarks" runat="server" AssociatedControlID="TXB_EndSeal_Remarks" Text="End Seal Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_EndSealRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_EndSeal_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_EndSeal_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelMainPanel" runat="server" AssociatedControlID="RBL_MainPanel" Text="Main Panel Central/ Not :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_MainPanel" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_MainPanel" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_MainPanel" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleMainPanelRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MainPanelRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelMainPanelRemarks" runat="server" AssociatedControlID="TXB_MainPanel_Remarks" Text="Main Panel Central (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_MainPanelRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_MainPanel_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_MainPanel_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelCutsPackets" runat="server" AssociatedControlID="RBL_CutsPackets" Text="Cuts in Packets :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_CutsPackets" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_CutsPackets" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_CutsPackets" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleCutsPacketsRemarksDiv(this);">
                                            <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="CutsPacketsRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelCutsPacketsRemarks" runat="server" AssociatedControlID="TXB_CutsPackets_Remarks" Text="Cuts in Packets (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_CutsPacketsRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_CutsPackets_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_CutsPackets_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelBackingStatus" runat="server" AssociatedControlID="RBL_BackingStatus" Text="Baking (Under/Over Baked) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_BackingStatus" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_BackingStatus" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_BackingStatus" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleBackingStatusRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Under/Over Baked" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BackingStatusRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelBackingStatusRemarks" runat="server" AssociatedControlID="TXB_BackingStatus_Remarks" Text="Baking of Biscuits (Under/Over Baked)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BackingStatusRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_BackingStatus_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_BackingStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelElongOval" runat="server" AssociatedControlID="RBL_ElongOval" Text="Shape Condition (Elongated or Oval) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ElongOval" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_ElongOval" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ElongOval" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleElongOvalRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ElongOvalRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelElongOvalRemarks" runat="server" AssociatedControlID="TXB_ElongOval_Remarks" Text="Shape Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ElongOvalRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_ElongOval_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ElongOval_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelCupping" runat="server" AssociatedControlID="RBL_Cupping" Text="Cupping Condition :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Cupping" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_Cupping" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Cupping" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleCuppingRemarksDiv(this);">
                                            <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="CuppingRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelCuppingRemarks" runat="server" AssociatedControlID="TXB_Cupping_Remarks" Text="Cupping Condition (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_CuppingRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_Cupping_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Cupping_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelImpression" runat="server" AssociatedControlID="RBL_Impression" Text="Impression on Biscuits :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Impression" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_Impression" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Impression" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleImpressionRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ImpressionRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelImpressionRemarks" runat="server" AssociatedControlID="TXB_Impression_Remarks" Text="Impression on Biscuits (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ImpressionRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_Impression_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Impression_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelSoggyStatus" runat="server" AssociatedControlID="RBL_SoggyStatus" Text="Biscuit Soggy:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_SoggyStatus" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_SoggyStatus" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_SoggyStatus" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSoggyStatusRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SoggyStatusRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelSoggyStatusRemarks" runat="server" AssociatedControlID="TXB_SoggyStatus_Remarks" Text="Biscuit Soggy (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_SoggyStatusRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_SoggyStatus_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_SoggyStatus_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelForeignBody" runat="server" AssociatedControlID="RBL_ForeignBody" Text="Foreign Body in Biscuit :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_ForeignBody" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_ForeignBody" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_ForeignBody" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleForeignBodyRemarksDiv(this);">
                                            <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ForeignBodyRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelForeignBodyRemarks" runat="server" AssociatedControlID="TXB_ForeignBody_Remarks" Text="Foreign Body in Biscuit (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_ForeignBodyRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_ForeignBody_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_ForeignBody_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelOffOdour" runat="server" AssociatedControlID="RBL_OffOdour" Text="Off Odour (Yes/No) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_OffOdour" runat="server" ValidationGroup="Submit" ErrorMessage="Required" ForeColor="Red" ControlToValidate="RBL_OffOdour" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_OffOdour" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleOffOdourRemarksDiv(this);">
                                            <asp:ListItem Text="No" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Yes" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="OffOdourRemarksDiv" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="LabelOffOdourRemarks" runat="server" AssociatedControlID="TXB_OffOdour_Remarks" Text="Off Odour (Not Ok)" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_OffOdourRemarks" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="TXB_OffOdour_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_OffOdour_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelRemarks" runat="server" AssociatedControlID="TXB_Remarks" Text="Additional Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" TextMode="MultiLine" Text="No Remarks" Rows="1" Width="100%"></asp:TextBox>
                                    </div>
                                </div>
                            </div>




                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click to SUBMIT" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="btnSubmit_Click" />
                                        <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnReset_Click" />
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
