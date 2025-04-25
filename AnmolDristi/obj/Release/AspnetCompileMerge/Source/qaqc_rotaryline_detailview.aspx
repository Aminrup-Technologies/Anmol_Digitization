<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_rotaryline_detailview.aspx.cs" Inherits="AnmolDristi.qaqc_rotaryline_detailview" %>

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
            <div class="page-title custom-page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Roatary Line & Oven End Report"></asp:Label>
                    </h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label></h2>
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
                                                            aria-controls="basicData" aria-selected="true"><i class="fa fa-id-badge mr-2"></i>Plant & Line Data</a>
                                                    </li>
                                                    <li class="nav-item">
                                                        <a class="nav-link" id="rawBiscuts-tab" data-toggle="tab" href="#rawBiscuts" role="tab"
                                                            aria-controls="rawBiscuts" aria-selected="false">Line Weights</a>
                                                    </li>

                                                    <li class="nav-item">
                                                        <a class="nav-link" id="ovenReport-tab" data-toggle="tab" href="#ovenReport" role="tab"
                                                            aria-controls="ovenReport" aria-selected="false">Oven End Data</a>
                                                    </li>
                                                </ul>

                                                <div class="tab-content ml-1" id="myTabContent">
                                                    <div class="tab-pane fade show active" id="basicData" role="tabpanel" aria-labelledby="basicData-tab">
                                                        <div class="x_content">
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly"></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="rawBiscuts" role="tabpanel" aria-labelledby="rawBiscuts-tab">
                                                        <div class="x_content">
                                                            <asp:GridView ID="LineWeights_Grid" runat="server" Visible="true" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:BoundField DataField="sl" HeaderText="Sl. No." />
                                                                    <asp:BoundField DataField="weight" HeaderText="Weight" />
                                                                </Columns>
                                                            </asp:GridView>

                                                            <div class="col-md-6" id="AvgWt_TB" runat="server" visible="true">
                                                                <table class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" style="width: 100%;">
                                                                    <thead>
                                                                        <tr>
                                                                            <th>Min Value</th>
                                                                            <th>Max Value</th>
                                                                            <th>Difference (Min-Max)</th>
                                                                            <th>Average Value</th>
                                                                        </tr>
                                                                    </thead>
                                                                    <tbody>
                                                                        <tr>
                                                                            <td>
                                                                                <asp:Label ID="lblMinValue" runat="server" Text="0.0"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="lblMaxValue" runat="server" Text="0.0"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="lblDiffMinMax" runat="server" Text="0.0"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="lblAvgWeight" runat="server" Text="0.0"></asp:Label>
                                                                            </td>
                                                                        </tr>
                                                                    </tbody>
                                                                </table>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="tab-pane fade" id="ovenReport" role="tabpanel" aria-labelledby="ovenReport-tab">
                                                        <div class="x_content">

                                                            <asp:GridView ID="OvenEnd_GridView" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                                                <Columns>
                                                                    <asp:BoundField DataField="sl" HeaderText="Sl. No." />
                                                                    <asp:BoundField DataField="ge" HeaderText="Weight" />
                                                                    <asp:BoundField DataField="wt" HeaderText="Weight" />
                                                                </Columns>
                                                            </asp:GridView>

                                                            <div class="col-md-6" id="ov_weights" runat="server" visible="true">
                                                                <table class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" style="width: 100%;">
                                                                    <thead>
                                                                        <tr>
                                                                            <th>X</th>
                                                                            <th>Min Value</th>
                                                                            <th>Max Value</th>
                                                                            <th>Difference (Min-Max)</th>
                                                                            <th>Average Value</th>
                                                                        </tr>
                                                                    </thead>
                                                                    <tbody>
                                                                        <tr>
                                                                            <td>
                                                                                <asp:Label ID="Label5" runat="server" Text="Gauge Value"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblMinGauge" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblMaxGauge" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblDiffGauge" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="lblAvgGaugeLength" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("avggaugevalue") %>'></asp:Label>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td>
                                                                                <asp:Label ID="Label18" runat="server" Text="Weights"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblMinValue" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblMaxValue" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="ov_lblDiffMinMax" runat="server" ReadOnly="true" ClientIDMode="Static"></asp:Label>
                                                                            </td>
                                                                            <td>
                                                                                <asp:Label ID="lblAvgWeights" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("avgweightvalue") %>'></asp:Label>
                                                                            </td>

                                                                        </tr>
                                                                    </tbody>
                                                                </table>
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
</asp:Content>
