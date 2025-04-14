<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="LeakAndSlant_Detailed.aspx.cs" Inherits="AnmolDristi.LeakAndSlant_Detailed" %>

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
                        <asp:Label ID="lbl_docname" runat="server" Text="QA - LEAK / SEAL & SLANTED PACK REPORT"></asp:Label>
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

                        <div class="x_content">


                            <asp:GridView ID="gvData" runat="server" Visible="true" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" AutoGenerateColumns="false" ShowFooter="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found">
                                <Columns>
                                    <asp:BoundField DataField="Plant" HeaderText="Plant" SortExpression="Plant" />
                                    <%--<asp:BoundField DataField="PlantId" HeaderText="Plant Id" SortExpression="PlantId" />--%>
                                    <asp:BoundField DataField="PlantLine" HeaderText="Plant Line" SortExpression="PlantLine" />
                                    <%--<asp:BoundField DataField="PlantLineId" HeaderText="Plant Line Id" SortExpression="PlantLineId" />--%>
                                    <asp:BoundField DataField="ProductCategory" HeaderText="Product Category" SortExpression="ProductCategory" />
                                    <%--<asp:BoundField DataField="ProductCategoryId" HeaderText="Product Category Id" SortExpression="ProductCategoryId" />--%>
                                    <asp:BoundField DataField="ProductBrand" HeaderText="Product Brand" SortExpression="ProductBrand" />
                                    <%--<asp:BoundField DataField="ProductBrandId" HeaderText="Product Brand Id" SortExpression="ProductBrandId" />--%>
                                    <asp:BoundField DataField="BrandSKU" HeaderText="Brand SKU" SortExpression="BrandSKU" />
                                    <%--<asp:BoundField DataField="BrandSKUId" HeaderText="Brand SKU Id" SortExpression="BrandSKUId" />--%>
                                    <asp:BoundField DataField="PackingMCNo" HeaderText="Packing M/C No." SortExpression="PackingMCNo" />
                                    <asp:BoundField DataField="LeakTestSealIntegrity" HeaderText="Leak Test / Seal Integrity" SortExpression="LeakTestSealIntegrity" />
                                    <asp:BoundField DataField="PassFailRemarks" HeaderText="Pass / Fail Remarks" SortExpression="PassFailRemarks" />
                                    <asp:BoundField DataField="PercentageSlanted" HeaderText="Slanted / Loose Pack (%)" SortExpression="PercentageSlanted" />
                                </Columns>
                            </asp:GridView>



                            <div class="col-md-12">
                                <div class="mb-12">
                                    <asp:Label ID="Lbl_btnSubmit" runat="server" AssociatedControlID="btnSubmit" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="btnSubmit" runat="server" Text="Re-Validate Inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="Submit" CausesValidation="true" />
                                        <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btnApprove_Click" />
                                        <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btnReject_Click" />
                                        <asp:Button ID="btnBack" runat="server" Text="Back" CssClass="btn btn-primary btn-sm" CausesValidation="false" OnClick="btnBack_Click"/>
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


</asp:Content>
