<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="datamastering_home.aspx.cs" Inherits="AnmolDristi.datamastering_home" %>

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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="add_company.aspx" id="btn_AddCompany" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="lbl_add_company" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Company
                            </a>
                            <a class="btn btn-app" href="add_region.aspx" id="btn_AddRegion" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="lbl_add_region" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Region
                            </a>
                            <a class="btn btn-app" href="add_branch.aspx" id="btn_AddBranch" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="lbl_add_branch" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Branches
                            </a>
                            <a class="btn btn-app" href="add_plants.aspx" id="btn_AddPlants" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="lbl_add_plants" runat="server" Text="0" Visible="false"></asp:Label></span>
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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="#" id="btn_HrmsDivision" runat="server">
                                <span class="badge bg-warning">WIP<asp:Label ID="Label1" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Division
                            </a>
                            <a class="btn btn-app" href="#" id="btn_HrmsCategory" runat="server">
                                <span class="badge bg-warning">WIP<asp:Label ID="Label2" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Category
                            </a>
                            <a class="btn btn-app" href="#" id="btn_HrmsGrade" runat="server">
                                <span class="badge bg-warning">WIP<asp:Label ID="Label6" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Grade
                            </a>
                            <a class="btn btn-app" href="#" id="btn_HrmsDept" runat="server">
                                <span class="badge bg-warning">WIP<asp:Label ID="Label8" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Department
                            </a>
                            <a class="btn btn-app" href="#" id="btn_HrmsSubDept" runat="server">
                                <span class="badge bg-warning">WIP<asp:Label ID="Label9" runat="server" Text="0" Visible="false"></asp:Label></span>
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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="New_User.aspx" id="btn_NewUser" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label17" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Add User
                            </a>
                            <a class="btn btn-app" href="viewusers.aspx" id="btn_ManageUser" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label18" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Manage User
                            </a>
                            <a class="btn btn-app" href="#" id="btn_UserRoles" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label19" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles
                            </a>
                            <a class="btn btn-app" href="#" id="btn_UserAccess" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label20" runat="server" Text="0" Visible="false"></asp:Label></span>
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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="add_prod_plants.aspx" id="btn_ProdPlants" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label10" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Plants
                            </a>
                            <a class="btn btn-app" href="add_plant_lines.aspx" id="btn_ProdLines" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label11" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Lines
                            </a>
                            <a class="btn btn-app" href="add_prodcategory.aspx" id="btn_ProdCategory" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label12" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Category
                            </a>
                            <a class="btn btn-app" href="add_brandtypes.aspx" id="btn_ProdBrands" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label13" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Brands
                            </a>
                            <a class="btn btn-app" href="qaqc_mst_sku.aspx" id="btn_ProdSKU" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label14" runat="server" Text="0" Visible="false"></asp:Label></span>
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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="global_formctrl.aspx" id="btn_FormGlobal" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label28" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Global
                            </a>
                            <a class="btn btn-app" href="qaqc_qcinspector_frmctrl.aspx" id="btn_FormQci" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label15" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>QCI
                            </a>
                            <a class="btn btn-app" href="aata_maida_frmctrl.aspx" id="btn_FormRm0" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label22" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-0
                            </a>
                            <a class="btn btn-app" href="rm_class_1_frmctrl.aspx" id="btn_FormRm1" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label23" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-1
                            </a>
                            <a class="btn btn-app" href="RM_Class2_FrmCtrl.aspx" id="btn_FormRm2" runat="server">
                                <span class="badge bg-warning">New<asp:Label ID="Label24" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-2
                            </a>
                            <a class="btn btn-app" href="rm_class_3_frmctrl.aspx" id="btn_FormRm3" runat="server">
                                <span class="badge bg-green">Ok<asp:Label ID="Label25" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-3
                            </a>
                            <a class="btn btn-app" href="rm_class_4_frmctrl.aspx" id="btn_FormRm4" runat="server">
                                <span class="badge bg-green">Ok<asp:Label ID="Label26" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>RM-4
                            </a>
                            <a class="btn btn-app" href="rm_class_5_frmctrl.aspx" id="btn_FormRm5" runat="server">
                                <span class="badge bg-warning">New<asp:Label ID="Label27" runat="server" Text="0" Visible="false"></asp:Label></span>
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
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="Form_Master.aspx" id="btn_FormsMaster" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label21" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Forms Master
                            </a>
                            <a class="btn btn-app" href="Form_Approval_Matrix.aspx" id="btn_FormsApprover" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label16" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Forms Approver
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="Authorization_Panel" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>Roles & Permissions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="RoleList.aspx" id="btn_SecRoles" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label29" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles
                            </a>
                            <a class="btn btn-app" href="PermissionList.aspx" id="btn_SecPermissions" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label30" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permisisons
                            </a>
                            <a class="btn btn-app" href="RolePermissionManager.aspx" id="btn_SecRolePermMap" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label34" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Roles - Permissions
                            </a>
                            <a class="btn btn-app" href="UserRoleManager.aspx" id="btn_SecUserRoleMap" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label35" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>User Role Map
                            </a>
                            <a class="btn btn-app" href="UiControlMapper.aspx" id="btn_SecUiMapper" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label37" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>UI Tag Mapper
                            </a>
                            <a class="btn btn-app" href="ViewUiControlMappings.aspx" id="btn_ViewUiCtrlMappings" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label3" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>View UI Mapper
                            </a>
                            <a class="btn btn-app" href="RolePermissionAudit.aspx" id="btn_SecAuditTrails" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label36" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Audit Trails
                            </a>
                        </div>
                    </div>
                </div>

                <div class="col-md-6" id="SelfSessions_Panel" runat="server" visible="true">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>User Sessions</h2>
                            <ul class="nav navbar-right panel_toolbox">
                                <li><a class="collapse-link"><i class="fa fa-chevron-up"></i></a></li>
                            </ul>
                            <div class="clearfix"></div>
                        </div>
                        <div class="x_content">
                            <a class="btn btn-app" href="DebugPermissions.aspx" id="btn_SessPermissions" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label31" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permissions
                            </a>
                            <a class="btn btn-app" href="PermDump.ashx" id="btn_SessPermJson" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label32" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Permissions JSON
                            </a>
                            <a class="btn btn-app" href="ReloadSessionPerms.aspx" id="btn_SessRefresh" runat="server">
                                <span class="badge bg-green">OK<asp:Label ID="Label33" runat="server" Text="0" Visible="false"></asp:Label></span>
                                <i class="fa fa-edit"></i>Refresh Sessions
                            </a>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>