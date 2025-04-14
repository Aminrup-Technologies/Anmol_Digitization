<%@ Page Title="AIL | PVC Tray / Mono CB Form" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="PVC_Tray.aspx.cs" Inherits="AnmolDristi.PVC__Tray" Async="true" %>

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

        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hdn_formid" runat="server" />
    <asp:HiddenField ID="hdn_shiftvalue" runat="server" />

    <script type="text/javascript">

        function showNotification(title, text, type) {
            new PNotify({
                title: title,
                text: text,
                type: type,
                styling: 'bootstrap3'
            });
        }
    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="PVC Tray / Mono CB Report"></asp:Label></h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QC/PKNG/05"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                     <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_MatVarietyName" runat="server" AssociatedControlID="TB_MatVarietyName" Text="Material / Variety" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_MatVarietyName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MatVarietyName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_MatVarietyName" runat="server" ControlToValidate="TB_MatVarietyName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[a-zA-Z, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MatVarietyName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Material / Variety" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Material / Variety" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="0" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="TB_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_BrandSKU" runat="server" ControlToValidate="TB_BrandSKU" ValidationGroup="Submit" ForeColor="Red" Display="Dynamic" ErrorMessage="Enter valid decimal value" ValidationExpression="^\d+(\.\d{1,2})?$"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="e.g. 10.25"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>--%>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_Supplier" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
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
                                    <asp:Label ID="Lbl_ChalanNo" runat="server" AssociatedControlID="TB_ChalanNo" Text="Chalan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChalanNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_ChalanNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChalanNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_ChalanDate" runat="server" AssociatedControlID="TB_ChalanDate" Text="Chalan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_ChalanDate" runat="server" ErrorMessage="*" ControlToValidate="TB_ChalanDate" ValidationGroup="Submit" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChalanDate" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" TextMode="Date" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="LOT/ Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_LotNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_VehicleNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
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

                            <!-- Length Input -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DimensionL" runat="server" AssociatedControlID="TB_DimensionL" Text="Observation (Length in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionL" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DimensionL" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DimensionL" runat="server" ValidationGroup="Submit" ControlToValidate="TB_DimensionL" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionL" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_DimensionL" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionL" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Length [0.00-1000.00]"></asp:TextBox>
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

                            <!-- Repeat for Width -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DimensionW" runat="server" AssociatedControlID="TB_DimensionW" Text="Observation (Width in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionW" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DimensionW" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DimensionW" runat="server" ValidationGroup="Submit" ControlToValidate="TB_DimensionW" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionW" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_DimensionW" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionW" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Width [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Standard Value TextBox for Height -->
                            <div class="col-md-3">
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

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DimensionH" runat="server" AssociatedControlID="TB_DimensionH" Text="Observation (Height in mm) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DimensionH" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DimensionH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DimensionH" runat="server" ValidationGroup="Submit" ControlToValidate="TB_DimensionH" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d{1,4}(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_DimensionH" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_DimensionH" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DimensionH" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="Height [0.00-1000.00]"></asp:TextBox>
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

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_GSM" runat="server" AssociatedControlID="TB_GSM" Text="Observation GSM / Wt per 10pc :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_GSM" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_GSM" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GSM" runat="server" ValidationGroup="Submit" ControlToValidate="TB_GSM" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d{1,4}(\.\d{1,2})?$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_GSM" runat="server" ErrorMessage="0.00 to 1000.00" ValidationGroup="Submit" ControlToValidate="TB_GSM" MinimumValue="0.00" MaximumValue="1000.00" Type="Double" Display="Dynamic" ForeColor="Red"></asp:RangeValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GSM" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit" Placeholder="GSM [0.00-1000.00]"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Remarks" runat="server" AssociatedControlID="TB_Remarks" Text="Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remarks" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click to SUBMIT" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="btnSubmit_Click" />
                                        <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" OnClick="btnReset_Click" CausesValidation="false" />
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
