<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SwitchUser.aspx.cs" Inherits="AnmolDristi.SwitchUser" MasterPageFile="~/Dristi.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="head" runat="server">
    <style type="text/css">
        .switch-user-banner {
            background: linear-gradient(135deg, #e67e22 0%, #d35400 100%);
            color: #fff;
            padding: 12px 20px;
            border-radius: 4px;
            margin-bottom: 20px;
            font-weight: 600;
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
            gap: 10px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.15);
        }
        .switch-user-banner .banner-info {
            display: flex;
            align-items: center;
            gap: 12px;
            flex-wrap: wrap;
        }
        .switch-user-banner .banner-info i {
            font-size: 20px;
        }
        .switch-user-banner .banner-detail {
            font-weight: 400;
            font-size: 13px;
            opacity: 0.9;
        }
        .switch-user-banner .btn-return {
            background: #fff;
            color: #d35400;
            border: none;
            padding: 6px 16px;
            border-radius: 3px;
            font-weight: 600;
            cursor: pointer;
            transition: background 0.2s;
        }
        .switch-user-banner .btn-return:hover {
            background: #f8f9fa;
        }
        .su-search-panel {
            background: #fff;
            border: 1px solid #e0e0e0;
            border-radius: 4px;
            padding: 20px;
            margin-bottom: 20px;
        }
        .su-search-panel h4 {
            margin-top: 0;
            margin-bottom: 15px;
            color: #333;
            font-weight: 600;
        }
        .su-results-panel {
            background: #fff;
            border: 1px solid #e0e0e0;
            border-radius: 4px;
            padding: 20px;
        }
        .su-results-panel h4 {
            margin-top: 0;
            margin-bottom: 15px;
            color: #333;
            font-weight: 600;
        }
        .su-message {
            padding: 10px 15px;
            border-radius: 4px;
            margin-bottom: 15px;
            font-weight: 500;
        }
        .su-message.danger {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
        .su-message.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }
        .su-message.warning {
            background: #fff3cd;
            color: #856404;
            border: 1px solid #ffeeba;
        }
        .su-no-access {
            text-align: center;
            padding: 60px 20px;
        }
        .su-no-access i {
            font-size: 48px;
            color: #dc3545;
            margin-bottom: 15px;
        }
        .su-no-access h3 {
            color: #dc3545;
            margin-bottom: 10px;
        }
        .su-no-access p {
            color: #666;
        }
        .emp-card {
            border: 1px solid #e9ecef;
            border-radius: 4px;
            padding: 12px 16px;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            transition: border-color 0.2s, box-shadow 0.2s;
        }
        .emp-card:hover {
            border-color: #007bff;
            box-shadow: 0 1px 4px rgba(0,123,255,0.15);
        }
        .emp-card .emp-info {
            flex: 1;
        }
        .emp-card .emp-code {
            font-weight: 700;
            color: #007bff;
            font-size: 15px;
        }
        .emp-card .emp-name {
            color: #333;
            font-size: 14px;
            margin-left: 10px;
        }
        .emp-card .emp-meta {
            font-size: 12px;
            color: #888;
            margin-top: 3px;
        }
        .emp-card .btn-switch {
            background: #28a745;
            color: #fff;
            border: none;
            padding: 6px 16px;
            border-radius: 3px;
            font-weight: 600;
            cursor: pointer;
            white-space: nowrap;
            margin-left: 10px;
        }
        .emp-card .btn-switch:hover {
            background: #218838;
        }
        .emp-card .btn-switch:disabled {
            background: #6c757d;
            cursor: not-allowed;
        }
    </style>
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">

        <%-- Page-level banner kept for designer wiring; Master pnlSuBanner is the live banner. --%>
        <asp:Panel ID="pnlImpersonationBanner" runat="server" Visible="false">
            <div class="switch-user-banner">
                <div class="banner-info">
                    <i class="fa fa-user-secret"></i>
                    <span>
                        <strong>Impersonating:</strong>
                        <asp:Label ID="lblImpersonatingUser" runat="server" Text=""></asp:Label>
                        &nbsp;|&nbsp;
                        <span class="banner-detail">
                            <strong>Original User:</strong>
                            <asp:Label ID="lblOriginalUser" runat="server" Text=""></asp:Label>
                        </span>
                    </span>
                </div>
                <asp:Button ID="btnReturnToOriginal" runat="server" Text="Return to My Account" CssClass="btn-return" OnClick="btnReturnToOriginal_Click" CausesValidation="false" />
            </div>
        </asp:Panel>

        <%-- Access Denied Panel --%>
        <asp:Panel ID="pnlNoAccess" runat="server" Visible="false">
            <div class="su-no-access">
                <i class="fa fa-lock"></i>
                <h3>Access Denied</h3>
                <p>You are not authorized to use the Switch User feature.<br />Contact your IT administrator to be added to the authorized users list.</p>
                <asp:HyperLink ID="lnkGoHome" runat="server" NavigateUrl="~/home.aspx" CssClass="btn btn-primary mt-3">
                    <i class="fa fa-home"></i> Go to Home
                </asp:HyperLink>
            </div>
        </asp:Panel>

        <%-- Main Search Panel --%>
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <div class="su-search-panel">
                <h4><i class="fa fa-search"></i> Search Employee</h4>
                <p class="text-muted" style="margin-bottom: 12px;">Search by Employee Code or Employee Name to find the user you want to switch to.</p>
                <div class="row">
                    <div class="col-md-8 col-sm-8">
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" placeholder="Enter Employee Code or Name (e.g. AHO445 or John Doe)" />
                    </div>
                    <div class="col-md-4 col-sm-4">
                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
                        <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-default" OnClick="btnClear_Click" CausesValidation="false" />
                    </div>
                </div>
            </div>

            <%-- Message Panel --%>
            <asp:Panel ID="pnlMessage" runat="server" Visible="false">
                <div id="divMessage" runat="server" class="su-message">
                    <asp:Label ID="lblMessage" runat="server" Text=""></asp:Label>
                </div>
            </asp:Panel>

            <%-- Results Panel --%>
            <asp:Panel ID="pnlResults" runat="server" Visible="false">
                <div class="su-results-panel">
                    <h4><i class="fa fa-users"></i> Search Results
                        <asp:Label ID="lblResultCount" runat="server" CssClass="badge badge-primary ml-2" Text=""></asp:Label>
                    </h4>
                    <asp:Repeater ID="rptEmployees" runat="server" OnItemCommand="rptEmployees_ItemCommand">
                        <ItemTemplate>
                            <div class="emp-card">
                                <div class="emp-info">
                                    <span class="emp-code"><%# Eval("EmployeeCode") %></span>
                                    <span class="emp-name"><%# Eval("EmployeeName") %></span>
                                    <div class="emp-meta">
                                        Module: <%# Eval("Module") %>
                                        &nbsp;|&nbsp; Status: <span style="color: #28a745; font-weight: 600;">Active</span>
                                    </div>
                                </div>
                                <asp:Button ID="btnSwitchTo" runat="server" Text="Switch User" CssClass="btn-switch"
                                    CommandName="SwitchTo" CommandArgument='<%# Eval("EmployeeCode") %>' />
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                    <%-- No Results Message --%>
                    <asp:Panel ID="pnlNoResults" runat="server" Visible="false">
                        <div class="text-center text-muted" style="padding: 30px;">
                            <i class="fa fa-info-circle" style="font-size: 24px;"></i>
                            <p style="margin-top: 10px;">No active employees found matching your search. Try a different code or name.</p>
                        </div>
                    </asp:Panel>
                </div>
            </asp:Panel>
        </asp:Panel>
    </div>
</asp:Content>
