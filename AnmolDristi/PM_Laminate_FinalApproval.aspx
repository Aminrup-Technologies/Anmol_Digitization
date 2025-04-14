<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="PM_Laminate_FinalApproval.aspx.cs" Inherits="AnmolDristi.PM_Laminate_FinalApproval" %>

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

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Daily Laminate Testing"></asp:Label>
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

                        <%--form start--%>
                        <div class="row">
                            <div class="col-12">
                                <div class="card">
                                    <div class="card-body">
                                        <div class="row">
                                            <div class="col-12">
                                                <div class="x-content">

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:DropDownList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:DropDownList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Supplier_Name") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="SizeDIV" runat="server">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Size" runat="server" AssociatedControlID="TB_Size" Text="Sample Size :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Size" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelSmell" runat="server" AssociatedControlID="RBL_Smell" Text="Smell :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:RadioButtonList ID="RBL_Smell" runat="server" CssClass="form-control form-control-sm rounded remove-border white-background-readonly" ReadOnly="true" RepeatLayout="Table" RepeatDirection="Horizontal" CellPadding="5" CellSpacing="5" RepeatColumns="2" Width="100%" onchange="toggleSmellRemarksDiv(this);">
                                                                    <asp:ListItem Text="Ok" Value="1"></asp:ListItem>
                                                                    <asp:ListItem Text="Not Ok" Value="0"></asp:ListItem>
                                                                </asp:RadioButtonList>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="SmellRemarksDiv" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="LabelSmellRemarks" runat="server" AssociatedControlID="TXB_Smell_Remarks" Text="Smell (Not Ok)" ForeColor="Brown" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Smell_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Smell_Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Challan_No") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%# Eval("Challan_Date","{0:d d-MM-yyyy}") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Lot_No") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Vehicle_No") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Bond" runat="server" AssociatedControlID="TB_Bond" Text="Bond Strength :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Bond" runat="server" CssClass="form-control form-control-sm rounded  white-background-readonly " ReadOnly="true" Text='<%#  Eval("Bond_Strength") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Seal" runat="server" AssociatedControlID="TB_Seal" Text="Bond Strength :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Seal" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly " ReadOnly="true" Text='<%#  Eval("Seal_Strength") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Length" runat="server" AssociatedControlID="TB_Length" Text="Observed Length Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:Label ID="Label_Std_Length" runat="server" Text='<%# "Std L value" + Eval("Std_Length") %>' EnableViewState="false" ForeColor="Red"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Length" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Obs_Length") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="LengthRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label19" runat="server" AssociatedControlID="TXB_Length_Remarks" Text="Length Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Length_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Length_Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Width" runat="server" AssociatedControlID="TB_Width" Text=" Observed Width Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:Label ID="Label_Std_Width" runat="server" Text='<%#Eval("Std_Width")%>' EnableViewState="false" ForeColor="Red"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Width" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval( "Obs_Width") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="WidthRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label2" runat="server" AssociatedControlID="TXB_Width_Remarks" Text="Width Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Width_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Width_Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" runat="server" visible="false">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Height" runat="server" AssociatedControlID="TB_Height" Text="Observed Height Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:Label ID="Label_Std_Height" runat="server" Text='<%#Eval("Std_Height")%>' EnableViewState="false" ForeColor="Red"></asp:Label>
                                                                <asp:TextBox ID="TB_Height" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval(" Obs_Height") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="HeightRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label3" runat="server" AssociatedControlID="TXB_Height_Remarks" Text="Width Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Height_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Height_Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_GSM" runat="server" AssociatedControlID="TB_GSM" Text="Observed GSM Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:Label ID="Label_Std_GSM" runat="server" Text='<%#Eval("GMS_Std")%>' EnableViewState="false" ForeColor="Red"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_GSM" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("GSM_Obs") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="GSMRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label5" runat="server" AssociatedControlID="TXB_GSM_Remarks" Text="GSM Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_GSM_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("GSM_Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_Remarks" runat="server" AssociatedControlID="TXB_Smell_Remarks" Text="Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Remarks") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>



                                                </div>

                                                <%--Button--%>
                                                <div class="col-md-6">
                                                    <div class="mb-3">
                                                        <asp:Label ID="Lbl_BasicbtnApprove" runat="server" AssociatedControlID="" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                        <div class="input-group input-group-sm">
                                                            <asp:Button ID="BtnValidate" runat="server" Text="Re-Validate inputs" CssClass="btn btn-warning btn-sm" CausesValidation="true" />
                                                            <asp:Button ID="BtnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="BtnApprove_Click" />
                                                            <asp:Button ID="BtnReject" runat="server" Text="Reject" CssClass="btn btn-sm btn-danger" CausesValidation="false" OnClick="BtnReject_Click" />
                                                            <asp:Button ID="BtnBack" runat="server" Text="Back" CssClass="btn btn-sm btn-info" CausesValidation="false" PostBackUrl="~/PM_Laminate_Approval.aspx" />
                                                        </div>
                                                    </div>
                                                </div>

                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <%-- form end--%>
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


    <script type="text/javascript">


        window.onload = function () {
            // Disable  RadioButtonLists
            disableRadioButtonLists();

            // Toggle remarks divs based on the selected value for each RadioButtonList
            toggleAllRadioRemarksDivs();

            // Check and toggle textboxes based on the value range
            toggleAllRemarksTextboxVisibility()
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

        function toggleAllRadioRemarksDivs() {
            // Define a mapping of RadioButtonLists and their respective remarks divs
            var mapping = {
            '<%= RBL_Smell.ClientID %>': 'SmellRemarksDiv',
            };

            // Loop through each mapping and toggle the div visibility based on the selected value
            for (var rblId in mapping) {
                var remarksDivId = mapping[rblId];
                toggleRemarksDiv(rblId, remarksDivId);
            }
        }

        function toggleRemarksDiv(rblId, remarksDivId) {
            var rbl = document.getElementById(rblId);

            if (rbl) {
                // Get the selected value from the RadioButtonList
                var selectedValue = rbl.querySelector('input[type="radio"]:checked').value;

                var remarksDiv = document.getElementById(remarksDivId);

                // If "Not Ok" (Value = "0") is selected, show the remarks div, otherwise hide it
                if (selectedValue === "0") {
                    remarksDiv.style.display = "block";
                } else {
                    remarksDiv.style.display = "none";
                }

            }
        }

        function toggleAllRemarksTextboxVisibility() {
            // Define a mapping of observed values, standard values, and remarks TextBoxes
            var fields = [
                { obsId: '<%= TB_Length.ClientID %>', stdId: '<%= Label_Std_Length.ClientID %>', remarksId: '<%= TXB_Length_Remarks.ClientID %>' },
                { obsId: '<%= TB_Width.ClientID %>', stdId: '<%= Label_Std_Width.ClientID %>', remarksId: '<%= TXB_Width_Remarks.ClientID %>' },
                { obsId: '<%= TB_Height.ClientID %>', stdId: '<%= Label_Std_Height.ClientID %>', remarksId: '<%= TXB_Height_Remarks.ClientID %>' },
                { obsId: '<%= TB_GSM.ClientID %>', stdId: '<%= Label_Std_GSM.ClientID %>', remarksId: '<%= TXB_GSM_Remarks.ClientID %>' }
            ];

            fields.forEach(function (field) {
                toggleRemarksTextboxVisibility(field.obsId, field.stdId, field.remarksId);
            });
        }

        function toggleRemarksTextboxVisibility(obsId, stdId, remarksId) {
            var tbObs = document.getElementById(obsId);
            var labelStd = document.getElementById(stdId);
            var tbRemarks = document.getElementById(remarksId);

            if (tbObs && labelStd && tbRemarks) {
                var observedValue = parseFloat(tbObs.value);
                var standardValue = parseFloat(labelStd.innerText);

                if (!isNaN(observedValue) && !isNaN(standardValue)) {
                    if (Math.abs(observedValue - standardValue) > 5) {
                        tbRemarks.style.display = "block"; // Show remarks
                    } else {
                        tbRemarks.style.display = "none"; // Hide remarks
                    }
                }
            }
        }

    </script>

</asp:Content>
