using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace AnmolDristi
{
    public partial class pwdchange : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static string UserPass = "";
        DataTable dt = new DataTable();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
            {
                Response.Redirect("login.aspx");
            }
            else if (!IsPostBack)
            {
                string CmdString1 = "select Security_Questions, QNo from tlb_security_questions where Category = '1' order by Id ";
                BindSecurityQ1(CmdString1);

                string CmdString2 = "select Security_Questions, QNo from tlb_security_questions where Category = '2' order by Id ";
                BindSecurityQ2(CmdString2);

                LoadLoginDetails();

                txt_oldpass.Focus();
            }
        }

        protected void btn_validateoldpassword_Click(object sender, EventArgs e)
        {
            ValidateOldPassword();
        }

        private void ValidateOldPassword()
        {
            string inputoldpass = txt_oldpass.Text.TrimEnd().ToString();
            if (UserPass == inputoldpass)
            {
                //ClientScript.RegisterStartupScript(this.GetType(), "alert8", "ShowPasswordModal();", true);

                txt_newpass1.ReadOnly = false;
                txt_newpass2.ReadOnly = false;

                txt_SQAns1.ReadOnly = false;
                txt_SQAns2.ReadOnly = false;

                txt_oldpass.BorderColor = System.Drawing.Color.Green;
                txt_oldpass.ReadOnly = true;

                DDL_SQ1.SelectedIndex = 0;
                DDL_SQ2.SelectedIndex = 0;

                txt_newpass1.Text = "";
                txt_newpass2.Text = "";

                txt_SQAns1.Text = "";
                txt_SQAns2.Text = "";

                btn_svpass.Enabled = true;

                newpwd_row1.Visible = true; newpwd_row2.Visible = true;
                newpwd_row3.Visible = true; newpwd_row4.Visible = true;
                newpwd_row5.Visible = true; newpwd_row6.Visible = true;
                newpwd_row7.Visible = true; newpwd_row8.Visible = true;
                newpwd_row9.Visible = true; newpwd_row10.Visible = true;
                newpwd_row11.Visible = true; newpwd_row12.Visible = true;
                emailinput_row1.Visible = true; emailinput_row2.Visible = true;
                DivSendOTP.Visible = true;

                btn_validateoldpassword.Enabled = false;
                btn_discardsvpass.Enabled = false;

                btn_validateoldpassword.Text = "Verified";

                //string CmdString1 = "select Security_Questions, QNo from tlb_security_questions where Category = '1' order by Id ";
                //BindSecurityQ1(CmdString1);

                //string CmdString2 = "select Security_Questions, QNo from tlb_security_questions where Category = '2' order by Id ";
                //BindSecurityQ2(CmdString2);
            }
            else
            {


                txt_oldpass.Text = "";
                txt_oldpass.Focus();
                txt_oldpass.BorderColor = System.Drawing.Color.Red;

                txt_newpass1.ReadOnly = true;
                txt_newpass2.ReadOnly = true;

                txt_newpass1.Text = "";
                txt_newpass2.Text = "";

                txt_SQAns1.ReadOnly = true;
                txt_SQAns2.ReadOnly = true;

                txt_SQAns1.Text = "";
                txt_SQAns2.Text = "";

                DDL_SQ1.SelectedIndex = 0;
                DDL_SQ2.SelectedIndex = 0;

                btn_svpass.Enabled = false;

                //ClientScript.RegisterStartupScript(this.GetType(), "alert8", "ShowPasswordModal();", true);
            }
        }

        private void BindSecurityQ1(string CmdString)
        {
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            SqlCommand Cmd = new SqlCommand(CmdString, dbcl.Conn);
            Cmd.CommandType = CommandType.Text;
            DDL_SQ1.DataSource = Cmd.ExecuteReader();
            DDL_SQ1.DataTextField = "Security_Questions";
            DDL_SQ1.DataValueField = "QNo";
            DDL_SQ1.DataBind();
            DDL_SQ1.Items.Insert(0, "Please Select Option");
            dbcl.DisconnectDb();
        }
        private void BindSecurityQ2(string CmdString)
        {
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            SqlCommand Cmd = new SqlCommand(CmdString, dbcl.Conn);
            Cmd.CommandType = CommandType.Text;
            DDL_SQ2.DataSource = Cmd.ExecuteReader();
            DDL_SQ2.DataTextField = "Security_Questions";
            DDL_SQ2.DataValueField = "QNo";
            DDL_SQ2.DataBind();
            DDL_SQ2.Items.Insert(0, "Please Select Option");
            dbcl.DisconnectDb();
        }

        public void LoadLoginDetails()
        {
            string query = "select * from MST_UserMaster where Id=@Id and EmployeeCode=@EmployeeCode";
            SqlParameter[] pram = {
                                          new SqlParameter("@Id",Session["USERID"].ToString()),
                                          new SqlParameter("@EmployeeCode",Session["WORKMAN"].ToString()),
                                      };
            dt = dbcl.SPreturn_dt(query, pram);
            if (dt.Rows.Count > 0)
            {
                string WorkStatus = dt.Rows[0]["EmployeeStatus"].ToString();

                if (WorkStatus == "Active")
                {
                    string UserID = dt.Rows[0]["EmployeeCode"].ToString();
                    txt_atsloginid.Text = UserID;

                    string Workman = dt.Rows[0]["Id"].ToString();
                    txt_atsworkmenno.Text = Workman;

                    UserPass = dt.Rows[0]["Password"].ToString();
                }
                else
                {
                    //ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage2", "<script>alert('User ID is InActive');</script>");
                }
            }
        }

        protected void btn_discardsvpass_Click(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.Redirect("login.aspx");
        }

        protected void btn_relogin_Click(object sender, EventArgs e)
        {
            Session.Abandon();
            Response.Redirect("login.aspx");
        }

        protected void btn_svpass_Click(object sender, EventArgs e)
        {
            if (UpdateLoginCredentials() == true)
            {
                //If Login Credentials Update Successfull
                btn_svpass.Enabled = false;
                btn_svpass.Text = "Success!";
                btn_svpass.CssClass = "btn btn-success btn-sm";

                btn_discardsvpass.Enabled = false;
                btn_discardsvpass.Text = "Success!";
                btn_discardsvpass.CssClass = "btn btn-success btn-sm";

                btn_relogin.Enabled = true;
                btn_cancel.Enabled = false;

                txt_newpass1.ReadOnly = true;
                txt_newpass2.ReadOnly = true;

                DDL_SQ1.Enabled = false;
                txt_SQAns1.ReadOnly = true;

                DDL_SQ2.Enabled = false;
                txt_SQAns2.ReadOnly = true;

                btnrow.Visible = false;

            }
            else
            {
                lbl_msgpass.ForeColor = System.Drawing.Color.Green;
                lbl_msgpass.Text = "Unsuccessfull Attempt.....!!";
                //If updating the login credentials failed
            }

            //ClientScript.RegisterStartupScript(this.GetType(), "alert8", "ShowPasswordModal();", true);
        }

        private Boolean UpdateLoginCredentials()
        {
            Boolean flag = false;
            try
            {
                if (DDL_SQ1.SelectedValue == "Please Select Option" || DDL_SQ2.SelectedValue == "Please Select Option")
                {
                    lbl_msgpass.ForeColor = System.Drawing.Color.Red;
                    lbl_msgpass.Text = "Please select valid security questions.";
                    return false;
                }

                dbcl.Sqlconnection();
                dbcl.ConnectDb();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = dbcl.Conn;
                string CmdString = "UPDATE MST_UserMaster set Password=@Password, SQ1=@SQ1, SQAns1=@SQAns1, SQ2=@SQ2, SQAns2=@SQAns2, Pass_UpdateDate=@Pass_UpdateDate , PassUpdatedByName=@PassUpdatedByName, PassUpdatedByWrk=@PassUpdatedByWrk, PasswordExpiry=@PasswordExpiry where Id=@Id and EmployeeCode=@EmployeeCode";
                cmd.CommandText = CmdString;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Id", txt_atsworkmenno.Text.ToString());
                cmd.Parameters.AddWithValue("@EmployeeCode", txt_atsloginid.Text.ToString());
                cmd.Parameters.AddWithValue("@Password", txt_newpass2.Text.Trim().ToString());
                cmd.Parameters.AddWithValue("@SQ1", DDL_SQ1.SelectedValue.ToString());
                cmd.Parameters.AddWithValue("@SQAns1", txt_SQAns1.Text.ToString());
                cmd.Parameters.AddWithValue("@SQ2", DDL_SQ2.SelectedValue.ToString());
                cmd.Parameters.AddWithValue("@SQAns2", txt_SQAns2.Text.ToString());
                cmd.Parameters.AddWithValue("@Pass_UpdateDate", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@PassUpdatedByName", Session["USERNAME"].ToString());
                cmd.Parameters.AddWithValue("@PassUpdatedByWrk", Session["WORKMAN"].ToString());
                cmd.Parameters.AddWithValue("@PasswordExpiry", DateTime.Today.AddDays(180));
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                flag = true;

                lbl_msgpass.ForeColor = System.Drawing.Color.Green;
                lbl_msgpass.Text = "Login Credentials Updated Successfully....!!";

                btn_relogin.Visible = true;
                btn_relogin.Enabled = true;
            }
            catch (Exception ex)
            {
                flag = false;
                lbl_msgpass.ForeColor = System.Drawing.Color.Red;
                lbl_msgpass.Text = "Error: " + ex.Message.ToString();
            }
            return flag;
        }


        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            if (UpdateSkipLoginCredentials() == true)
            {
                Response.Redirect("home.aspx");
            }
            else
            {
                Response.Redirect("login.aspx");
            }

        }

        private Boolean UpdateSkipLoginCredentials()
        {
            Boolean flag = false;
            try
            {
                dbcl.Sqlconnection();
                dbcl.ConnectDb();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = dbcl.Conn;
                string CmdString = "UPDATE MST_UserMaster set Pass_UpdateDate=@Pass_UpdateDate , PassUpdatedByName=@PassUpdatedByName, PassUpdatedByWrk=@PassUpdatedByWrk, PasswordExpiry=@PasswordExpiry where Id=@Id and EmployeeCode=@EmployeeCode";
                cmd.CommandText = CmdString;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Id", txt_atsworkmenno.Text.ToString());
                cmd.Parameters.AddWithValue("@EmployeeCode", txt_atsloginid.Text.ToString());
                cmd.Parameters.AddWithValue("@Pass_UpdateDate", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                cmd.Parameters.AddWithValue("@PassUpdatedByName", Session["USERNAME"].ToString());
                cmd.Parameters.AddWithValue("@PassUpdatedByWrk", Session["WORKMAN"].ToString());
                cmd.Parameters.AddWithValue("@PasswordExpiry", DateTime.Today.AddDays(15));
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                flag = true;

                lbl_msgpass.ForeColor = System.Drawing.Color.Green;
                lbl_msgpass.Text = "Login Credentials Updated Successfully....!!";

                btn_relogin.Visible = true;
                btn_relogin.Enabled = true;
            }
            catch (Exception ex)
            {
                flag = false;
                lbl_msgpass.ForeColor = System.Drawing.Color.Red;
                lbl_msgpass.Text = "Error: " + ex.Message.ToString();
            }
            return flag;
        }


        protected void btn_SendOTP_Click(object sender, EventArgs e)
        {
            string userEmail = txt_Email.Text.Trim();
            if (!string.IsNullOrEmpty(userEmail))
            {
                // Generate a random 6-digit OTP
                Random rand = new Random();
                int otp = rand.Next(100000, 999999);
                Session["OTP"] = otp;

                // Compose email
                string subject = "Your OTP Code";
                string body = $"Your OTP code is: {otp}";

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                using (MailMessage mm = new MailMessage("sapsystem@anmolindustries.com", userEmail))
                {
                    mm.Subject = subject;
                    mm.Body = body;
                    mm.IsBodyHtml = false;

                    using (SmtpClient smtp = new SmtpClient("smtp.office365.com", 587))
                    {
                        //smtp.Host = "smtp.office365.com";
                        smtp.EnableSsl = true;
                        //smtp.Port = 587;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential("sapsystem@anmolindustries.com", "Basis@2024");
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Send(mm);
                    }
                }

                lbl_msgpass.Text = "OTP has been sent to your email.";
                lbl_msgpass.ForeColor = System.Drawing.Color.Green;
                divVerifyOTP.Visible = true; // Show OTP input
            }
        }


        protected void btn_VerifyOTP_Click(object sender, EventArgs e)
        {
            string enteredOTP = txt_OTP.Text.Trim();
            if (Session["OTP"] != null && enteredOTP == Session["OTP"].ToString())
            {
                lbl_msgpass.Text = "Email verified successfully!";
                lbl_msgpass.ForeColor = System.Drawing.Color.Green;
                Session.Remove("OTP"); // Clear OTP after use

                btn_svpass.Enabled = true;
            }
            else
            {
                lbl_msgpass.Text = "Invalid OTP. Please try again.";
                lbl_msgpass.ForeColor = System.Drawing.Color.Red;

                btn_svpass.Enabled = false;
            }
        }


    }
}