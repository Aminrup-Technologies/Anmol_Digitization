<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_brandtypes.aspx.cs" Inherits="AnmolDristi.add_brandtypes" MaintainScrollPositionOnPostback="true" %>

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
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Add & Manage Brands Specific to Plant and Line"></asp:Label>
                            </h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" ValidationGroup="Submit" InitialValue="0"></asp:RequiredFieldValidator>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                                    </div>

                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
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
                                    <asp:Label ID="lbl_BrandID" runat="server" AssociatedControlID="txtBrandID" Text="Brand ID" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_BrandID" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="txtBrandID" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_BrandID" runat="server" ControlToValidate="txtBrandID" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Numeric Only" ValidationExpression="^\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtBrandID" runat="server" CssClass="form-control form-control-sm rounded" Text="Auto Binding" ReadOnly="true" Placeholder="Category ID (numeric)" MaxLength="10"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="txtBrandName" Text="Brand Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_txtBrandName" runat="server" ErrorMessage="*" ControlToValidate="txtBrandName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_txtBrandName" runat="server" ControlToValidate="txtBrandName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtBrandName" runat="server" CssClass="form-control form-control-sm rounded" placeholder="Brand Name (3-50 characters)" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="txtBrandLocalName" Text="Brand Local Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="Requiredfieldvalidator1" runat="server" ErrorMessage="*" ControlToValidate="txtBrandLocalName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="Regularexpressionvalidator1" runat="server" ControlToValidate="txtBrandLocalName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtBrandLocalName" runat="server" Text="0000" ReadOnly="true" CssClass="form-control form-control-sm rounded" placeholder="Brand Local Name (Optional)" MaxLength="255"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="txtBrandSapCode" Text="Brand SAP Code" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="Requiredfieldvalidator2" runat="server" ErrorMessage="*" ControlToValidate="txtBrandSapCode" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="Regularexpressionvalidator2" runat="server" ControlToValidate="txtBrandSapCode" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtBrandSapCode" runat="server" Text="0000" ReadOnly="true" CssClass="form-control form-control-sm rounded" placeholder="Brand SAP Code (Optional)" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- View Status -->
                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label8" runat="server" AssociatedControlID="chkViewStatus" Text="View Status" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:CheckBox ID="chkViewStatus" runat="server" CssClass="form-check-input" />
                                    </div>
                                </div>
                            </div>--%>

                            <!-- Delete Status -->
                            <%--<div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label9" runat="server" AssociatedControlID="chkDeleteStatus" Text="Delete Status" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group-sm">
                                        <asp:CheckBox ID="chkDeleteStatus" runat="server" CssClass="form-check-input" />
                                    </div>
                                </div>
                            </div>--%>
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
                                <asp:Button ID="btn_cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btn_cancel_Click" />
                                <asp:Button ID="btn_insert" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" />
                                <asp:Button ID="btn_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="btnSubmit_Click" />
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
                                <asp:GridView ID="GridViewLineCatBrands" runat="server" Width="100%" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" Font-Size="8" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" DataKeyNames="Id" OnRowEditing="GridViewLineCatBrands_RowEditing" OnRowCancelingEdit="GridViewLineCatBrands_RowCancelingEdit" OnRowUpdating="GridViewLineCatBrands_RowUpdating" OnRowDeleting="GridViewLineCatBrands_RowDeleting">
                                    <Columns>
                                        <asp:TemplateField HeaderText="ID">
                                            <ItemTemplate>
                                                <%# Eval("Id") %>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Plant ID">
                                            <ItemTemplate>
                                                <%# Eval("plant_id") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtPlantId" runat="server" Text='<%# Bind("plant_id") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Line ID">
                                            <ItemTemplate>
                                                <%# Eval("line_id") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtLineId" runat="server" Text='<%# Bind("line_id") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Category ID">
                                            <ItemTemplate>
                                                <%# Eval("category_id") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtCategoryId" runat="server" Text='<%# Bind("category_id") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Brand ID">
                                            <ItemTemplate>
                                                <%# Eval("brand_id") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtBrandId" runat="server" Text='<%# Bind("brand_id") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Brand Name">
                                            <ItemTemplate>
                                                <%# Eval("brand_name") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtBrandName" runat="server" Text='<%# Bind("brand_name") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Brand Local Name">
                                            <ItemTemplate>
                                                <%# Eval("brand_localname") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtBrandLocalName" runat="server" Text='<%# Bind("brand_localname") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Brand SAP Code">
                                            <ItemTemplate>
                                                <%# Eval("brand_sapcode") %>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtBrandSapCode" runat="server" Text='<%# Bind("brand_sapcode") %>' CssClass="form-control form-control-sm rounded"/>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Added On">
                                            <ItemTemplate>
                                                <%# Eval("added_on", "{0:dd/MM/yyyy}") %>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="View Status">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="view_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' Enabled="false" />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:CheckBox ID="CheckBox1" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' />
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Delete Status">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="delete_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' Enabled="false" />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:CheckBox ID="CheckBox2" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' />
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Actions">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" Text="Edit" CssClass="btn btn-sm btn-info" CausesValidation="false" />
                                                <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" CssClass="btn btn-sm btn-danger" CausesValidation="false" />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update" Text="Update" CssClass="btn btn-sm btn-success" CausesValidation="false" />
                                                <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel" Text="Cancel" CssClass="btn btn-sm btn-secondary" CausesValidation="false" />
                                            </EditItemTemplate>
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
</asp:Content>
