<%@ Page Title="RAW MATERIAL FORM" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="RM_Class5.aspx.cs" Inherits="AnmolDristi.RM_Class5" Async="true" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        function showNotification(title, text, type) {
            new PNotify({
                title: title,
                text: text,
                type: type,
                styling: 'bootstrap3'
            });
        }
        function toggleColorRemarksDiv(radioButtonList) {
            console.log("toggleColorRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("ColorRemarksDiv");
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
        function toggleDispersibilityRemarksDiv(radioButtonList) {
            console.log("toggleDispersibilityRemarksDiv function called");
            var selectedValue = radioButtonList.querySelector("input:checked").value;
            var remarksDiv = document.getElementById("DispersibilityRemarksDIV");
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
    </script>

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Preservatives and Enhancers"></asp:Label></h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QC/RM"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" Text="Plant :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                  <%--  [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                     <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0" ValidationGroup="Submit"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true"  ></asp:DropDownList>

                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" Text="Material :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                     <%--<asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Material" Display="Dynamic" InitialValue="0" ValidationGroup="Submit"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true"  ></asp:DropDownList>

                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3" id="CategoryDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_ProductCategory" runat="server" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*"  ForeColor="Red" InitialValue="0" ValidationGroup="Submit" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" Text="Product Brand :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Supplier" runat="server" Text="Supplier :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%-- <asp:RequiredFieldValidator ID="RFV_TB_Supplier" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_Supplier" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_ChalanNo" runat="server" Text="Chalan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_ChalanNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_ChalanNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChalanNo" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_ChalanDate" runat="server"  Text="Chalan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_ChalanDate" runat="server" ErrorMessage="*" ControlToValidate="TB_ChalanDate" ValidationGroup="Submit" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_ChalanDate" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server"  Text="Quantity :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_Quantity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Quantity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Quantity" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Quantity" ForeColor="Red" ErrorMessage="Numeric Only" ValidationExpression="\d+" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <asp:RangeValidator ID="RV_TB_Quantity" runat="server" ErrorMessage="N1 to N2" ValidationGroup="Submit" ControlToValidate="TB_Quantity" MinimumValue="50" MaximumValue="100" Type="Integer" Display="Static" ForeColor="Red" ></asp:RangeValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Quantity" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_LotNo" runat="server"  Text="LOT/ Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_LotNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_LotNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_VehicleNo" runat="server"  Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_TB_VehicleNo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_VehicleNo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server"  Text="MFG Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_MFG_Date" runat="server" ErrorMessage="*" ControlToValidate="TB_MFG_Date" ValidationGroup="Submit" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MFG_Date" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date" Placeholder=""></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" Text="Grade :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%-- <asp:RequiredFieldValidator ID="RFV_TB_Grade" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TB_Grade" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Grade" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3" id="ColorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Color" runat="server"  Text=" Color  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_RBL_Color" runat="server" ErrorMessage="*"  ForeColor="Red" ValidationGroup="Submit" ControlToValidate="RBL_Color" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Color" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleColorRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="ColorRemarksDiv" runat="server" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_ColorRemarksDiv" runat="server"  Text="Color Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_ColorRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_Color_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Color_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="SmellDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Smell" runat="server" Text="Odour/Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_RBL_Smell" runat="server" ErrorMessage="*"  ForeColor="Red" ValidationGroup="Submit" ControlToValidate="RBL_Smell" Display="Dynamic"></asp:RequiredFieldValidator>--%>
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
                                    <asp:Label ID="Lbl_SmellRemarks" runat="server"  Text="Smell Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_TXB_Smell_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_Smell_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Smell_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="AppearanceDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Appearance" runat="server"  Text="Appearance :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_RBL_Appearance" runat="server" ErrorMessage="*"  ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_Appearance" Display="Dynamic"></asp:RequiredFieldValidator>--%>
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
                                    <asp:Label ID="Lbl_AppearancepRemarks" runat="server"  Text=" Appearance Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_Appearance_Remarks" runat="server"  ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_Appearance_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Appearance_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="TasteFlavorDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TasteFlavor" runat="server"  Text="Taste/Flavor :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_RBL_TasteFlavor" runat="server" ErrorMessage="*"  ValidationGroup="Submit" ForeColor="Red" ControlToValidate="RBL_TasteFlavor" Display="Dynamic"></asp:RequiredFieldValidator>--%>
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
                                    <asp:Label ID="Lbl_TasteFlavorRemarks" runat="server"  Text=" Taste/Flavor Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_TasteFlavor_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_TasteFlavor_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_TasteFlavor_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3" id="ImpuritiesDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Foreign_Impurities" runat="server"  Text="ForeignMatter/Impurities :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                  <%--  <asp:RequiredFieldValidator ID="RFV_TB_Foreign_Impurities" runat="server" ValidationGroup="Submit" ErrorMessage="Input Required"  ControlToValidate="TB_Foreign_Impurities" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Foreign_Impurities" runat="server" CssClass="form-control form-control-sm rounded" ></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Purity" runat="server" Text="Purity(%) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_Purity" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Purity" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Purity" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Purity" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Purity" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3" id="PHDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_PH" runat="server"  Text=" PH Value :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_TB_PH" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_PH" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_PH" runat="server" ControlToValidate="TB_PH" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_PH" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3" id="MoistureDIV" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Moisture" runat="server"  Text="Moisture(%) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_Moisture" runat="server" ErrorMessage="*" ValidationGroup="Submit"  ControlToValidate="TB_Moisture" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Moisture" runat="server"  ControlToValidate="TB_Moisture" ValidationGroup="Submit" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Moisture" runat="server" CssClass="form-control form-control-sm rounded" ></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label7" runat="server" Text="Acid Value(%) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_AcidValue" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_AcidValue" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_AcidValue" runat="server" ValidationGroup="Submit" ControlToValidate="TB_AcidValue" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_AcidValue" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label8" runat="server" Text="MP/CP  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_MP" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_MP" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_MP" runat="server" ValidationGroup="Submit" ControlToValidate="TB_MP" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_MP" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3" id="Div1" runat="server">
                                <div class="mb-3">
                                    <asp:Label ID="Label9" runat="server"  Text="Dispersibility in Water :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                   <%-- <asp:RequiredFieldValidator ID="RFV_RBL_Dispersibility" runat="server" ErrorMessage="*"  ForeColor="Red" ValidationGroup="Submit" ControlToValidate="RBL_Dispersibility" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:RadioButtonList ID="RBL_Dispersibility" runat="server" CssClass="form-control form-control-sm rounded remove-border" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleDispersibilityRemarksDiv(this);">
                                            <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3" id="DispersibilityRemarksDIV" runat="server" style="display: none;">
                                <div class="mb-3">
                                    <asp:Label ID="Label10" runat="server"  Text="Dispersibility Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_DispersibilityRemarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_Color_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DispersibilityRemarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label11" runat="server" Text="DRC in 1 hr  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_DRC" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_DRC" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DRC" runat="server" ValidationGroup="Submit" ControlToValidate="TB_DRC" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DRC" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label12" runat="server" Text="Water Insoluble Matter(%)  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_WIM" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_WIM" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_WIM" runat="server" ValidationGroup="Submit" ControlToValidate="TB_WIM" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_WIM" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label13" runat="server" Text="Mono Glyceride Content (%)  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_GlycerideCont" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_GlycerideCont" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_GlycerideCont" runat="server" ValidationGroup="Submit" ControlToValidate="TB_GlycerideCont" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_GlycerideCont" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label14" runat="server" Text="Neutralizing Value  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_NeutralizingVal" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_NeutralizingVal" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_NeutralizingVal" runat="server" ValidationGroup="Submit" ControlToValidate="TB_NeutralizingVal" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_NeutralizingVal" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                             <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label15" runat="server" Text="Brix  :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <%--<asp:RequiredFieldValidator ID="RFV_TB_Brix" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TB_Brix" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Brix" runat="server" ValidationGroup="Submit" ControlToValidate="TB_Brix" ForeColor="Red" ErrorMessage="Decimal Only" ValidationExpression="^\d+(\.\d+)?$" Display="Dynamic"></asp:RegularExpressionValidator>--%>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Brix" runat="server" CssClass="form-control form-control-sm rounded" Placeholder=""  TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Remarks" runat="server" AssociatedControlID="TB_Remarks" Text="Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Remarks" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="TB_Remarks" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Remarks" runat="server" CssClass="form-control form-control-sm rounded"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click to SUBMIT" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary btn-sm" CausesValidation="false"  />
                                        <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" />
                                    </div>
                                </div>
                            </div>

                        </div>

                    </div>
                </div>
            </div>
             <%--<div class="row">
                <div class="col-md-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="Label16" runat="server" Text="Approval Matrix"></asp:Label></h2>
                            <div class="clearfix"></div>

                        </div>
                        <div class="x_content">
                            <!-- Approver Flow Diagram -->
                            <div class="approver-flow">
                                <div class="approver-item">
                                    <p>
                                        <asp:Label ID="Label17" runat="server" Text="Approver 1" />
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
                                        <asp:Label ID="Label18" runat="server" Text="Approver 2" />
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
                                        <asp:Label ID="Label19" runat="server" Text="Approver 3" />
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
            </div>--%>
        </div>
    </div>
</asp:Content>
