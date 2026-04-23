<%@ Page Title="Transfer Users" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="DeviceTransfer.aspx.cs" Inherits="AnmolDristi.DeviceTransfer" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="page-title">
                <div class="title_left">
                    <h5>Device User Management</h5>
                </div>
            </div>
            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Selective Sync / Transfer <small>Copy users between devices</small></h2>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>Source Device (Copy From) <span class="text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlSourceDevice" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="ddlSourceDevice_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>Target Device (Paste To) <span class="text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlTargetDevice" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-4 text-right pt-4">
                                    <asp:Button ID="btnCompare" runat="server" Text="Compare Devices" CssClass="btn btn-info btn-sm" OnClick="btnCompare_Click" />
                                    <asp:Button ID="btnTransfer" runat="server" Text="Transfer Selected" CssClass="btn btn-success btn-sm" OnClick="btnTransfer_Click" />
                                </div>
                            </div>
                            <div class="row mt-2">
                                <div class="col-md-12">
                                    <asp:Label ID="lbl_msg" runat="server" CssClass="font-weight-bold text-info" Text="Select a source device to load users."></asp:Label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_content">
                            <div class="card-box table-responsive">
                                <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="False"
                                    CssClass="table table-striped table-bordered dt-responsive nowrap" DataKeyNames="EnrollNumber" OnRowDataBound="gvUsers_RowDataBound">
                                    <Columns>
                                        <asp:TemplateField>
                                            <HeaderTemplate>
                                                <asp:CheckBox ID="chkAll" runat="server" onclick="checkAll(this);" />
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:CheckBox ID="chkSelect" runat="server" />
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:BoundField DataField="EnrollNumber" HeaderText="User ID (Enroll No)" />
                                        <asp:BoundField DataField="UserName" HeaderText="Name" />

                                        <asp:TemplateField HeaderText="Sync Status">
                                            <ItemTemplate>
                                                <asp:Label ID="lblSyncStatus" runat="server" Text='<%# Eval("SyncStatus") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:BoundField DataField="LocationDetails" HeaderText="Found In" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function checkAll(objRef) {
            var GridView = objRef.parentNode.parentNode.parentNode;
            var inputList = GridView.getElementsByTagName("input");
            for (var i = 0; i < inputList.length; i++) {
                var row = inputList[i].parentNode.parentNode;
                if (inputList[i].type == "checkbox" && objRef != inputList[i]) {
                    inputList[i].checked = objRef.checked;
                }
            }
        }
    </script>
</asp:Content>
