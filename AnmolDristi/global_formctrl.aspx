<%@ Page Title="Global Form Controller" Language="C#" MaintainScrollPositionOnPostback="true" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="global_formctrl.aspx.cs" Inherits="AnmolDristi.global_formctrl" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <asp:HiddenField ID="hf_SelectedFieldName" runat="server" />
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label>
                    </h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">

                        <div class="x_content">

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Lbl_DDL_FormName" runat="server" AssociatedControlID="DDL_FormName" Text="Form Name " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_FormName_Value" runat="server" AssociatedControlID="DDL_FormName" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_FormName" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_FormName" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_FormName" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_FormName_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="mb-12">
                                    <asp:Label ID="Lbl_BtnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="Fetch Records" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="BtnSubmit_Click" />
                                        <asp:Button ID="BtnCreate" runat="server" Text="Create New" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="BtnCreate_Click" />
                                        <asp:Button ID="Btn_fallback" runat="server" Text="View Fallback" CssClass="btn btn-sm btn-info" CausesValidation="false" OnClick="Btn_fallback_Click"/>
                                        <asp:Button ID="Btn_home" runat="server" Text="Reset" CssClass="btn btn-sm btn-danger" CausesValidation="false" OnClick="Btn_home_Click" />
                                    </div>
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
                                        <div class="card-box col-md-12 col-sm-12" style="width: 100%; height:auto; overflow: scroll;">
                                            <asp:UpdatePanel ID="upd_GridOnly" runat="server">
                                                <ContentTemplate>
                                                    <asp:GridView ID="GV_Matrix" runat="server" Width="100%" AutoGenerateColumns="False" CssClass="table table-striped table-bordered table-sm"
                                                        ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowDataBound="GV_Matrix_RowDataBound"
                                                        OnRowEditing="GV_Matrix_RowEditing" OnRowUpdating="GV_Matrix_RowUpdating" OnRowCancelingEdit="GV_Matrix_RowCancelingEdit" OnRowDeleting="GV_Matrix_RowDeleting"
                                                        DataKeyNames="Id">

                                                        <Columns>
                                                            <asp:TemplateField HeaderText="SL No.">
                                                                <ItemStyle HorizontalAlign="Center" Width="50px" />
                                                                <ItemTemplate>
                                                                    <%# Container.DataItemIndex + 1 %>
                                                                </ItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Field Name">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_FieldName" runat="server" Text='<%# Eval("field_name") %>'></asp:Label>
                                                                </ItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Section Name">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lblSubForm" runat="server" Text='<%# Eval("Sub_Form") %>'></asp:Label>
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txtSubForm" runat="server" Text='<%# Eval("Sub_Form") %>' Width="100px"></asp:TextBox>
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Display Name">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_DisplayName" runat="server" Text='<%# Eval("DisplayName") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_DisplayName" runat="server" Text='<%# Eval("DisplayName") %>' Width="120px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="RFV?">
                                                                <ItemStyle HorizontalAlign="Center"/>
                                                                <ItemTemplate>
                                                                    <%--<asp:CheckBox ID="chk_RFV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RFV_YesNo")) %>' Enabled="false" />--%>
                                                                    <asp:CheckBox ID="chk_RFV_YesNo" runat="server" Checked='<%# Eval("RFV_YesNo") != DBNull.Value && Convert.ToBoolean(Eval("RFV_YesNo")) %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:CheckBox ID="chk_RFV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RFV_YesNo")) %>' />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="RFV Error">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_RFV_ErrorMsg" runat="server" Text='<%# Eval("RFV_ErrorMsg") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_RFV_ErrorMsg" runat="server" Text='<%# Eval("RFV_ErrorMsg") %>' Width="120px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="REV?">
                                                                <ItemStyle HorizontalAlign="Center"/>
                                                                <ItemTemplate>
                                                                    <%--<asp:CheckBox ID="chk_REV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("REV_YesNo")) %>' Enabled="false" />--%>
                                                                    <asp:CheckBox ID="chk_REV_YesNo" runat="server" Checked='<%# Eval("REV_YesNo") != DBNull.Value && Convert.ToBoolean(Eval("REV_YesNo")) %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:CheckBox ID="chk_REV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("REV_YesNo")) %>' />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="REV Error">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_REV_ErrorMsg" runat="server" Text='<%# Eval("REV_ErrorMsg") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_REV_ErrorMsg" runat="server" Text='<%# Eval("REV_ErrorMsg") %>' Width="120px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="REV Expression">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_REV_Expression" runat="server" Text='<%# Eval("REV_Expression") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:DropDownList ID="ddl_REV_Expression" runat="server" Width="140px">
                                                                        <asp:ListItem Text="--Select Regex--" Value=""></asp:ListItem>
                                                                        <asp:ListItem Text="Numeric" Value="^\d+$" />
                                                                        <asp:ListItem Text="CS Numeric" Value="^(\d+\s*(,\s*\d+\s*)*)?$" />
                                                                        <asp:ListItem Text="Decimal" Value="^\d+(\.\d{1,4})?$" />
                                                                        <asp:ListItem Text="CS Decimal" Value="^(\d+(\.\d+)?)(\s*,\s*\d+(\.\d+)?)*$" />
                                                                        <asp:ListItem Text="Alpha Only" Value="^[A-Za-z]+$" />
                                                                        <asp:ListItem Text="CS Alpha Only" Value="^([A-Za-z]+\s*(,\s*[A-Za-z]+\s*)*)?$" />
                                                                        <asp:ListItem Text="Alphanumeric" Value="^[A-Za-z0-9]+$" />
                                                                        <asp:ListItem Text="CS Alphanumeric" Value="^([A-Za-z]+[\s\-]?[0-9]*\s*(,\s*[A-Za-z]+[\s\-]?[0-9]*)*)?$" />
                                                                        <asp:ListItem Text="CS Dates" Value="^([0-3][0-9]-[0-1][0-9]-\d{4})(\s*,\s*[0-3][0-9]-[0-1][0-9]-\d{4})*$" />
                                                                        <asp:ListItem Text="Time Span" Value="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" />
                                                                    </asp:DropDownList>
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="RV?">
                                                                <ItemStyle HorizontalAlign="Center"/>
                                                                <ItemTemplate>
                                                                    <%--<asp:CheckBox ID="chk_RV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RV_Yesno")) %>' Enabled="false" />--%>
                                                                    <asp:CheckBox ID="chk_RV_YesNo" runat="server" Checked='<%# Eval("RV_Yesno") != DBNull.Value && Convert.ToBoolean(Eval("RV_Yesno")) %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:CheckBox ID="chk_RV_YesNo" runat="server" Checked='<%# Convert.ToBoolean(Eval("RV_Yesno")) %>' />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="RV Error">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_RV_ErrorMsg" runat="server" Text='<%# Eval("RV_ErrorMsg") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_RV_ErrorMsg" runat="server" Text='<%# Eval("RV_ErrorMsg") %>' Width="120px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Min">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_RV_MinValue" runat="server" Text='<%# Eval("RV_MinValue") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_RV_MinValue" runat="server" Text='<%# Eval("RV_MinValue") %>' Width="100px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Max">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_RV_MaxValue" runat="server" Text='<%# Eval("RV_MaxValue") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txt_RV_MaxValue" runat="server" Text='<%# Eval("RV_MaxValue") %>' Width="100px" />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="View Mode">
                                                                <ItemStyle HorizontalAlign="Center"/>
                                                                <ItemTemplate>
                                                                    <%--<asp:CheckBox ID="chk_ViewMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("ViewMode")) %>' Enabled="false" />--%>
                                                                    <asp:CheckBox ID="chk_ViewMode" runat="server" Checked='<%# Eval("ViewMode") != DBNull.Value && Convert.ToBoolean(Eval("ViewMode")) %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:CheckBox ID="chk_ViewMode" runat="server" Checked='<%# Convert.ToBoolean(Eval("ViewMode")) %>' />
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Control Type">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lbl_ControlType" runat="server" Text='<%# Eval("ControlType") %>' />
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:DropDownList ID="ddl_ControlType" runat="server" Width="140px">
                                                                        <asp:ListItem Text="--Select Control Type--" Value=""></asp:ListItem>
                                                                        <asp:ListItem Text="TextBox" Value="textbox" />
                                                                        <asp:ListItem Text="DropDownList" Value="dropdownlist" />
                                                                        <asp:ListItem Text="RadioButtonList" Value="radiobuttonlist" />
                                                                        <asp:ListItem Text="CheckBox" Value="checkbox" />
                                                                        <asp:ListItem Text="Label" Value="label" />
                                                                        <asp:ListItem Text="timebox" Value="timebox" />
                                                                    </asp:DropDownList>
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:TemplateField HeaderText="Default Value">
                                                                <ItemTemplate>
                                                                    <asp:Label ID="lblDefaultValue" runat="server" Text='<%# Eval("DefaultValue") %>'></asp:Label>
                                                                </ItemTemplate>
                                                                <EditItemTemplate>
                                                                    <asp:TextBox ID="txtDefaultValue" runat="server" Text='<%# Eval("DefaultValue") %>' Width="100px"></asp:TextBox>
                                                                </EditItemTemplate>
                                                            </asp:TemplateField>

                                                            <asp:CommandField ShowEditButton="True" CausesValidation="false" ShowDeleteButton="True" HeaderText="Actions" />
                                                            <%--<asp:TemplateField>
                                                                <ItemTemplate>
                                                                    <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CssClass="btn btn-sm btn-danger" Text="Delete" OnClientClick="return confirm('Are you sure you want to delete this record?');" />
                                                                </ItemTemplate>
                                                            </asp:TemplateField>--%>
                                                        </Columns>
                                                    </asp:GridView>



                                                </ContentTemplate>
                                            </asp:UpdatePanel>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                    </div>
                </div>
            </div>
        </div>

        <asp:Button ID="btnShowModalFieldControl" runat="server"
            Style="display: none;"
            OnClientClick="$('#modal_FieldControl').modal('show'); return false;" />
        <!-- Large Modal: Field Control Create/Update -->
        <div class="modal fade bs-field-modal-lg" id="modal_FieldControl" data-backdrop="static">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h4 class="modal-title">Add / Update Field Control</h4>
                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                    </div>

                    <div class="modal-body">
                        <div class="container-fluid">
                            <asp:HiddenField ID="hdn_FieldControlID" runat="server" />

                            <!-- Sub_Form (Optional Now) -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Sub Form:</label>
                                </div>
                                <div class="col-md-9">
                                    <asp:TextBox ID="txt_SubForm" runat="server" CssClass="form-control form-control-sm rounded" />
                                </div>
                            </div>

                            <!-- Field Name (Required) -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Field Name:<span class="text-danger">*</span></label>
                                </div>
                                <div class="col-md-9">
                                    <asp:TextBox ID="txt_FieldName" runat="server" CssClass="form-control form-control-sm rounded" />
                                    <asp:RequiredFieldValidator ID="RFV_FieldName" runat="server" ControlToValidate="txt_FieldName" InitialValue="" ErrorMessage="Field Name required" ForeColor="Red" Display="Dynamic" ValidationGroup="FieldPopup" />
                                </div>
                            </div>

                            <!-- Display Name (Required) -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Display Name:<span class="text-danger">*</span></label>
                                </div>
                                <div class="col-md-9">
                                    <asp:TextBox ID="txt_DisplayName" runat="server" CssClass="form-control form-control-sm rounded" />
                                    <asp:RequiredFieldValidator ID="RFV_DisplayName" runat="server" ControlToValidate="txt_DisplayName" InitialValue="" ErrorMessage="Display Name required" ForeColor="Red" Display="Dynamic" ValidationGroup="FieldPopup" />
                                </div>
                            </div>

                            <!-- RFV -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Required Field (RFV)?</label>
                                </div>
                                <div class="col-md-2">
                                    <asp:CheckBox ID="chk_RFV" runat="server" />
                                </div>
                                <div class="col-md-7">
                                    <asp:TextBox ID="txt_RFVError" runat="server" CssClass="form-control form-control-sm rounded" placeholder="RFV Error Msg" />
                                </div>
                            </div>

                            <!-- REV -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Regex Validation (REV)?</label>
                                </div>
                                <div class="col-md-2">
                                    <asp:CheckBox ID="chk_REV" runat="server" />
                                </div>
                                <div class="col-md-7">
                                    <asp:TextBox ID="txt_REVError" runat="server" CssClass="form-control form-control-sm rounded mb-1" placeholder="REV Error Msg" />
                                    <asp:DropDownList ID="ddl_RegexType" runat="server" CssClass="form-control form-control-sm rounded">
                                        <asp:ListItem Text="--Select Regex--" Value=""></asp:ListItem>
                                        <asp:ListItem Text="Numeric" Value="^\d+$" />
                                        <asp:ListItem Text="CS Numeric" Value="^(\d+\s*(,\s*\d+\s*)*)?$" />
                                        <asp:ListItem Text="Decimal" Value="^\d+(\.\d{1,4})?$" />
                                        <asp:ListItem Text="CS Decimal" Value="^(\d+(\.\d+)?)(\s*,\s*\d+(\.\d+)?)*$" />
                                        <asp:ListItem Text="Alpha Only" Value="^[A-Za-z]+$" />
                                        <asp:ListItem Text="CS Alpha Only" Value="^([A-Za-z]+\s*(,\s*[A-Za-z]+\s*)*)?$" />
                                        <asp:ListItem Text="Alphanumeric" Value="^[A-Za-z0-9]+$" />
                                        <asp:ListItem Text="CS Alphanumeric" Value="^([A-Za-z]+[\s\-]?[0-9]*\s*(,\s*[A-Za-z]+[\s\-]?[0-9]*)*)?$" />
                                        <asp:ListItem Text="CS Dates" Value="^([0-3][0-9]-[0-1][0-9]-\d{4})(\s*,\s*[0-3][0-9]-[0-1][0-9]-\d{4})*$" />
                                        <asp:ListItem Text="Time Span" Value="^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$" />
                                        
                                    </asp:DropDownList>
                                </div>
                            </div>

                            <!-- RV -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Range Validation (RV)?</label>
                                </div>
                                <div class="col-md-2">
                                    <asp:CheckBox ID="chk_RV" runat="server" />
                                </div>
                                <div class="col-md-7">
                                    <asp:TextBox ID="txt_RVError" runat="server" CssClass="form-control form-control-sm rounded mb-1" placeholder="RV Error Msg" />
                                    <div class="d-flex gap-2">
                                        <asp:TextBox ID="txt_RVMin" runat="server" CssClass="form-control form-control-sm rounded me-1" placeholder="Min" />
                                        <asp:TextBox ID="txt_RVMax" runat="server" CssClass="form-control form-control-sm rounded" placeholder="Max" />
                                    </div>
                                </div>
                            </div>

                            <!-- Control Type Dropdown -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Control Type</label>
                                </div>
                                <div class="col-md-9">
                                    <asp:DropDownList ID="ddl_ControlType" runat="server" CssClass="form-control form-control-sm rounded">
                                        <asp:ListItem Text="--Select Control Type--" Value=""></asp:ListItem>
                                        <asp:ListItem Text="TextBox" Value="textbox" />
                                        <asp:ListItem Text="DropDownList" Value="dropdownlist" />
                                        <asp:ListItem Text="RadioButtonList" Value="radiobuttonlist" />
                                        <asp:ListItem Text="CheckBox" Value="checkbox" />
                                        <asp:ListItem Text="Label" Value="label" />
                                        <asp:ListItem Text="timebox" Value="timebox" />
                                    </asp:DropDownList>
                                </div>
                            </div>

                            <!-- View Mode as Checkbox -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>View Mode:</label>
                                </div>
                                <div class="col-md-9">
                                    <asp:CheckBox ID="chk_ViewMode" runat="server" Checked="true" Text="Visible (ViewMode = 1)" />
                                </div>
                            </div>

                            <!-- Sub_Form (Optional Now) -->
                            <div class="row mb-2">
                                <div class="col-md-3">
                                    <label>Default Value:</label>
                                </div>
                                <div class="col-md-9">
                                    <asp:TextBox ID="txt_DefaultValue" runat="server" CssClass="form-control form-control-sm rounded" />
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="modal-footer">
                        <asp:Button ID="btn_CloseFieldPopup" runat="server" Text="Cancel" CausesValidation="false" CssClass="btn btn-sm btn-danger" data-dismiss="modal" />
                        <asp:Button ID="btn_SaveFieldPopup" runat="server" Text="Save Field" CssClass="btn btn-sm btn-success" OnClick="btn_SaveFieldPopup_Click" ValidationGroup="FieldPopup" />
                    </div>
                </div>
            </div>
        </div>


        <!-- Small modal -->
        <asp:Button ID="ShowPopup" runat="server" Text="Button" class="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
        <div id="MyPopup" class="modal fade bs-example-modal-sm" data-backdrop="static" tabindex="-1" role="dialog" aria-hidden="true">
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

    <script type="text/javascript">
        function ShowPopup(title, body) {
            $("#MyPopup .modal-title").html(title);
            $("#MyPopup .modal-body").html(body);
            $("#MyPopup").modal("show");
        }

        function ShowPopup1() {
            $("#modal_FieldControl").modal("show");
        }

    </script>
</asp:Content>
