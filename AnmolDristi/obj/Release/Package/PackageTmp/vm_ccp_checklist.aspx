<%@ Page Title="AIL | QC - CCP Checklist Report" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="vm_ccp_checklist.aspx.cs" Inherits="AnmolDristi.vm_ccp_checklist" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <style type="text/css">
        .remove-border {
            border: none !important; /* This will ensure the border is removed */
            /* Add any other necessary styling */
        }

        .table-responsive {
            overflow-x: auto; /* Allow horizontal scroll */
            white-space: nowrap; /* Prevent text wrapping */
        }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Search Filters for CCP Checklist Report</h2>
                            <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>

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
                                    <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="*" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" ValidationGroup="Submit"></asp:DropDownList>

                                    </div>

                                </div>
                            </div>

                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="lblPackageDate" runat="server" AssociatedControlID="TXT_PackageDate" Text="Date From" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RFV_PackageDate" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TXT_PackageDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TXT_PackageDate" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Select Packaging Date" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>


                            <div class="col-md-2">
                                <div class="mb-2">
                                    <asp:Label ID="Label6" runat="server" AssociatedControlID="TXT_PackageDate" Text="Date To" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*" ValidationGroup="Submit" ControlToValidate="TXT_PackageDate" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                    <div class="input-group-sm">
                                        <asp:TextBox ID="TextBox1" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="Select Packaging Date" TextMode="Date"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>


        <div class="action-buttons" style="display: flex; justify-content: center; align-items: center; text-align: center;">
            <p style="margin-right: 10px; margin-bottom: 0;">Click SUBMIT to view Data!!</p>
            <asp:Button ID="btn_view_Cancel" runat="server" Text="Cancel" CssClass="btn btn-danger btn-sm" CausesValidation="false" PostBackUrl="~/qaqc_qahome_rpt.aspx" />
            <asp:Button ID="btn_view_Reset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="btn_view_Reset_Click" />
            <asp:Button ID="btn_view_submit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="btn_view_submit_Click" />
        </div>






        <div class="container">

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="label_lower" runat="server" Text="View and Select for Detailed View" Style="margin-right: 6px;"></asp:Label>
                            </h2>

                            <%--<a href="#" class="btn btn-primary btn-sm" data-toggle="modal" data-target="#exportModal">
                            <i class="fa fa-file-export"></i>Export
                        </a>--%>
                            <asp:Button ID="ExportBtn" runat="server" Text="Export" CssClass="btn btn-info btn-sm" CausesValidation="false" OnClick="ExportBtn_Click" />

                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="table-responsive">
                            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" OnRowCommand="GridView1_RowCommand">
                                <Columns>

                                    <asp:TemplateField HeaderText="Sl" HeaderStyle-Width="5%">
                                        <ItemTemplate>
                                            <asp:Label ID="lblSl" runat="server" Text='<%# Container.DataItemIndex + 1 %>'></asp:Label><br />
                                            DBID:<asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("DBID") %>' Visible="true" />
                                        </ItemTemplate>
                                    </asp:TemplateField>


                                    <asp:TemplateField HeaderText="Plant Details" HeaderStyle-Width="10%">
                                        <ItemTemplate>
                                            Plant:<asp:Label ID="lblPlant" runat="server" Text='<%# Eval("PlantName") %>'></asp:Label><br />
                                        </ItemTemplate>
                                    </asp:TemplateField>


                                    <asp:TemplateField HeaderText="Submission Details" HeaderStyle-Width="10%">
                                        <ItemTemplate>
                                            Date:<asp:Label ID="lblSubmittedDate" runat="server" Text='<%# Eval("SDate", "{0:dd/MM/yyyy}") %>'></asp:Label><br />
                                            Time:<asp:Label ID="lblSubmittedTime" runat="server" Text='<%# Eval("STime", "{0:hh\\:mm\\:ss}") %>'></asp:Label>
                                            [<asp:Label ID="lblShift" runat="server" Text='<%# Eval("SShift") %>'></asp:Label>]<br />
                                            <asp:Label ID="lblSubmittedBy" runat="server" Text='<%# Eval("EmpName") %>'></asp:Label>
                                            [<asp:Label ID="lblEmployeeCode" runat="server" Text='<%# Eval("EmpCode") %>'></asp:Label>]
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <%--<asp:TemplateField HeaderText="Metal Check" HeaderStyle-Width="30%">
                                        <ItemTemplate>
                                            <asp:Label ID="MetalCheck" runat="server" Text='<%# Eval("MetalCheck") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>--%>

                                    <asp:TemplateField HeaderText="Remarks" HeaderStyle-Width="20%">
                                        <ItemTemplate>
                                            <asp:Label ID="lblRemarks" runat="server" Text='<%# Eval("Remarks") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Approvals" HeaderStyle-Width="20%">
                                        <ItemTemplate>
                                            L1:<asp:Label ID="lbl_Approver1" runat="server" Text='<%# Eval("L1") %>' /> [<asp:Label ID="lbl_Approver1_Status" runat="server" Text='<%# Eval("Approver1_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                            L2:<asp:Label ID="lbl_Approver2" runat="server" Text='<%# Eval("L2") %>' /> [<asp:Label ID="lbl_Approver2_Status" runat="server" Text='<%# Eval("Approver2_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                            L3:<asp:Label ID="lbl_DottedLineApproverEmployeeCode" runat="server" Text='<%# Eval("L3") %>' /> [<asp:Label ID="lbl_DottedApprover_Status" runat="server" Text='<%# Eval("DottedApprover_Status") == "0" ? "Approved" : "Pending" %>' />]<br />
                                        </ItemTemplate>
                                        <ItemStyle CssClass="text" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Actions" HeaderStyle-Width="5%">
                                        <ItemTemplate>
                                            <asp:Button ID="btn_viewdetails" runat="server" Text="View" Font-Size="Smaller" CssClass="btn btn-sm btn-warning" CommandName="View" CausesValidation="false" CommandArgument="<%# Container.DataItemIndex %>" />
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                </Columns>
                            </asp:GridView>
                        </div>
                        <div class="x_content">
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>


</asp:Content>
