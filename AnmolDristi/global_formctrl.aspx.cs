using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class global_formctrl : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx");
                }
                else
                {
                    lbl_docname.Text = "Global Form Fields Controller";
                    FormBinder();
                }

            }
        }

        private void FormBinder()
        {
            // Construct the SQL query with parameters
            string query = "SELECT [DocumentName], [FormID] FROM [MST_FormsMaster] ";
            string textField = "DocumentName"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "FormID"; // Assuming this is the correct field for storing in the DropDownList


            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_FormName, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_FormName);
                string DDL_Form_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'An error occurred!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowFormBinderErrorNotification", DDL_Form_Error_script, false);
            }
        }

        protected void DDL_FormName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_FormName.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                lbl_DDL_FormName_Value.Text = selectedFormValue;
                PlantBinder(selectedFormValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

                string DDL_Form_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowFormInvalidErrorNotification", DDL_Form_Error_script, false);
            }
        }

        private void PlantBinder(string selectedFormValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            string textField = "plant_name";
            string valueField = "plant_id";

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FormID", selectedFormValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string DDL_Plant_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", DDL_Plant_Error_script, false);
            }
        }

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();

                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                //BindGridViewbyFormPlant(selectedFormValue, selectedPlantValue);
                PlantLinesBinder(selectedFormValue, selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                string DDL_Plant_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", DDL_Plant_Error_script, false);
            }
        }

        private void PlantLinesBinder(string selectedFormValue, string selectedPlantValue)
        {
            string query = "SELECT line_name, line_id FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
            string textField = "line_name";
            string valueField = "line_id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_PlantLine, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                string PlantLinesBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantLinesBinderErrorNotification", PlantLinesBinder_Error_script, false);
            }
        }

        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }

        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
            string textField = "category_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "category_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string PN_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No line categories found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
            }
        }

        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductCategory.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;
                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }

        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
            string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string ProductBrands_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No Brands found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            BindFieldControlGrid();
        }

        protected void GV_Matrix_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditField")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = GV_Matrix.Rows[index];

                txt_FieldName.Text = ((Label)row.FindControl("lbl_FieldName"))?.Text.Trim();
                txt_DisplayName.Text = ((Label)row.FindControl("lbl_DisplayName"))?.Text.Trim();

                chk_RFV.Checked = ((Label)row.FindControl("lbl_RFV_YesNo"))?.Text == "1";
                txt_RFVError.Text = ((Label)row.FindControl("lbl_RFV_ErrorMsg"))?.Text.Trim();

                chk_REV.Checked = ((Label)row.FindControl("lbl_REV_YesNo"))?.Text == "1";
                txt_REVError.Text = ((Label)row.FindControl("lbl_REV_ErrorMsg"))?.Text.Trim();

                string revExpr = ((Label)row.FindControl("lbl_REV_Expression"))?.Text.Trim();
                if (!string.IsNullOrEmpty(revExpr) && ddl_RegexType.Items.FindByValue(revExpr) != null)
                    ddl_RegexType.SelectedValue = revExpr;
                else
                    ddl_RegexType.ClearSelection();

                chk_RV.Checked = ((Label)row.FindControl("lbl_RV_Yesno"))?.Text == "1";
                txt_RVError.Text = ((Label)row.FindControl("lbl_RV_ErrorMsg"))?.Text.Trim();
                txt_RVMin.Text = ((Label)row.FindControl("lbl_RV_MinValue"))?.Text.Trim();
                txt_RVMax.Text = ((Label)row.FindControl("lbl_RV_MaxValue"))?.Text.Trim();

                chk_ViewMode.Checked = ((Label)row.FindControl("lbl_ViewMode"))?.Text == "1";

                hf_SelectedFieldName.Value = txt_FieldName.Text;

                // Trigger the hidden button to show modal via client-side
                // Show modal via hidden button
                ShowFieldControlModal();

                //ScriptManager.RegisterStartupScript(this, this.GetType(), Guid.NewGuid().ToString(), "$('#FieldControlModal').modal('show');", true);
            }
        }

        protected void GV_Matrix_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GV_Matrix.EditIndex = e.NewEditIndex;
            BindFieldControlGrid(); // Rebind with edit row
        }

        protected void GV_Matrix_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GV_Matrix.EditIndex = -1;
            BindFieldControlGrid();
        }

        protected void GV_Matrix_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GV_Matrix.Rows[e.RowIndex];

            // Extract control values safely
            string fieldName = ((Label)row.FindControl("lbl_FieldName"))?.Text.Trim() ?? "";
            string displayName = ((TextBox)row.FindControl("txt_DisplayName"))?.Text.Trim() ?? "";
            string subForm = ((TextBox)row.FindControl("txtSubForm"))?.Text.Trim() ?? "";

            int rfvYesNo = ((CheckBox)row.FindControl("chk_RFV_YesNo"))?.Checked == true ? 1 : 0;
            string rfvError = ((TextBox)row.FindControl("txt_RFV_ErrorMsg"))?.Text.Trim() ?? "";

            int revYesNo = ((CheckBox)row.FindControl("chk_REV_YesNo"))?.Checked == true ? 1 : 0;
            string revError = ((TextBox)row.FindControl("txt_REV_ErrorMsg"))?.Text.Trim() ?? "";
            string revExpression = ((DropDownList)row.FindControl("ddl_REV_Expression"))?.SelectedValue?.Trim();

            int rvYesNo = ((CheckBox)row.FindControl("chk_RV_YesNo"))?.Checked == true ? 1 : 0;
            string rvError = ((TextBox)row.FindControl("txt_RV_ErrorMsg"))?.Text.Trim() ?? "";
            string rvMin = ((TextBox)row.FindControl("txt_RV_MinValue"))?.Text.Trim();
            string rvMax = ((TextBox)row.FindControl("txt_RV_MaxValue"))?.Text.Trim();

            int viewMode = ((CheckBox)row.FindControl("chk_ViewMode"))?.Checked == true ? 1 : 0;

            // Selected values from dropdowns outside GridView
            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            string formName = DDL_FormName.SelectedItem.Text;

            int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
            string plantName = DDL_Plant.SelectedItem.Text;

            int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);

            int category_id = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
            string category_name = DDL_ProductCategory.SelectedItem.Text;

            int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
            string materialName = DDL_ProductBrand.SelectedItem.Text;

            string user = Session["UserName"]?.ToString() ?? "system";
            string controltype = ((DropDownList)row.FindControl("ddl_ControlType"))?.SelectedValue?.Trim();
            int id = Convert.ToInt32(GV_Matrix.DataKeys[e.RowIndex].Values[0]);
            string defaultvalue = ((TextBox)row.FindControl("txtDefaultValue"))?.Text.Trim() ?? "";
            //bool isDuplicate = false;

            // Duplicate check using all required unique columns
            //using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            //{
            //    using (SqlCommand cmd = new SqlCommand(@"
            //    SELECT COUNT(*) FROM MST_FormFieldControls
            //    WHERE FormID = @FormID 
            //      AND field_name = @FieldName 
            //      AND plant_id = @PlantID 
            //      AND material_id = @MaterialID 
            //      AND LineId = @LineId
            //      AND Id <> @Id 
            //      AND IsDeleted = 0 
            //      AND IsLatest = 1", con))
            //    {
            //        cmd.Parameters.AddWithValue("@FormID", formId);
            //        cmd.Parameters.AddWithValue("@FieldName", fieldName);
            //        cmd.Parameters.AddWithValue("@PlantID", plantId);
            //        cmd.Parameters.AddWithValue("@MaterialID", materialId);
            //        cmd.Parameters.AddWithValue("@LineId", lineId);
            //        cmd.Parameters.AddWithValue("@Id", id);

            //        con.Open();
            //        int count = Convert.ToInt32(cmd.ExecuteScalar());
            //        isDuplicate = count > 0;
            //    }
            //}

            //if (isDuplicate)
            //{
            //    ScriptManager.RegisterStartupScript(this, this.GetType(), "dupAlert",
            //        "alert('A record with the same Form, Field Name, Plant, Line, and Brand already exists.');", true);
            //    return;
            //}

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_SaveOrUpdate_FormFieldControl", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@FormID", formId);
                        cmd.Parameters.AddWithValue("@FormName", formName);
                        cmd.Parameters.AddWithValue("@Sub_Form", string.IsNullOrWhiteSpace(subForm) ? DBNull.Value : (object)subForm);
                        cmd.Parameters.AddWithValue("@material_id", materialId);
                        cmd.Parameters.AddWithValue("@material_name", materialName);
                        cmd.Parameters.AddWithValue("@plant_id", plantId);
                        cmd.Parameters.AddWithValue("@plant_name", plantName);
                        cmd.Parameters.AddWithValue("@LineId", lineId);

                        cmd.Parameters.AddWithValue("@category_id", category_id);
                        cmd.Parameters.AddWithValue("@category_name", category_name);

                        cmd.Parameters.AddWithValue("@field_name", fieldName);
                        cmd.Parameters.AddWithValue("@DisplayName", displayName);

                        cmd.Parameters.AddWithValue("@RFV_YesNo", rfvYesNo);
                        cmd.Parameters.AddWithValue("@RFV_ErrorMsg", string.IsNullOrWhiteSpace(rfvError) ? DBNull.Value : (object)rfvError);

                        cmd.Parameters.AddWithValue("@REV_YesNo", revYesNo);
                        cmd.Parameters.AddWithValue("@REV_ErrorMsg", string.IsNullOrWhiteSpace(revError) ? DBNull.Value : (object)revError);
                        cmd.Parameters.AddWithValue("@REV_Expression", string.IsNullOrWhiteSpace(revExpression) ? DBNull.Value : (object)revExpression);

                        cmd.Parameters.AddWithValue("@RV_Yesno", rvYesNo);
                        cmd.Parameters.AddWithValue("@RV_ErrorMsg", string.IsNullOrWhiteSpace(rvError) ? DBNull.Value : (object)rvError);
                        cmd.Parameters.AddWithValue("@RV_MinValue", string.IsNullOrWhiteSpace(rvMin) ? DBNull.Value : (object)rvMin);
                        cmd.Parameters.AddWithValue("@RV_MaxValue", string.IsNullOrWhiteSpace(rvMax) ? DBNull.Value : (object)rvMax);

                        cmd.Parameters.AddWithValue("@ViewMode", viewMode);
                        cmd.Parameters.AddWithValue("@UserName", user);
                        cmd.Parameters.AddWithValue("@Remarks", "Updated inline from GridView");
                        cmd.Parameters.AddWithValue("@ControlType", controltype);
                        cmd.Parameters.AddWithValue("@DefaultValue", defaultvalue);

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        ScriptManager.RegisterStartupScript(this, this.GetType(), "updateSuccess",
                            "new PNotify({ title: 'Success', text: 'Field updated successfully.', type: 'success', styling: 'bootstrap3' });", true);
                    }
                }
            }
            catch (SqlException ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "sqlError",
                    $"alert('SQL Error: {ex.Message}');", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "error",
                    $"alert('Unexpected Error: {ex.Message}');", true);
            }

            GV_Matrix.EditIndex = -1;
            BindFieldControlGrid();
        }

        protected void GV_Matrix_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GV_Matrix.DataKeys[e.RowIndex].Value);
            string user = Session["UserName"]?.ToString() ?? "system";

            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE MST_FormFieldControls
                    SET 
                        IsDeleted = 1,
                        IsLatest = 0,
                        DeletedOn = GETDATE(),
                        DeletedBy = @User,
                        DeletedRemarks = 'Deleted from GridView'
                    WHERE Id = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@User", user);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ScriptManager.RegisterStartupScript(this, this.GetType(), "deleteNotify",
                    "new PNotify({ title: 'Deleted', text: 'Record soft-deleted.', type: 'info', styling: 'bootstrap3' });", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "error",
                    $"alert('Error deleting record: {ex.Message}');", true);
            }

            GV_Matrix.EditIndex = -1;
            BindFieldControlGrid(); // Rebind your GridView
        }


        protected void GV_Matrix_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Existing logic: setting selected value in dropdown during editing
            if (e.Row.RowType == DataControlRowType.DataRow && e.Row.RowIndex == GV_Matrix.EditIndex)
            {
                DropDownList ddl = (DropDownList)e.Row.FindControl("ddl_REV_Expression");
                string currentValue = DataBinder.Eval(e.Row.DataItem, "REV_Expression")?.ToString();
                if (ddl != null && !string.IsNullOrEmpty(currentValue))
                {
                    ListItem li = ddl.Items.FindByValue(currentValue);
                    if (li != null)
                        ddl.SelectedValue = currentValue;
                }

                DropDownList ddlControlType = (DropDownList)e.Row.FindControl("ddl_ControlType");
                string currentValue1 = DataBinder.Eval(e.Row.DataItem, "ControlType").ToString();

                if (ddlControlType != null)
                {
                    ddlControlType.SelectedValue = currentValue1;
                }
            }

            // Add confirmation prompt for delete
            //if (e.Row.RowType == DataControlRowType.DataRow)
            //{
            //    foreach (Control control in e.Row.Cells[e.Row.Cells.Count - 1].Controls)
            //    {
            //        if (control is LinkButton btnDelete && btnDelete.CommandName == "Delete")
            //        {
            //            btnDelete.OnClientClick = "return confirm('Are you sure you want to delete this record?');";
            //        }
            //    }
            //}
        }


        private void ShowFieldControlModal()
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowPopup",
                $"document.getElementById('{btnShowModalFieldControl.ClientID}').click();", true);
        }


        protected void btn_SaveFieldPopup_Click1(object sender, EventArgs e)
        {
            try
            {
                //int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                //string formName = DDL_FormName.SelectedItem.Text;

                //int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                //string plantName = DDL_Plant.SelectedItem.Text;

                //int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                //string lineName = DDL_PlantLine.SelectedItem.Text;

                //string productCategory = DDL_ProductCategory.SelectedItem.Text;
                //int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                //string materialName = DDL_ProductBrand.SelectedItem.Text;

                //string subForm = txt_SubForm.Text.Trim();
                //string fieldName = txt_FieldName.Text.Trim();
                //string displayName = txt_DisplayName.Text.Trim();

                //int rfvYesNo = chk_RFV.Checked ? 1 : 0;
                //string rfvError = txt_RFVError.Text.Trim();

                //int revYesNo = chk_REV.Checked ? 1 : 0;
                //string revError = txt_REVError.Text.Trim();
                //string revExpression = ddl_RegexType.SelectedValue;

                //int rvYesNo = chk_RV.Checked ? 1 : 0;
                //string rvError = txt_RVError.Text.Trim();
                //string rvMin = txt_RVMin.Text.Trim();
                //string rvMax = txt_RVMax.Text.Trim();

                //int viewMode = chk_ViewMode.Checked ? 1 : 0;

                //string user = Session["UserName"] != null ? Session["UserName"].ToString() : "system";

                int formId = !string.IsNullOrWhiteSpace(DDL_FormName.SelectedValue) ? Convert.ToInt32(DDL_FormName.SelectedValue) : 0;
                string formName = DDL_FormName.SelectedItem?.Text?.Trim() ?? "";

                int plantId = !string.IsNullOrWhiteSpace(DDL_Plant.SelectedValue) ? Convert.ToInt32(DDL_Plant.SelectedValue) : 0;
                string plantName = DDL_Plant.SelectedItem?.Text?.Trim() ?? "";

                int lineId = !string.IsNullOrWhiteSpace(DDL_PlantLine.SelectedValue) ? Convert.ToInt32(DDL_PlantLine.SelectedValue) : 0;
                string lineName = DDL_PlantLine.SelectedItem?.Text?.Trim() ?? "";

                int category_id = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                string category_name = DDL_ProductCategory.SelectedItem.Text;

                string productCategory = DDL_ProductCategory.SelectedItem?.Text?.Trim() ?? "";

                int materialId = !string.IsNullOrWhiteSpace(DDL_ProductBrand.SelectedValue) ? Convert.ToInt32(DDL_ProductBrand.SelectedValue) : 0;
                string materialName = DDL_ProductBrand.SelectedItem?.Text?.Trim() ?? "";

                string subForm = string.IsNullOrWhiteSpace(txt_SubForm.Text) ? null : txt_SubForm.Text.Trim();

                string fieldName = txt_FieldName.Text.Trim();
                string displayName = txt_DisplayName.Text.Trim();

                // Required Field Validation (RFV)
                int rfvYesNo = chk_RFV.Checked ? 1 : 0;
                string rfvError = string.IsNullOrWhiteSpace(txt_RFVError.Text) ? "" : txt_RFVError.Text.Trim();

                // Regular Expression Validation (REV)
                int revYesNo = chk_REV.Checked ? 1 : 0;
                string revError = string.IsNullOrWhiteSpace(txt_REVError.Text) ? "" : txt_REVError.Text.Trim();
                string revExpression = (ddl_RegexType.SelectedValue == "--Select Regex--" || string.IsNullOrWhiteSpace(ddl_RegexType.SelectedValue)) ? null : ddl_RegexType.SelectedValue.Trim();

                // Range Validation (RV)
                int rvYesNo = chk_RV.Checked ? 1 : 0;
                string rvError = string.IsNullOrWhiteSpace(txt_RVError.Text) ? "" : txt_RVError.Text.Trim();
                string rvMin = string.IsNullOrWhiteSpace(txt_RVMin.Text) ? null : txt_RVMin.Text.Trim();
                string rvMax = string.IsNullOrWhiteSpace(txt_RVMax.Text) ? null : txt_RVMax.Text.Trim();

                // View Mode
                int viewMode = chk_ViewMode.Checked ? 1 : 0;
                string controltype = (ddl_ControlType.SelectedValue == "--Select Regex--" || string.IsNullOrWhiteSpace(ddl_ControlType.SelectedValue)) ? null : ddl_ControlType.SelectedValue.Trim();

                string defaultValue = string.IsNullOrWhiteSpace(txt_DefaultValue.Text) ? null : txt_DefaultValue.Text.Trim();
                // User Identity
                string user = Session["USERNAME"] != null ? Session["USERNAME"].ToString() : "ADMIN";


                // DB insert/update call
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("usp_SaveOrUpdate_FormFieldControl", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FormID", formId);
                    cmd.Parameters.AddWithValue("@FormName", formName);
                    cmd.Parameters.AddWithValue("@Sub_Form", (object)subForm ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@plant_id", plantId);
                    cmd.Parameters.AddWithValue("@plant_name", plantName);
                    cmd.Parameters.AddWithValue("@LineId", lineId);
                    //cmd.Parameters.AddWithValue("@LineName", lineName);

                    cmd.Parameters.AddWithValue("@category_id", category_id);
                    cmd.Parameters.AddWithValue("@category_name", category_name);

                    cmd.Parameters.AddWithValue("@material_id", materialId);
                    cmd.Parameters.AddWithValue("@material_name", materialName);

                    cmd.Parameters.AddWithValue("@field_name", fieldName);
                    cmd.Parameters.AddWithValue("@DisplayName", displayName);

                    cmd.Parameters.AddWithValue("@RFV_YesNo", rfvYesNo);
                    cmd.Parameters.AddWithValue("@RFV_ErrorMsg", rfvError);

                    cmd.Parameters.AddWithValue("@REV_YesNo", revYesNo);
                    cmd.Parameters.AddWithValue("@REV_ErrorMsg", revError);
                    //cmd.Parameters.AddWithValue("@REV_Expression", revExpression);

                    cmd.Parameters.AddWithValue("@REV_Expression", (object)revExpression ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RV_MinValue", (object)rvMin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RV_MaxValue", (object)rvMax ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@RV_Yesno", rvYesNo);
                    cmd.Parameters.AddWithValue("@RV_ErrorMsg", rvError);
                    //cmd.Parameters.AddWithValue("@RV_MinValue", rvMin);
                    //cmd.Parameters.AddWithValue("@RV_MaxValue", rvMax);

                    cmd.Parameters.AddWithValue("@ViewMode", viewMode);
                    cmd.Parameters.AddWithValue("@UserName", user);
                    cmd.Parameters.AddWithValue("@Remarks", "Created/Updated from UI");
                    cmd.Parameters.AddWithValue("@ControlType", controltype);
                    cmd.Parameters.AddWithValue("@DefaultValue", defaultValue);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // Optional: show success message
                //ScriptManager.RegisterStartupScript(this, this.GetType(), "Success", "alert('Field control saved successfully.');", true);

                string title = "Notifications :";
                string body = "Data saved Successfully";
                ClientScript.RegisterStartupScript(this.GetType(), "Popup2", "ShowPopup('" + title + "', '" + body + "');", true);

                // Refresh your grid
                BindFieldControlGrid();

                // Close modal
                //ScriptManager.RegisterStartupScript(this, this.GetType(), "HideModal", "$('#modal_FieldControl').modal('hide');", true);
            }
            catch (Exception ex)
            {
                // Show error
                //ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"alert('Error: {ex.Message}');", true);

                string title = "Notifications :";
                string body = ex.Message.ToString();
                ClientScript.RegisterStartupScript(this.GetType(), "Popup3", "ShowPopup('" + title + "', '" + body + "');", true);
            }
        }

        protected void btn_SaveFieldPopup_Click(object sender, EventArgs e)
        {
            try
            {
                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                string formName = DDL_FormName.SelectedItem.Text;

                int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                string plantName = DDL_Plant.SelectedItem.Text;

                int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                string lineName = DDL_PlantLine.SelectedItem.Text;

                int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                string materialName = DDL_ProductBrand.SelectedItem.Text;

                int category_id = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                string category_name = DDL_ProductCategory.SelectedItem.Text;

                //string productCategory = DDL_ProductCategory.SelectedItem.Text;
                string subForm = txt_SubForm.Text.Trim(); // optional

                string fieldName = txt_FieldName.Text.Trim();
                string displayName = txt_DisplayName.Text.Trim();

                int rfvYesNo = chk_RFV.Checked ? 1 : 0;
                string rfvError = string.IsNullOrWhiteSpace(txt_RFVError.Text) ? "" : txt_RFVError.Text.Trim();

                int revYesNo = chk_REV.Checked ? 1 : 0;
                string revError = string.IsNullOrWhiteSpace(txt_REVError.Text) ? "" : txt_REVError.Text.Trim();
                string revExpression = (ddl_RegexType.SelectedValue == "--Select Regex--" || string.IsNullOrWhiteSpace(ddl_RegexType.SelectedValue)) ? null : ddl_RegexType.SelectedValue.Trim();

                int rvYesNo = chk_RV.Checked ? 1 : 0;
                string rvError = string.IsNullOrWhiteSpace(txt_RVError.Text) ? "" : txt_RVError.Text.Trim();
                string rvMin = string.IsNullOrWhiteSpace(txt_RVMin.Text) ? null : txt_RVMin.Text.Trim();
                string rvMax = string.IsNullOrWhiteSpace(txt_RVMax.Text) ? null : txt_RVMax.Text.Trim();

                int viewMode = chk_ViewMode.Checked ? 1 : 0;
                string controltype = (ddl_ControlType.SelectedValue == "--Select Regex--" || string.IsNullOrWhiteSpace(ddl_ControlType.SelectedValue)) ? null : ddl_ControlType.SelectedValue.Trim();
                string user = Session["UserName"]?.ToString() ?? "system";
                string defaultValue = string.IsNullOrWhiteSpace(txt_DefaultValue.Text) ? null : txt_DefaultValue.Text.Trim();
                // Check for duplicate record
                bool isDuplicate = false;
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM MST_FormFieldControls
                    WHERE FormID = @FormID 
                      AND plant_id = @PlantID 
                      AND LineID = @LineID 
                      AND material_id = @MaterialID
                      AND category_id = @CategoryID
                      AND field_name = @FieldName
                      AND IsDeleted = 0
                      AND IsLatest = 1", con))
                    {
                        cmd.Parameters.AddWithValue("@FormID", formId);
                        cmd.Parameters.AddWithValue("@PlantID", plantId);
                        cmd.Parameters.AddWithValue("@LineID", lineId);
                        cmd.Parameters.AddWithValue("@MaterialID", materialId);
                        cmd.Parameters.AddWithValue("@CategoryID", category_id);
                        cmd.Parameters.AddWithValue("@FieldName", fieldName);

                        con.Open();
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        isDuplicate = count > 0;
                    }
                }

                if (isDuplicate)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "dupNotify", @"
                    new PNotify({
                        title: 'Duplicate Found',
                        text: 'A field with the same name already exists for the selected Form, Plant, Line, and Brand.',
                        type: 'error',
                        styling: 'bootstrap3'
                    });", true);
                    return;
                }

                // Save to DB via SP
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_SaveOrUpdate_FormFieldControl", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@FormID", formId);
                        cmd.Parameters.AddWithValue("@FormName", formName);
                        cmd.Parameters.AddWithValue("@Sub_Form", (object)subForm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@material_id", materialId);
                        cmd.Parameters.AddWithValue("@material_name", materialName);
                        cmd.Parameters.AddWithValue("@plant_id", plantId);
                        cmd.Parameters.AddWithValue("@plant_name", plantName);
                        cmd.Parameters.AddWithValue("@LineId", lineId);

                        cmd.Parameters.AddWithValue("@category_id", category_id);
                        cmd.Parameters.AddWithValue("@category_name", category_name);

                        cmd.Parameters.AddWithValue("@field_name", fieldName);
                        cmd.Parameters.AddWithValue("@DisplayName", displayName);

                        cmd.Parameters.AddWithValue("@RFV_YesNo", rfvYesNo);
                        cmd.Parameters.AddWithValue("@RFV_ErrorMsg", rfvError);

                        cmd.Parameters.AddWithValue("@REV_YesNo", revYesNo);
                        cmd.Parameters.AddWithValue("@REV_ErrorMsg", revError);
                        cmd.Parameters.AddWithValue("@REV_Expression", string.IsNullOrWhiteSpace(revExpression) ? DBNull.Value : (object)revExpression);

                        cmd.Parameters.AddWithValue("@RV_Yesno", rvYesNo);
                        cmd.Parameters.AddWithValue("@RV_ErrorMsg", rvError);
                        cmd.Parameters.AddWithValue("@RV_MinValue", string.IsNullOrWhiteSpace(rvMin) ? DBNull.Value : (object)rvMin);
                        cmd.Parameters.AddWithValue("@RV_MaxValue", string.IsNullOrWhiteSpace(rvMax) ? DBNull.Value : (object)rvMax);

                        cmd.Parameters.AddWithValue("@ViewMode", viewMode);
                        cmd.Parameters.AddWithValue("@UserName", user);
                        cmd.Parameters.AddWithValue("@Remarks", "Created/Updated from UI");
                        cmd.Parameters.AddWithValue("@ControlType", controltype);
                        cmd.Parameters.AddWithValue("@DefaultValue", defaultValue);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ScriptManager.RegisterStartupScript(this, this.GetType(), "success", @"
                    new PNotify({
                        title: 'Success',
                        text: 'Field saved successfully.',
                        type: 'success',
                        styling: 'bootstrap3'
                    });", true);

                BindFieldControlGrid();
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "error", $@"
                    new PNotify({{
                        title: 'Error',
                        text: 'An error occurred: {ex.Message}',
                        type: 'error',
                        styling: 'bootstrap3'
                    }});", true);
            }
        }


        private void BindFieldControlGrid_OLD()
        {
            try
            {
                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT field_name, DisplayName, RFV_YesNo, RFV_ErrorMsg, REV_YesNo, REV_ErrorMsg, REV_Expression, RV_Yesno, RV_ErrorMsg, RV_MinValue, RV_MaxValue, ViewMode FROM MST_FormFieldControls WHERE FormID = @FormID AND plant_id = @PlantID AND material_id = @MaterialID AND IsLatest = 1 AND IsDeleted = 0", con))
                    {
                        cmd.Parameters.AddWithValue("@FormID", formId);
                        cmd.Parameters.AddWithValue("@PlantID", plantId);
                        cmd.Parameters.AddWithValue("@MaterialID", materialId);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                GV_Matrix.DataSource = dt;
                                GV_Matrix.DataBind();
                            }
                            else
                            {
                                dt.Rows.Add(dt.NewRow()); // for empty row
                                GV_Matrix.DataSource = dt;
                                GV_Matrix.DataBind();
                                GV_Matrix.Rows[0].Cells.Clear();
                                GV_Matrix.Rows[0].Cells.Add(new TableCell());
                                GV_Matrix.Rows[0].Cells[0].ColumnSpan = 12;
                                GV_Matrix.Rows[0].Cells[0].Text = "No field-level controls found.";
                                GV_Matrix.Rows[0].Cells[0].HorizontalAlign = HorizontalAlign.Center;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string title = "Notifications :";
                string body = ex.Message.ToString();
                ClientScript.RegisterStartupScript(this.GetType(), "Popup5", "ShowPopup('" + title + "', '" + body + "');", true);
            }
        }

        private void BindFieldControlGrid()
        {
            try
            {
                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT * FROM MST_FormFieldControls WHERE FormID = @FormID 
                      AND plant_id = @PlantID 
                      AND material_id = @MaterialID
                      AND category_id = @CategoryID 
                      AND IsLatest = 1 
                      AND IsDeleted = 0", con))
                    {
                        cmd.Parameters.AddWithValue("@FormID", formId);
                        cmd.Parameters.AddWithValue("@PlantID", plantId);
                        cmd.Parameters.AddWithValue("@MaterialID", materialId);
                        cmd.Parameters.AddWithValue("@CategoryID", categoryId);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                GV_Matrix.DataSource = dt;
                                GV_Matrix.DataBind();
                            }
                            else
                            {
                                dt.Rows.Add(dt.NewRow()); // Insert an empty row
                                GV_Matrix.DataSource = dt;
                                GV_Matrix.DataBind();

                                GV_Matrix.Rows[0].Cells.Clear();
                                TableCell cell = new TableCell
                                {
                                    ColumnSpan = GV_Matrix.Columns.Count,
                                    Text = "No field-level controls found.",
                                    HorizontalAlign = HorizontalAlign.Center
                                };
                                GV_Matrix.Rows[0].Cells.Add(cell);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string title = "Notifications :";
                string body = ex.Message.Replace("'", "\\'"); // prevent JS issues
                ClientScript.RegisterStartupScript(this.GetType(), "Popup5", $"ShowPopup('{title}', '{body}');", true);
            }
        }

        protected void BtnCreate_Click(object sender, EventArgs e)
        {
            //int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            //int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
            //int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
            //int brandId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
            //string subForm = txt_SubForm.Text.Trim(); // Optional
            //bool alreadyExists = false;

            //using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            //{
            //    using (SqlCommand cmd = new SqlCommand(@"
            //    SELECT COUNT(*) 
            //    FROM MST_FormFieldControls 
            //    WHERE FormID = @FormID 
            //      AND plant_id = @PlantID 
            //      AND material_id = @MaterialID 
            //      AND ISNULL(LineID, 0) = @LineID
            //      AND IsDeleted = 0
            //      AND IsLatest = 1", con))
            //    {
            //        cmd.Parameters.AddWithValue("@FormID", formId);
            //        cmd.Parameters.AddWithValue("@PlantID", plantId);
            //        cmd.Parameters.AddWithValue("@MaterialID", brandId);
            //        cmd.Parameters.AddWithValue("@LineID", lineId);
            //        //cmd.Parameters.AddWithValue("@SubForm", string.IsNullOrEmpty(subForm) ? "" : subForm);

            //        con.Open();
            //        int count = (int)cmd.ExecuteScalar();
            //        alreadyExists = count > 0;
            //    }
            //}

            //if (alreadyExists)
            //{
            //    // Show a message using JS or label
            //    string script = @"<script type='text/javascript'>
            //        new PNotify({
            //            title: 'Already Exists',
            //            text: 'Entry already exists for the selected combination!',
            //            type: 'error',
            //            styling: 'bootstrap3'
            //        });
            //    </script>";
            //    ClientScript.RegisterStartupScript(this.GetType(), "ExistNotify", script, false);
            //}
            //else
            //{
            //    // Clear previous modal fields
            //    ClearModalControls();

            //    // Show modal to create new entry
            //    ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowModal", "$('#modal_FieldControl').modal('show');", true);
            //}

            // Clear previous modal fields
            ClearModalControls();

            // Show modal to create new entry
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowModal", "$('#modal_FieldControl').modal('show');", true);
        }

        private void ClearModalControls()
        {
            txt_FieldName.Text = "";
            txt_DisplayName.Text = "";
            txt_RFVError.Text = "";
            chk_RFV.Checked = false;

            txt_REVError.Text = "";
            ddl_RegexType.ClearSelection();
            chk_REV.Checked = false;

            txt_RVError.Text = "";
            txt_RVMin.Text = "";
            txt_RVMax.Text = "";
            chk_RV.Checked = false;

            chk_ViewMode.Checked = false;
            hf_SelectedFieldName.Value = "";
        }

    }
}