using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Configuration;

namespace AnmolDristi
{
    public partial class login : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        DataTable dt = new DataTable();
        static string User_Photo = string.Empty;
        // Default folders
        static readonly string rootFolder = @"C:\";
        static readonly string localFolder = @"D:\";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Img_CpLogo.ImageUrl = "WebData/anmol_logo_GPTW.jpg";
                lbl_companyname.Text = "Anmol Industries Limited";
                lbl_currentyr.Text = DateTime.Now.Year.ToString();
                lbl_compfooter.Text = "Anmol Industries Limited";
                lbl_owner.Text = "IT Department";
                txt_loginid.Focus();
            }
        }

        protected void btn_signin_Click(object sender, EventArgs e)
        {
            //Response.Redirect("home.aspx");

            try
            {
                CredentialChecker1();
            }
            catch (ThreadAbortException)
            {
                // Ignore the ThreadAbortException as it's expected after Response.Redirect
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception ex)
        {
            string title = "Notifications :";
            string body = ex.Message;
            ClientScript.RegisterStartupScript(this.GetType(), "Popup", "ShowPopup('" + title + "', '" + body + "');", true);
        }


        private void CredentialChecker1()
        {
            if (txt_loginid.Text != "" && txt_password.Text != "")
            {
                //string id = "ATS00200";
                //string pass = "UDB17v";

                string id = txt_loginid.Text;
                string pass = txt_password.Text;

                string query = "SELECT * from MST_UserMaster where EmployeeCode=@EmployeeCode and Password=@Password";
                SqlParameter[] pram = {
                                          new SqlParameter("@EmployeeCode",id),
                                          new SqlParameter("@Password",pass),
                                      };
                dt = dbcl.SPreturn_dt(query, pram);
                if (dt.Rows.Count > 0)
                {
                    string WorkStatus = dt.Rows[0]["EmployeeStatus"].ToString();

                    if (WorkStatus == "Active")
                    {
                        string EmployeeCode = dt.Rows[0]["Id"].ToString();
                        int userId = Convert.ToInt32(dt.Rows[0]["Id"]);
                        string RegionId = dt.Rows[0]["RegionId"].ToString();
                        string BranchId = dt.Rows[0]["BranchId"].ToString();
                        string PlantId = dt.Rows[0]["PlantId"].ToString();
                        string Workman = dt.Rows[0]["EmployeeCode"].ToString();

                        string Module = dt.Rows[0]["Module"].ToString();
                        //string User_FirstName = dt.Rows[0]["EmployeeName"].ToString();

                        // Assuming the input string is in dt.Rows[0]["EmployeeName"]
                        string fullName = dt.Rows[0]["EmployeeName"].ToString();

                        // Split the full name by spaces and take the first part
                        string User_FirstName = fullName.Split(' ')[0];

                        string User_FullName = dt.Rows[0]["EmployeeName"].ToString();

                        //string User_Type = dt.Rows[0]["User_RoleType"].ToString();
                        //string User_Permission = dt.Rows[0]["Role_Permission"].ToString();

                        string User_Worksite = dt.Rows[0]["DepartmentId"].ToString();
                        string User_WRKSTCode = dt.Rows[0]["SubDepartmentId"].ToString();
                        string User_Grade = dt.Rows[0]["GradeId"].ToString();
                        string User_Desg = dt.Rows[0]["Designation_ID"].ToString();

                        //User_Photo = dt.Rows[0]["PrfPicFile"].ToString();
                        //string User_PhotoPath = dt.Rows[0]["PrfPicPath"].ToString();

                        Session["USERID"] = EmployeeCode;
                        Session["Password"] = pass;
                        Session["WORKMAN"] = Workman;
                        Session["USERFNAME"] = User_FirstName;
                        Session["USERNAME"] = User_FullName;
                        Session["CurrentUserId"] = userId;          // NEW: numeric Id (49 for ADMIN)
                        //Session["USERTYPE"] = User_Type;
                        //Session["PERMISSION"] = User_Permission;
                        Session["REGION"] = RegionId;
                        Session["BRANCH"] = BranchId;
                        Session["MODULE"] = Module;
                        Session["PLANTID"] = PlantId;
                        Session["U_SITE"] = User_Worksite;
                        Session["U_SITECODE"] = User_WRKSTCode;

                        Session["DESG"] = User_Desg;
                        Session["GRADE"] = User_Grade;
                        //if (User_Photo == null || User_Photo == "")
                        //{
                        //    Session["User_Photo"] = "No_Image.jpg";
                        //}
                        //else
                        //{
                        //    //Check for physical file
                        //    bool File = FlieExistence();
                        //    if (File == true)
                        //    {
                        //        Session["User_Photo"] = User_Photo;
                        //    }
                        //    else
                        //    {
                        //        Session["User_Photo"] = "No_Image.jpg";
                        //    }
                        //}
                        //dbcl.WriteToFile("User " + User_FullName + "[" + Workman + "]" + " Logined Successfully");

                        // Get the user's IP address
                        //string ipAddress = HttpContext.Current.Request.UserHostAddress;

                        // Notify admin or security team
                        //LoginNotifier.NotifyLogin(User_FullName, ipAddress);

                        UpdateUserLastLogin(userId);                       // update last login
                        //int activeSessionId = CreateActiveSession(userId, Workman);
                        //Session["ActiveSessionId"] = activeSessionId;

                        // After validating user credentials...
                        var result = SessionHelper.CreateActiveSession(userId, Workman);
                        if (result != null)
                        {
                            Session["ActiveSessionId"] = result.Item1;          // DB identity
                            Session["LoginToken"] = result.Item2.ToString();    // GUID token
                        }

                        Response.Redirect("home.aspx", false);
                    }
                    else
                    {
                        ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('User ID is InActive');</script>");
                        txt_loginid.Text = "";
                    }

                }
                else
                {
                    ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('Unknown user / password.');</script>");
                    txt_loginid.Text = "";
                }
            }
            else
            {
                ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('Unknown user / password.');</script>");
                txt_loginid.Text = "";
            }
        }

        private bool FlieExistence()
        {
            if (File.Exists(Path.Combine(rootFolder, User_Photo)))
            {
                return true;
            }
            else if (File.Exists(Path.Combine(localFolder, User_Photo)))
            {
                return true;
            }
            else
            {
                string title = "Notifications :";
                string body = "NO Physical File Found...!!";
                ClientScript.RegisterStartupScript(this.GetType(), "Popup", "ShowPopup('" + title + "', '" + body + "');", true);
                return false;
            }
        }

        // helper to get client IP (basic)
        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip)) ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip ?? "Unknown";
        }

        // Call this after successful credential check (right before redirect)
        private int CreateActiveSession(int userId, string Workman)
        {
            string sessionId = Session.SessionID;
            string ip = GetClientIP();
            string browser = Request.Browser.Browser + " " + Request.Browser.Version;
            string host = Request.UserHostName;

            string sql = @"
            INSERT INTO ActiveUserSession (UserId, EmployeeCode, SessionId, LoginTime, ClientIP, HostName, BrowserInfo, IsActive)
            VALUES (@UserId, @EmployeeCode, @SessionId, GETDATE(), @ClientIP, @HostName, @BrowserInfo, 1);
            SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)Workman ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                cmd.Parameters.AddWithValue("@ClientIP", ip);
                cmd.Parameters.AddWithValue("@HostName", host ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", browser ?? (object)DBNull.Value);

                con.Open();
                object o = cmd.ExecuteScalar();
                int activeSessionId = Convert.ToInt32(o);
                return activeSessionId;
            }
        }

        private void UpdateUserLastLogin(int userId)
        {
            string sql = "UPDATE MST_UserMaster SET LastLoginDate = GETDATE() WHERE Id = @Id";
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

    }
}