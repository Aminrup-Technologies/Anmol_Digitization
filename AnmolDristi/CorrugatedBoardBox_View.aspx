<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="CorrugatedBoardBox_View.aspx.cs" Inherits="AnmolDristi.CorrugatedBoardBox_View" %>
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
                            <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h2>
                        <div class="clearfix"></div>
                    </div>

                    <div class="row">
                        <div class="col-12">
                            <div class="tab-content ml-1" id="myTabContent">
                                <div class="x-content">

                                    <div class="col-md-3">
                                        <div class="mb-3">
                                            <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                            <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                            <div class="input-group-sm">
                                                <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col-md-3">
                                        <div class="mb-3">
                                            <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                            <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                            <div class="input-group-sm">
                                                <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="col-md-3">
                                        <div class="mb-3">
                                            <asp:Label ID="Lbl_Date_From" runat="server" AssociatedControlID="TB_Date_From" Text="Date From :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                            <asp:RequiredFieldValidator ID="RFV_Date_From" runat="server" ErrorMessage="Date is required " ControlToValidate="TB_Date_From" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                            <asp:RegularExpressionValidator ID="REV_Date_From" runat="server" ControlToValidate="TB_Date_From" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                            <div class="input-group-sm">
                                                <asp:TextBox ID="TB_Date_From" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="col-md-3">
                                        <div class="mb-3">
                                            <asp:Label ID="Lbl_Date_To" runat="server" AssociatedControlID="TB_Date_To" Text="Date To :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                            <asp:RequiredFieldValidator ID="RFV_Date_To" runat="server" ErrorMessage="Date is required " ControlToValidate="TB_Date_To" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                            <asp:RegularExpressionValidator ID="REV_Date_To" runat="server" ControlToValidate="TB_Date_To" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                            <div class="input-group-sm">
                                                <asp:TextBox ID="TB_Date_To" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
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

        <%--Button--%>
        <div class="col-md-12 d-flex justify-content-center align-items-center">
            <div class="mb-3 ">
                <div class="input-group input-group-sm">
                    <asp:Label ID="lblInstruction" runat="server" Text="Click SUBMIT to view data!!!" CssClass="clearfix" Style="padding-right: 5em" />
                    <asp:Button ID="ReportbtnCancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" OnClick="ReportbtnCancel_Click" />
                    <asp:Button ID="ReportbtnSubmit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="ReportbtnSubmit_Click" />
                    <asp:Button ID="ReportbtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="ReportbtnReset_Click" />
                </div>
            </div>
        </div>


        <div class="row">
            <div class="col-md-12 col-sm-12 ">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>
                            <asp:Label ID="lbl_viewname" runat="server" Text="Label"></asp:Label>
                            <asp:Button ID="ExportBtn" runat="server" Text="Export" CssClass="btn btn-info btn-sm" CausesValidation="false" />
                        </h2>
                        <div class="clearfix"></div>
                    </div>

                    <asp:GridView ID="GridView1" runat="server" Width="100%" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" AutoGenerateColumns="false"
                        ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowCommand="GridView1_RowCommand">
                        <Columns>

                            <asp:TemplateField HeaderText="Sl" HeaderStyle-Width="5%">
                                <ItemTemplate>
                                    <asp:Label ID="lblSl" runat="server" Text='<%# Container.DataItemIndex + 1 %>'></asp:Label><br />
                                    DBID:<asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("DBID") %>' Visible="true" />
                                </ItemTemplate>
                            </asp:TemplateField>


                            <asp:TemplateField HeaderText="Plant Details" HeaderStyle-Width="20%">
                                <ItemTemplate>
                                    Plant:<asp:Label ID="lbl_PlantName" runat="server" Text='<%# Eval("PlantName") %>' Font-Bold="true" /><br />
                                    Brand:<asp:Label ID="lbl_ProductBrand" runat="server" Text='<%# Eval("ProductBrand") %>' Font-Bold="true" /><br />
                                </ItemTemplate>
                            </asp:TemplateField>


                            <asp:TemplateField HeaderText="Submission Details" HeaderStyle-Width="20%">
                                <ItemTemplate>
                                    Date:<asp:Label ID="lblSubmittedDate" runat="server" Text='<%# Eval("SDate", "{0:dd/MM/yyyy}") %>'></asp:Label><br />
                                    Time:<asp:Label ID="lblSubmittedTime" runat="server" Text='<%# Eval("STime", "{0:hh\\:mm\\:ss}") %>'></asp:Label>
                                    <%--[<asp:Label ID="lblShift" runat="server" Text='<%# Eval("SShift") %>'></asp:Label>]<br />--%>
                                    <asp:Label ID="lblSubmittedBy" runat="server" Text='<%# Eval("EmpName") %>'></asp:Label>
                                    [<asp:Label ID="lblEmployeeCode" runat="server" Text='<%# Eval("EmpCode") %>'></asp:Label>]
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                <ItemTemplate>
                                    <asp:Label ID="lblRemarks" runat="server" Text='<%# Eval("Remarks") %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>


                            <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="5%">
                                <ItemTemplate>
                                    <asp:Button ID="btn_viewdetails" runat="server" Text="View" Font-Size="Smaller" CssClass="btn btn-sm btn-warning" CommandName="View" CausesValidation="false" CommandArgument='<%# Eval("DBID") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>



                </div>
            </div>
        </div>


    </div>
</div>


</asp:Content>
