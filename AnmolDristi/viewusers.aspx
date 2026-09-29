<%@ Page Title="User Management" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="viewusers.aspx.cs" Inherits="AnmolDristi.viewusers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ASP.NET Pager to match Gentelella */
        .pagination-ys table {
            margin: 0;
        }

        .pagination-ys table td {
            padding: 0;
            border: none;
        }

        .pagination-ys table td a, .pagination-ys table td span {
            position: relative;
            float: left;
            padding: 6px 12px;
            margin-left: -1px;
            line-height: 1.42857143;
            text-decoration: none;
            border: 1px solid #ddd;
            background-color: #fff;
            color: #73879C;
        }

        .pagination-ys table td span {
            background-color: #2A3F54;
            color: white;
            border-color: #2A3F54;
            cursor: default;
        }

        .pagination-ys table td a:hover {
            background-color: #eeeeee;
        }

        .action-btn {
            margin: 2px;
        }
    </style>

    <script type="text/javascript">
        // Ported Modal Function
        function ShowPopup(title, body) {
            $("#popTitle").text(title);
            $("#popBody").html(body);
            $("#MyPopup").modal("show");
        }
    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">
            <div class="page-title">
                <div class="title_left">
                    <h3><i class="fa fa-users"></i> User Management <small>Admin Control Panel</small></h3>
                </div>
            </div>

            <div class="clearfix"></div>

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Employee Directory</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>

                        <div class="x_content">
                            <div class="row mb-3">
                                <div class="col-md-3 col-sm-3 form-group has-feedback">
                                    <label>Search Name or Code:</label>
                                    <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control has-feedback-left" placeholder="e.g. Arjun or S126"></asp:TextBox>
                                    <span class="fa fa-user form-control-feedback left" aria-hidden="true" style="margin-top: 24px;"></span>
                                </div>

                                <div class="col-md-2 col-sm-2 form-group">
                                    <label>HR Status:</label>
                                    <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="All Statuses" Value="All"></asp:ListItem>
                                        <asp:ListItem Text="Active" Value="Active"></asp:ListItem>
                                        <asp:ListItem Text="Blocked" Value="Blocked"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <div class="col-md-3 col-sm-3 form-group">
                                    <label>System Activity:</label>
                                    <asp:DropDownList ID="ddlActivityFilter" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="All Activity" Value="All"></asp:ListItem>
                                        <asp:ListItem Text="Online Now" Value="OnlineNow"></asp:ListItem>
                                        <asp:ListItem Text="Active (Last 30 Days)" Value="Active30"></asp:ListItem>
                                        <asp:ListItem Text="Inactive (> 30 Days)" Value="Inactive30"></asp:ListItem>
                                        <asp:ListItem Text="Never Logged In" Value="Never"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <div class="col-md-4 col-sm-4 form-group" style="padding-top: 24px;">
                                    <asp:LinkButton ID="btnSearch" runat="server" CssClass="btn btn-primary" OnClick="btnSearch_Click">
                                        <i class="fa fa-search"></i> Search
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnClear" runat="server" CssClass="btn btn-default" OnClick="btnClear_Click">
                                        <i class="fa fa-refresh"></i> Clear
                                    </asp:LinkButton>

                                    <a href="New_User.aspx" class="btn btn-success pull-right"><i class="fa fa-plus"></i> Add User</a>
                                </div>
                            </div>

                            <div class="table-responsive">
                                <asp:GridView ID="gvEmployees" runat="server"
                                    CssClass="table table-striped table-bordered jambo_table bulk_action"
                                    GridLines="None" AutoGenerateColumns="False"
                                    AllowPaging="True" PageSize="20"
                                    OnPageIndexChanging="gvEmployees_PageIndexChanging"
                                    OnRowCommand="gvEmployees_RowCommand">

                                    <HeaderStyle CssClass="headings" />
                                    <PagerStyle CssClass="pagination-ys" HorizontalAlign="Right" />

                                    <Columns>
                                        <asp:BoundField DataField="EmployeeCode" HeaderText="Emp Code" />
                                        <asp:BoundField DataField="EmployeeName" HeaderText="Employee Name" ControlStyle-Font-Bold="true"/>

                                        <asp:BoundField DataField="GradeName" HeaderText="Grade" />
                                        <asp:BoundField DataField="DesignationName" HeaderText="Designation" />
                                        <asp:BoundField DataField="DepartmentName" HeaderText="Department" />
                                        <asp:BoundField DataField="RegionName" HeaderText="Region" />

                                        <asp:TemplateField HeaderText="Reporting Manager">
                                            <ItemTemplate>
                                                <strong><%# Eval("ReportingManager") != DBNull.Value && !string.IsNullOrEmpty(Eval("ReportingManager").ToString()) ? Eval("ReportingManager") : "N/A" %></strong><br />
                                                <small class="text-muted"><%# Eval("ReportingManagerCode") %></small>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <%-- ENHANCED STATUS COLUMN --%>
                                        <asp:TemplateField HeaderText="Status & Activity">
                                            <ItemTemplate>
                                                <span class='label <%# Eval("EmployeeStatus").ToString().Equals("Active", StringComparison.OrdinalIgnoreCase) ? "label-success" : "label-danger" %>' style="padding: 4px 8px; font-size: 11px;">
                                                    <%# Eval("EmployeeStatus") %>
                                                </span>
                                                
                                                <div style='margin-top: 6px; font-size: 11px;'>
                                                    <span style='color: <%# (Eval("IsOnline") != DBNull.Value && Convert.ToBoolean(Eval("IsOnline"))) ? "#1ABB9C" : "#73879C" %>; font-weight: bold;'>
                                                        <i class='fa <%# (Eval("IsOnline") != DBNull.Value && Convert.ToBoolean(Eval("IsOnline"))) ? "fa-circle" : "fa-circle-o" %>'></i> 
                                                        <%# (Eval("IsOnline") != DBNull.Value && Convert.ToBoolean(Eval("IsOnline"))) ? "Online" : "Offline" %>
                                                    </span>
                                                </div>

                                                <div style="color: #73879C; margin-top: 2px; font-size: 11px;">
                                                    <i class="fa fa-clock-o"></i> <%# Eval("LastLogin") != DBNull.Value ? Convert.ToDateTime(Eval("LastLogin")).ToString("dd-MMM-yy HH:mm") : "Never Logged In" %>
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Action">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn btn-info btn-xs action-btn"
                                                    CommandName="EditEmployee" CommandArgument='<%# Eval("EmployeeCode") %>' ToolTip="Edit User">
                                                    <i class="fa fa-pencil"></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnToggleStatus" runat="server"
                                                    CssClass='<%# Eval("EmployeeStatus").ToString() == "Active" ? "btn btn-danger btn-xs action-btn" : "btn btn-success btn-xs action-btn" %>'
                                                    CommandName="ToggleStatus" CommandArgument='<%# Eval("EmployeeCode") %>'
                                                    ToolTip='<%# Eval("EmployeeStatus").ToString() == "Active" ? "Block User" : "Activate User" %>'
                                                    OnClientClick="return confirm('Are you sure you want to change this user\'s status?');">
                                                    <i class='<%# Eval("EmployeeStatus").ToString() == "Active" ? "fa fa-ban" : "fa fa-check" %>'></i>
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="btnResetPass" runat="server" CssClass="btn btn-warning btn-xs action-btn"
                                                    CommandName="ResetPassword" CommandArgument='<%# Eval("EmployeeCode") %>'
                                                    OnClientClick="return confirm('Are you sure you want to trigger a password reset for this user?');"
                                                    ToolTip="Reset Password">
                                                    <i class="fa fa-key"></i>
                                                </asp:LinkButton>
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

    <div id="MyPopup" class="modal fade" role="dialog">
        <div class="modal-dialog modal-sm">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title" id="popTitle"></h4>
                </div>
                <div class="modal-body" id="popBody"></div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
</asp:Content>