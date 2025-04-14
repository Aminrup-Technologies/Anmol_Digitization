<%@ Page Title="Production | Misc Reports" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="prod_misc_rpts.aspx.cs" Inherits="AnmolDristi.prod_misc_rpts" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="row">
                <div class="col-md-12" id="QC_Panel" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Production Misc. Forms</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">

                            <a class="btn btn-app" href="qaqc_oven_viewpage.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="Label2" runat="server" Text="0" Visible="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Oven Report
                            </a>

                            <a class="btn btn-app" href="FinalCbbWtVM.aspx">
                                <span class="badge bg-green">
                                    <asp:Label ID="Label4" runat="server" Text="0" Visible="true"></asp:Label></span>
                                <i class="fa fa-edit"></i>Final CBB Weight
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
