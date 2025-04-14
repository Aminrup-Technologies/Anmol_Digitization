<%@ Page Title="Production | Oven Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_oven_report.aspx.cs" Inherits="AnmolDristi.qaqc_oven_report" %>

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

        /*.nav-tabs .nav-link.active {
            background-color: #17a2b8;
            color: white;
            border: 2px solid #17a2b8;
            border-radius: 5px;
        }

        .nav-tabs .nav-link:hover {
            background-color: #e9ecef;
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
    <%-- <script type="text/javascript">
    function ValidateVarietyPktLength(sender, args) {
        var value = args.Value;
        args.IsValid = value.length >= 3 && value.length <= 20;
    }
</script>--%>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />
    <asp:HiddenField ID="hdn_formid" runat="server" />
    <script type="text/javascript">
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

        function validateRadioButtons(source, args) {
            var yesChecked = document.getElementById('<%= RB_Yes.ClientID %>').checked;
            var noChecked = document.getElementById('<%= RB_No.ClientID %>').checked;
            args.IsValid = yesChecked || noChecked;  // Ensures at least one option is selected
        }

        function toggleFields() {
            var yesChecked = document.getElementById('<%= RB_Yes.ClientID %>').checked;
            var startTime = document.getElementById('<%= TB_StartTime.ClientID %>');
            var stopTime = document.getElementById('<%= TB_StopTime.ClientID %>');
            var reason = document.getElementById('<%= TB_Reason.ClientID %>');

            if (!yesChecked) {  // If "No" is selected, set defaults
                startTime.value = "00:00:00";
                stopTime.value = "00:00:00";
                reason.value = "N/A";
                startTime.readOnly = true;
                stopTime.readOnly = true;
                reason.readOnly = true;
            } else {  // If "Yes" is selected, enable input fields
                startTime.value = "";
                stopTime.value = "";
                reason.value = "";
                startTime.readOnly = false;
                stopTime.readOnly = false;
                reason.readOnly = false;
            }
        }

        document.addEventListener("DOMContentLoaded", function () {
            toggleFields();
            document.getElementById('<%= RB_Yes.ClientID %>').addEventListener('change', toggleFields);
            document.getElementById('<%= RB_No.ClientID %>').addEventListener('change', toggleFields);
        });

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
                        <div class="row">
                            <div class="col-12">
                                <div class="card">
                                    <div class="card-body">
                                        <div class="row">
                                            <div class="col-12">
                                                <ul class="nav nav-tabs mb-4" id="myTab" role="tablist">
                                                    <li class="nav-item">
                                                        <a class="nav-link active text-info" id="basicData-tab" data-toggle="tab" href="#basicData" role="tab"
                                                            aria-controls="basicData" aria-selected="true"><%--<i class="fa fa-id-badge mr-2"></i>--%> Basic Data</a>
                                                    </li>


                                                    <li class="nav-item">
                                                        <a class="nav-link" id="ovenReport-tab" data-toggle="tab" href="#ovenReport" role="tab"
                                                            aria-controls="ovenReport" aria-selected="false">Oven Report</a>
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

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Plant Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
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

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_VartyPkt" runat="server" AssociatedControlID="TB_VartyPkt" Text="Variety Packet:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_VartyPkt" runat="server" ErrorMessage="Required" ControlToValidate="TB_VartyPkt" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_VartyPkt" runat="server" ControlToValidate="TB_VartyPkt" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:CustomValidator ID="CV_TB_VartyPkt_Length"  runat="server"  ControlToValidate="TB_VartyPkt" ErrorMessage="Variety Packet must be between 3 and 20 characters"  ForeColor="Red"  ValidationGroup="Submit"  ClientValidationFunction="ValidateVarietyPktLength"  Display="Dynamic"> </asp:CustomValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_VartyPkt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Variety Packet (3-20 characters)" MaxLength="20"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_RejectionKgs" runat="server" AssociatedControlID="TB_RejectionKgs" Text="Rejection Qty (in Kgs):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_RejectionKgs" runat="server" ErrorMessage="Required" ControlToValidate="TB_RejectionKgs" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_RejectionKgs" runat="server" ControlToValidate="TB_RejectionKgs" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_RejectionKgs" runat="server" ValidationGroup="Submit" ControlToValidate="TB_RejectionKgs" ErrorMessage="Rejection (in Kgs) should be between 0.00 and 1000.00" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>

                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RejectionKgs" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Rejection (in Kgs)" MaxLength="10"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-12 text-center">
                                                                <asp:Button ID="Btn_Basic_Data" runat="server" Text="Proceed Next" OnClick="Btn_Basic_Data_Click" CausesValidation="true" ValidationGroup="Submit" CssClass="btn btn-sm btn-primary" />
                                                                <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btnReset_Click" />
                                                                <asp:Button ID="Button1" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                <asp:Label ID="lblMessage" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <%--  oven section start --%>
                                                    <div class="tab-pane fade" id="ovenReport" role="tabpanel" aria-labelledby="ovenReport-tab">
                                                        <div class="x_content">
                                                            <div class="col-md-12">
                                                                <h4 class="text-left text-info">Sample Details</h4>
                                                                <hr />
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_BiscuitsPerPkt" runat="server" AssociatedControlID="TB_BiscuitsPerPkt" Text="No. of Biscuits(in Pcs):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_BiscuitsPerPkt" runat="server" ErrorMessage="Required" ControlToValidate="TB_BiscuitsPerPkt" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_BiscuitsPerPkt" runat="server" ControlToValidate="TB_BiscuitsPerPkt" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_BiscuitsPerPkt" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_BiscuitsPerPkt" ErrorMessage="No. of Bis./Pkt. should be between 1 and 1000" ForeColor="Red" MinimumValue="1" MaximumValue="1000" Type="Integer" Display="Dynamic"></asp:RangeValidator>

                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BiscuitsPerPkt" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter No. of Biscuits(in Pcs)" MaxLength="100"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <!-- Gauge (in mm) Section -->
                                                            <%-- <div class="row mb-3">--%>
                                                            <%-- <div class="col-md-12 text-center mb-3">
                                                                    <h4 style="color: blue;">DRY</h4>
                                                                </div>--%>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Gauge" runat="server" AssociatedControlID="TB_Gauge" Text=" Dry Gauge Size (mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Gauge" runat="server" ErrorMessage="Required" ControlToValidate="TB_Gauge" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_Gauge" runat="server" ControlToValidate="TB_Gauge" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_Gauge" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_Gauge" ErrorMessage="Gauge should be between 0.00 mm and 1000.00 mm" ForeColor="Red" MinimumValue="0.00" MaximumValue="100.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Gauge" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Gauge (in mm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3" id="TB_BakingTime_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_BakingTime" runat="server" AssociatedControlID="TB_BakingTime" Text="Baking Time (mm:ss) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_BakingTime" runat="server" ErrorMessage="Required" ValidationGroup="Submit2" ControlToValidate="TB_BakingTime" Display="Dynamic" ForeColor="Red" InitialValue="00:00"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_BakingTime" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_BakingTime" ForeColor="Red" ErrorMessage="Please enter time in mm:ss format" ValidationExpression="^([0-9][0-9]):([0-5][0-9])$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_BakingTime" runat="server" ControlToValidate="TB_BakingTime" ValidationGroup="Submit2" ErrorMessage="[00:00 - 15:00]" ForeColor="Red" MinimumValue="00:00" MaximumValue="15:00" Type="String" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BakingTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Baking Time (mm:ss)" Text="00:00"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_BTRPM" runat="server" AssociatedControlID="TB_BTRPM" Text="B.T/R.P.M:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_BTRPM" runat="server" ErrorMessage="Required" ControlToValidate="TB_BTRPM" ValidationGroup="Submit2" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_BTRPM" runat="server" ControlToValidate="TB_BTRPM" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_BTRPM" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_BTRPM" ErrorMessage="RPM must be between 0 and 1000" MinimumValue="0" MaximumValue="5000" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BTRPM" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=" Enter B.T/R.P.M" MaxLength="10"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <div class="col-md-12">
                                                                <hr />
                                                            </div>
                                                            <div class="col-md-12">
                                                                <h4 class="text-left text-info">Dimensions Details</h4>
                                                                <hr />
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Length" runat="server" AssociatedControlID="TB_Length" Text="Length (for square shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Length" runat="server" ErrorMessage="Required" ControlToValidate="TB_Length" InitialValue="" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_Length" runat="server" ControlToValidate="TB_Length" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_Length" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_Length" ErrorMessage="Length should be between 1.00 mm and 1000.00 mm" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Length" runat="server" CssClass="form-control form-control-sm rounded" Text="" Placeholder="Enter Length (in mm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Width" runat="server" AssociatedControlID="TB_Width" Text="Width (for square shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Width" runat="server" ErrorMessage="Required" ControlToValidate="TB_Width" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_Width" runat="server" ControlToValidate="TB_Width" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_Width" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_Width" ErrorMessage="Width should be between 1.00 mm and 1000.00 mm" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Width" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Width (in mm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Diameter" runat="server" AssociatedControlID="TB_Diameter" Text="Diameter (for round shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Diameter" runat="server" ErrorMessage="Required" ControlToValidate="TB_Diameter" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_Diameter" runat="server" ControlToValidate="TB_Diameter" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_Diameter" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_Diameter" ErrorMessage="1.00 mm and 1000.00 mm" ForeColor="Red" MinimumValue="0.00" MaximumValue="500.00" Type="Double" Display="Dynamic"></asp:RangeValidator>

                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Diameter" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Diameter (in mm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-12">
                                                                <hr />
                                                            </div>
                                                            <div class="col-md-12">
                                                                <h4 class="text-left text-info">Weitage Details</h4>
                                                                <hr />
                                                            </div>

                                                            <div class="col-md-3" id="TB_wgtwtoil_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_wgtwtoil" runat="server" AssociatedControlID="TB_wgtwtoil" Text="Weight without oil /Dry Weight (g):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_wgtwtoil" ValidationGroup="Submit2" runat="server" ErrorMessage="*" InitialValue="" ControlToValidate="TB_wgtwtoil" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_wgtwtoil" ValidationGroup="Submit2" runat="server" ControlToValidate="TB_wgtwtoil" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_wgtwtoil" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_wgtwtoil" ErrorMessage="Weight with oil should be between 0.00 and 100.00 kg" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_wgtwtoil" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Weight with oil (0.00 - 100.00 g)" Text="" oninput="calculateOilPercentage()"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>



                                                            <div class="col-md-3" id="TB_wgtwoil_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_wgtwoil" runat="server" AssociatedControlID="TB_wgtwoil" Text="Weight with oil / Dipped Weight (g):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_wgtwoil" runat="server" ValidationGroup="Submit2" ErrorMessage="*" InitialValue="" ControlToValidate="TB_wgtwoil" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_wgtwoil" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_wgtwoil" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_wgtwoil" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_wgtwoil" ErrorMessage="Weight without oil should be between 0.00 and 1000.00 g" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_wgtwoil" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Weight without oil (0.00 - 1000.00 g)" Text="" oninput="calculateOilPercentage()"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>




                                                            <div class="col-md-3" id="TB_oilpercent_DIV" runat="server" visible="true">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_oilpercent" runat="server" AssociatedControlID="TB_oilpercent" Text="Oil Percentage (%):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_oilpercent" runat="server" ValidationGroup="Submit2" InitialValue="0.00" ErrorMessage="*" ControlToValidate="TB_oilpercent" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_oilpercent" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_oilpercent" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="\d+(\.\d{1,2})?" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_oilpercent" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_oilpercent" ErrorMessage="0.00% and 50.00%" ForeColor="Red" MinimumValue="0.00" MaximumValue="50.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_oilpercent" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Oil Percentage (0.00% - 100.00%)" Text="0.00" ReadOnly="false"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Weight" runat="server" AssociatedControlID="TB_Weight" Text="Dry Weight (gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Weight" runat="server" ErrorMessage="Required" ControlToValidate="TB_Weight" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_Weight" runat="server" ControlToValidate="TB_Weight" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_Weight" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_Weight" ErrorMessage="Weight should be between 0.00 gm and 1000.00 gm" ForeColor="Red" MinimumValue="0.00" MaximumValue="500.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Weight" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Weight (in gm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_DippedWeight" runat="server" AssociatedControlID="TB_DippedWeight" Text="Dipped Weight (gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_DippedWeight" runat="server" ErrorMessage="Required" ControlToValidate="TB_DippedWeight" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_DippedWeight" runat="server" ControlToValidate="TB_DippedWeight" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_DippedWeight" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_DippedWeight" ErrorMessage="Dipped Weight should be between 0.00 gm and 1000.00 gm" ForeColor="Red" MinimumValue="0.00" MaximumValue="500.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DippedWeight" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Dipped Weight (in gm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>--%>

                                                            <%--<div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_PktWeight" runat="server" AssociatedControlID="TB_PktWeight" Text="Pkt. Weight (in gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_PktWeight" runat="server" ErrorMessage="Required" ControlToValidate="TB_PktWeight" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_PktWeight" runat="server" ControlToValidate="TB_PktWeight" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Numeric Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <asp:RangeValidator ID="RV_TB_PktWeight" runat="server" ValidationGroup="Submit2" ControlToValidate="TB_PktWeight" ErrorMessage="Pkt. Weight should be between 0.00 gm and 1000.00 gm" ForeColor="Red" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic"></asp:RangeValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_PktWeight" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Pkt. Weight (in gm)" MaxLength="5"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>--%>


                                                            <div class="col-md-12">
                                                                <hr />
                                                            </div>
                                                            <div class="col-md-12">
                                                                <h4 class="text-left text-info">Oven Downtime Details (If Applicable)</h4>
                                                                <hr />
                                                            </div>

                                                            <div class="col-md-12">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_Applicable" runat="server" Text="Is Applicable?" ForeColor="Blue" Font-Bold="true"></asp:Label>
                                                                    <asp:RadioButton ID="RB_Yes" runat="server" GroupName="Applicable" Text=" Yes" />
                                                                    <asp:RadioButton ID="RB_No" runat="server" GroupName="Applicable" Text=" No" />
                                                                    <asp:CustomValidator ID="CV_Applicable" runat="server"
                                                                        ErrorMessage="Please select Yes or No."
                                                                        ForeColor="Red" Display="Dynamic" ValidationGroup="Submit2"
                                                                        ClientValidationFunction="validateRadioButtons"></asp:CustomValidator>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_StartTime" runat="server" AssociatedControlID="TB_StartTime" Text="Oven Stopage Start Time:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_StartTime" runat="server" ErrorMessage="Required" ControlToValidate="TB_StartTime" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_StartTime" runat="server" ControlToValidate="TB_StartTime" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Valid Time Format (HH:MM:SS) Required" ValidationExpression="^([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StartTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Start Time (HH:MM:SS)" MaxLength="8"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_StopTime" runat="server" AssociatedControlID="TB_StopTime" Text="Oven Stopage Stop Time:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_StopTime" runat="server" ErrorMessage="Required" ControlToValidate="TB_StopTime" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <asp:RegularExpressionValidator ID="REV_TB_StopTime" runat="server" ControlToValidate="TB_StopTime" ForeColor="Red" ValidationGroup="Submit2" ErrorMessage="Valid Time Format (HH:MM:SS) Required" ValidationExpression="^([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StopTime" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Stop Time (HH:MM:SS)" MaxLength="8"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Reason" runat="server" AssociatedControlID="TB_Reason" Text="Stoppage Reason:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <asp:RequiredFieldValidator ID="RFV_TB_Reason" runat="server" ErrorMessage="Required" ControlToValidate="TB_Reason" ValidationGroup="Submit2" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Reason" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Reason" MaxLength="100"></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-12 text-center">
                                                                <asp:Button ID="btn_oven_save" runat="server" Text="Final Submit" OnClick="btn_oven_save_Click" CausesValidation="true" ValidationGroup="Submit2" CssClass="btn btn-sm btn-primary" />
                                                                <asp:Button ID="btn_oven_reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btn_oven_reset_Click" />
                                                                <asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
                                                                <asp:Label ID="lbl_oven" runat="server" ForeColor="Red" Font-Bold="true"></asp:Label>
                                                            </div>

                                                        </div>

                                                    </div>
                                                    <%--  oven section end --%>
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
