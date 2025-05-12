<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qaqc_rotaryline_approval.aspx.cs" Inherits="AnmolDristi.qaqc_rotaryline_approval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="right_col" role="main">
        <div class="container">
            <div class="clearfix"></div>
            <div class="row">
                <div class="col-md-12" id="View_Panel" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_upper" runat="server" Text="Label"></asp:Label>
                            </h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2" runat="server" visible="false">
                                <div class="mb-2">
                                    <asp:Label ID="Label7" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_BrandSKU_Value" runat="server" AssociatedControlID="DDL_BrandSKU" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label8" runat="server" AssociatedControlID="TxtDateFrom" Text="Date From" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TxtDateFrom" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TxtDateFrom" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TxtDateFrom" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label9" runat="server" AssociatedControlID="TxtDateTo" Text="Date To" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_TxtDateTo" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Submit" ControlToValidate="TxtDateTo" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TxtDateTo" runat="server" CssClass="form-control form-control-sm rounded" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                        </div>
                    </div>
                </div>

                <div class="col-md-12 text-center">
                    <asp:Label ID="lbl_oven" runat="server" Text="Click SUBMIT to view Data!!" ForeColor="SlateGray" Font-Bold="true"></asp:Label>
                    <asp:Button ID="ReportbtnCancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" PostBackUrl="~/qaqc_qaapprovals.aspx" />
                    <asp:Button ID="btn_view_reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" OnClick="btn_view_reset_Click" CausesValidation="false" />
                    <asp:Button ID="btn_view_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" OnClick="btn_view_submit_Click" CausesValidation="false" />

                </div>


                <div class="col-md-12" id="Div1" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_lower" runat="server" Text="Label"></asp:Label>
                            </h2>
                            <asp:Button ID="btn_view_export" runat="server" Text="Export" class="btn btn-primary btn-sm" CausesValidation="false" OnClick="btn_view_export_Click" />


                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="table-responsive">
                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" ShowHeaderWhenEmpty="true" 
                                        CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" OnRowCommand="GridView1_RowCommand" EmptyDataText="No records for selected filter">

                                <Columns>
                                    <asp:TemplateField HeaderText="Sl">
                                        <ItemTemplate>
                                            <asp:Label ID="lblSl" runat="server" Text='<%# Container.DataItemIndex + 1 %>'></asp:Label><br />
                                            <br />
                                            <asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("DBID") %>' Visible="false" />
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Plant and Line Details">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPlant" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Plant: " + "<strong>" +  Eval("PlantName") + "</strong>" %>'></asp:Label>
                                            <br />
                                            <asp:Label ID="lblLine" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Line: " + "<strong>" + Eval("LineName")  + "</strong>" %>'></asp:Label>
                                            <br />
                                            <asp:Label ID="lblCategory" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Category:" + "<strong>" + Eval("ProductCategory") + "</strong>" %>'></asp:Label>
                                            <br />
                                            <asp:Label ID="lblBrand" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Brand:" + "<strong>" + Eval("ProductBrand") + "</strong>" %>'></asp:Label>
                                            <br />
                                            <%--<asp:Label ID="lblSKUId" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "SKU: " + "<strong>" + Eval("SKUId") + "</strong>" %>'></asp:Label>--%>
                                        </ItemTemplate>
                                    </asp:TemplateField>


                                    <asp:TemplateField HeaderText="Submission Details">
                                        <ItemTemplate>
                                            <asp:Label ID="Label2" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Date: <span style=\"color: red; font-weight: bold;\">" + Eval("SDate","{0:dd-MM-yyyy}") + "</span>" %>'></asp:Label>
                                            <br />
                                            <asp:Label ID="Label3" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Time: <span style=\"color: red; font-weight: bold;\">" + DataBinder.Eval(Container.DataItem, "STime", "{0:hh\\:mm\\:ss}") + "</span>" %>'></asp:Label>
                                            <br />
                                            <asp:Label ID="Label10" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Submitter:" + "<strong>" + Eval("EmpName")+ "</strong>"%>'></asp:Label>
                                            <br />
                                            <asp:Label ID="lblName" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Submitter Code:" + "<strong>" + Eval("EmpCode")+ "</strong>"%>'></asp:Label>
                                            <br />
                                            <asp:Label ID="lblShift" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Shift: " + "<strong>" + Eval("SShift") + "</strong>" %>'></asp:Label>
                                            <br />
                                            <%--<asp:Label ID="lblVariety" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Variety: "+Eval("Variety") %>'></asp:Label><br />--%>
                                        </ItemTemplate>
                                    </asp:TemplateField>



                                    <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                        <ItemTemplate>
                                            <asp:Label ID="lblRemarks" runat="server" Text='<%# Eval("Remarks") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Approvals" HeaderStyle-Width="20%">
                                        <ItemTemplate>
                                            L1:<asp:Label ID="lbl_Approver1" runat="server" Text='<%# Eval("L1") %>' />
                                            [<asp:Label ID="lbl_Approver1_Status" runat="server" Text='<%# Eval("Approver1_Status") != null && Convert.ToString(Eval("Approver1_Status")) == "1" ? "Approved" : "Pending" %>' />]<br />
                                            L2:<asp:Label ID="lbl_Approver2" runat="server" Text='<%# Eval("L2") %>' />
                                            [<asp:Label ID="lbl_Approver2_Status" runat="server" Text='<%# Eval("Approver2_Status") != null && Convert.ToString(Eval("Approver2_Status")) == "1" ? "Approved" : "Pending" %>' />]<br />
                                            L3:<asp:Label ID="lbl_DottedLineApproverEmployeeCode" runat="server" Text='<%# Eval("L3") %>' />
                                            [<asp:Label ID="lbl_DottedApprover_Status" runat="server" Text='<%# Eval("DottedApprover_Status") != null && Convert.ToString(Eval("DottedApprover_Status")) == "1" ? "Approved" : "Pending" %>' />]<br />
                                        </ItemTemplate>
                                        <ItemStyle CssClass="text" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="5%">
                                        <ItemTemplate>
                                            <asp:Button ID="btn_viewdetails" runat="server" Text="Approve" Font-Size="Smaller" CssClass="btn btn-sm btn-warning" CommandName="View" CausesValidation="false" CommandArgument="<%# Container.DataItemIndex %>" />
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

</asp:Content>
