using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class VMS_MyVisits : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Security Check: Look for the NEW Dual-Auth session keys
                if (Session["VerifiedMobile"] == null && Session["VerifiedEmail"] == null)
                {
                    // Safe Redirect Pattern
                    Response.Redirect("VMS_SelfLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                LoadVisitorHistory();
            }
        }

        private void LoadVisitorHistory()
        {
            // Safely extract whichever contact method they verified with
            string contact = "";
            if (Session["VerifiedMobile"] != null && !string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
            {
                contact = Session["VerifiedMobile"].ToString();
            }
            else if (Session["VerifiedEmail"] != null && !string.IsNullOrEmpty(Session["VerifiedEmail"].ToString()))
            {
                contact = Session["VerifiedEmail"].ToString();
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        v.LogID,
                        v.CreatedDate,
                        v.PurposeOfVisit,
                        v.ApprovalStatus,
                        v.QRToken,
                        ISNULL(o.AliasName, b.Branch_Description) AS AliasName,
                        u.EmployeeName AS HostName
                    FROM 
                        VMS_VisitLog v
                    LEFT JOIN 
                        VMS_OfficeSettings o ON v.TargetBranchID = o.Branch_ID
                    LEFT JOIN 
                        MST_Branch b ON v.TargetBranchID = b.Branch_ID
                    LEFT JOIN 
                        MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    WHERE 
                        (v.VisitorMobile = @Contact OR v.VisitorEmail = @Contact)
                        AND v.CreatedDate >= DATEADD(month, -6, GETDATE())
                    ORDER BY 
                        v.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Contact", contact);

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        rptVisits.DataSource = dt;
                        rptVisits.DataBind();
                    }
                }
            }
        }

        protected void rptVisits_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                // 1. Style the Status Badge dynamically
                Label lblStatus = (Label)e.Item.FindControl("lblStatus");
                string status = lblStatus.Text.ToLower();

                if (status == "approved")
                    lblStatus.CssClass = "badge badge-approved";
                else if (status == "rejected")
                    lblStatus.CssClass = "badge badge-rejected";
                else
                    lblStatus.CssClass = "badge badge-pending";

                // 2. Show the "View Digital Pass" footer ONLY if approved
                Panel pnlFooter = (Panel)e.Item.FindControl("pnlFooter");
                if (status == "approved")
                {
                    // Ensure the token exists before showing the button
                    object tokenObj = DataBinder.Eval(e.Item.DataItem, "QRToken");
                    if (tokenObj != null && !string.IsNullOrEmpty(tokenObj.ToString()))
                    {
                        pnlFooter.Visible = true;
                    }
                }
            }
        }
    }
}