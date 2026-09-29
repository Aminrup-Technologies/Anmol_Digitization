<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="qualitydatamastering_home.aspx.cs" Inherits="AnmolDristi.qualitydatamastering_home" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="right_col" role="main">
        <div class="">

            <div class="row">

                <div class="col-md-6" id="Org_MST" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Organization Masters</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="add_company.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label3" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Company
                            </a>
                            <a class="btn btn-app" href="add_region.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label4" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Region
                            </a>
                            <a class="btn btn-app" href="add_branch.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label5" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Branches
                            </a>
                            <a class="btn btn-app" href="add_plants.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label7" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Plants
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="HRMS_MST" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>HRMS Masters (ZingHR)</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:Label ID="Label1" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Division

                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:Label ID="Label2" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Category

                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Grade

                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:Label ID="Label8" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Department

                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-warning">WIP
                                    <asp:Label ID="Label9" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Sub-Department

                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="User_Mgmnt" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>User Management</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="New_User.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label17" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Add User
                            </a>
                            <a class="btn btn-app" href="viewusers.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label18" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Manage User
                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label19" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles
                            </a>
                            <a class="btn btn-app" href="#">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label20" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Access
                            </a>
                        </div>
                    </div>
                </div>

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
                            <a class="btn btn-app" href="add_brandtypes.aspx" id="btn_managevariety" runat="server">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="lbl_VarietyCount" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Brands

                            </a>
                            <a class="btn btn-app" href="qaqc_mst_sku.aspx" id="btn_managesku" runat="server">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="lbl_SKUCount" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>SKU

                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="QC_Forms_CTRL" runat="server" visible="true">
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
                            <a class="btn btn-app" href="global_formctrl.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label28" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Global
                            </a>
                            <a class="btn btn-app" href="qaqc_qcinspector_frmctrl.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label15" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>QCI
                            </a>
                            <a class="btn btn-app" href="aata_maida_frmctrl.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label22" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-0
                            </a>
                            <a class="btn btn-app" href="rm_class_1_frmctrl.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label23" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-1
                            </a>
                            <a class="btn btn-app" href="RM_Class2_FrmCtrl.aspx">
                                <span class="badge bg-warning">New
                                    <asp:Label ID="Label24" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-2
                            </a>
                            <a class="btn btn-app" href="rm_class_3_frmctrl.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label25" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-3
                            </a>
                            <a class="btn btn-app" href="rm_class_4_frmctrl.aspx">
                                <span class="badge bg-green">Ok
                                    <asp:Label ID="Label26" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-4
                            </a>
                            <a class="btn btn-app" href="rm_class_5_frmctrl.aspx">
                                <span class="badge bg-warning">New
                                    <asp:Label ID="Label27" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-5
                            </a>
                        </div>
                    </div>
                </div>


                <div class="col-md-6" id="QC_Approval_Matrix" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Forms & Approval Matrix</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="Form_Master.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label21" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Forms Master
                            </a>
                            <a class="btn btn-app" href="Form_Approval_Matrix.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label16" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Forms Approver
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="RolesNPermissions" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Roles & Permissions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="RoleList.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label29" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles
                            </a>
                            <a class="btn btn-app" href="PermissionList.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label30" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permisisons
                            </a>
                            <a class="btn btn-app" href="RolePermissionManager.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label34" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles - Permissions
                            </a>
                            <a class="btn btn-app" href="UserRoleManager.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label35" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>User Role Map
                            </a>
                            <a class="btn btn-app" href="UiControlMapper.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label37" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>UI Tag Mapper
                            </a>

                            <a class="btn btn-app" href="ViewUiControlMappings.aspx" id="btn_ViewUiCtrlMappings" runat="server">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label38" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>View UI Mapper
                            </a>

                            <a class="btn btn-app" href="RolePermissionAudit.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label36" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Audit Trails
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="SelfSessions" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>User Sessions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a>
                                </li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="DebugPermissions.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label31" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permissions
                            </a>
                            <a class="btn btn-app" href="PermDump.ashx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label32" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permissions JSON
                            </a>
                            <a class="btn btn-app" href="ReloadSessionPerms.aspx">
                                <span class="badge bg-green">OK
                                    <asp:Label ID="Label33" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Refresh Sessions
                            </a>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
