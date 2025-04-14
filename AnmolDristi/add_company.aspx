<%@ Page Title="AIL | Manage Company" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="add_company.aspx.cs" Inherits="AnmolDristi.add_company" %>

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
                                <asp:Label ID="lbl_docnumber" runat="server" Text="Add Company Details"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">

                            <div class="row">
                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_Company_Description" runat="server" AssociatedControlID="TB_Company_Description" Text="Company Description :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_Company_Description" runat="server" ErrorMessage="*" ControlToValidate="TB_Company_Description" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_Company_Description" runat="server" ControlToValidate="TB_Company_Description" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[a-zA-Z]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_Company_Description" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Company Description (3-100 characters)" MaxLength="100"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-3">
                                    <div class="mb-3">
                                        <asp:Label ID="Lbl_Company_ID" runat="server" AssociatedControlID="TB_Company_ID" Text="Company ID :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                        <asp:RequiredFieldValidator ID="RFV_Company_ID" runat="server" ErrorMessage="*" ControlToValidate="TB_Company_ID" ValidationGroup="Submit" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="REV_Company_ID" runat="server" ControlToValidate="TB_Company_ID" ForeColor="Red" ValidationGroup="Submit" ErrorMessage="Alphanumeric Only" ValidationExpression="^[0-9]*$" Display="Dynamic"></asp:RegularExpressionValidator>
                                        <div class="input-group-sm">
                                            <asp:TextBox ID="TB_Company_ID" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Company ID (3-20 characters)" MaxLength="20"></asp:TextBox>
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
                                <asp:Button ID="btn_cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="btn_cancel_Click" />
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
                                    <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 450px; overflow: scroll;">
                                        <asp:GridView ID="GridView1" runat="server" Width="100%" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowEditing="GridView1_RowEditing" OnRowUpdating="GridView1_RowUpdating" OnRowCancelingEdit="GridView1_RowCancelingEdit" OnRowDeleting="GridView1_RowDeleting" DataKeyNames="Id">
                                            <Columns>
                                                <asp:BoundField DataField="Id" HeaderText="ID" ReadOnly="True" />
                                                <asp:BoundField DataField="Company_Description" HeaderText="Company Description" />
                                                <asp:BoundField DataField="Company_ID" HeaderText="Company ID" />
                                                <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" ControlStyle-CssClass="btn btn-primary btn-sm" />
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
