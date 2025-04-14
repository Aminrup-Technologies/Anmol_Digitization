<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="frmmastering_home.aspx.cs" Inherits="AnmolDristi.frmmastering_home" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <%--<div class="page-title">
                <div class="title_left">
                    <h2>Quality Report Forms</h2>
                </div>
            </div>

            <div class="clearfix"></div>--%>

            <div class="row">
                <div class="col-md-6" id="Production_MST" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Production Masters</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="add_prod_plants.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label10" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Plants

                            </a>
                            <a class="btn btn-app" href="add_plant_lines.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label11" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Lines

                            </a>
                            <a class="btn btn-app" href="add_prodcategory.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label12" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Category

                            </a>
                            <a class="btn btn-app" href="add_brandtypes.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label13" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Brands

                            </a>
                            <a class="btn btn-app" href="qaqc_mst_sku.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label14" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>SKU

                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="Forms_CTRL" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Forms Input Controller</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="qaqc_qcinspector_frmctrl.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label15" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Plants
                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-red">WIP
                                    <asp:Label ID="Label16" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Lines
                            </a>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
