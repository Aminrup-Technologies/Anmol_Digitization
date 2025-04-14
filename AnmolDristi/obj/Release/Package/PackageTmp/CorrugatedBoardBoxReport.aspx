<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="CorrugatedBoardBoxReport.aspx.cs" Inherits="AnmolDristi.CorrugatedBoardBoxReport" %>

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

    <script type="text/javascript">
        function validateRange(min, max, remarksDivId) {
            var inputValue = parseFloat(document.getElementById('<%=TB_BurstingStrength.ClientID%>').value);
            var remarksDiv = document.getElementById(remarksDivId);
            if (!isNaN(inputValue) && inputValue !== "" && (inputValue < min || inputValue > max)) {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateRange2(min, max, remarksDivId) {
            var inputValue = parseFloat(document.getElementById('<%=TB_CompressionStrength.ClientID%>').value);
            var remarksDiv = document.getElementById(remarksDivId);
            if (!isNaN(inputValue) && inputValue !== "" && (inputValue < min || inputValue > max)) {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateRange3(min, max, remarksDivId) {
            var inputValue = parseFloat(document.getElementById('<%=TB_FlutePercent.ClientID%>').value);
            var remarksDiv = document.getElementById(remarksDivId);
            if (!isNaN(inputValue) && inputValue !== "" && (inputValue < min || inputValue > max)) {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

        function validateRange4(min, max, remarksDivId) {
            var inputValue = parseFloat(document.getElementById('<%=TB_MoisturePercent.ClientID%>').value);
            var remarksDiv = document.getElementById(remarksDivId);
            if (!isNaN(inputValue) && inputValue !== "" && (inputValue < min || inputValue > max)) {
                remarksDiv.style.display = "block";
            } else {
                remarksDiv.style.display = "none";
            }
        }

    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hdn_formid" runat="server" />
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
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
                        <div class="x_content">
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3" >
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_NoOfPkt" runat="server" AssociatedControlID="TB_NoOfPkt" Text="No of Packets :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_NoOfPkt" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_NoOfPkt" Display="Dynamic" ForeColor="Red" InitialValue=""></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_NoOfPkt" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NoOfPkt" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_NoOfPkt" runat="server" ControlToValidate="TB_NoOfPkt" ErrorMessage="[10 - 40]" ForeColor="Red" MinimumValue="10" MaximumValue="60" Type="Integer" Display="Static"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_NoOfPkt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Number of Packets [10 x 40]" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_NoOfPkt" runat="server" AssociatedControlID="TB_NoOfPkt" Text="No of Packets :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_NoOfPkt" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_NoOfPkt" Display="Dynamic" ForeColor="Red" InitialValue=""></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_NoOfPkt" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NoOfPkt" ForeColor="Red" ErrorMessage="Input Required" ValidationExpression="^\d+\s*[\*xX]\s*\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:CustomValidator ID="CV_TB_NoOfPkt" runat="server" ControlToValidate="TB_NoOfPkt" ValidationGroup="Submit" ErrorMessage="Invalid input format! Use: 10 * 40" Display="Dynamic" ForeColor="Red"></asp:CustomValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_NoOfPkt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Number of Packets [10 * 40]" Text=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_SupplierName" runat="server" AssociatedControlID="TB_SupplierName" Text="Supplier Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_SupplierName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_SupplierName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_SupplierName" runat="server" ControlToValidate="TB_SupplierName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_SupplierName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Supplier Name" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SizeDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Size" runat="server" AssociatedControlID="TB_Size" Text="Sample Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Size" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Size" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Size" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Size" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="CV_TB_Size" runat="server" ControlToValidate="TB_Size" ErrorMessage="[10 - 40]" ForeColor="Red" MinimumValue="10" MaximumValue="40"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Size(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Challan No." MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date Required" ControlToValidate="TB_ChallanDate" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" Placeholder="Select Challan Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot and Gate No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_LotNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Lot and Gate No." MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_VehicleNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Vehicle No." MaxLength="15"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <!-- Label for Dimension L (in mm) -->
                                    <asp:Label ID="Lbl_TB_DimensionStdL" runat="server" AssociatedControlID="TB_DimensionStdL" Text="Standard Dimension L (mm):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Required Field Validator for Dimension L -->
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionStdL" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionStdL"
                                        ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>

                                    <!-- Range Validator for Dimension L to ensure the input is within a valid range -->
                                    <asp:RangeValidator ID="RV_TB_DimensionStdL" runat="server" ControlToValidate="TB_DimensionStdL" ValidationGroup="Submit"
                                        ErrorMessage="Invalid Length" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>

                                    <!-- Input TextBox for Dimension L (in mm) -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionStdL" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter Length in mm" OnKeyUp="validateDimensionL()" ClientIDMode="Static"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Hidden Remark Section for Dimension L -->
                            <div class="col-md-3" id="remarkSectionL" style="display: none;">
                                <div class="mb-3">
                                    <!-- Label for Remark when Dimension L is out of range -->
                                    <asp:Label ID="Lbl_Remark_DimensionL" runat="server" AssociatedControlID="TB_Remarks" Text="Please provide a remark for deviation:"
                                        ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Input TextBox for Remark for Dimension L -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remark_DimensionL" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter remark for deviation"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <script type="text/javascript">
                                function validateDimensionL() {
                                    // Get the value of the Dimension L input field
                                    var dimensionLValue = parseFloat(document.getElementById('<%= TB_DimensionStdL.ClientID %>').value);

                                    // Get the remark section element for Dimension L
                                    var remarkSectionL = document.getElementById("remarkSectionL");

                                    // Valid range for Dimension L: 0 to 10000mm (as specified in the range validator)
                                    var lowerLimitL = 0;
                                    var upperLimitL = 10000;

                                    // Check if the Dimension L value is outside the valid range
                                    if (isNaN(dimensionLValue) || dimensionLValue < lowerLimitL || dimensionLValue > upperLimitL) {
                                        // Show the remark section if the value is outside the valid range
                                        remarkSectionL.style.display = "block";
                                    } else {
                                        // Hide the remark section if the value is within the valid range
                                        remarkSectionL.style.display = "none";
                                    }
                                }
                            </script>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <!-- Label for Dimension W (in mm) -->
                                    <asp:Label ID="Lbl_TB_DimensionStdW" runat="server" AssociatedControlID="TB_DimensionStdW" Text="Standard Dimension W (mm):"
                                        ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Required Field Validator for Dimension W -->
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionStdW" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionStdW"
                                        ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>

                                    <!-- Range Validator for Dimension W to ensure the input is within a valid range -->
                                    <asp:RangeValidator ID="RV_TB_DimensionStdW" runat="server" ControlToValidate="TB_DimensionStdW" ValidationGroup="Submit"
                                        ErrorMessage="Invalid Width" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>

                                    <!-- Input TextBox for Dimension W (in mm) -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionStdW" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter Width in mm" OnKeyUp="validateDimensionW()" ClientIDMode="Static"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Hidden Remark Section for Dimension W -->
                            <div class="col-md-3" id="remarkSectionW" style="display: none;">
                                <div class="mb-3">
                                    <!-- Label for Remark when Dimension W is out of range -->
                                    <asp:Label ID="Lbl_Remark_DimensionW" runat="server" AssociatedControlID="TB_Remark_DimensionW"
                                        Text="Please provide a remark for deviation:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Input TextBox for Remark for Dimension W -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remark_DimensionW" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter remark for deviation"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <script type="text/javascript">
                                function validateDimensionW() {
                                    // Get the value of the Dimension W input field
                                    var dimensionWValue = parseFloat(document.getElementById('<%= TB_DimensionStdW.ClientID %>').value);

                                    // Get the remark section element for Dimension W
                                    var remarkSectionW = document.getElementById("remarkSectionW");

                                    // Valid range for Dimension W: 0 to 10000mm (as specified in the range validator)
                                    var lowerLimitW = 0;
                                    var upperLimitW = 10000;

                                    // Check if the Dimension W value is outside the valid range
                                    if (isNaN(dimensionWValue) || dimensionWValue < lowerLimitW || dimensionWValue > upperLimitW) {
                                        // Show the remark section if the value is outside the valid range
                                        remarkSectionW.style.display = "block";
                                    } else {
                                        // Hide the remark section if the value is within the valid range
                                        remarkSectionW.style.display = "none";
                                    }
                                }
                            </script>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <!-- Label for Dimension H (in mm) -->
                                    <asp:Label ID="Lbl_TB_DimensionStdH" runat="server" AssociatedControlID="TB_DimensionStdH" Text="Standard Dimension H (mm):"
                                        ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Required Field Validator for Dimension H -->
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionStdH" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionStdH"
                                        ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>

                                    <!-- Range Validator for Dimension H to ensure the input is within a valid range -->
                                    <asp:RangeValidator ID="RV_TB_DimensionStdH" runat="server" ControlToValidate="TB_DimensionStdH" ValidationGroup="Submit"
                                        ErrorMessage="Invalid Height" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>

                                    <!-- Input TextBox for Dimension H (in mm) -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionStdH" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter Height in mm" OnKeyUp="validateDimensionH()" ClientIDMode="Static"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Hidden Remark Section for Dimension H -->
                            <div class="col-md-3" id="remarkSectionH" style="display: none;">
                                <div class="mb-3">
                                    <!-- Label for Remark when Dimension H is out of range -->
                                    <asp:Label ID="Lbl_Remark_DimensionH" runat="server" AssociatedControlID="TB_Remark_DimensionH"
                                        Text="Please provide a remark for deviation:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Input TextBox for Remark for Dimension H -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remark_DimensionH" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter remark for deviation"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <script type="text/javascript">
                                function validateDimensionH() {
                                    // Get the value of the Dimension H input field
                                    var dimensionHValue = parseFloat(document.getElementById('<%= TB_DimensionStdH.ClientID %>').value);

                                    // Get the remark section element for Dimension H
                                    var remarkSectionH = document.getElementById("remarkSectionH");

                                    // Valid range for Dimension H: 0 to 10000mm (as specified in the range validator)
                                    var lowerLimitH = 0;
                                    var upperLimitH = 10000;

                                    // Check if the Dimension H value is outside the valid range
                                    if (isNaN(dimensionHValue) || dimensionHValue < lowerLimitH || dimensionHValue > upperLimitH) {
                                        // Show the remark section if the value is outside the valid range
                                        remarkSectionH.style.display = "block";
                                    } else {
                                        // Hide the remark section if the value is within the valid range
                                        remarkSectionH.style.display = "none";
                                    }
                                }
                            </script>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DimensionObsL" runat="server" AssociatedControlID="TB_DimensionObsL" Text="Observed Dimension L (mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionObsL" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionObsL" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionObsL" runat="server" ControlToValidate="TB_DimensionObsL" ValidationGroup="Submit" ErrorMessage="Invalid Length" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionObsL" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Observed Length in mm"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DimensionObsW" runat="server" AssociatedControlID="TB_DimensionObsW" Text="Observed Dimension W (mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionObsW" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionObsW" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionObsW" runat="server" ControlToValidate="TB_DimensionObsW" ValidationGroup="Submit" ErrorMessage="Invalid Width" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionObsW" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Observed Width in mm"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DimensionObsH" runat="server" AssociatedControlID="TB_DimensionObsH" Text="Observed Dimension H (mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionObsH" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DimensionObsH" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionObsH" runat="server" ControlToValidate="TB_DimensionObsH" ValidationGroup="Submit" ErrorMessage="Invalid Height" MinimumValue="0" MaximumValue="10000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionObsH" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Observed Height in mm"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <!-- Label for Standard GSM -->
                                    <asp:Label ID="Lbl_TB_GSMStd" runat="server" AssociatedControlID="TB_GSMStd" Text="Standard GSM:"
                                        ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Required Field Validator for Standard GSM -->
                                    <asp:RequiredFieldValidator ID="RFV_TB_GSMStd" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_GSMStd"
                                        ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>

                                    <!-- Range Validator for Standard GSM to ensure the input is within a valid range -->
                                    <asp:RangeValidator ID="RV_TB_GSMStd" runat="server" ControlToValidate="TB_GSMStd" ValidationGroup="Submit"
                                        ErrorMessage="Invalid GSM" MinimumValue="0" MaximumValue="1000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>

                                    <!-- Input TextBox for Standard GSM -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GSMStd" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Standard GSM" OnKeyUp="validateGSM()" ClientIDMode="Static"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Hidden Remark Section for Standard GSM -->
                            <div class="col-md-3" id="remarkSectionGSM" style="display: none;">
                                <div class="mb-3">
                                    <!-- Label for Remark when Standard GSM is out of range -->
                                    <asp:Label ID="Lbl_Remark_GSMStd" runat="server" AssociatedControlID="TB_Remark_GSMStd"
                                        Text="Please provide a remark for deviation:" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>

                                    <!-- Input TextBox for Remark for Standard GSM -->
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remark_GSMStd" runat="server" CssClass="form-control form-control-sm rounded"
                                            Placeholder="Enter remark for deviation"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <script type="text/javascript">
                                function validateGSM() {
                                    var gsmValue = parseFloat(document.getElementById('<%= TB_GSMStd.ClientID %>').value);
                                    var remarkSectionGSM = document.getElementById("remarkSectionGSM");
                                    var lowerLimitGSM = 0;
                                    var upperLimitGSM = 1000;
                                    if (isNaN(gsmValue) || gsmValue < lowerLimitGSM || gsmValue > upperLimitGSM) {
                                        remarkSectionGSM.style.display = "block";
                                    } else {
                                        remarkSectionGSM.style.display = "none";
                                    }
                                }
                            </script>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GSMObs" runat="server" AssociatedControlID="TB_GSMObs" Text="Observed GSM:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GSMObs" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_GSMObs" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_GSMObs" runat="server" ControlToValidate="TB_GSMObs" ValidationGroup="Submit" ErrorMessage="Invalid GSM" MinimumValue="0" MaximumValue="1000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GSMObs" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Observed GSM"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_BurstingStrength" runat="server" AssociatedControlID="TB_BurstingStrength" Text="BS (Kg/cm²):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BurstingStrength" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_BurstingStrength" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_BurstingStrength" runat="server" ControlToValidate="TB_BurstingStrength" ValidationGroup="Submit" ErrorMessage="[5 - 12]" MinimumValue="5" MaximumValue="12" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BurstingStrength" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter BS (Kg/cm²)" onkeyup="validateRange(5, 12, 'divRemarks_BurstingStrength')"></asp:TextBox>
                                    </div>
                                    <!-- Remarks Field -->
                                    <div class="mt-2" id="divRemarks_BurstingStrength" style="display: none;">
                                        <asp:Label ID="Lbl_Remarks_BurstingStrength" runat="server" Text="BS Remarks:" ForeColor="Red" Font-Size="Small"></asp:Label>
                                        <asp:TextBox ID="TB_Remarks_BurstingStrength" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter BS- Out of Range Remarks"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_CompressionStrength" runat="server" AssociatedControlID="TB_CompressionStrength" Text="Compression Strength (kg/cm²):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_CompressionStrength" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_CompressionStrength" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_CompressionStrength" runat="server" ControlToValidate="TB_CompressionStrength" ValidationGroup="Submit" ErrorMessage="[100 - 1000]" MinimumValue="100" MaximumValue="1000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_CompressionStrength" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Compression Strength (kg/cm²)" onkeyup="validateRange2(100, 1000, 'divRemarks_CompressionStrength')"></asp:TextBox>
                                    </div>
                                    <!-- Remarks Field -->
                                    <div class="mt-2" id="divRemarks_CompressionStrength" style="display: none;">
                                        <asp:Label ID="Lbl_Remarks_CompressionStrength" runat="server" Text="Compression Strength Remarks:" ForeColor="Red" Font-Size="Small"></asp:Label>
                                        <asp:TextBox ID="TB_Remarks_CompressionStrength" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Compression Strength Remarks"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_FlutePercent" runat="server" AssociatedControlID="TB_FlutePercent" Text="Flute %:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_FlutePercent" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_FlutePercent" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_FlutePercent" runat="server" ControlToValidate="TB_FlutePercent" ValidationGroup="Submit" ErrorMessage="[40 - 60]" MinimumValue="40" MaximumValue="60" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FlutePercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Flute Percentage" onkeyup="validateRange3(40, 60, 'divRemarks_TB_FlutePercent')"></asp:TextBox>
                                    </div>
                                    <!-- Remarks Field -->
                                    <div class="mt-2" id="divRemarks_TB_FlutePercent" style="display: none;">
                                        <asp:Label ID="Label2" runat="server" Text="Flute % Remarks:" ForeColor="Red" Font-Size="Small"></asp:Label>
                                        <asp:TextBox ID="TB_Remarks_FlutePercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Flute % Remarks"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_MoisturePercent" runat="server" AssociatedControlID="TB_MoisturePercent" Text="Moisture %:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_MoisturePercent" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MoisturePercent" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RangeValidator ID="RV_TB_MoisturePercent" runat="server" ControlToValidate="TB_MoisturePercent" ValidationGroup="Submit" ErrorMessage="[7 - 9]" MinimumValue="7" MaximumValue="9" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MoisturePercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Moisture Percentage" onkeyup="validateRange4(7, 9, 'divRemarks_MoisturePercent')"></asp:TextBox>
                                    </div>
                                    <!-- Remarks Field -->
                                    <div class="mt-2" id="divRemarks_MoisturePercent" style="display: none;">
                                        <asp:Label ID="Label3" runat="server" Text="Moisture % Remarks:" ForeColor="Red" Font-Size="Small"></asp:Label>
                                        <asp:TextBox ID="TB_Remarks_MoisturePercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Moisture % Remarks"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Remarks" runat="server" AssociatedControlID="TB_Remarks" Text="Remarks:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" TextMode="MultiLine" Placeholder="Enter any additional remarks" Rows="4" MaxLength="500"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click to SUBMIT" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="btnSubmit_Click" />
                                        <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnReset_Click" />
                                        <%--<asp:Button ID="Button1" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BtnReset_Click" />--%>
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
