<%@ Page Title="Ui Control Mapper" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="UiControlMapper.aspx.cs" Inherits="AnmolDristi.UiControlMapper" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12  ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>UI Control → Permission Mapper</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Select :<span class="text text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlPages" OnSelectedIndexChanged="ddlPages_ServerChange" runat="server" />
                                        <asp:Button ID="btnScan" runat="server" Text="Scan Pages (server file scan)" OnClick="btnScan_Click" />
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align">Select :<span class="text text-danger">*</span></label>
                                        <asp:ListBox ID="lbControls" runat="server" Width="400px" />
                                        <asp:DropDownList ID="ddlPermissions" runat="server" />
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-6 center-margin">
                                <div class="ln_solid"></div>
                                <div class="item form-group row">
                                    <div class="col-md-6 col-sm-12">
                                        <asp:Label ID="lblMessage" runat="server" Text="Click SUBMIT to Save Data!!"></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12">
                                        <button type="button" class="btn btn-danger btn-sm collapse-link">Cancel</button>
                                        <button type="reset" class="btn btn-warning btn-sm">Reset</button>
                                        <asp:Button ID="btnMap" runat="server" Text="Map selected control to permission" CssClass="btn btn-primary btn-sm" OnClick="btnMap_Click" />
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
