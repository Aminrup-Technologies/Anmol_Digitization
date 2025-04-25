<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="CCP_Checklist_detailed.aspx.cs" Inherits="AnmolDristi.CCP_Checklist_detailed" %>

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
                        <asp:Label ID="lbl_docname" runat="server" Text="CCP Checklist Report"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="ANMOL/DOC/DAN/QA/05"></asp:Label>
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
                                                            aria-controls="basicData" aria-selected="true"><%--<i class="fa fa-id-badge mr-2"></i>--%>Plant</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Metal_Check-tab" data-toggle="tab" href="#Metal_Check" role="tab"
                                                            aria-controls="Metal_Check" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Magnet Check</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Standard_Sieve-tab" data-toggle="tab" href="#Standard_Sieve" role="tab"
                                                            aria-controls="Standard_Sieve" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Sieve Condition</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link text-info" id="Metal_Dctector_Area-tab" data-toggle="tab" href="#Metal_Dctector_Area" role="tab"
                                                            aria-controls="Metal_Dctector_Area" aria-selected="false">
                                                            <%-- <i class="fa fa-clock-o mr-2"></i>--%>Metal Detector</a>
                                                    </li>
                                                </ul>

                                                <div class="tab-content ml-1" id="myTabContent">

                                                    <%---Basic User Info Starts--%>
                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x_content">

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>


                                                        </div>
                                                    </div>

                                                    <%--Metal check--%>
                                                    <div class="tab-pane fade" id="Metal_Check" role="tabpanel" aria-labelledby="Metal_Check-tab">
                                                        <div class="x_content">

                                                            <asp:GridView ID="GridView_MetalCheck" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" ShowFooter="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found">

                                                                <Columns>
                                                                    <asp:BoundField DataField="Sl" HeaderText="Sl. No." />
                                                                    <asp:BoundField DataField="Location" HeaderText="Location" />
                                                                    <asp:BoundField DataField="Qty of Metal Found(gm)" HeaderText="Qty of Metal Found (gm)" />
                                                                    <%--<asp:BoundField DataField="CleanedStatus" HeaderText="Cleaned Status" />--%>
                                                                    <asp:TemplateField HeaderText="Cleaned Status">
                                                                        <ItemTemplate>
                                                                            <%# Convert.ToString(Eval("CleanedStatus")) == "1" ? "Ok" : "Not Ok" %>
                                                                        </ItemTemplate>
                                                                    </asp:TemplateField>
                                                                    <asp:BoundField DataField="Remarks for not ok" HeaderText="Remarks" />
                                                                </Columns>


                                                            </asp:GridView>



                                                        </div>
                                                    </div>


                                                    <!-- STANDARD SIEVE-->
                                                    <div class="tab-pane fade" id="Standard_Sieve" role="tabpanel" aria-labelledby="Standard_Sieve-tab">
                                                        <div class="x_content">

                                                            <asp:GridView ID="GridView_Shieve" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found">


                                                                <Columns>
                                                                    <asp:BoundField DataField="Sl" HeaderText="Sl. No." />
                                                                    <asp:BoundField DataField="Sieve No" HeaderText="Sieve No" />
                                                                    <asp:BoundField DataField="Initial Sample" HeaderText="Initial Sample" />
                                                                    <asp:BoundField DataField="Final Retention" HeaderText="Final Retention" />
                                                                    <asp:BoundField DataField="Percentage Retention" HeaderText="Percentage Retention" />
                                                                </Columns>



                                                            </asp:GridView>

                                                        </div>
                                                    </div>

                                                    <%--Metal_Dctector_Area--%>
                                                    <div class="tab-pane fade" id="Metal_Dctector_Area" role="tabpanel" aria-labelledby="Metal_Dctector_Area-tab">
                                                        <div class="x_content">

                                                            <asp:GridView ID="Magnetgrid" runat="server" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found">
                                                                <Columns>
                                                                    <asp:BoundField DataField="PlantLine" HeaderText="Plant Line" />
                                                                    <asp:BoundField DataField="FF_Status" HeaderText="FF Status" />
                                                                    <asp:BoundField DataField="FF_Remarks" HeaderText="FF Remarks" />
                                                                    <asp:BoundField DataField="NFE_Status" HeaderText="NFE Status" />
                                                                    <asp:BoundField DataField="NFE_Remarks" HeaderText="NFE Remarks" />
                                                                    <asp:BoundField DataField="SS_Status" HeaderText="SS Status" />
                                                                    <asp:BoundField DataField="SS_Remarks" HeaderText="SS Remarks" />

                                                                </Columns>
                                                            </asp:GridView>

                                                        </div>

                                                        <div class="col-md-12">&nbsp;</div>

                                                        <!-- Remarks -->
                                                        <div class="col-md-12">
                                                            <div class="mb-3">
                                                                <asp:Label ID="Label5" runat="server" Text="Overall Remarks" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                <div class="input-group-sm">
                                                                    <asp:TextBox ID="TB_MDRemarks" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" TextMode="MultiLine" Rows="2"></asp:TextBox>
                                                                </div>
                                                            </div>
                                                        </div>

                                                    </div>


                                                    <%--Buttons--%>
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
            </div>
        </div>
    </div>

</asp:Content>
