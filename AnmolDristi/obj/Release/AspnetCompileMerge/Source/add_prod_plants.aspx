<%@ Page Title="AIL | Production Plants" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_prod_plants.aspx.cs" Inherits="AnmolDristi.add_prod_plants" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Add & Manage Production Plants Details"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_plant_id" runat="server" AssociatedControlID="TB_plant_id" Text="Plant ID :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_plant_id" runat="server" ErrorMessage="Required" ControlToValidate="TB_plant_id" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_plant_id" runat="server" ControlToValidate="TB_plant_id" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Numeric Only" ValidationExpression="^[0-9]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_plant_id" runat="server" CssClass="form-control form-control-sm rounded" ReadOnly="true" Placeholder="Plant ID (3-20 characters)" MaxLength="20"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_plant_name" runat="server" AssociatedControlID="TB_plant_name" Text="Plant Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_plant_name" runat="server" ErrorMessage="Required" ControlToValidate="TB_plant_name" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_plant_name" runat="server" ControlToValidate="TB_plant_name" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_plant_name" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Plant Name (3-50 characters)" MaxLength="50"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_SapCode" runat="server" AssociatedControlID="TB_SapCode" Text="SAP Code:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_SapCode" runat="server" ErrorMessage="Required" ControlToValidate="TB_SapCode" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_SapCode" runat="server" ControlToValidate="TB_SapCode" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[0-9]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_SapCode" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="SAP Code (up to 50 characters)" MaxLength="5"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_LocalName" runat="server" AssociatedControlID="TB_LocalName" Text="Local Name:" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_LocalName" runat="server" ErrorMessage="Required" ControlToValidate="TB_LocalName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_LocalName" runat="server" ControlToValidate="TB_LocalName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_LocalName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Local Name (up to 255 characters)" MaxLength="255"></asp:TextBox>
                                        </div>
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
                                <asp:Button ID="btn_insert" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" />
                                <asp:Button ID="btn_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="btn_submit_Click" />
                            </div>
                        </div>
                    </div>

                    <div class="col-md-12 col-sm-12">
                        <div class="x_panel">
                            <div class="x_title">
                                <h2>Peform CRUD operations for the below data</h2>
                                <ul class="nav navbar-right panel_toolbox">
                                    <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                                </ul>
                                <div class="clearfix"></div>
                            </div>
                            <div class="x_content">
                                <div class="row">
                                    <div class="card-box col-md-12 col-sm-12 table table-striped table-hover table-bordered table-responsive table-sm table-condensed" style="width: 100%; height: 450px; overflow: scroll;">
                                        <asp:GridView ID="GridViewProdPlants" runat="server" AllowPaging="True" PageSize="10" AutoGenerateColumns="False" DataKeyNames="id" OnRowEditing="GridViewProdPlants_RowEditing" OnRowUpdating="GridViewProdPlants_RowUpdating" OnRowCancelingEdit="GridViewProdPlants_RowCancelingEdit" OnRowDeleting="GridViewProdPlants_RowDeleting" OnPageIndexChanging="GridViewProdPlants_PageIndexChanging">
                                            <Columns>
                                                <asp:BoundField DataField="id" HeaderText="ID" ReadOnly="True" />
                                                <asp:TemplateField HeaderText="Plant ID">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblPlantID" runat="server" Text='<%# Eval("plant_id") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="plant_id" runat="server" Text='<%# Bind("plant_id") %>' ReadOnly="true"></asp:TextBox>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Plant Name">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblPlantName" runat="server" Text='<%# Eval("plant_name") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="plant_name" runat="server" Text='<%# Bind("plant_name") %>' ReadOnly="false"></asp:TextBox>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="SAP Code">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblSapCode" runat="server" Text='<%# Eval("sap_code") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="sap_code" runat="server" Text='<%# Bind("sap_code") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Local Name">
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblLocalName" runat="server" Text='<%# Eval("local_name") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="local_name" runat="server" Text='<%# Bind("local_name") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="View Status">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="view_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' Enabled="false" />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:CheckBox ID="view_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("view_status")) %>' />
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Delete Status">
                                                    <ItemTemplate>
                                                        <asp:CheckBox ID="delete_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' Enabled="false" />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <asp:CheckBox ID="delete_status" runat="server" Checked='<%# Convert.ToBoolean(Eval("delete_status")) %>' />
                                                    </EditItemTemplate>
                                                </asp:TemplateField>
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
    </div>
</asp:Content>
