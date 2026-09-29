using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnmolDristi
{
    public partial class PolicyMatrixAdmin : System.Web.UI.Page
    {
        private string ConnString => ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadFilterDropdowns();
                PopulateModalDropdowns();
                BindGrid(); // initial grid
            }
        }

        private void LoadFilterDropdowns()
        {
            // Assumes Mst_Region, Mst_Branch, Mst_Grade, Mst_Designation exist.
            // You will need to replace these helper calls with your actual data retrieval (or query these tables).
            BindDropdownFromQuery(ddlRegion, "SELECT Id, Region_Description FROM Mst_Region ORDER BY Region_Description", "Region_Description", "Id", true);
            BindDropdownFromQuery(ddlBranch, "SELECT Id, Branch_Description FROM Mst_Branch ORDER BY Branch_Description", "Branch_Description", "Id", true);
            BindDropdownFromQuery(ddlGrade, "SELECT Id, Grade_Description FROM Mst_Grade ORDER BY Grade_Description", "Grade_Description", "Id", true);
            BindDropdownFromQuery(ddlDesignation, "SELECT Id, Designation_Description FROM Mst_Designation ORDER BY Designation_Description", "Designation_Description", "Id", true);


            //BindDropdownFromQuery(ddlRegion, "SELECT Region_ID, Region_Description FROM Mst_Region ORDER BY Region_Description", "Region_Description", "Region_ID", true);
            //BindDropdownFromQuery(ddlBranch, "SELECT Branch_ID, Branch_Description FROM Mst_Branch ORDER BY Branch_Description", "Branch_Description", "Branch_ID", true);
            //BindDropdownFromQuery(ddlGrade, "SELECT Grade_ID, Grade_Description FROM Mst_Grade ORDER BY Grade_Description", "Grade_Description", "Grade_ID", true);
            //BindDropdownFromQuery(ddlDesignation, "SELECT Designation_ID, Designation_Description FROM Mst_Designation ORDER BY Designation_Description", "Designation_Description", "Designation_ID", true);
        }

        private void PopulateModalDropdowns()
        {
            BindDropdownFromQuery(ddlModalRegion, "SELECT Id, Region_Description FROM Mst_Region ORDER BY Region_Description", "Region_Description", "Id", true);
            BindDropdownFromQuery(ddlModalBranch, "SELECT Id, Branch_Description FROM Mst_Branch ORDER BY Branch_Description", "Branch_Description", "Id", true);
            BindDropdownFromQuery(ddlModalGrade, "SELECT Id, Grade_Description FROM Mst_Grade ORDER BY Grade_Description", "Grade_Description", "Id", true);
            BindDropdownFromQuery(ddlModalDesignation, "SELECT Id, Designation_Description FROM Mst_Designation ORDER BY Designation_Description", "Designation_Description", "Id", true);

            //BindDropdownFromQuery(ddlModalRegion, "SELECT Region_ID, Region_Description FROM Mst_Region ORDER BY Region_Description", "Region_Description", "Region_ID", true);
            //BindDropdownFromQuery(ddlModalBranch, "SELECT Branch_ID, Branch_Description FROM Mst_Branch ORDER BY Branch_Description", "Branch_Description", "Branch_ID", true);
            //BindDropdownFromQuery(ddlModalGrade, "SELECT Grade_ID, Grade_Description FROM Mst_Grade ORDER BY Grade_Description", "Grade_Description", "Grade_ID", true);
            //BindDropdownFromQuery(ddlModalDesignation, "SELECT Designation_ID, Designation_Description FROM Mst_Designation ORDER BY Designation_Description", "Designation_Description", "Designation_ID", true);
        }

        private void BindDropdownFromQuery(DropDownList ddl, string sql, string textField, string valueField, bool includeAll)
        {
            ddl.Items.Clear();
            if (includeAll) ddl.Items.Add(new ListItem("-- All --", ""));
            using (var cn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                ddl.DataSource = dt;
                ddl.DataTextField = textField;
                ddl.DataValueField = valueField;
                ddl.DataBind();
            }
        }

        protected void ddlFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void BindGrid()
        {
            int regionId;
            int branchId;
            int gradeId;

            object regionParam = DBNull.Value;
            object branchParam = DBNull.Value;
            object gradeParam = DBNull.Value;

            if (int.TryParse(ddlRegion.SelectedValue, out regionId)) regionParam = regionId;
            if (int.TryParse(ddlBranch.SelectedValue, out branchId)) branchParam = branchId;
            if (int.TryParse(ddlGrade.SelectedValue, out gradeId)) gradeParam = gradeId;

            using (SqlConnection cn = new SqlConnection(ConnString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetPolicies", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RegionId", regionParam);
                cmd.Parameters.AddWithValue("@BranchId", branchParam);
                cmd.Parameters.AddWithValue("@GradeId", gradeParam);
                cmd.Parameters.AddWithValue("@DesignationId", DBNull.Value);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvPolicies.DataSource = dt;
                    gvPolicies.DataBind();
                }
            }
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            hfPolicyId.Value = "0";
            txtPolicyJson.Text = "{}";
            txtPriority.Text = "1";
            chkIsActive.Checked = true;
            txtEffectiveFrom.Text = DateTime.Now.ToString("yyyy-MM-dd");
            txtEffectiveTo.Text = DateTime.Now.AddYears(10).ToString("yyyy-MM-dd");
            // show modal
            //ScriptManager.RegisterStartupScript(this, this.GetType(), "showModal", "showPolicyModal();", true);

            // inside code-behind where you want to open the modal
            ScriptManager.RegisterStartupScript(
                this,
                this.GetType(),
                "showModal",
                string.Format("$('#{0}').modal('show');", policyModal.ClientID),
                true
            );

        }

        //protected void gvPolicies_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        //{
        //    if (e.CommandName == "EditRow")
        //    {
        //        int id = Convert.ToInt32(e.CommandArgument);
        //        LoadIntoModal(id);
        //        ScriptManager.RegisterStartupScript(this, this.GetType(), "showModal", "showPolicyModal();", true);
        //    }
        //    else if (e.CommandName == "DeleteRow")
        //    {
        //        int id = Convert.ToInt32(e.CommandArgument);
        //        DeletePolicy(id);
        //        BindGrid();
        //        ScriptManager.RegisterStartupScript(this, this.GetType(), "pnotify", "showPNotify('success','Deleted','Policy deleted');", true);
        //    }
        //}

        protected void gvPolicies_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditRow")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                LoadIntoModal(id);

                // Use policyModal.ClientID so the selector matches the rendered id
                ScriptManager.RegisterStartupScript(
                    this,
                    this.GetType(),
                    "showModal_" + id, // unique key (helps avoid duplicate script collisions)
                    string.Format("$('#{0}').modal('show');", policyModal.ClientID),
                    true
                );
            }
            else if (e.CommandName == "DeleteRow")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                DeletePolicy(id);
                BindGrid();

                // Notification (keeps existing approach)
                ScriptManager.RegisterStartupScript(
                    this,
                    this.GetType(),
                    "pnotify_deleted_" + id,
                    "showPNotify('success','Deleted','Policy deleted');",
                    true
                );
            }
        }


        private void LoadIntoModal(int id)
        {
            using (SqlConnection cn = new SqlConnection(ConnString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetPolicyById", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PolicyId", id);
                cn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        hfPolicyId.Value = rdr["PolicyId"].ToString();
                        ddlModalRegion.SelectedValue = rdr["RegionId"] == DBNull.Value ? "" : rdr["RegionId"].ToString();
                        ddlModalBranch.SelectedValue = rdr["BranchId"] == DBNull.Value ? "" : rdr["BranchId"].ToString();
                        ddlModalGrade.SelectedValue = rdr["GradeId"] == DBNull.Value ? "" : rdr["GradeId"].ToString();
                        ddlModalDesignation.SelectedValue = rdr["DesignationId"] == DBNull.Value ? "" : rdr["DesignationId"].ToString();
                        txtPriority.Text = rdr["Priority"].ToString();
                        txtPolicyJson.Text = FormatJsonSafe(rdr["PolicyJson"] == DBNull.Value ? "{}" : rdr["PolicyJson"].ToString());
                        chkIsActive.Checked = Convert.ToBoolean(rdr["IsActive"]);
                        txtEffectiveFrom.Text = rdr["EffectiveFrom"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["EffectiveFrom"]).ToString("yyyy-MM-dd");
                        txtEffectiveTo.Text = rdr["EffectiveTo"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["EffectiveTo"]).ToString("yyyy-MM-dd");
                    }
                }
            }
        }

        protected void btnSavePolicy_Click(object sender, EventArgs e)
        {
            try
            {
                int policyId = 0;
                int.TryParse(hfPolicyId.Value ?? "0", out policyId);

                int regionId;
                int branchId;
                int gradeId;
                int designationId;
                int priority;

                int? regionParam = null;
                int? branchParam = null;
                int? gradeParam = null;
                int? designationParam = null;

                if (int.TryParse(ddlModalRegion.SelectedValue, out regionId)) regionParam = regionId;
                if (int.TryParse(ddlModalBranch.SelectedValue, out branchId)) branchParam = branchId;
                if (int.TryParse(ddlModalGrade.SelectedValue, out gradeId)) gradeParam = gradeId;
                if (int.TryParse(ddlModalDesignation.SelectedValue, out designationId)) designationParam = designationId;

                if (!int.TryParse(txtPriority.Text.Trim(), out priority)) priority = 1;

                bool isActive = chkIsActive.Checked;
                string policyJsonRaw = txtPolicyJson.Text.Trim();

                string formattedJson = "";
                if (!IsValidJson(policyJsonRaw, out formattedJson))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "pnotify", "showPNotify('error','Invalid JSON','Policy JSON is invalid.');", true);
                    return;
                }

                DateTime effectiveFrom;
                DateTime effectiveTo;
                object efParam = DBNull.Value;
                object etParam = DBNull.Value;
                if (DateTime.TryParse(txtEffectiveFrom.Text.Trim(), out effectiveFrom)) efParam = effectiveFrom.Date;
                if (DateTime.TryParse(txtEffectiveTo.Text.Trim(), out effectiveTo)) etParam = effectiveTo.Date;

                using (SqlConnection cn = new SqlConnection(ConnString))
                using (SqlCommand cmd = new SqlCommand(policyId == 0 ? "dbo.usp_InsertPolicy" : "dbo.usp_UpdatePolicy", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (policyId == 0)
                    {
                        cmd.Parameters.AddWithValue("@RegionId", regionParam.HasValue ? (object)regionParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@BranchId", branchParam.HasValue ? (object)branchParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@GradeId", gradeParam.HasValue ? (object)gradeParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@DesignationId", designationParam.HasValue ? (object)designationParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@PolicyJson", formattedJson);
                        cmd.Parameters.AddWithValue("@Priority", priority);
                        cmd.Parameters.AddWithValue("@EffectiveFrom", efParam);
                        cmd.Parameters.AddWithValue("@EffectiveTo", etParam);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUser());
                        cn.Open();
                        object newIdObj = cmd.ExecuteScalar();
                        int newPolicyId = Convert.ToInt32(newIdObj);

                        string payload = BuildPayloadJson(newPolicyId, regionParam, branchParam, gradeParam, designationParam, formattedJson, priority, efParam, etParam, isActive, GetCurrentUser());
                        InsertPolicyHistory(newPolicyId, "Insert", GetCurrentUser(), payload);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PolicyId", policyId);
                        cmd.Parameters.AddWithValue("@RegionId", regionParam.HasValue ? (object)regionParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@BranchId", branchParam.HasValue ? (object)branchParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@GradeId", gradeParam.HasValue ? (object)gradeParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@DesignationId", designationParam.HasValue ? (object)designationParam.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@PolicyJson", formattedJson);
                        cmd.Parameters.AddWithValue("@Priority", priority);
                        cmd.Parameters.AddWithValue("@EffectiveFrom", efParam);
                        cmd.Parameters.AddWithValue("@EffectiveTo", etParam);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUser());
                        cn.Open();
                        cmd.ExecuteNonQuery();

                        string payload = BuildPayloadJson(policyId, regionParam, branchParam, gradeParam, designationParam, formattedJson, priority, efParam, etParam, isActive, GetCurrentUser());
                        InsertPolicyHistory(policyId, "Update", GetCurrentUser(), payload);
                    }
                }

                BindGrid();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "closeModal", "hidePolicyModal();", true);
                ScriptManager.RegisterStartupScript(this, this.GetType(), "pnotify", "showPNotify('success','Saved','Policy saved successfully');", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "pnotify", string.Format("showPNotify('error','Error','{0}');", ex.Message.Replace("'", "\\'")), true);
            }
        }

        private void InsertPolicyHistory(int policyId, string changeType, string changedBy, string payload)
        {
            using (SqlConnection cn = new SqlConnection(ConnString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_InsertPolicyHistory", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PolicyId", policyId);
                cmd.Parameters.AddWithValue("@ChangeType", changeType);
                cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Payload", payload ?? (object)DBNull.Value);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private string BuildPayloadJson(int policyId, int? regionId, int? branchId, int? gradeId, int? designationId,
                                        string policyJson, int priority, object effectiveFrom, object effectiveTo,
                                        bool isActive, string changedBy)
        {
            JObject obj = new JObject();
            obj["PolicyId"] = policyId;
            obj["RegionId"] = regionId;
            obj["BranchId"] = branchId;
            obj["GradeId"] = gradeId;
            obj["DesignationId"] = designationId;
            obj["Priority"] = priority;
            obj["EffectiveFrom"] = effectiveFrom == DBNull.Value ? null : Convert.ToDateTime(effectiveFrom).ToString("yyyy-MM-dd");
            obj["EffectiveTo"] = effectiveTo == DBNull.Value ? null : Convert.ToDateTime(effectiveTo).ToString("yyyy-MM-dd");
            obj["IsActive"] = isActive;
            try
            {
                obj["PolicyJson"] = JToken.Parse(string.IsNullOrWhiteSpace(policyJson) ? "{}" : policyJson);
            }
            catch
            {
                obj["PolicyJson"] = policyJson;
            }
            obj["ChangedBy"] = changedBy;
            obj["ChangedAtIsT"] = GetIndianStandardTime().ToString("yyyy-MM-ddTHH:mm:ss");
            return obj.ToString(Formatting.None);
        }

        private DateTime GetIndianStandardTime()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);
        }

        private void DeletePolicy(int id)
        {
            using (SqlConnection cn = new SqlConnection(ConnString))
            {
                cn.Open();
                string sel = "SELECT * FROM dbo.PolicyMatrix WHERE PolicyId = @Id";
                using (SqlCommand cmd = new SqlCommand(sel, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            string payload = BuildPayloadJson(
                                id,
                                rdr["RegionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["RegionId"]),
                                rdr["BranchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["BranchId"]),
                                rdr["GradeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["GradeId"]),
                                rdr["DesignationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["DesignationId"]),
                                rdr["PolicyJson"] == DBNull.Value ? "{}" : rdr["PolicyJson"].ToString(),
                                rdr["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["Priority"]),
                                rdr["EffectiveFrom"] == DBNull.Value ? (object)DBNull.Value : rdr["EffectiveFrom"],
                                rdr["EffectiveTo"] == DBNull.Value ? (object)DBNull.Value : rdr["EffectiveTo"],
                                false,
                                GetCurrentUser()
                            );
                            InsertPolicyHistory(id, "Delete", GetCurrentUser(), payload);
                        }
                    }
                }

                string upd = "UPDATE dbo.PolicyMatrix SET IsActive = 0, ModifiedBy = @User, ModifiedOn = CONVERT(datetime2,((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')) WHERE PolicyId = @Id";
                using (SqlCommand ucmd = new SqlCommand(upd, cn))
                {
                    ucmd.Parameters.AddWithValue("@User", GetCurrentUser());
                    ucmd.Parameters.AddWithValue("@Id", id);
                    ucmd.ExecuteNonQuery();
                }
            }
        }

        private bool IsValidJson(string input, out string formatted)
        {
            formatted = input;
            if (string.IsNullOrWhiteSpace(input)) return false;
            try
            {
                JToken parsed = JToken.Parse(input);
                formatted = parsed.ToString(Formatting.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FormatJsonSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";
            try
            {
                JToken parsed = JToken.Parse(input);
                return parsed.ToString(Formatting.Indented);
            }
            catch
            {
                return input;
            }
        }

        private string GetCurrentUser()
        {
            return Page.User != null && Page.User.Identity != null && !string.IsNullOrEmpty(Page.User.Identity.Name) ? Page.User.Identity.Name : "SYSTEM";
        }

        #region JSON helpers

        private bool IsValidJson_OLD(string input, out string formatted)
        {
            formatted = input;
            if (string.IsNullOrWhiteSpace(input)) return false;
            try
            {
                var parsed = JToken.Parse(input);
                formatted = parsed.ToString(Formatting.None); // store compact
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FormatJsonSafe_OLD(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";
            try
            {
                var parsed = JToken.Parse(input);
                return parsed.ToString(Formatting.Indented);
            }
            catch
            {
                return input;
            }
        }

        #endregion

        #region IST conversion helpers

        private DateTime GetIndianStandardTime_OLD()
        {
            // Returns current time in IST
            var utcNow = DateTime.UtcNow;
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // windows tz id
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);
        }

        private DateTime ConvertUtcToIST(DateTime utcDate)
        {
            if (utcDate.Kind == DateTimeKind.Unspecified) utcDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, istZone);
        }

        private DateTime ConvertISTToUtc(DateTime istDate)
        {
            // assume input is local IST (unspecified or local)
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            if (istDate.Kind == DateTimeKind.Utc) return istDate;
            var unspecified = DateTime.SpecifyKind(istDate, DateTimeKind.Unspecified);
            var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, istZone);
            return utc;
        }

        #endregion
    }
}