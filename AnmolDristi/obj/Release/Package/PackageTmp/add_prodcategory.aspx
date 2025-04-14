<%@ Page Title="AIL | Manage Product Category" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_prodcategory.aspx.cs" Inherits="AnmolDristi.add_prodcategory" %>

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
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Add & Manage Line wise Category Produced"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0" ValidationGroup="Submit"></asp:RequiredFieldValidator>
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

                            <!-- Category ID -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="txtCategoryID" Text="Category ID" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_lastcategory_code" runat="server" AssociatedControlID="txtCategoryID" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="txtCategoryID" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_CategoryID" runat="server" ControlToValidate="txtCategoryID" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Numeric Only" ValidationExpression="^\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtCategoryID" runat="server" CssClass="form-control form-control-sm rounded" ReadOnly="true" Placeholder="Auto Binding" MaxLength="10"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Category Name -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="txtCategoryName" Text="Category Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_CategoryName" runat="server" ErrorMessage="Required" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="txtCategoryName" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_CategoryName" runat="server" ControlToValidate="txtCategoryName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtCategoryName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Category Name (3-50 characters)" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Category SAP Code -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="txtCategorySapCode" Text="Category SAP Code" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="txtCategorySapCode" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ControlToValidate="txtCategorySapCode" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtCategorySapCode" runat="server" CssClass="form-control form-control-sm rounded" Text="0000" ReadOnly="true" Placeholder="Category SAP Code" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <!-- Local Name -->
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label7" runat="server" AssociatedControlID="txtLocalName" Text="Local Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="txtLocalName" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="RegularExpressionValidator2" runat="server" ControlToValidate="txtLocalName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txtLocalName" runat="server" CssClass="form-control form-control-sm rounded" Text="0000" ReadOnly="true" Placeholder="Local Name" MaxLength="50"></asp:TextBox>
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
                                <asp:Button ID="btn_cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btn_cancel_Click"/>
                                <asp:Button ID="btn_insert" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" />
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
                                <asp:GridView ID="GridViewLineCategory" runat="server" Width="100%" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                    AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" AllowPaging="True" PageSize="50" EmptyDataText="No Data Found" DataKeyNames="id" OnRowEditing="GridViewLineCategory_RowEditing" OnRowCancelingEdit="GridViewLineCategory_RowCancelingEdit" OnRowUpdating="GridViewLineCategory_RowUpdating" OnRowDeleting="GridViewLineCategory_RowDeleting" OnPageIndexChanging="GridViewLineCategory_PageIndexChanging">
                                    <Columns>
                                        <asp:BoundField DataField="id" HeaderText="ID" ReadOnly="True" HeaderStyle-Width="5%" ItemStyle-Width="5%" />
                                        <asp:TemplateField HeaderText="Plant ID">
                                            <ItemTemplate>
                                                <asp:Label ID="lblPlantID" runat="server" Text='<%# Eval("plant_id") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtPlantID" runat="server" Text='<%# Bind("plant_id") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Line ID" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                            <ItemTemplate>
                                                <asp:Label ID="lblLineID" runat="server" Text='<%# Eval("line_id") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtLineID" runat="server" Text='<%# Bind("line_id") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Category ID" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:Label ID="lblCategoryID" runat="server" Text='<%# Eval("category_id") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtCategoryID" runat="server" Text='<%# Bind("category_id") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Category Name" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:Label ID="lblCategoryName" runat="server" Text='<%# Eval("category_name") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtCategoryName" runat="server" Text='<%# Bind("category_name") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Category SAP Code" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:Label ID="lblCategorySapCode" runat="server" Text='<%# Eval("category_sapcode") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtCategorySapCode" runat="server" Text='<%# Bind("category_sapcode") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Local Name" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:Label ID="lblLocalName" runat="server" Text='<%# Eval("local_name") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtLocalName" runat="server" Text='<%# Bind("local_name") %>'></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="added_on" HeaderText="Added On" DataFormatString="{0:dd/MM/yyyy}" HeaderStyle-Width="10%" ItemStyle-Width="10%" />
                                        <asp:TemplateField HeaderText="View Status" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="view_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' Enabled="false" />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:CheckBox ID="view_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' />
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Delete Status" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                            <ItemTemplate>
                                                <asp:CheckBox ID="delete_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' Enabled="false" />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:CheckBox ID="delete_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' />
                                            </EditItemTemplate>
                                        </asp:TemplateField>
                                        <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" HeaderStyle-Width="10%" ItemStyle-Width="10%" />
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
