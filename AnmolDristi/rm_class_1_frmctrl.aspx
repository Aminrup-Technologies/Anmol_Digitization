<%@ Page Title="AIL | QC RM Class 1 Controller" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="rm_class_1_frmctrl.aspx.cs" Inherits="AnmolDristi.rm_class_1_frmctrl" %>

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
                                <asp:Label ID="lbl_docnumber" runat="server" Text="QC RM Class 1 Report : Input Controller"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_Material" Text="Material Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Material_Value" runat="server" AssociatedControlID="DDL_Material" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                             <asp:RequiredFieldValidator ID="RFV_DDL_Material" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_Material" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Material" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Material_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                             <div class="input-group-sm">
                                 <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>

                             </div>

                                </div>
                            </div>

                            <%--<div class="col-md-3">
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
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                             <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false"></asp:DropDownList>
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
                            <h2>Selected Plant Input Controller Fields | Edit to modify input parameters</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: auto; overflow: scroll;">
                                    <asp:GridView ID="GridView1" runat="server" Width="100%" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" Font-Size="8" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowEditing="GridView1_RowEditing" OnRowUpdating="GridView1_RowUpdating" OnRowCancelingEdit="GridView1_RowCancelingEdit" OnRowDeleting="GridView1_RowDeleting" DataKeyNames="Id">
                                        <Columns>
                                            <asp:BoundField DataField="Id" HeaderText="Id" ReadOnly="True" Visible="true" HeaderStyle-Width="2%" ItemStyle-Width="2%" />
                                            <asp:BoundField DataField="material_id" HeaderText="Material ID" Visible="false" ReadOnly="True" HeaderStyle-Width="3%" ItemStyle-Width="3%" />
                                            <asp:BoundField DataField="material_name" HeaderText="Material Name" Visible="false" ReadOnly="True" HeaderStyle-Width="5%" ItemStyle-Width="5%" />
                                            <asp:BoundField DataField="plant_id" HeaderText="Plant ID" Visible="false" ReadOnly="True" HeaderStyle-Width="3%" ItemStyle-Width="3%" />
                                            <asp:BoundField DataField="plant_name" HeaderText="Plant Name" Visible="false" ReadOnly="True" HeaderStyle-Width="5%" ItemStyle-Width="5%" />
                                            <%--<asp:BoundField DataField="brand_id" HeaderText="Brand ID" Visible="false" ReadOnly="True" HeaderStyle-Width="3%" ItemStyle-Width="3%" />
                                            <asp:BoundField DataField="brand_name" HeaderText="Brand Name" Visible="false" ReadOnly="True" HeaderStyle-Width="5%" ItemStyle-Width="5%" />--%>
                                            <asp:BoundField DataField="field_name" HeaderText="Input Field Name" ReadOnly="True" HeaderStyle-Width="5%" ItemStyle-Width="5%" />

                                            <asp:TemplateField HeaderText="UI Display Name" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblDisplayName" runat="server" Text='<%# Eval("DisplayName") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtDisplayName" runat="server" Text='<%# Eval("DisplayName") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Show" HeaderStyle-Width="2%" ItemStyle-Width="2%">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="ViewMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("ViewMode")) %>' Enabled="false" />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:CheckBox ID="ViewMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("ViewMode")) %>' />
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Hide / Soft Delete" HeaderStyle-Width="2%" ItemStyle-Width="2%">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="DeleteMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("DeleteMode")) %>' Enabled="false" />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:CheckBox ID="DeleteMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("DeleteMode")) %>' />
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RFV Yes /No" HeaderStyle-Width="2%" ItemStyle-Width="2%">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="RFV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RFV_YesNo")) %>' Enabled="false" />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:CheckBox ID="RFV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RFV_YesNo")) %>' />
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RFV Error Msg" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRFVErrorMsg" runat="server" Text='<%# Eval("RFV_ErrorMsg") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtRFVErrorMsg" runat="server" Text='<%# Eval("RFV_ErrorMsg") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="REV Yes /No" HeaderStyle-Width="2%" ItemStyle-Width="2%">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="REV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("REV_YesNo")) %>' Enabled="false" />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:CheckBox ID="REV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("REV_YesNo")) %>' Width="100%" />
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="REV Error Msg" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblREVErrorMsg" runat="server" Text='<%# Eval("REV_ErrorMsg") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtREVErrorMsg" runat="server" Text='<%# Eval("REV_ErrorMsg") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="REV Expression" HeaderStyle-Width="10%" ItemStyle-Width="10%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblREVExpression" runat="server" Text='<%# Eval("REV_Expression") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtREVExpression" runat="server" Text='<%# Eval("REV_Expression") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RV Yes /No" HeaderStyle-Width="2%" ItemStyle-Width="2%">
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="RV_Yesno" runat="server" Checked='<%# Convert.ToBoolean(Eval("RV_Yesno")) %>' Enabled="false" />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:CheckBox ID="RV_Yesno" runat="server" Checked='<%# Convert.ToBoolean(Eval("RV_Yesno")) %>' />
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RV ErrorMsg" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRVErrorMsg" runat="server" Text='<%# Eval("RV_ErrorMsg") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtRVErrorMsg" runat="server" Text='<%# Eval("RV_ErrorMsg") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RV MinValue" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRVMinValue" runat="server" Text='<%# Eval("RV_MinValue") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtRVMinValue" runat="server" Text='<%# Eval("RV_MinValue") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="RV MaxValue" HeaderStyle-Width="5%" ItemStyle-Width="5%">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRVMaxValue" runat="server" Text='<%# Eval("RV_MaxValue") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtRVMaxValue" runat="server" Text='<%# Eval("RV_MaxValue") %>' Width="100px"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:CommandField ShowEditButton="True" HeaderText="Action" ShowDeleteButton="True" HeaderStyle-Width="5%" />
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
