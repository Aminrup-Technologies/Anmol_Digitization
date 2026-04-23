<%@ Page Title="Add Machine" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="AddMachine.aspx.cs" Inherits="AnmolDristi.AddMachine" AspCompat="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Canteen Management System</h5>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Add Biometric Device <small>ZKTeco</small></h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>Machine Name <span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtMachineName" runat="server" CssClass="form-control form-control-sm rounded" placeholder="e.g., Gate 1 Canteen"></asp:TextBox>
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>IP Address <span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtIPAddress" runat="server" CssClass="form-control form-control-sm rounded" placeholder="192.168.1.201"></asp:TextBox>
                                    </div>
                                </div>

                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>Port <span class="text text-danger">*</span></label>
                                        <asp:TextBox ID="txtPort" runat="server" CssClass="form-control form-control-sm rounded" Text="4370"></asp:TextBox>
                                    </div>
                                </div>
                            </div>

                            <%--button start--%>
                            <div class="col-md-12 center-margin mt-3">
                                <div class="ln_solid"></div>
                                <div class="item form-group row">
                                    <div class="col-md-6 col-sm-12">
                                        <asp:Label ID="lbl_msg" runat="server" Text="Click SUBMIT to Save Data!!" CssClass="font-weight-bold text-info"></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-danger btn-sm collapse-link">Cancel</button>
                                        <button type="reset" class="btn btn-warning btn-sm">Reset</button>
                                        <asp:Button ID="btnSave" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" OnClick="btnSave_Click" />
                                    </div>
                                </div>
                            </div>
                            <%--button end--%>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>View and Manage : Configured Devices</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-12 col-sm-12">
                                    <div class="card-box table-responsive">
                                        <asp:GridView ID="gvMachines" runat="server" AutoGenerateColumns="False"
                                            CssClass="table table-striped table-bordered dt-responsive nowrap"
                                            DataKeyNames="MachineID,IPAddress,PortNumber" OnRowCommand="gvMachines_RowCommand">
                                            <Columns>
                                                <asp:BoundField DataField="MachineName" HeaderText="Machine Name" />
                                                <asp:BoundField DataField="IPAddress" HeaderText="IP Address" />
                                                <asp:BoundField DataField="PortNumber" HeaderText="Port" />
                                                <asp:TemplateField HeaderText="Status">
                                                    <ItemTemplate>
                                                        <span class='badge <%# Convert.ToBoolean(Eval("IsActive")) ? "badge-success" : "badge-danger" %>'>
                                                            <%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %>
                                                        </span>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="Actions">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="btnTest" runat="server" CommandName="TestConnection"
                                                            CommandArgument="<%# ((GridViewRow) Container).RowIndex %>"
                                                            CssClass="btn btn-info btn-xs"><i class="fa fa-wifi"></i> Test</asp:LinkButton>

                                                        <asp:LinkButton ID="btnSyncLogs" runat="server" CommandName="SyncLogs"
                                                            CommandArgument="<%# ((GridViewRow) Container).RowIndex %>"
                                                            CssClass="btn btn-warning btn-xs"><i class="fa fa-download"></i> Logs</asp:LinkButton>

                                                        <asp:LinkButton ID="btnSyncUsers" runat="server" CommandName="SyncUsers"
                                                            CommandArgument="<%# ((GridViewRow) Container).RowIndex %>"
                                                            CssClass="btn btn-primary btn-xs"><i class="fa fa-users"></i> Users</asp:LinkButton>
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
