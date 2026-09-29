<%-- ========================================================================
     File:    ApprovalConfig.aspx
     Layer:   UI — Presentation
     When:    2026-08-24
     What:    ITSM admin page for request-type approval flags and IT Head
              configuration. Uses Gentelella x_panel layout per Dristi.Master.
     Why:     AdminLTE classes (box, content-header) do not exist in the
              Gentelella theme. Structure normalized to TicketList reference.
     ======================================================================== --%>
<%@ Page Title="ITSM Approval Configuration" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ApprovalConfig.aspx.cs" Inherits="AnmolDristi.ITSM.UI.ApprovalConfig" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="row">
            <div class="col-md-12 col-sm-12 col-xs-12">

                <!-- Page Header -->
                <div class="x_panel">
                    <div class="x_title">
                        <h2><i class="fa fa-cogs"></i> ITSM Approval Configuration</h2>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Label ID="lblMessage" runat="server" Font-Bold="true"
                            Style="display: block; margin-bottom: 10px;"></asp:Label>
                    </div>
                </div>

                <!-- Request Type Approval Flags -->
                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-list"></i> Request Type Approval Policy</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:GridView ID="gvRequestTypes" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false" OnRowCommand="gvRequestTypes_RowCommand"
                            DataKeyNames="Id">
                            <Columns>
                                <asp:BoundField DataField="Code" HeaderText="Code" ItemStyle-Width="80px" />
                                <asp:BoundField DataField="Name" HeaderText="Request Type" />
                                <asp:TemplateField HeaderText="Manager Approval" ItemStyle-Width="150px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkManager" runat="server"
                                            Checked='<%# Eval("RequiresManagerApproval") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="IT Head Approval" ItemStyle-Width="150px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkItHead" runat="server"
                                            Checked='<%# Eval("RequiresItHeadApproval") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Action" ItemStyle-Width="100px" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkSave" runat="server" CommandName="SaveFlags"
                                            CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-xs"
                                            Text="Save" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>

                <!-- IT Head Approver Configuration -->
                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-user-secret"></i> IT Head Approver</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:Panel ID="pnlCurrentItHead" runat="server">
                            <div class="form-group">
                                <label>Currently Configured IT Head:</label>
                                <asp:Label ID="lblCurrentItHead" runat="server" CssClass="form-control-static" />
                                <asp:Label ID="lblCurrentItHeadStatus" runat="server" CssClass="label label-info" />
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlNoItHead" runat="server" Visible="false">
                            <div class="alert alert-warning">
                                No IT Head approver is currently configured. IT Head approval will fail for request types that require it.
                            </div>
                        </asp:Panel>

                        <div class="form-group">
                            <label>Select Active Employee as IT Head:</label>
                            <asp:DropDownList ID="ddlItHead" runat="server" CssClass="form-control"
                                AppendDataBoundItems="true">
                                <asp:ListItem Value="" Text="-- Select Employee --" />
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvItHead" runat="server"
                                ControlToValidate="ddlItHead" ValidationGroup="ItHead"
                                ErrorMessage="Please select an employee." Display="Dynamic"
                                ForeColor="Red" />
                        </div>

                        <asp:Button ID="btnSetItHead" runat="server" Text="Set as IT Head"
                            CssClass="btn btn-primary" ValidationGroup="ItHead"
                            OnClick="btnSetItHead_Click" />
                    </div>
                </div>

                <!-- Configuration History -->
                <div class="x_panel">
                    <div class="x_title">
                        <h3><i class="fa fa-history"></i> IT Head Configuration History</h3>
                        <div class="clearfix"></div>
                    </div>
                    <div class="x_content">
                        <asp:GridView ID="gvItHeadHistory" runat="server" CssClass="table table-bordered table-striped"
                            AutoGenerateColumns="false">
                            <Columns>
                                <asp:BoundField DataField="ItHeadApproverCode" HeaderText="Employee Code" />
                                <asp:BoundField DataField="ItHeadApproverName" HeaderText="Employee Name" />
                                <asp:BoundField DataField="ItHeadApproverStatus" HeaderText="Status" />
                                <asp:TemplateField HeaderText="Active" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:Label ID="lblActive" runat="server"
                                            CssClass='<%# (bool)Eval("IsActive") ? "label label-success" : "label label-default" %>'
                                            Text='<%# (bool)Eval("IsActive") ? "Yes" : "No" %>' />
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
