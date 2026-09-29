using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;

namespace AnmolDristi
{
    public partial class ManageExpenseSettings : BasePage
    {
        private string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // protect page if needed
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetExpenseSettings", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OnlyActive", 0); // show all by default; change to 1 if you want active-only
                cn.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                gvSettings.DataSource = dt;
                gvSettings.DataBind();
            }
        }

        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            hfSettingId.Value = "0";
            lblExpenseMessage.Text = "";
            // full postback approach: use ClientScript to show modal after render
            ClientScript.RegisterStartupScript(this.GetType(), "showExpenseModal", "ShowExpenseModal();", true);
        }

        private void ClearForm()
        {
            txtExpenseType.Text = "";
            chkAttachmentRequired.Checked = false;
            txtThreshold.Text = "";
            txtNotes.Text = "";
            lblExpenseMessage.Text = "";
        }

        protected void gvSettings_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            //if (!int.TryParse(Convert.ToString(e.CommandArgument), out int id)) return;

            int id = 0;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            if (e.CommandName == "EditRow")
            {
                LoadIntoForm(id);
                ClientScript.RegisterStartupScript(this.GetType(), "showExpenseModal", "ShowExpenseModal();", true);
            }
            else if (e.CommandName == "DeleteRow")
            {
                SoftDelete(id);
                BindGrid();
            }
        }

        private void LoadIntoForm(int id)
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 * FROM dbo.MST_ExpenseSettings WHERE SettingId = @Id", cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        hfSettingId.Value = id.ToString();
                        txtExpenseType.Text = Convert.ToString(r["ExpenseType"]);
                        chkAttachmentRequired.Checked = r["AttachmentRequired"] == DBNull.Value ? false : Convert.ToBoolean(r["AttachmentRequired"]);
                        txtThreshold.Text = r["ThresholdAmount"] == DBNull.Value ? "" : Convert.ToDecimal(r["ThresholdAmount"]).ToString("0.##");
                        txtNotes.Text = r["Notes"] == DBNull.Value ? "" : Convert.ToString(r["Notes"]);
                    }
                }
            }
        }

        private void SoftDelete(int id)
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_DeleteExpenseSettings", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SettingId", id);
                cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLogin());
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        protected void btnSaveSetting_Click(object sender, EventArgs e)
        {
            lblExpenseMessage.Text = "";

            string expenseType = txtExpenseType.Text.Trim();
            if (string.IsNullOrEmpty(expenseType))
            {
                lblExpenseMessage.Text = "Expense Type is required.";
                ClientScript.RegisterStartupScript(this.GetType(), "showExpenseModal", "ShowExpenseModal();", true);
                return;
            }

            int settingId = 0;
            int.TryParse(hfSettingId.Value, out settingId);
            bool attachmentRequired = chkAttachmentRequired.Checked;

            decimal? threshold = ParseDecimalOrNull(txtThreshold.Text);

            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SaveExpenseSettings", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SettingId", settingId);
                cmd.Parameters.AddWithValue("@ExpenseType", expenseType);
                cmd.Parameters.AddWithValue("@AttachmentRequired", attachmentRequired);
                cmd.Parameters.AddWithValue("@ThresholdAmount", (object)threshold ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text.Trim());
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLogin());
                cn.Open();
                object res = cmd.ExecuteScalar(); // NewSettingId
            }

            BindGrid();

            // hide modal (full postback, client script will run)
            ClientScript.RegisterStartupScript(this.GetType(), "hideExpenseModal", "$('#modalExpenseSetting').modal('hide');", true);
        }

        private decimal? ParseDecimalOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            decimal tmp;
            if (decimal.TryParse(s, out tmp)) return tmp;
            return null;
        }

        private DateTime? ParseDateOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            DateTime tmp;
            if (DateTime.TryParse(s, out tmp)) return tmp.Date;
            return null;
        }

        private string GetCurrentUserLogin()
        {
            return Session["WORKMAN"].ToString();
        }
    }
}