<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Oven_Report_Detailed.aspx.cs" Inherits="AnmolDristi.Oven_Report_Detailed" %>

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
                        <asp:Label ID="lbl_docname" runat="server" Text="Detailed View Page"></asp:Label>
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
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Plant Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_PlantLine" runat="server"  CssClass="form-control form-control-sm rounded white-background-readonly" ></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ></asp:DropDownList>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" ></asp:DropDownList>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_RejectionKgs" runat="server" AssociatedControlID="TB_RejectionKgs" Text="Rejection Qty (in Kgs):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_RejectionKgs" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Rejection") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <%--  oven section start --%>
                                                    <div class="tab-pane fade" id="ovenReport" role="tabpanel" aria-labelledby="ovenReport-tab">
                                                        <div class="x_content">

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_BTRPM" runat="server" AssociatedControlID="TB_BTRPM" Text="B.T/R.P.M:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BTRPM" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("BT_RPM") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Gauge" runat="server" AssociatedControlID="TB_Gauge" Text=" Dry Gauge (mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Gauge" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Dry_Gauge") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Weight" runat="server" AssociatedControlID="TB_Weight" Text="Dry Weight (gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Weight" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Dry_Weight") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_DippedWeight" runat="server" AssociatedControlID="TB_DippedWeight" Text="Dipped Weight (gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_DippedWeight" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Dipped_Weight") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Length" runat="server" AssociatedControlID="TB_Length" Text="Length (for square shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Length" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Square_Shape_Length") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Width" runat="server" AssociatedControlID="TB_Width" Text="Width (for square shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Width" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Square_Shape_Width") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Diameter" runat="server" AssociatedControlID="TB_Diameter" Text="Diameter (for round shape in mm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Diameter" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Round_Shape_Diameter") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_PktWeight" runat="server" AssociatedControlID="TB_PktWeight" Text="Pkt. Weight (in gm):" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_PktWeight" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("PktWeight") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_BiscuitsPerPkt" runat="server" AssociatedControlID="TB_BiscuitsPerPkt" Text="No. of Bis./Pkt.:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_BiscuitsPerPkt" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("BiscuitsPerPkt") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_StartTime" runat="server" AssociatedControlID="TB_StartTime" Text="Oven Stopage Start Time:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StartTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Oven_Start_Time") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_StopTime" runat="server" AssociatedControlID="TB_StopTime" Text="Oven Stopage Stop Time:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_StopTime" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Oven_Stop_Time") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                            <div class="col-md-3">
                                                                <div class="mb-3">
                                                                    <asp:Label ID="Lbl_TB_Reason" runat="server" AssociatedControlID="TB_Reason" Text="Stoppage Reason:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                                    <div class="input-group-sm">
                                                                        <asp:TextBox ID="TB_Reason" runat="server" CssClass="form-control form-control-sm rounded white-background-readonly" Text='<%# Eval("Reason") %>'></asp:TextBox>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                        </div>
                                                    </div>
                                                    <%--  oven section end --%>
                                                </div>

                                                <%--Button--%>
                                                <div class="col-md-3">
                                                    <div class="mb-3">
                                                        <asp:Label ID="Lbl_BasicbtnApprove" runat="server" AssociatedControlID="" Text="Click on your ACTION" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                        <div class="input-group input-group-sm">
                                                            <asp:Button ID="BtnValidate" runat="server" Text="Re-Validate inputs" CssClass="btn btn-warning btn-sm" ValidationGroup="" CausesValidation="true" />
                                                            <asp:Button ID="BtnApprove" runat="server" Text="Approve" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="BtnApprove_Click" />
                                                            <asp:Button ID="BtnReject" runat="server" Text="Reject" CssClass="btn btn-sm btn-danger" CausesValidation="false" OnClick="BtnReject_Click" />
                                                            <%--<asp:Button ID="btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />--%>
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
