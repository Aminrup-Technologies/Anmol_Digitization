<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="app_qc_inspectorreport.aspx.cs" Inherits="AnmolDristi.app_qc_inspectorreport" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <%--<div class="page-title">
                <div class="title_left">
                    <h5>Create Memo Summary</h5>
                </div>
            </div>

            <div class="clearfix"></div>--%>

            <div class="row">
                <div class="col-md-12 col-sm-12  ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Search Filters for QC Inspector Report</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_Plant_Value" runat="server" AssociatedControlID="DDL_Plant" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ValidationGroup="Filter" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" ValidationGroup="Submit" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_PlantLine_Value" runat="server" AssociatedControlID="DDL_PlantLine" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue=""></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductCategory_Value" runat="server" AssociatedControlID="DDL_ProductCategory" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>

                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    [<asp:Label ID="lbl_DDL_ProductBrand_Value" runat="server" AssociatedControlID="DDL_ProductBrand" Text="N/A" ForeColor="LightBlue" Font-Bold="true" Font-Size="Smaller"></asp:Label>]
                                    
                                    <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label7" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="false" OnSelectedIndexChanged="DDL_BrandSKU_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label3" runat="server" AssociatedControlID="txt_date1" Text="Date From" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_txt_date1" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" ControlToValidate="txt_date1" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txt_date1" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px"></asp:TextBox>
                                    </div>

                                </div>
                            </div>

                            <div class="col-md-3">
                                <div class="mb-3">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="txt_date2" Text="Date To" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_txt_date2" runat="server" ErrorMessage="*" ForeColor="Red" ValidationGroup="Filter" ControlToValidate="txt_date2" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="txt_date2" runat="server" CssClass="form-control form-control-sm rounded" class='date' type="date" name="date" BorderColor="Blue" BorderWidth="2px"></asp:TextBox>
                                    </div>

                                </div>
                            </div>

                            <!-- Small modal -->
                            <asp:Button ID="ShowPopup" runat="server" Text="Button" class="btn btn-primary" Visible="false" data-toggle="modal" data-target=".bs-example-modal-sm" />
                            <div id="MyPopup" class="modal fade bs-example-modal-sm" tabindex="-1" role="dialog" aria-hidden="true">
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
                    </div>

                    <%--button start--%>
                    <div class="col-md-6 center-margin">
                        <%--<div class="ln_solid"></div>--%>
                        <div class="item form-group row">
                            <div class="col-md-6 col-sm-12">
                                <asp:Label ID="lbl_msg" runat="server" Text="Click SUBMIT to view Data!!"></asp:Label>
                            </div>
                            <div class="col-md-6 col-sm-12">
                                <asp:Button ID="ReportbtnCancel" runat="server" Text="Home" CssClass="btn btn-danger btn-sm" ValidationGroup="Filter" CausesValidation="false" PostBackUrl="~/qaqc_qaapprovals.aspx" />
                                <button type="reset" class="btn btn-warning btn-sm">Reset</button>
                                <asp:Button ID="btn_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="true" ValidationGroup="Submit" OnClick="btn_submit_Click" />
                            </div>
                        </div>
                    </div>
                    <%--button end--%>
                </div>


                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Select for Detailed View</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li>
                                    <asp:Button ID="Button1" runat="server" Text="Export to Excel" OnClick="ExportExcel" CssClass="btn btn-primary btn-sm" CausesValidation="false" />
                                </li>
                                <li>
                                    <asp:Button ID="btnBulkApprove" runat="server" Text="Bulk Approve Selected" CssClass="btn btn-success btn-sm" OnClick="btnBulkApprove_Click" OnClientClick="return confirm('Are you sure you want to approve all selected records?');" />
                                </li>
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row">
                                <div class="card-box col-md-12 col-sm-12" style="width: 100%; height: 500px; overflow: scroll;">

                                    <asp:GridView ID="GridView1" runat="server" Width="100%" CssClass="table table-striped table-hover table-bordered table-sm table-condensed text-wrap"
                                        AutoGenerateColumns="false" ShowHeaderWhenEmpty="true" EmptyDataText="No Data Found" OnRowCommand="GridView1_RowCommand">
                                        <HeaderStyle BackColor="#2A3F54" ForeColor="White" />
                                        <Columns>

                                            <asp:TemplateField ItemStyle-Width="3%" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="text-center">
                                                <HeaderTemplate>
                                                    <input type="checkbox" id="chkAll" onclick="SelectAllCheckboxes(this);" title="Select All" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <asp:CheckBox ID="chkSelect" runat="server" />
                                                    <asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("rowid") %>' Visible="false"></asp:Label>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Context" HeaderStyle-Width="15%">
                                                <ItemTemplate>
                                                    <b>Date:</b> <%# Eval("SubmittedDate", "{0:dd-MMM-yyyy}") %><br />
                                                    <b>Time/Shift:</b> <%# BindSubmittedTime(Eval("SubmittedTime")) %> / <%# Eval("Shift") %><br />
                                                    <span class="text-muted"><i class="fa fa-user"></i>&nbsp;<%# Eval("EmployeeName") %></span>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Product Variant" HeaderStyle-Width="20%">
                                                <ItemTemplate>
                                                    <b>Plant:</b> <%# Eval("PlantName") %> (<%# Eval("LineName") %>)<br />
                                                    <b>Brand:</b> <%# Eval("ProductBrand") %><br />
                                                    <b>SKU:</b> <%# Eval("SKU_name") %>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Key STD Metrics" HeaderStyle-Width="25%">
    <ItemTemplate>
        <div style="font-size:12px; line-height: 1.5;">
            <b>Moisture:</b> <%# Eval("Moisture") %>% 
            <span class="text-primary" style="font-size:11px; font-weight:bold;">
                <%# GetStdLimitText(Eval("PlantId"), Eval("LineId"), Eval("ProductId"), Eval("BrandId"), Eval("SubmittedDate"), Eval("SubmittedTime"), "Moisture") %>
            </span><br />
            
            <b>Pkt Wgt:</b> <%# Eval("PacketWeight") %>g 
            <span class="text-primary" style="font-size:11px; font-weight:bold;">
                <%# GetStdLimitText(Eval("PlantId"), Eval("LineId"), Eval("ProductId"), Eval("BrandId"), Eval("SubmittedDate"), Eval("SubmittedTime"), "PktWgt") %>
            </span><br />
            
            <b>Baking:</b> <%# Eval("BakingTime") %> 
            <span class="text-primary" style="font-size:11px; font-weight:bold;">
                <%# GetStdLimitText(Eval("PlantId"), Eval("LineId"), Eval("ProductId"), Eval("BrandId"), Eval("SubmittedDate"), Eval("SubmittedTime"), "BakingTime") %>
            </span><br />
            
            <b>Oil %:</b> <%# Eval("OilPercentage") %>%
            <span class="text-primary" style="font-size:11px; font-weight:bold;">
                <%# GetStdLimitText(Eval("PlantId"), Eval("LineId"), Eval("ProductId"), Eval("BrandId"), Eval("SubmittedDate"), Eval("SubmittedTime"), "oilpercent") %>
            </span>
        </div>
    </ItemTemplate>
</asp:TemplateField>

<asp:TemplateField HeaderText="Workflow Status" HeaderStyle-Width="20%">
    <ItemTemplate>
        <div style="font-size:12px; line-height: 1.6;">
            A1: <%# GetApproverDisplay(Eval("Approver1EmployeeCode"), Eval("Approver1_Status")) %><br />
            A2: <%# GetApproverDisplay(Eval("Approver2EmployeeCode"), Eval("Approver2_Status")) %><br />
            CQA: <%# GetApproverDisplay(Eval("DottedLineApproverEmployeeCode"), Eval("DottedApprover_Status")) %>
        </div>
    </ItemTemplate>
</asp:TemplateField>

                                            <asp:TemplateField HeaderText="Action" HeaderStyle-Width="5%" ItemStyle-CssClass="text-center">
                                                <ItemTemplate>
                                                    <asp:Button ID="btn_viewdetails" runat="server" Text="View & Audit" CssClass="btn btn-sm btn-info" CommandName="ViewApprove" CausesValidation="false" CommandArgument="<%# Container.DataItemIndex %>" />
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
    <script type="text/javascript">
        function ShowPopup(title, body) {
            $("#MyPopup .modal-title").html(title);
            $("#MyPopup .modal-body").html(body);
            $("#MyPopup").modal("show");
        }

        function SelectAllCheckboxes(chkAll) {
            var grid = document.getElementById('<%= GridView1.ClientID %>');
            var inputs = grid.getElementsByTagName("input");
            for (var i = 0; i < inputs.length; i++) {
                if (inputs[i].type == "checkbox" && inputs[i].id !== chkAll.id) {
                    inputs[i].checked = chkAll.checked;
                }
            }
        }
    </script>
</asp:Content>
