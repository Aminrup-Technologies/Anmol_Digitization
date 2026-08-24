<%@ Page Title="ITSM Approval Configuration" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="ApprovalConfig.aspx.cs" Inherits="AnmolDristi.ITSM.UI.ApprovalConfig" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <section class="content-header">
        <h1>ITSM Approval Configuration</h1>
    </section>

    <section class="content">
        <asp:Label ID="lblMessage" runat="server" CssClass="text-danger" />

        <!-- Request Type Approval Flags -->
        <div class="box box-primary">
            <div class="box-header with-border">
                <h3 class="box-title">Request Type Approval Policy</h3>
            </div>
            <div class="box-body">
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
        <div class="box box-primary">
            <div class="box-header with-border">
                <h3 class="box-title">IT Head Approver</h3>
            </div>
            <div class="box-body">
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
        <div class="box box-primary">
            <div class="box-header with-border">
                <h3 class="box-title">IT Head Configuration History</h3>
            </div>
            <div class="box-body">
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
    </section>
</asp:Content>
