<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="PM_LaminateTesting.aspx.cs" Inherits="AnmolDristi.PM_LaminateTesting" %>

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
    <asp:HiddenField ID="hdn_formid" runat="server" />

    <script type="text/javascript">

        function toggleSmellRemarksDiv(radioButtonList) {
            console.log("toggleSmellRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("SmellRemarksDiv");
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

        function toggleLengthRemarksDIV(textBox) {
            console.log("toggleLengthRemarksDIV function called");
            //var standardLengthElement = parseFloat(document.getElementById('<%= span_L.ClientID %>'));
            //console.log("toggleLengthRemarksDIV", standardLengthElement.value);
            //var standardLength = parseFloat(standardLengthElement.innerText || standardLengthElement.innerText);
            var standardLength = parseFloat(document.getElementById('<%= span_L.ClientID %>'));

            var length = parseFloat(document.getElementById('<%=TB_Length.ClientID%>').value);
            var tolerance = 5;
            var lowerBound = standardLength - tolerance;
            var upperBound = standardLength + tolerance;

            var remarksDiv = document.getElementById("LengthRemarksDIV");
            if (!isNaN(length) && (length < lowerBound || length > upperBound)) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleWidthRemarksDIV(textBox) {
            console.log("toggleWidthRemarksDIV function called");
            var standardWidth = parseFloat(document.getElementById('<%= span_W.ClientID %>').textContent);
            var width = parseFloat(document.getElementById('<%=TB_Width.ClientID%>').value);
            var tolerance = 5;
            var lowerBound = standardWidth - tolerance;
            var upperBound = standardWidth + tolerance;

            var remarksDiv = document.getElementById("WidthRemarksDIV");
            if (!isNaN(width) && width !== "" && (width < lowerBound || width > upperBound)) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleHeightRemarksDIV(textBox) {
            console.log("toggleHeightRemarksDIV function called");
            var standardHeight = parseFloat(document.getElementById('<%= span_H.ClientID %>').textContent);
            var height = parseFloat(document.getElementById('<%=TB_Height.ClientID%>').value);
            var tolerance = 5;
            var lowerBound = standardHeight - tolerance;
            var upperBound = standardHeight + tolerance;

            var remarksDiv = document.getElementById("LengthRemarksDIV");
            if (!isNaN(height) && height !== "" && (height < lowerBound || height > upperBound)) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

        function toggleGSMRemarksDIV(textBox) {
            console.log("toggleGSMRemarksDIV function called");
            var standarGsm = parseFloat(document.getElementById('<%= span_GSM.ClientID %>').textContent);
            var gsm = parseFloat(document.getElementById('<%=TB_GSM.ClientID%>').value);
            var tolerance = 5;
            var lowerBound = standarGsm - tolerance;
            var upperBound = standarGsm + tolerance;

            var remarksDiv = document.getElementById("LengthRemarksDIV");
            if (!isNaN(gsm) && gsm !== "" && (gsm < lowerBound || gsm > upperBound)) {
                remarksDiv.style.display = "block";

            } else {
                remarksDiv.style.display = "none";
            }
        }

    </script>

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

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>] 
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>] 
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
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

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required" ControlToValidate="TB_Supplier" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Supplier" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Supplier" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Supplier (3-20 characters)" MaxLength="20"></asp:TextBox>
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
                                        <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Size(in pkts)"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="LabelSmell" runat="server" AssociatedControlID="RBL_Smell" Text="Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_RBL_Smell" runat="server" ErrorMessage="*" ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Smell" Display="Dynamic"></asp:RequiredFieldValidator>
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
                                    <asp:Label ID="LabelSmellRemarks" runat="server" AssociatedControlID="TXB_Smell_Remarks" Text="Smell (Not Ok)" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_SmellRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Smell_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Smell_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_ChallanNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanNo" runat="server" ControlToValidate="TB_ChallanNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Challan No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChallanDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_ChallanDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_ChallanDate" runat="server" ControlToValidate="TB_ChallanDate" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_LotNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_LotNo" runat="server" ControlToValidate="TB_LotNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Lot/Gate No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_VehicleNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_VehicleNo" runat="server" ControlToValidate="TB_VehicleNo" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Vehicle No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Bond" runat="server" AssociatedControlID="TB_Bond" Text="Bond Strength :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Bond" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Bond" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Bond" runat="server" ControlToValidate="TB_Bond" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Bond" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Bond Strength Value "></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Seal" runat="server" AssociatedControlID="TB_Seal" Text="Seal Strength :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Seal" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Seal" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Seal" runat="server" ControlToValidate="TB_Seal" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d{1,3})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Seal" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Seal Strength Value "></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Standard Length Input -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Std_DimensionL" runat="server" AssociatedControlID="TB_Std_DimensionL" Text="Standard Length (mm):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Std_DimensionL" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionL" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Std_DimensionL" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionL" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Std_DimensionL" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionL" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Std_DimensionL" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Standard Length [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Observed Value TextBox for Length -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Length" runat="server" AssociatedControlID="TB_Length" Text="Observered Length Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:Literal ID="span_L" runat="server" Text='<%# string.IsNullOrEmpty(Eval("Dimension_Std_L")?.ToString()) ? "0.00" : Eval("Dimension_Std_L") %>'></asp:Literal>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Length" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Length" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Length" runat="server" ControlToValidate="TB_Length" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Length" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Length" oninput="toggleLengthRemarksDIV(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Remarks TextBox for Length -->
                            <div class="col-md-3" id="LengthRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label19" runat="server" AssociatedControlID="TXB_Length_Remarks" Text="Length Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_LengthRemarks" runat="server" ErrorMessage="*" ForeColor="Red"  ControlToValidate="TXB_Length_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Length_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Standard Width -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Std_DimensionW" runat="server" AssociatedControlID="TB_Std_DimensionW" Text="Standard Width (mm):" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Std_DimensionW" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionW" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Std_DimensionW" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionW" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Std_DimensionW" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionW" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Std_DimensionW" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Standard Width [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Observed Value TextBox for Width -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Width" runat="server" AssociatedControlID="TB_Width" Text="Observered Width Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:Literal ID="span_W" runat="server" Text='<%# string.IsNullOrEmpty(Eval("Dimension_Std_W")?.ToString()) ? "0.00" : Eval("Dimension_Std_W") %>'></asp:Literal>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Width" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Width" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Width" runat="server" ControlToValidate="TB_Width" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Width" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Width" oninput="toggleWidthRemarksDIV(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Remarks TextBox for Width -->
                            <div class="col-md-3" id="WidthRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="TXB_Width_Remarks" Text="Width Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Width_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Width_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Width_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Standard Value TextBox for Height -->
                            <div class="col-md-3" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Std_DimensionH" runat="server" AssociatedControlID="TB_Std_DimensionH" Text="Standard Height (mm) :" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Std_DimensionH" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Std_DimensionH" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionH" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d{1,4}(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Std_DimensionH" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_Std_DimensionH" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Std_DimensionH" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Std. Height [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Observed Value TextBox for Height -->
                            <div class="col-md-3" id="Dimension_Height_Row" runat="server" visible="false">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Height" runat="server" AssociatedControlID="TB_Height" Text="Observered Height Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:Literal ID="span_H" runat="server" Text='<%# string.IsNullOrEmpty(Eval("Dimension_Std_H")?.ToString()) ? "0.00" : Eval("Dimension_Std_H") %>'></asp:Literal>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Height" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Height" InitialValue="0.00" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Height" runat="server" ControlToValidate="TB_Height" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Height" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Height" Text="0.00" oninput="toggleHeightRemarksDIV(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                             <!-- Remarks TextBox for Height -->
                            <div class="col-md-3" id="HeightRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="TXB_Height_Remarks" Text="Height Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Height_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Height_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Height_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Standard Value TextBox for GSM -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Std_GSM" runat="server" AssociatedControlID="TB_Std_GSM" Text="Standard GSM / Wt per 10pc :" ForeColor="Black" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Std_GSM" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Std_GSM" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Std_GSM" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Std_GSM" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d{1,4}(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Std_GSM" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_Std_GSM" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Std_GSM" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Std. GSM [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Observed Value TextBox for GSM -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_GSM" runat="server" AssociatedControlID="TB_GSM" Text="Observered GSM Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:Literal ID="span_GSM" runat="server" Text='<%# string.IsNullOrEmpty(Eval("GMS_Std")?.ToString()) ? "0.00" : Eval("GMS_Std") %>'></asp:Literal>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GSM" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_GSM" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GSM" runat="server" ControlToValidate="TB_GSM" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GSM" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="GSM " oninput="toggleGSMRemarksDIV(this);"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Remarks TextBox for GSM -->
                            <div class="col-md-3" id="GSMRemarksDIV" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="TXB_GSM_Remarks" Text="GSM Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_GSM_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="" ControlToValidate="TXB_GSM_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_GSM_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Remarks" runat="server" AssociatedControlID="TXB_Remarks" Text="Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TXB_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TXB_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TXB_Remarks" runat="server" ControlToValidate="TXB_Remarks" ForeColor="Red" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Remarks"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--Button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="BtnSubmit_Click" />
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
