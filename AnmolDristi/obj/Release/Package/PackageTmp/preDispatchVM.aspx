<%@ Page Title="QC | Pre-Dispatch Clearance Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="preDispatchVM.aspx.cs" Inherits="AnmolDristi.preDispatchVM" %>

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
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Line Filter -->
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Product Category Filter -->
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- Product Brand Filter -->
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <!-- SKU Type Filter -->
                            <div class="col-md-2">
                                <div class="mb-2">
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
            <asp:Button ID="ReportbtnCancel" runat="server" Text="Home" CssClass="btn btn-danger btn-sm" CausesValidation="false" PostBackUrl="~/qaqc_qaapprovals.aspx" />
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
                        <!-- GridView for displaying filtered data -->
                        <div class="table-responsive">
                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" ShowHeaderWhenEmpty="true" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" OnRowCommand="GridView1_RowCommand">
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
                        <div class="x_content"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
