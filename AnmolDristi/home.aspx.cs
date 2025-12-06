using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class home : BasePage
    {
        // Cache duration (seconds)
        private const int MasterLookupCacheSeconds = 600; // 10 minutes

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // require USERID and USERNAME as minimum for a logged-in user
                if (Session["USERID"] != null && Session["WORKMAN"] != null)
                {
                    lbl_username.Text = CurrentEmployeeName ?? string.Empty;
                    lbl_empcode.Text = lbl_workmansl.Text = CurrentEmployeeCode ?? string.Empty;
                    lbl_wrkcopmany.Text = "Anmol Industries Limited";

                    var regionName = GetDescriptionFromSession("REGION", "MST_Region", "Region_Description") ?? "All Regions";
                    var branchName = GetDescriptionFromSession("BRANCH", "MST_Branch", "Branch_Description") ?? "All Branches";
                    var gradeName = GetDescriptionFromSession("GRADE", "MST_Grade", "Grade_Description") ?? "All Grades";
                    var desigName = GetDescriptionFromSession("DESG", "MST_Designation", "Designation_Description") ?? "All Designations";


                    lbl_desg.Text = desigName;
                    lbl_skillcat.Text = gradeName;
                    lbl_region.Text = regionName;
                    lbl_branch.Text = branchName;

                    string script = "new PNotify({title: 'Regular Success', text: 'Welcome Back!!', type: 'success', styling: 'bootstrap3'});";

                    // If in an UpdatePanel/ScriptManager environment, use ScriptManager; otherwise ClientScript
                    if (ScriptManager.GetCurrent(Page) != null)
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowWelcomeNotification", script, true);
                    }
                    else
                    {
                        ClientScript.RegisterStartupScript(this.GetType(), "ShowWelcomeNotification", script, true);
                    }
                }
                else
                {
                    // no session - redirect to login or handle as per your app flow
                    Response.Redirect("~/Login.aspx");
                }
            }
        }

        private int? GetLoggedInRegionId()
        {
            return SessionValueToNullableInt("REGION");
        }
        private int? GetLoggedInBranchId()
        {
            return SessionValueToNullableInt("BRANCH");
        }
        private int? GetLoggedInGradeId()
        {
            return SessionValueToNullableInt("GRADE");
        }
        private int? GetLoggedInDesignationId()
        {
            return SessionValueToNullableInt("DESG");
        }

        private int? SessionValueToNullableInt(string key)
        {
            if (Session[key] == null) return null;
            int val;
            if (int.TryParse(Session[key].ToString(), out val))
                return val;
            return null;
        }

        private string GetDescriptionFromSession(string sessionKey, string tableName, string textColumn)
        {
            // get id from session
            if (Session[sessionKey] == null) return null;
            int id;
            if (!int.TryParse(Session[sessionKey].ToString(), out id)) return null;

            // cache key
            string cacheKey = $"MasterDesc::{tableName}::{textColumn}::{id}";
            var cached = HttpRuntime.Cache[cacheKey] as string;
            if (!string.IsNullOrEmpty(cached)) return cached;

            // whitelist -> fixed SQL only for allowed combos
            string sql = null;
            if (tableName == "MST_Region" && textColumn == "Region_Description")
            {
                sql = "SELECT TOP 1 Region_Description FROM MST_Region WHERE Region_ID = @Id";
            }
            else if (tableName == "MST_Branch" && textColumn == "Branch_Description")
            {
                sql = "SELECT TOP 1 Branch_Description FROM MST_Branch WHERE Branch_ID = @Id";
            }
            else if (tableName == "MST_Grade" && textColumn == "Grade_Description")
            {
                sql = "SELECT TOP 1 Grade_Description FROM MST_Grade WHERE Grade_ID = @Id";
            }
            else if (tableName == "MST_Designation" && textColumn == "Designation_Description")
            {
                sql = "SELECT TOP 1 Designation_Description FROM MST_Designation WHERE Designation_ID = @Id";
            }
            else
            {
                // unsupported - do not build arbitrary SQL
                Trace.Warn("GetDescriptionFromSession", $"Unsupported lookup request: {tableName}.{textColumn}");
                return null;
            }

            // execute query
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (var con = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        var name = Convert.ToString(result);
                        HttpRuntime.Cache.Insert(cacheKey, name, null, DateTime.UtcNow.AddSeconds(MasterLookupCacheSeconds), System.Web.Caching.Cache.NoSlidingExpiration);
                        return name;
                    }
                }
            }
            catch (Exception ex)
            {
                // Replace with your logging mechanism
                Trace.Warn("GetDescriptionFromSession - DB error", ex.ToString());
            }

            return null;
        }

    }
}
