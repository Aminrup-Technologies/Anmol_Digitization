<%@ Page Title="AIL | Production Lines" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_plant_lines.aspx.cs" Inherits="AnmolDristi.add_plant_lines" %>

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
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Plant : Production Lines"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="Required" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0" ValidationGroup="Submit"></asp:RequiredFieldValidator>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Line_ID" runat="server" AssociatedControlID="TB_Line_ID" Text="Line ID :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Line_ID" runat="server" ErrorMessage="Required" ControlToValidate="TB_Line_ID" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_Line_ID" runat="server" ControlToValidate="TB_Line_ID" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Numeric Only" ValidationExpression="^[0-9]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Line_ID" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Line ID (3-20 characters)" ReadOnly="true" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Line_Name" runat="server" AssociatedControlID="TB_Line_Name" Text="Line Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Line_Name" runat="server" ErrorMessage="Required" ControlToValidate="TB_Line_Name" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_Line_Name" runat="server" ControlToValidate="TB_Line_Name" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Line_Name" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Line Description (3-50 characters)" MaxLength="50"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Local_Name" runat="server" AssociatedControlID="TB_Local_Name" Text="Local Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Local_Name" runat="server" ErrorMessage="Required" ControlToValidate="TB_Local_Name" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_Local_Name" runat="server" ControlToValidate="TB_Local_Name" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s\-]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Local_Name" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Local Name (up to 255 characters)" MaxLength="255"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_Line_Sap_Code" runat="server" AssociatedControlID="TB_Line_Sap_Code" Text="Line SAP Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_Line_Sap_Code" runat="server" ErrorMessage="Required" ControlToValidate="TB_Line_Sap_Code" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_Line_Sap_Code" runat="server" ControlToValidate="TB_Line_Sap_Code" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z0-9\s]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Line_Sap_Code" runat="server" CssClass="form-control form-control-sm rounded" Text="0000" Placeholder="Line SAP Code (up to 50 characters)" MaxLength="50"></asp:TextBox>
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
                                <asp:Button ID="btn_cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btn_cancel_Click" />
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
                                <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 450px; overflow: scroll;">
                                    <asp:GridView ID="GridViewPlantLines" runat="server" AllowPaging="True" PageSize="10" Width="100%" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed"
                                        AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" DataKeyNames="id"
                                        OnRowEditing="GridViewPlantLines_RowEditing" OnRowCancelingEdit="GridViewPlantLines_RowCancelingEdit"
                                        OnRowUpdating="GridViewPlantLines_RowUpdating" OnRowDeleting="GridViewPlantLines_RowDeleting" OnPageIndexChanging="GridViewPlantLines_PageIndexChanging">
                                        <Columns>
                                            <asp:BoundField DataField="id" HeaderText="ID" ReadOnly="True" />
                                            <asp:TemplateField HeaderText="Plant ID">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblPlantID" runat="server" Text='<%# Eval("plant_id") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtPlantID" runat="server" Text='<%# Bind("plant_id") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Line ID">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblLineID" runat="server" Text='<%# Eval("line_id") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtLineID" runat="server" Text='<%# Bind("line_id") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Line Name">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblLineName" runat="server" Text='<%# Eval("line_name") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtLineName" runat="server" Text='<%# Bind("line_name") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Local Name">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblLocalName" runat="server" Text='<%# Eval("local_name") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtLocalName" runat="server" Text='<%# Bind("local_name") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Line SAP Code">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblLineSapCode" runat="server" Text='<%# Eval("line_sap_code") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtLineSapCode" runat="server" Text='<%# Bind("line_sap_code") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="added_on" HeaderText="Added On" DataFormatString="{0:dd/MM/yyyy}" />
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
</asp:Content>
