<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Form_Master.aspx.cs" Inherits="AnmolDristi.Form_Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h3>
                        <asp:Label ID="lbl_docname" runat="server" Text="N/A"></asp:Label>
                    </h3>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">

                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_FormName" runat="server" AssociatedControlID="TB_FormName" Text="Form Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_FormName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_FormName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_FormName" runat="server" ControlToValidate="TB_FormName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z_/ ]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_FormName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Form Name"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DocNo" runat="server" AssociatedControlID="TB_DocNo" Text="Document Number :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DocNo" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DocNo" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DocNo" runat="server" ControlToValidate="TB_DocNo" ForeColor="Red" ValidationGroup="Submit" ErrorMessage=" Alphanumeric Only " ValidationExpression="^[A-Za-z0-9/]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DocNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Document Number"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_DocName" runat="server" AssociatedControlID="TB_DocName" Text="Document Name :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_DocName" runat="server" ErrorMessage="Input Required" ControlToValidate="TB_DocName" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_DocName" runat="server" ControlToValidate="TB_DocName" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphabet Only" ValidationExpression="^[A-Za-z&./\- ]+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_DocName" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Document Name"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_IssueDate" runat="server" AssociatedControlID="TB_IssueDate" Text="Issue Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_IssueDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_IssueDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_IssueDate" runat="server" ControlToValidate="TB_IssueDate" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_IssueDate" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_IssueNo" runat="server" AssociatedControlID="TB_IssueNo" Text="Issue No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_IssueNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_IssueNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_IssueNo" runat="server" ControlToValidate="TB_IssueNo" ForeColor="Red" ErrorMessage="Integer Only" ValidationExpression="^\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_IssueNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Issue No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_RevisionDate" runat="server" AssociatedControlID="TB_RevisionDate" Text="Revision Date :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_RevisionDate" runat="server" ErrorMessage="Date is required " ValidationGroup="Submit" ControlToValidate="TB_RevisionDate" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_RevisionDate" runat="server" ControlToValidate="TB_RevisionDate" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_RevisionDate" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_RevisionNo" runat="server" AssociatedControlID="TB_RevisionNo" Text="Revision No. :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_RevisionNo" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_RevisionNo" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_RevisionNo" runat="server" ControlToValidate="TB_RevisionNo" ForeColor="Red" ErrorMessage="Integer Only" ValidationExpression="^\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_RevisionNo" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Revision No"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_TB_Frequency" runat="server" AssociatedControlID="TB_Frequency" Text="Frequency :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TB_Frequency" runat="server" ErrorMessage="Input Required" ValidationGroup="Submit" ControlToValidate="TB_Frequency" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="REV_TB_Frequency" runat="server" ControlToValidate="TB_Frequency" ForeColor="Red" ErrorMessage="Integer Only" ValidationExpression="^\d+$" Display="Dynamic"></asp:RegularExpressionValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TB_Frequency" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Frequency"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_Department" runat="server" AssociatedControlID="DDL_Department" Text="Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Department" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Department" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Department" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Department_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Lbl_DDL_SubDepartment" runat="server" AssociatedControlID="DDL_SubDepartment" Text="Sub Department " ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_SubDepartment" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_SubDepartment" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_SubDepartment" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_SubDepartment_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
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
                                    <asp:GridView ID="GV_FormsMaster" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" OnRowCommand="GV_FormsMaster_RowCommand" OnRowEditing="GV_FormsMaster_RowEditing" OnRowUpdating="GV_FormsMaster_RowUpdating" OnRowCancelingEdit="GV_FormsMaster_RowCancelingEdit">
                                        <Columns>
                                            <asp:TemplateField HeaderText="SL No.">
                                                <ItemTemplate>
                                                    <%# Container.DataItemIndex + 1 %>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Form ID">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="LB_FormID" runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("FormID") %>' Text='<%# Eval("FormID") %>' CssClass="btn btn-link" />
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:BoundField DataField="FormName" HeaderText="Form Name" />
                                            <asp:BoundField DataField="DocumentNumber" HeaderText="Document Number" />
                                            <asp:BoundField DataField="DocumentName" HeaderText="Document Name" />
                                            <asp:BoundField DataField="IssueDate" HeaderText="Issue Date" DataFormatString="{0:yyyy-MM-dd}" />
                                            <asp:CommandField ShowEditButton="True" />
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
