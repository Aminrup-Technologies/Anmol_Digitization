<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="FinalCbbWtVM.aspx.cs" Inherits="AnmolDristi.FinalCbbWtVM" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }

        .table-responsive {
            overflow-x: auto; /* Allow horizontal scroll */
            white-space: nowrap; /* Prevent text wrapping */
        }
    </style>



</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Label"></asp:Label>
                            </h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <!-- Plant Name Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Line Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Product Category Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Product Brand Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- SKU Type Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Date From Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lblPackageDate" runat="server" AssociatedControlID="TXT_PackageDateFrom" Text="Date From" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PackageDateFrom" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TXT_PackageDateFrom" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_PackageDateFrom" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Select Packaging Date" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Date To Filter -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="TXT_PackageDateTo" Text="Date To" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PackageDateTo" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TXT_PackageDateTo" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_PackageDateTo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Select Packaging Date" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Action Buttons -->
        <div class="action-buttons" style="display: flex; justify-content: center; align-items: center; text-align: center;">
            <asp:Label ID="lblInstruction" runat="server" Text="Click SUBMIT to view data!!!" CssClass="clearfix" Style="padding-right: 5em" />
            <asp:Button ID="Cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="Cancel_Click" />
            <asp:Button ID="Submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="Submit_Click" />
            <asp:Button ID="Reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="Reset_Click" />

        </div>

        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="label_lower" runat="server" Text="Filtered Data" Style="margin-right: 6px;"></asp:Label>

                                <asp:Button ID="ExportBtn" runat="server" Text="Export" CssClass="btn btn-info btn-sm" CausesValidation="false" />
                            </h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="table-responsive">
                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" ShowHeaderWhenEmpty="true" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap">
                                <Columns>
                                    <asp:TemplateField HeaderText="Sl">
                                        <ItemTemplate>
                                            <asp:Label ID="lblSl" runat="server" ReadOnly="true" ClientIDMode="Static" Text=""></asp:Label><strong><%# Container.DataItemIndex + 1 %></strong>
                                            <br />
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Record No.">
                                        <ItemTemplate>
                                            <asp:Label ID="lblId" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("Id") %>'></asp:Label><br />
                                            <asp:Label ID="lblCBB_PK" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("CBB_PK") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Submitted On">
                                        <ItemTemplate>
                                            <asp:Label ID="lblSubmittedDate" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("SubmittedDate", "{0:dd-MM-yyyy}") %>'></asp:Label><br />
                                            <asp:Label ID="lblSubmittedTime" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("SubmittedTime", "{0:hh\\:mm\\:ss}") %>'></asp:Label><br />
                                            <asp:Label ID="lblShift" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("Shift") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Submitted By">
                                        <ItemTemplate>
                                            <asp:Label ID="lblSubmittedByEmployeeCode" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("SubmittedByEmployeeCode") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Plant & Product">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPlantName" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("PlantName") %>'></asp:Label><br />
                                            <asp:Label ID="lblLine" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("Line") %>'></asp:Label><br />
                                            <asp:Label ID="lblProductCategory" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("ProductCategory") %>'></asp:Label><br />
                                            <asp:Label ID="lblProductBrand" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("ProductBrand") %>'></asp:Label><br />
                                            <asp:Label ID="lblSKUId" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("SKUId") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Batch No & MRP">
                                        <ItemTemplate>
                                            <asp:Label ID="lblBatchNo" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("BatchNo") %>'></asp:Label><br />
                                            <asp:Label ID="lblMRP" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("MRP") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Gross Weights">
                                        <ItemTemplate>
                                            <asp:Label ID="lblGrossWeightJson" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("GrossWeightJson") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Average Gross Weight">
                                        <ItemTemplate>
                                            <asp:Label ID="lblAverageGrossWeight" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# Eval("AverageGrossWeight") %>'></asp:Label>
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


</asp:Content>
