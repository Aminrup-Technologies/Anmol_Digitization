using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace AnmolDristi
{
    public partial class datamastering_home : BasePage
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Enforce page-level permissions to prevent direct URL access
            RequirePermission("UI.AdminControl.View");
            RequirePermission("UI.DataMST.View");

            if (!IsPostBack)
            {
                // 2. Validate Session
                if (Session["CurrentUserId"] != null)
                {
                    int loggedInUserId = Convert.ToInt32(Session["CurrentUserId"]);
                    ApplyPageSecurity(loggedInUserId);
                }
                else
                {
                    Response.Redirect("login.aspx", false);
                }
            }
        }

        private void ApplyPageSecurity(int userId)
        {
            // 1. Fetch permitted Control/Panel IDs for the current user
            List<string> permittedControls = GetUserPermittedControls(userId);

            // 2. Manage Panel Visibility (Divs)
            // If the panel ID is NOT in the list, hide the entire section
            TogglePanel(Org_MST, "Org_MST", permittedControls);
            TogglePanel(HRMS_MST, "HRMS_MST", permittedControls);
            TogglePanel(User_Mgmnt, "User_Mgmnt", permittedControls);
            TogglePanel(Production_MST, "Production_MST", permittedControls);
            TogglePanel(QC_Forms_CTRL, "QC_Forms_CTRL", permittedControls);
            TogglePanel(QC_Approval_Matrix, "QC_Approval_Matrix", permittedControls);
            TogglePanel(Authorization_Panel, "Authorization_Panel", permittedControls);
            TogglePanel(SelfSessions_Panel, "SelfSessions_Panel", permittedControls);

            // 3. Manage Button Interaction (Icons)
            // Locks (grays out) specific buttons the user shouldn't click
            LockControl(btn_AddCompany, "btn_AddCompany", permittedControls);
            LockControl(btn_AddRegion, "btn_AddRegion", permittedControls);
            LockControl(btn_AddBranch, "btn_AddBranch", permittedControls);
            LockControl(btn_AddPlants, "btn_AddPlants", permittedControls);

            LockControl(btn_HrmsDivision, "btn_HrmsDivision", permittedControls);
            LockControl(btn_HrmsCategory, "btn_HrmsCategory", permittedControls);
            LockControl(btn_HrmsGrade, "btn_HrmsGrade", permittedControls);
            LockControl(btn_HrmsDept, "btn_HrmsDept", permittedControls);
            LockControl(btn_HrmsSubDept, "btn_HrmsSubDept", permittedControls);

            LockControl(btn_NewUser, "btn_NewUser", permittedControls);
            LockControl(btn_ManageUser, "btn_ManageUser", permittedControls);
            LockControl(btn_UserRoles, "btn_UserRoles", permittedControls);
            LockControl(btn_UserAccess, "btn_UserAccess", permittedControls);

            LockControl(btn_ProdPlants, "btn_ProdPlants", permittedControls);
            LockControl(btn_ProdLines, "btn_ProdLines", permittedControls);
            LockControl(btn_ProdCategory, "btn_ProdCategory", permittedControls);
            LockControl(btn_ProdBrands, "btn_ProdBrands", permittedControls);
            LockControl(btn_ProdSKU, "btn_ProdSKU", permittedControls);

            LockControl(btn_FormGlobal, "btn_FormGlobal", permittedControls);
            LockControl(btn_FormQci, "btn_FormQci", permittedControls);
            LockControl(btn_FormRm0, "btn_FormRm0", permittedControls);
            LockControl(btn_FormRm1, "btn_FormRm1", permittedControls);
            LockControl(btn_FormRm2, "btn_FormRm2", permittedControls);
            LockControl(btn_FormRm3, "btn_FormRm3", permittedControls);
            LockControl(btn_FormRm4, "btn_FormRm4", permittedControls);
            LockControl(btn_FormRm5, "btn_FormRm5", permittedControls);

            LockControl(btn_FormsMaster, "btn_FormsMaster", permittedControls);
            LockControl(btn_FormsApprover, "btn_FormsApprover", permittedControls);

            LockControl(btn_SecRoles, "btn_SecRoles", permittedControls);
            LockControl(btn_SecPermissions, "btn_SecPermissions", permittedControls);
            LockControl(btn_SecRolePermMap, "btn_SecRolePermMap", permittedControls);
            LockControl(btn_SecUserRoleMap, "btn_SecUserRoleMap", permittedControls);
            LockControl(btn_SecUiMapper, "btn_SecUiMapper", permittedControls);
            LockControl(btn_SecAuditTrails, "btn_SecAuditTrails", permittedControls);

            LockControl(btn_SessPermissions, "btn_SessPermissions", permittedControls);
            LockControl(btn_SessPermJson, "btn_SessPermJson", permittedControls);
            LockControl(btn_SessRefresh, "btn_SessRefresh", permittedControls);
        }

        private void TogglePanel(HtmlGenericControl panel, string panelId, List<string> permittedControls)
        {
            if (panel != null)
            {
                // Panel is only visible if the user is explicitly mapped to it
                panel.Visible = permittedControls.Contains(panelId);
            }
        }

        private void LockControl(HtmlAnchor control, string controlId, List<string> permittedControls)
        {
            if (control == null) return;

            // If user is NOT mapped to this button's ID, gray it out and disable it
            if (!permittedControls.Contains(controlId))
            {
                control.Attributes.Remove("href");
                control.Attributes.Add("style", "pointer-events: none; opacity: 0.4; cursor: not-allowed;");
                control.Attributes.Add("disabled", "disabled");
            }
        }

        private List<string> GetUserPermittedControls(int userId)
        {
            // Caching for performance
            if (Session["UserPermittedControls"] != null)
            {
                return (List<string>)Session["UserPermittedControls"];
            }

            List<string> controlIds = new List<string>();

            // UPDATED SQL: Now JOINS the new UiControlMap table
            string query = @"
        SELECT DISTINCT m.ControlId
        FROM UserRole ur WITH(NOLOCK)
        INNER JOIN RolePermission rp WITH(NOLOCK) ON ur.RoleId = rp.RoleId
        INNER JOIN UiControlMap m WITH(NOLOCK) ON rp.PermissionId = m.PermissionId
        WHERE ur.UserId = @UserId";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    try
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                controlIds.Add(dr["ControlId"].ToString().Trim());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle/Log
                    }
                }
            }

            Session["UserPermittedControls"] = controlIds;
            return controlIds;
        }

        private List<string> GetUserPermittedControls_OLD(int userId)
        {
            if (Session["UserPermittedControls"] != null)
            {
                return (List<string>)Session["UserPermittedControls"];
            }

            List<string> controlIds = new List<string>();

            string query = @"
                SELECT DISTINCT p.ControlId
                FROM UserRole ur WITH(NOLOCK)
                INNER JOIN RolePermission rp WITH(NOLOCK) ON ur.RoleId = rp.RoleId
                INNER JOIN Permission p WITH(NOLOCK) ON rp.PermissionId = p.PermissionId
                WHERE ur.UserId = @UserId 
                  AND p.IsObsolete = 0 
                  AND p.ControlId IS NOT NULL";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    try
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                controlIds.Add(dr["ControlId"].ToString().Trim());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log database error here if necessary
                    }
                }
            }
            // Store in Session for future page loads
            Session["UserPermittedControls"] = controlIds;
            return controlIds;
        }
    }
}