<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="WebsiteCredentialsView.aspx.cs" Inherits="AnmolDristi.WebsiteCredentialsView" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Website Credential View</h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content row mb-3">
                            <div class="col-md-2">
                                <label class="small text-primary font-weight-bold">Added By Code</label>
                                <asp:TextBox ID="txtAddedByCode" runat="server" CssClass="form-control form-control-sm rounded" />
                            </div>

                            <div class="col-md-2">
                                <label class="small text-primary font-weight-bold">Added By Name</label>
                                <asp:TextBox ID="txtAddedByName" runat="server" CssClass="form-control form-control-sm rounded" />
                            </div>

                            <div class="col-md-2">
                                <label class="small text-primary font-weight-bold">Website Type</label>
                                <asp:DropDownList ID="ddlWebsiteType" runat="server" CssClass="form-control form-control-sm rounded">
                                    <asp:ListItem Text="All" Value="" />
                                    <asp:ListItem Text="Govt" Value="Govt" />
                                    <asp:ListItem Text="Commercial" Value="Commercial" />
                                    <asp:ListItem Text="Internal" Value="Internal" />
                                </asp:DropDownList>
                            </div>

                            <div class="col-md-3">
                                <label class="small text-primary font-weight-bold">Search Site Name</label>
                                <asp:TextBox ID="txtSearchSiteName" runat="server" CssClass="form-control form-control-sm rounded" />
                            </div>

                            <div class="col-md-2 mt-4">
                                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-info btn-sm" OnClick="btnSearch_Click" />
                            </div>

                            <%--<asp:Button ID="btnTestDecrypt" runat="server" Text="Test Decrypt" CssClass="btn btn-info mt-2" OnClick="btnTestDecrypt_Click" />--%>
                        </div>

                        <asp:GridView ID="GridViewFiltered" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered table-sm"
                            EmptyDataText="No credentials found." AllowPaging="true" PageSize="20" OnRowCommand="GridViewFiltered_RowCommand">
                            <Columns>
                                <asp:BoundField DataField="SiteName" HeaderText="Site Name" />
                                <asp:BoundField DataField="Purpose" HeaderText="Purpose" />
                                <asp:BoundField DataField="UsageFrequency" HeaderText="Frequency" />
                                <asp:BoundField DataField="AddedByCode" HeaderText="Added By Code" />
                                <asp:BoundField DataField="AddedByName" HeaderText="Added By Name" />
                                <asp:TemplateField HeaderText="Details">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnViewDetails" runat="server" Text="View" CssClass="btn btn-sm btn-info"
                                            CommandName="ViewDetails" CommandArgument='<%# Eval("Id") %>' />
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>

            <div class="modal fade" id="detailsModal" tabindex="-1" role="dialog" aria-labelledby="detailsModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg" role="document">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title" id="detailsModalLabel">Website Credential Details</h5>
                            <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div class="modal-body">
                            <asp:Label ID="lblModalContent" runat="server" Text="" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
