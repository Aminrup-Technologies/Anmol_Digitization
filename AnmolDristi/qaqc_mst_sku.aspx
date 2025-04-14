<%@ Page Title="AIL | QA/QC | MST SKU" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_mst_sku.aspx.cs" Inherits="AnmolDristi.MST.qaqc_mst_sku" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <%--<div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h3>
                </div>
            </div>--%>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Plant->Line->Category->Brand | Brand SKU Mastering"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                    </div>

                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lbl_DDL_PlantLine" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" ></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
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
                                <asp:Button ID="btn_cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btn_cancel_Click"/>
                                <asp:Button ID="btn_insert" runat="server" Text="Insert" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btn_insert_Click" />
                                <asp:Button ID="btn_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="btn_submit_Click" />
                            </div>
                        </div>
                    </div>
                    <%--button end--%>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Selected Brand Input Controller Fields | Edit to modify input parameters</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 450px; overflow: scroll;">
                                    <asp:GridView ID="GridView1" runat="server" Width="100%" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" Font-Size="8" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowEditing="GridView1_RowEditing" OnRowUpdating="GridView1_RowUpdating" OnRowCancelingEdit="GridView1_RowCancelingEdit" OnRowDeleting="GridView1_RowDeleting" DataKeyNames="Id">
                                        <Columns>
                                            <asp:BoundField DataField="Id" HeaderText="Id" ReadOnly="True" />
                                            <asp:BoundField DataField="SKUId" HeaderText="SKU Id" ReadOnly="True" />
                                            <asp:BoundField DataField="brand_id" HeaderText="Brand Id" ReadOnly="True" />
                                            <asp:BoundField DataField="brand_name" HeaderText="Brand Name" ReadOnly="True" />
                                            <asp:BoundField DataField="SKU_name" HeaderText="SKU Name" />
                                            <asp:BoundField DataField="SKU_descp" HeaderText="SKU Description" />
                                            <asp:BoundField DataField="SKU_orientation" HeaderText="SKU Orientation" />
                                            <asp:BoundField DataField="SKU_details1" HeaderText="SKU Details 1" />
                                            <asp:BoundField DataField="SKU_details2" HeaderText="SKU Details 2" />
                                            <asp:BoundField DataField="ViewMode" HeaderText="View Mode" />
                                            <asp:BoundField DataField="TimeStamp" HeaderText="Timestamp" ReadOnly="True" />
                                            <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" />
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
