using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace AnmolDristi
{
    public partial class ManageEmployeeTolerance : BasePage
    {
        private string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            // Role check — put this if admin-only
            //if (!User.IsInRole("Admin"))
            //{
            //    Response.Redirect("~/AccessDenied.aspx"); // or show friendly message
            //    return;
            //}

            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetEmployeeTolerance", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                // no filters for admin listing
                // optionally pass a filter:
                // cmd.Parameters.AddWithValue("@EmployeeCode", someLoginIdOrNull);
                cn.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                gvTolerance.DataSource = dt;
                gvTolerance.DataBind();
            }
        }

        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            hfId.Value = "0";
            lblFormMessage.Text = "";
            ScriptManager.RegisterStartupScript(this, GetType(), "showModal", "showToleranceModal();", true);
        }

        private void ClearForm()
        {
            txtLoginId.Text = "";
            txtMinKM.Text = "";
            txtMaxKM.Text = "";
            txtMaxKmModificationCount.Text = KmToleranceHelper.DefaultMaxModificationCount.ToString();
            txtEffectiveFrom.Text = "";
            txtEffectiveTo.Text = "";
            chkIsActive.Checked = true;
        }

        protected void gvTolerance_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int id = 0;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            if (e.CommandName == "EditRow")
            {
                LoadIntoForm(id);
                ClientScript.RegisterStartupScript(this.GetType(), "showModal", "showToleranceModal();", true);
            }
            //if (e.CommandName == "EditRow")
            //{
            //    // 1) Fill server controls
            //    LoadIntoForm(id);

            //    // 2) Update the UpdatePanel so the new HTML is included in partial response
            //    var up = this.FindControl("upModal") as System.Web.UI.UpdatePanel;
            //    if (up != null)
            //    {
            //        up.Update();

            //        // 3) Register startup script against the UpdatePanel so it executes after the update
            //        // Using ScriptManager.RegisterStartupScript with the UpdatePanel ensures correct timing
            //        ScriptManager.RegisterStartupScript(up, this.GetType(), "showModal", "if (typeof $ !== 'undefined' && typeof $.fn.modal !== 'undefined') { $('#modalEditTolerance').modal('show'); $('#" + txtLoginId.ClientID + "').focus(); }", true);
            //    }
            //    else
            //    {
            //        // fallback if UpdatePanel absent
            //        ScriptManager.RegisterStartupScript(this, this.GetType(), "showModal", "if (typeof $ !== 'undefined' && typeof $.fn.modal !== 'undefined') { $('#modalEditTolerance').modal('show'); $('#" + txtLoginId.ClientID + "').focus(); }", true);
            //    }
            //}
            else if (e.CommandName == "DeleteRow")
            {
                SoftDelete(id);
                BindGrid();
            }
        }

        private void LoadIntoForm(int id)
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 * FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = @Id", cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        hfId.Value = id.ToString();
                        //txtLoginId.Text = Convert.ToString(r["Login_Id"]);
                        txtLoginId.Text = Convert.ToString(r["EmployeeCode"]);
                        txtMinKM.Text = r["MinAllowedKM"] == DBNull.Value ? "" : Convert.ToDecimal(r["MinAllowedKM"]).ToString("0.##");
                        //txtMinKM.Text = r["MinKM"] == DBNull.Value ? "" : Convert.ToDecimal(r["MinKM"]).ToString("0.##");
                        txtMaxKM.Text = r["MaxAllowedKM"] == DBNull.Value ? "" : Convert.ToDecimal(r["MaxAllowedKM"]).ToString("0.##");
                        if (HasColumn(r, "MaxKmModificationCount") && r["MaxKmModificationCount"] != DBNull.Value)
                            txtMaxKmModificationCount.Text = Convert.ToInt32(r["MaxKmModificationCount"]).ToString();
                        else
                            txtMaxKmModificationCount.Text = KmToleranceHelper.DefaultMaxModificationCount.ToString();
                        txtEffectiveFrom.Text = r["EffectiveFrom"] == DBNull.Value ? "" : ((DateTime)r["EffectiveFrom"]).ToString("yyyy-MM-dd");
                        txtEffectiveTo.Text = r["EffectiveTo"] == DBNull.Value ? "" : ((DateTime)r["EffectiveTo"]).ToString("yyyy-MM-dd");
                        chkIsActive.Checked = r["IsActive"] == DBNull.Value ? true : Convert.ToBoolean(r["IsActive"]);
                    }
                }
            }
        }

        private void SoftDelete(int id)
        {
            using (SqlConnection cn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_DeleteEmployeeTolerance", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ToleranceId", id);
                cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLogin());
                cmd.ExecuteNonQuery();
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblFormMessage.Text = "";

            // basic server-side validation
            string loginId = txtLoginId.Text.Trim();
            if (string.IsNullOrEmpty(loginId))
            {
                lblFormMessage.Text = "Login Id is required.";
                ScriptManager.RegisterStartupScript(this, GetType(), "showModal", "showToleranceModal();", true);
                return;
            }

            int id = 0;
            int.TryParse(hfId.Value, out id);

            decimal? minKm = ParseDecimalOrNull(txtMinKM.Text);
            decimal? maxKm = ParseDecimalOrNull(txtMaxKM.Text);
            int? maxKmModificationCount = ParseIntOrNull(txtMaxKmModificationCount.Text);

            DateTime? effFrom = ParseDateOrNull(txtEffectiveFrom.Text);
            DateTime? effTo = ParseDateOrNull(txtEffectiveTo.Text);

            if (!maxKmModificationCount.HasValue || maxKmModificationCount.Value < 1)
            {
                lblFormMessage.Text = "Maximum KM Modifications must be at least 1.";
                ScriptManager.RegisterStartupScript(this, GetType(), "showModal", "showToleranceModal();", true);
                return;
            }

            // reduction/increase tolerances are independent; no min <= max comparison
            // effective dates validation
            if (effFrom.HasValue && effTo.HasValue && effFrom.Value > effTo.Value)
            {
                lblFormMessage.Text = "Effective From cannot be after Effective To.";
                ScriptManager.RegisterStartupScript(this, GetType(), "showModal", "showToleranceModal();", true);
                return;
            }

            // save via SP

            using (SqlConnection cn = new SqlConnection(connStr))

            using (SqlCommand cmd = new SqlCommand("dbo.usp_SaveEmployeeTolerance", cn))
            {
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ToleranceId", id);
                cmd.Parameters.AddWithValue("@EmployeeCode", loginId);
                cmd.Parameters.AddWithValue("@MinAllowedKM", (object)minKm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxAllowedKM", (object)maxKm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaxKmModificationCount", maxKmModificationCount.Value);
                cmd.Parameters.AddWithValue("@EffectiveFrom", (object)effFrom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EffectiveTo", (object)effTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLogin());
                var newId = cmd.ExecuteScalar(); // returns NewToleranceId
            }

            BindGrid();
            // close modal via script
            ScriptManager.RegisterStartupScript(this, GetType(), "hideModal", "$('#modalEditTolerance').modal('hide');", true);
        }

        private int? ParseIntOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            int tmp;
            if (int.TryParse(s, out tmp)) return tmp;
            return null;
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
            // adapt this to your auth: e.g., HttpContext.Current.User.Identity.Name or session
            //if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity != null)
            //    return HttpContext.Current.User.Identity.Name;
            //return "SYSTEM";

            return Session["WORKMAN"].ToString();
        }

        protected void gvTolerance_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            // optional: style/role based hiding, formatting, etc.
        }

        // helper: register client script safely (ScriptManager if available else ClientScript)
        private void RegisterScript(string key, string script)
        {
            if (ScriptManager.GetCurrent(Page) != null)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), key, script, true);
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), key, script, true);
            }
        }

        private void ShowModalFromServer()
        {
            RegisterScript("showModal", "if (typeof $ !== 'undefined') { $('#modalEditTolerance').modal('show'); } else { alert('jQuery not available - cannot show modal.'); }");
        }

        private void HideModalFromServer()
        {
            RegisterScript("hideModal", "if (typeof $ !== 'undefined') { $('#modalEditTolerance').modal('hide'); }");
        }

    }
}