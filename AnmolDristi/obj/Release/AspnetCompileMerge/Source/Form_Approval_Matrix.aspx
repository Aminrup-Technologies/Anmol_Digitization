<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Form_Approval_Matrix.aspx.cs" Inherits="AnmolDristi.Form_Approval_Matrix" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

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

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_FormName" runat="server" AssociatedControlID="DDL_FormName" Text="Form Name " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_FormName_Value" runat="server" AssociatedControlID="DDL_FormName" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_FormName" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_FormName" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_FormName" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_FormName_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Plant" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Line" runat="server" AssociatedControlID="DDL_Line" Text=" Line Id" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Line_Value" runat="server" AssociatedControlID="DDL_Line" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Line" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Line" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Line" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Line_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Department" runat="server" AssociatedControlID="DDL_Department" Text="Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Department_Value" runat="server" AssociatedControlID="DDL_Department" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Department" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Department" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Department" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Department_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_SubDepartment" runat="server" AssociatedControlID="DDL_SubDepartment" Text="Sub Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_SubDepartment_Value" runat="server" AssociatedControlID="DDL_SubDepartment" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_SubDepartment" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_SubDepartment" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_SubDepartment" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_SubDepartment_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_App1" runat="server" AssociatedControlID="TB_App1" Text="Approver 1 Employee Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_App1" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_App1" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_App1" runat="server" ControlToValidate="TB_App1" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[A-Z0-9]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_App1" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="XYZ123" AutoPostBack="true" OnTextChanged="TB_App1_TextChanged"></asp:TextBox>
                                    </div>
                                    <asp:Label ID="Lbl_EmployeeName1" runat="server" Text="" ForeColor="Green" Font-Bold="true" Font-Size="Small"></asp:Label><br />
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_App2" runat="server" AssociatedControlID="TB_App2" Text="Approver 2 Employee Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_App2" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_App2" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_App2" runat="server" ControlToValidate="TB_App2" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[A-Z0-9]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_App2" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="XYZ123" AutoPostBack="true" OnTextChanged="TB_App2_TextChanged"></asp:TextBox>
                                    </div>
                                    <asp:Label ID="Lbl_EmployeeName2" runat="server" Text="" ForeColor="Green" Font-Bold="true" Font-Size="Small"></asp:Label><br />
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_App3" runat="server" AssociatedControlID="TB_App3" Text="DottedLine Approver Employee Code :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_App3" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_App3" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_App3" runat="server" ControlToValidate="TB_App3" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[A-Z0-9]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_App3" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="XYZ123" AutoPostBack="true" OnTextChanged="TB_App3_TextChanged"></asp:TextBox>
                                    </div>
                                    <asp:Label ID="Lbl_EmployeeName3" runat="server" Text="" ForeColor="Green" Font-Bold="true" Font-Size="Small"></asp:Label><br />
                                </div>
                            </div>

                            <%--Button--%>
                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_BtnSubmit" runat="server" AssociatedControlID="BtnSubmit" Text="Click to SAVE" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <div class="input-group input-group-sm">
                                        <asp:Button ID="BtnSubmit" runat="server" Text="Save" CssClass="btn btn-primary btn-sm" ValidationGroup="Submit" CausesValidation="true" OnClick="BtnSubmit_Click" />
                                        <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="BtnReset_Click" />
                                        <asp:Button ID="Btn_home" runat="server" Text="HOME" CssClass="btn btn-sm btn-danger" CausesValidation="false" PostBackUrl="~/home.aspx" />
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
                                        <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 450px; overflow: scroll;">
                                            <asp:GridView ID="GV_FormsApprovalMatrix" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" ShowHeaderWhenEmpty="true"
                                                OnRowCommand="GV_FormsApprovalMatrix_RowCommand" OnRowEditing="GV_FormsApprovalMatrix_RowEditing"
                                                OnRowUpdating="GV_FormsApprovalMatrix_RowUpdating" OnRowCancelingEdit="GV_FormsApprovalMatrix_RowCancelingEdit">

                                                <Columns>
                                                    <asp:TemplateField HeaderText="ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_Id" runat="server" Text='<%# Eval("Id") %>'></asp:Label>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Form ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_FormID" runat="server" Text='<%# Eval("FormID") %>'></asp:Label>
                                                        </ItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Plant ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_PlantID" runat="server" Text='<%# Eval("plant_id") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_PlantID" runat="server" Text='<%# Bind("plant_id") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Line ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_LineID" runat="server" Text='<%# Eval("line_id") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_LineID" runat="server" Text='<%# Bind("line_id") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Department ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_DepartmentID" runat="server" Text='<%# Eval("Department_ID") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_DepartmentID" runat="server" Text='<%# Bind("Department_ID") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Sub-Department ID">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_SubDepartmentID" runat="server" Text='<%# Eval("SubDepartment_ID") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_SubDepartmentID" runat="server" Text='<%# Bind("SubDepartment_ID") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Approver 1 Employee Code">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_Approver1" runat="server" Text='<%# Eval("Approver1EmployeeCode") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_Approver1" runat="server" Text='<%# Bind("Approver1EmployeeCode") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Approver 2 Employee Code">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_Approver2" runat="server" Text='<%# Eval("Approver2EmployeeCode") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_Approver2" runat="server" Text='<%# Bind("Approver2EmployeeCode") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:TemplateField HeaderText="Dotted Line Approver Employee Code">
                                                        <ItemTemplate>
                                                            <asp:Label ID="Lbl_DottedLineApprover" runat="server" Text='<%# Eval("DottedLineApproverEmployeeCode") %>'></asp:Label>
                                                        </ItemTemplate>
                                                        <EditItemTemplate>
                                                            <asp:TextBox ID="Txt_DottedLineApprover" runat="server" Text='<%# Bind("DottedLineApproverEmployeeCode") %>' CssClass="form-control"></asp:TextBox>
                                                        </EditItemTemplate>
                                                    </asp:TemplateField>

                                                    <asp:CommandField ShowEditButton="True" CausesValidation="false" />
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
    </div>

</asp:Content>
