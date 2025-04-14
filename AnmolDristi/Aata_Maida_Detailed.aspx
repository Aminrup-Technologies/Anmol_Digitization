<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Aata_Maida_Detailed.aspx.cs" Inherits="AnmolDristi.Aata_Maida_Detailed" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }

        .white-background-readonly {
            background-color: white !important;
            color: black !important;
            cursor: default;
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
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <script type="text/javascript">

        window.onload = function () {
            // Disable  RadioButtonLists
            disableRadioButtonLists();

            // Toggle remarks divs based on the selected value for each RadioButtonList
            toggleAllRadioDivs();


        };

        function disableRadioButtonLists() {
            // Get all RadioButtonLists by their class
            var radioButtonLists = document.querySelectorAll('.form-control');

            radioButtonLists.forEach(function (rbl) {
                var radioItems = rbl.getElementsByTagName('input');
                for (var i = 0; i < radioItems.length; i++) {
                    radioItems[i].disabled = true; // Disable interaction
                }
            });
        }

        function toggleAllRadioDivs() {
            // Define a mapping of RadioButtonLists and their respective remarks divs 
            // Also specify the value that should trigger the remarks div
            var mapping = {
                '<%= RBL_ManufNameAdd.ClientID %>': { div: 'ManufNameAddRemarksDiv', triggerValue: "0" },
                '<%= RBL_FassaiNoLogo.ClientID %>': { div: 'FassaiNoLogoRemarksDiv', triggerValue: "0" },
                '<%= RBL_BBDateYesNo.ClientID %>': { div: 'BBDateYesNoRemarksDiv', triggerValue: "0" },
                '<%= RBL_Packing_Condition.ClientID %>': { div: 'PackingConditionRemarksDiv', triggerValue: "0" },
                '<%= RBL_ColorApp.ClientID %>': { div: 'ColorAppRemarksDiv', triggerValue: "0" }, // Example: This one shows remarks for "1"
                '<%= RBL_Odour.ClientID %>': { div: 'OdourRemarksDiv', triggerValue: "0" },
                '<%= RBL_TasteFlavor.ClientID %>': { div: 'TasteFlavorRemarksDiv', triggerValue: "0" },
                '<%= RBL_Impurities.ClientID %>': { div: 'ImpuritiesRemarksDiv', triggerValue: "0" },
                '<%= RBL_Grittiness.ClientID %>': { div: 'GrittinessRemarksDiv', triggerValue: "1" },
            };

            // Loop through each mapping and toggle the div visibility based on the selected value     
            for (var rblId in mapping) {
                var remarksDivId = mapping[rblId].div;
                var triggerValue = mapping[rblId].triggerValue;
                toggleRadioRemarksDiv(rblId, remarksDivId, triggerValue);
            }
        }

        function toggleRadioRemarksDiv(rblId, remarksDivId, triggerValue) {
            var rbl = document.getElementById(rblId);

            if (rbl) {
                var selectedRadio = rbl.querySelector('input[type="radio"]:checked');
                if (!selectedRadio) return; // If no radio button is selected, do nothing

                var selectedValue = selectedRadio.value;
                var remarksDiv = document.getElementById(remarksDivId);

                // If selected value matches the triggerValue, show remarks div, otherwise hide it         
                remarksDiv.style.display = (selectedValue === triggerValue) ? "block" : "none";
            }
        }


        

    </script>

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

    <div class="right_col" role="main" style="min-height: 2128.8px">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="QC - Atta/Refined Wheat Flour Report"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Detailed View"></asp:Label>
                            </h2>
                            <div class="clearfix"></div>
                        </div>


                        <div class="x-content">

                            <div class="col-12">
                                <h4 class="text-left text-info">Step-1 : RM Basic Details</h4>
                                <hr />
                            </div>

                            <div class="col-md-3" id="MaterialDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_DDL_Material" runat="server" AssociatedControlID="DDL_Material" Text="Material Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Material" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="PlantDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ValidationGroup="Submit"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="PlantLineDIV" runat="server">
                                    <div class="mb-3">
                                        <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Readonly="true"  ValidationGroup="Submit" ></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3" id="ProductBrand" runat="server">
                                    <div class="mb-3">
                                        <asp:Label ID="Label_DDL_ProductBrand" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Readonly="true"  ValidationGroup="Submit" AutoPostBack="true" ></asp:DropDownList>
                                        </div>
                                    </div>
                                </div>--%>

                            <div class="col-md-3" id="BrandDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BrandName" runat="server" AssociatedControlID="TB_BrandName" Text="Brand Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BrandName" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BrandName" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BrandName" ValidationGroup="Submit" runat="server" ControlToValidate="TB_BrandName" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BrandName" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Supplier Brand Name"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SupplierDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Supplier" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Supplier" runat="server" ControlToValidate="TB_Supplier" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Text="" Placeholder="Supplier (3-20 characters)" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="QuantityDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Quantity" runat="server" AssociatedControlID="TB_Quantity" Text="Quantity Supplied:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" ValidationGroup="Submit" runat="server" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Quantity Value(in pkts)"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Size(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="SizeRemarksDIV" style="display: none;">
                                    <div class="mb-3">
                                        <asp:Label ID="Label_TXB_Size_Remarks" runat="server" AssociatedControlID="TXB_Size_Remarks" Text="Size Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TXB_Size_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Size_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TXB_Size_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Readonly="true"  ></asp:TextBox>
                                        </div>
                                    </div>
                                </div>--%>

                            <div class="col-md-3" id="ChallanNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_ChallanNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Single quote (') is not allowed" ValidationExpression="^[^']*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" MaxLength="50" Placeholder="Challan No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="ChallanDateDIV" runat="server">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Readonly="true"  TextMode="Date" ValidationGroup="Submit"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" id="MfgDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Mfg" runat="server" AssociatedControlID="TB_Mfg" Text=" Mfg. Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Mfg" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_Mfg" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Readonly="true"  ValidationGroup="Submit" Placeholder="" TextMode="Date"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Mfg" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BatchNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BatchNo" runat="server" AssociatedControlID="TB_BatchNo" Text="Mfg. Batch No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BatchNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_BatchNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BatchNo" runat="server" ControlToValidate="TB_BatchNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[^']*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BatchNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Batch No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="LotNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_LotNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Lot/Gate No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="VehicleNoDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_VehicleNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Vehicle No"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_ManufNameAdd_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_FassaiNoLogo_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_BBDateYesNoRemarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12" id="MfgNameDIV" runat="server">
                                <div class="mb-12">
                                    <asp:Label ID="Lbl_TB_MfgName" runat="server" AssociatedControlID="TB_MfgName" Text="Manufacturer Name and Address : In clear readable form" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_MfgName" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_MfgName" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <asp:RegularExpressionValidator ID="REV_TB_MfgName" runat="server" ControlToValidate="TB_MfgName" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z0-9\s,.\-\/#]{3,100}$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MfgName" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" TextMode="MultiLine" Placeholder="Name and address (3-100 characters)" Rows="2" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-6" id="BeforeDateDIV" runat="server">
                                <div class="mb-6">
                                    <asp:RequiredFieldValidator ID="RFV_TB_BeforeDate" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_BeforeDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:Label ID="Lbl_TB_BeforeDate" runat="server" AssociatedControlID="TB_BeforeDate" Text="Best Before Date : 30 days from manufacturing date & In clear readable form" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BeforeDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" TextMode="Date" oninput="validateBestBeforeDate()"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6" id="FssaiNoDIV" runat="server">
                                <div class="mb-6">
                                    <asp:Label ID="Lbl_TB_FssaiNo" runat="server" AssociatedControlID="TB_FssaiNo" Text="FSSAI License No. : In clear readable form & must match with the material" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_FssaiNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_FssaiNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <asp:RegularExpressionValidator ID="REV_TB_FssaiNo" runat="server" ControlToValidate="TB_FssaiNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Number Only 14 digit" Maxlength="14" ValidationExpression="^[1-9]\d*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FssaiNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="FSAAI License No"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_PackingCondition_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_ColorApp_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_Odour_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_TasteFlavor_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_Impurities_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Moisture" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Moisture Value" oninput="validateMoistureValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="MoistureRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Moisture_Remarks" runat="server" AssociatedControlID="TXB_Moisture_Remarks" Text="Moisture Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Moisture_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Moisture_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Moisture_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Absorption" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Absorption Value" oninput="validateAbsorptionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AbsorptionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Absorption_Remarks" runat="server" AssociatedControlID="TXB_Absorption_Remarks" Text="Absorption Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Absorption_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Absorption_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Absorption_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Sedimentation" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Sedimentation Value " oninput="validateSedimentationValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SedimentationRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Sedimentation_Remarks" runat="server" AssociatedControlID="TXB_Sedimentation_Remarks" Text="Sedimentation Value Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Sedimentation_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Sedimentation_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Sedimentation_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TXB_Grittiness_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Acidity" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder=" Acidity Value " oninput="validateAcidityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AcidityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Acidity_Remarks" runat="server" AssociatedControlID="TXB_Acidity_Remarks" Text="Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Acidity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Acidity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Acidity_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Granularity" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Granularity on 70 mesh " oninput="validateGranularityValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Granularity_Remarks" runat="server" AssociatedControlID="TXB_Granularity_Remarks" Text="Granularity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Granularity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Granularity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Granularity_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_GranularityRetention" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Granularity " oninput="validateGranularityRetentionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GranularityRetentionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_GranularityRetention_Remarks" runat="server" AssociatedControlID="TXB_GranularityRetention_Remarks" Text="Granularity 85 Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GranularityRetention_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GranularityRetention_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GranularityRetention_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Retention" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Granularity " oninput="validateRetentionValue(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="RetentionRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_Retention_Remarks" runat="server" AssociatedControlID="TXB_Retention_Remarks" Text="Granularity 36 Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Retention_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Retention_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Retention_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="BromateDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_Bromate" runat="server" AssociatedControlID="TB_Bromate" Text="Bromate and Iodate :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Bromate" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Bromate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Bromate" runat="server" ControlToValidate="TB_Bromate" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z\s]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Bromate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Bromate and Iodate (3-20 characters)" MaxLength="20"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_TotalAsh" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Total Ash Value " ></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AshRemarksDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_Ash_Remarks" runat="server" AssociatedControlID="TXB_Ash_Remarks" Text="Total Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Ash_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Ash_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Ash_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_InsolubleAsh" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Insoluble Ash Value "></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="InsolubleAshRemarksDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label_TXB_InsolubleAsh_Remarks" runat="server" AssociatedControlID="TXB_InsolubleAsh_Remarks" Text="Acid Insoluble Ash Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_InsolubleAsh_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_InsolubleAsh_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_InsolubleAsh_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_GlutentContent" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Glutent Content(in mm)" ></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="GlutentContentRemarksDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_GlutentContent_Remarks" runat="server" AssociatedControlID="TXB_GlutentContent_Remarks" Text="Glutent Content Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_GlutentContent_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_GlutentContent_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GlutentContent_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_AlcoholicAcidity" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" ValidationGroup="Submit" Placeholder="Alcoholic Acidity Value " ></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AlcoholicAcidityRemarksDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TXB_AlcoholicAcidity_Remarks" runat="server" AssociatedControlID="TXB_AlcoholicAcidity_Remarks" Text="Alcoholic Acidity Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_AlcoholicAcidity_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_AlcoholicAcidity_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_AlcoholicAcidity_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--Update and reset button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnUpdate" runat="server" AssociatedControlID="Update" Text="Click to UPDATE" Visible="false" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="Update" runat="server" Text="Update" CssClass="btn btn-info btn-sm" Visible="false" CausesValidation="false" OnClick="Update_Click" />
                                        <asp:Button ID="Reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" Visible="false" CausesValidation="false" OnClick="Reset_Click" />
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



                            <%--Image part--%>

                            <div class="col-md-3" id="FU_MaterialImage_img" runat="server">
                                <asp:Label ID="LblMaterialImg" runat="server" Text="Material Bag Image" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                <asp:Image ID="imgMaterial" runat="server" ImageUrl='<%# Eval("Material_Image") != null ? ResolveUrl(Eval("Material_Image").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="Material Bag Image" Width="100px" Height="100px" />
                            </div>


                            <div class="col-md-12">
                                <hr>
                            </div>

                            <div class="col-md-12">
                                <h4 class="text-center text-success">Final Step : Data Submission</h4>
                                <hr>
                            </div>

                            <%--Button--%>
                            <div class="col-md-4">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_BasicbtnApprove" runat="server" AssociatedControlID="" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnValidate" runat="server" Text="Re-Validate inputs" CssClass="btn btn-warning btn-sm" CausesValidation="true" />
                                        <asp:Button ID="BtnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="BtnApprove_Click" />
                                        <asp:Button ID="BtnReject" runat="server" Text="Reject" CssClass="btn btn-sm btn-danger" CausesValidation="false" OnClick="BtnReject_Click" />
                                        <asp:Button ID="BtnBack" runat="server" Text="Back" CssClass="btn btn-sm btn-info" CausesValidation="false" PostBackUrl="~/Aata_Maida_Approval.aspx" />
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


