<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="viewusers.aspx.cs" Inherits="AnmolDristi.viewusers" %>

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
                                        <asp:GridView ID="gvEmployees" runat="server" class="table table-striped table-hover table-bordered table-responsive table-sm table-condensed" AutoGenerateColumns="False" OnRowCommand="gvEmployees_RowCommand">
                                            <Columns>
                                                <asp:TemplateField HeaderText="Employee Code">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="lnkEmployeeCode" runat="server" CommandName="EditEmployee" CommandArgument='<%# Eval("EmployeeCode") %>'> <%# Eval("EmployeeCode") %>
                                                        </asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                                <asp:BoundField DataField="EmployeeName" HeaderText="Employee Name" />

                                                <asp:TemplateField HeaderText="Status">
                                                    <ItemTemplate>
                                                        <asp:Button
                                                            ID="btnToggleStatus"
                                                            runat="server"
                                                            Text='<%# Eval("EmployeeStatus").ToString() == "Active" ? "Block" : "Activate" %>'
                                                            CssClass='<%# Eval("EmployeeStatus").ToString() == "Active" ? "btn btn-danger btn-sm" : "btn btn-success btn-sm" %>'
                                                            CommandName="ToggleStatus"
                                                            CommandArgument='<%# Eval("EmployeeCode") %>' />
                                                    </ItemTemplate>
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
        </div>
    </div>
</asp:Content>
