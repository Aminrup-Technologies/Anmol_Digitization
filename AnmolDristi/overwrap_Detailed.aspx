<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="overwrap_Detailed.aspx.cs" Inherits="AnmolDristi.overwrap_Detailed" %>

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
                        <asp:Label ID="lbl_docname" runat="server" Text="HM Bag/ PP Bag Report"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/CORP/QC/PKNG/03"></asp:Label>
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

                                                    <%--<div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:DropDownList>
                                                            </div>
                                                        </div>
                                                    </div>--%>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_MatVarietyName" runat="server" AssociatedControlID="TB_MatVarietyName" Text="Material / Variety" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:RequiredFieldValidator ID="RFV_TB_MatVarietyName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_MatVarietyName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                            <asp:RegularExpressionValidator ID="REV_TB_MatVarietyName" runat="server" ControlToValidate="TB_MatVarietyName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9, /]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_MatVarietyName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Enter Material / Variety" MaxLength="50"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Supplier" runat="server" AssociatedControlID="TB_Supplier" Text="Supplier :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Supplier" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("SupplierName") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_ChallanNo" runat="server" AssociatedControlID="TB_ChallanNo" Text="Challan No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_ChallanNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("ChallanNo") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_ChallanDate" runat="server" AssociatedControlID="TB_ChallanDate" Text="Challan Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_ChallanDate" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%# Eval("ChallanDate","{0:d d-MM-yyyy}") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_LotNo" runat="server" AssociatedControlID="TB_LotNo" Text="Lot/Gate No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_LotNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("LotGateNo") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_VehicleNo" runat="server" AssociatedControlID="TB_VehicleNo" Text="Vehicle No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_VehicleNo" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("VehicleNo") %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_SealingValue" runat="server" AssociatedControlID="TB_SealingValue" Text="Sealing Value:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_SealingValue" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>


                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_Std_Dimension" runat="server" AssociatedControlID="TB_Std_Dimension" Text=" Dimension(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:Label ID="Label_TB_Std_Dimension" runat="server" ForeColor="Red"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_Std_Dimension" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="Std_DimensionRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label19" runat="server" AssociatedControlID="TXB_Std_Dimension_Remarks" Text="Dimension Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Std_Dimension_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_TB_GSM" runat="server" AssociatedControlID="TB_GSM" Text="GSM/WT per 10 PS(in mm.) :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <asp:Label ID="Label_Std_GSM" runat="server" ForeColor="Red"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TB_GSM" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3" id="GSMRemarksDIV" style="display: none;">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Label5" runat="server" AssociatedControlID="TXB_GSM_Remarks" Text="GSM/WT per 10 PS Remarks" ForeColor="Red" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_GSM_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="col-md-3">
                                                        <div class="mb-3">
                                                            <asp:Label ID="Lbl_Remarks" runat="server" AssociatedControlID="TXB_Remarks" Text="Remarks :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                            <div class="input-group-sm">
                                                                <asp:TextBox ID="TXB_Remarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ReadOnly="true" Text='<%#  Eval("Remarks") %>' TextMode="MultiLine" Rows="2" Width="100%"></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </div>



                                                </div>

                                                <%--Button--%>
                                                <div class="col-md-12">
                                                    <div class="mb-12">
                                                        <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                        <div class="input-group input-group-sm">
                                                            <asp:Button ID="btnSubmit" runat="server" Text="Re-Validate Inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="Submit" CausesValidation="true" />
                                                            <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btnApprove_Click" />
                                                            <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btnReject_Click" />
                                                            <asp:Button ID="btnBack" runat="server" Text="Back" CssClass="btn btn-primary btn-sm" CausesValidation="false" OnClick="btnBack_Click" />
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

            // Check and toggle textboxes based on the value range
            toggleAllRemarksTextboxVisibility()
        };


        function toggleAllRemarksTextboxVisibility() {
            // Define a mapping of observed values, standard values, and remarks TextBoxes
            var fields = [
                { obsId: '<%= TB_Std_Dimension.ClientID %>', stdId: '<%= Label_Std_GSM.ClientID %>', remarksId: '<%= TXB_Std_Dimension_Remarks.ClientID %>' },
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
