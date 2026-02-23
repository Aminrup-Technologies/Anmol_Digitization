<%@ Page Title="Ui Control Mapper" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="UiControlMapper.aspx.cs" Inherits="AnmolDristi.UiControlMapper" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .mapper-box { height: 250px; overflow-y: auto; }
    </style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="container">
            <div class="row">
                <div class="col-md-12 col-sm-12">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>UI Control &rarr; Permission Mapper</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align font-weight-bold">Step 1: Select a Page <span class="text-danger">*</span></label>
                                        <div class="input-group">
                                            <asp:DropDownList ID="ddlPages" AutoPostBack="true" OnSelectedIndexChanged="ddlPages_ServerChange" runat="server" CssClass="form-control form-control-sm" />
                                            <span class="input-group-btn">
                                                <asp:Button ID="btnScan" runat="server" Text="Rescan Files" OnClick="btnScan_Click" CssClass="btn btn-secondary btn-sm" />
                                            </span>
                                        </div>
                                    </div>
                                    
                                    <div class="form-group mt-3">
                                        <label class="label-align font-weight-bold">Step 2: Select a Control on the Page <span class="text-danger">*</span></label>
                                        <%-- Added AutoPostBack and OnSelectedIndexChanged so mapping updates dynamically --%>
                                        <asp:ListBox ID="lbControls" runat="server" CssClass="form-control mapper-box" AutoPostBack="true" OnSelectedIndexChanged="lbControls_SelectedIndexChanged" />
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="label-align font-weight-bold">Step 3: Choose Permission to Map <span class="text-danger">*</span></label>
                                        <asp:DropDownList ID="ddlPermissions" runat="server" CssClass="form-control form-control-sm mb-3" />
                                        
                                        <div class="alert alert-info mt-4">
                                            <i class="fa fa-info-circle"></i> 
                                            <strong>Current Status: </strong> 
                                            <asp:Label ID="lblMappingStatus" runat="server" Text="Select a control to view mapping."></asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="col-md-12">
                                <div class="ln_solid"></div>
                                <div class="form-group row align-items-center">
                                    <div class="col-md-6 col-sm-12 font-weight-bold">
                                        <asp:Label ID="lblMessage" runat="server" Text="Click Map Control to save data."></asp:Label>
                                    </div>
                                    <div class="col-md-6 col-sm-12 text-right">
                                        <button type="button" class="btn btn-secondary btn-sm" onclick="window.location.href=window.location.href;">Reset</button>
                                        <asp:Button ID="btnMap" runat="server" Text="Map Selected Control" CssClass="btn btn-primary btn-sm" OnClick="btnMap_Click" />
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