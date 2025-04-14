<%@ Page Title="AIL | QC Inspector Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_qcinspector_rpt_.aspx.cs" Inherits="AnmolDristi.qaqc_qcinspector_rpt_" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .thumbnail {
            position: relative;
            overflow: hidden;
            width: 100px; /* Set the initial width of the thumbnail */
            height: 100px; /* Set the initial height of the thumbnail */
            transition: width 0.3s, height 0.3s; /* Add smooth transition effect */
        }

            .thumbnail:hover {
                width: 150px; /* Set the enlarged width on hover */
                height: 150px; /* Set the enlarged height on hover */
            }

        .thumbnail-image {
            width: 100%;
            height: 100%;
            object-fit: cover; /* Ensure the image covers the entire container */
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <%--<div class="page-title">
                <div class="title_left">
                    <h5>Create Memo Summary</h5>
                </div>
            </div>

            <div class="clearfix"></div>--%>

            <div class="row">
                <div class="col-md-12 col-sm-12  ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Search Filters for QC Inspector Report</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                    </div>

                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="NoSubmit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="NoSubmit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="NoSubmit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label7" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="noSubmit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_BrandSKU_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="txt_date1" Text="Date From" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_txt_date1" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_date1" CausesValidation="true" ValidationGroup="Submit" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txt_date1" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px"></asp:TextBox>
                                    </div>

                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="txt_date2" Text="Date To" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_txt_date2" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="txt_date2" CausesValidation="true" ValidationGroup="Submit" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txt_date2" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px"></asp:TextBox>
                                    </div>

                                </div>
                            </div>

                            <!-- Small modal -->
                            <asp:Button ID="ShowPopup" runat="server" Text="Button" class="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
                                <div class="modal-dialog modal-sm">
                                    <div class="modal-content">

                                        <div class="modal-header">
                                            <h4 class="modal-title" id="myModalLabel2"></h4>
                                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                                <span aria-hidden="true">×</span>
                                            </button>
                                        </div>
                                        <div class="modal-body">
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary btn-sm" data-dismiss="modal">Close</button>
                                        </div>

                                    </div>
                                </div>
                            </div>
                            <!-- Small Modal - END---->
                        </div>
                    </div>

                    <%--button start--%>
                    <div class="col-md-6 center-margin">
                        <%--<div class="ln_solid"></div>--%>
                        <div class="item form-group row">
                            <div class="col-md-6 col-sm-12">
                                <asp:Label ID="lbl_msg" runat="server" Text="Click SUBMIT to view Data!!"></asp:Label>
                            </div>
                            <div class="col-md-6 col-sm-12">
                                <asp:Button ID="btnHome" runat="server" Text="Home" CausesValidation="false" CssClass="btn btn-danger btn-sm" PostBackUrl="~/home.aspx" />
                                <asp:Button ID="btnReset" runat="server" Text="Reset" CausesValidation="false" CssClass="btn btn-warning btn-sm" PostBackUrl="~/qaqc_qcinspector_rpt_.aspx" />
                                <asp:Button ID="btn_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="false" ValidationGroup="Submit" OnClick="btn_submit_Click" />
                            </div>
                        </div>
                    </div>
                    <%--button end--%>
                </div>


                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Select for Detailed View ||
                                <asp:Button ID="Button1" runat="server" Text="Export" OnClick="ExportExcel" CssClass="btn btn-primary btn-sm" CausesValidation="false" /></h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 450px; overflow: scroll;">
                                    <asp:GridView ID="GridView1" runat="server" Width="100%" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap"
                                        AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowCommand="GridView1_RowCommand">
                                        <Columns>

                                            <asp:TemplateField HeaderText="Sl" HeaderStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblSl" runat="server" Text='<%# Container.DataItemIndex + 1 %>'></asp:Label><br />
                                                    DBID:<asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("DBID") %>' Visible="true" />
                                                </ItemTemplate>
                                            </asp:TemplateField>


                                            <asp:TemplateField HeaderText="Plant Details" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    Plant:<asp:Label ID="lbl_PlantName" runat="server" Text='<%# Eval("PlantName") %>' Font-Bold="true" /><br />
                                                    Line:<asp:Label ID="lbl_Line" runat="server" Text='<%# Eval("LineName") %>' Font-Bold="true" /><br />
                                                    Category:<asp:Label ID="lbl_ProductCategory" runat="server" Text='<%# Eval("ProductCategory") %>' Font-Bold="true" /><br />
                                                    Brand:<asp:Label ID="lbl_ProductBrand" runat="server" Text='<%# Eval("ProductBrand") %>' Font-Bold="true" /><br />
                                                </ItemTemplate>
                                            </asp:TemplateField>


                                            <asp:TemplateField HeaderText="Submission Details" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    Date:<asp:Label ID="lblSubmittedDate" runat="server" Text='<%# Eval("SDate", "{0:dd/MM/yyyy}") %>'></asp:Label><br />
                                                    Time:<asp:Label ID="lblSubmittedTime" runat="server" Text='<%# Eval("STime", "{0:hh\\:mm\\:ss}") %>'></asp:Label>
                                                    [<asp:Label ID="lblShift" runat="server" Text='<%# Eval("SShift") %>'></asp:Label>]<br />
                                                    <asp:Label ID="lblSubmittedBy" runat="server" Text='<%# Eval("EmpName") %>'></asp:Label>
                                                    [<asp:Label ID="lblEmployeeCode" runat="server" Text='<%# Eval("EmpCode") %>'></asp:Label>]
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRemarks" runat="server" Text='<%# Eval("Remarks") %>'></asp:Label>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Approvals" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    L1:<asp:Label ID="lbl_Approver1" runat="server" Text='<%# Eval("L1") %>' />
                                                    [<asp:Label ID="lbl_Approver1_Status" runat="server" Text='<%# Eval("Approver1_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                                    L2:<asp:Label ID="lbl_Approver2" runat="server" Text='<%# Eval("L2") %>' />
                                                    [<asp:Label ID="lbl_Approver2_Status" runat="server" Text='<%# Eval("Approver2_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                                    L3:<asp:Label ID="lbl_DottedLineApproverEmployeeCode" runat="server" Text='<%# Eval("L3") %>' />
                                                    [<asp:Label ID="lbl_DottedApprover_Status" runat="server" Text='<%# Eval("DottedApprover_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                                </ItemTemplate>
                                                <ItemStyle CssClass="text" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Button ID="btn_viewdetails" runat="server" Text="View" Font-Size="Smaller" CssClass="btn btn-sm btn-warning" CommandName="View" CausesValidation="false" CommandArgument="<%# Container.DataItemIndex %>" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
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
    <script type="text/javascript">
        function ShowPopup(title, body) {
            $("#MyPopup .modal-title").html(title);
            $("#MyPopup .modal-body").html(body);
            $("#MyPopup").modal("show");
        }
    </script>
</asp:Content>
