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

                    hf_SelectedRecordType.Value = string.Empty;
                }

            }
        }

        protected void Btn_home_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.RawUrl); // reloads the same page
        }

        private void FormBinder()
        {
            // Construct the SQL query with parameters
            string query = "SELECT FormName, FormID FROM MST_FormsMaster ";
            string textField = "FormName"; // Assuming this is the correct field for displaying in the DropDownList
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
            hf_SelectedRecordType.Value = "Specific";
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
            
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
            //BindFieldControlGrid(); // Rebind with edit row
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
        }

        protected void GV_Matrix_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GV_Matrix.EditIndex = -1;
            //BindFieldControlGrid(); // Rebind with edit row
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
        }

        protected void GV_Matrix_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GV_Matrix.Rows[e.RowIndex];

            string recordType = ((Label)row.FindControl("lbl_RecordType"))?.Text.Trim().ToUpper(); // e.g., "FALLBACK" or "SPECIFIC"

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

            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            string formName = DDL_FormName.SelectedItem.Text;

            string user = Session["UserName"]?.ToString() ?? "system";
            string controltype = ((DropDownList)row.FindControl("ddl_ControlType"))?.SelectedValue?.Trim();
            int id = Convert.ToInt32(GV_Matrix.DataKeys[e.RowIndex].Values[0]);
            string defaultvalue = ((TextBox)row.FindControl("txtDefaultValue"))?.Text.Trim() ?? "";

            //int? plantId = null, lineId = null, categoryId = null, materialId = null;
            string plantName = null, categoryName = null, materialName = null;
            string recordTypeGV = GV_Matrix.DataKeys[e.RowIndex].Values["RecordType"].ToString();

            int plantId = 0, lineId = 0, categoryId = 0, materialId = 0;

            // Try parsing dropdown values
            int.TryParse(DDL_Plant.SelectedValue, out plantId);
            int.TryParse(DDL_PlantLine.SelectedValue, out lineId);
            int.TryParse(DDL_ProductCategory.SelectedValue, out categoryId);
            int.TryParse(DDL_ProductBrand.SelectedValue, out materialId);

            // Nullables to hold final values to be saved
            int? finalPlantId = null;
            int? finalLineId = null;
            int? finalCategoryId = null;
            int? finalMaterialId = null;

            // Check if values are meaningful (not 0 or 1)
            if (plantId > 0)
            {
                finalPlantId = plantId;
                plantName = DDL_Plant.SelectedItem.Text;
            }

            if (lineId > 0)
            {
                finalLineId = lineId;
            }

            if (categoryId > 0)
            {
                finalCategoryId = categoryId;
                categoryName = DDL_ProductCategory.SelectedItem.Text;
            }

            if (materialId > 0)
            {
                finalMaterialId = materialId;
                materialName = DDL_ProductBrand.SelectedItem.Text;
            }

            if (recordType == "SPECIFIC")
            {
                plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                plantName = DDL_Plant.SelectedItem.Text;

                lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);

                categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                categoryName = DDL_ProductCategory.SelectedItem.Text;

                materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                materialName = DDL_ProductBrand.SelectedItem.Text;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_SaveOrUpdate_FormFieldControl", conn))
                    {
                        
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@FormID", formId);//1
                        cmd.Parameters.AddWithValue("@FormName", formName);//2
                        cmd.Parameters.AddWithValue("@Sub_Form", string.IsNullOrWhiteSpace(subForm) ? DBNull.Value : (object)subForm);//3

                        cmd.Parameters.AddWithValue("@plant_id", finalPlantId.HasValue ? (object)plantId : DBNull.Value);//4
                        cmd.Parameters.AddWithValue("@plant_name", string.IsNullOrWhiteSpace(plantName) ? DBNull.Value : (object)plantName);//5

                        cmd.Parameters.AddWithValue("@LineId", finalLineId.HasValue ? (object)lineId : DBNull.Value);//6

                        cmd.Parameters.AddWithValue("@category_id", finalCategoryId.HasValue ? (object)categoryId : DBNull.Value);//7
                        cmd.Parameters.AddWithValue("@category_name", string.IsNullOrWhiteSpace(categoryName) ? DBNull.Value : (object)categoryName);//8

                        cmd.Parameters.AddWithValue("@material_id", finalMaterialId.HasValue ? (object)materialId : DBNull.Value);//9
                        cmd.Parameters.AddWithValue("@material_name", string.IsNullOrWhiteSpace(materialName) ? DBNull.Value : (object)materialName);//10

                        cmd.Parameters.AddWithValue("@field_name", fieldName);//11
                        cmd.Parameters.AddWithValue("@DisplayName", displayName);//12

                        cmd.Parameters.AddWithValue("@RFV_YesNo", rfvYesNo);//13
                        cmd.Parameters.AddWithValue("@RFV_ErrorMsg", string.IsNullOrWhiteSpace(rfvError) ? DBNull.Value : (object)rfvError);//14

                        cmd.Parameters.AddWithValue("@REV_YesNo", revYesNo);//15
                        cmd.Parameters.AddWithValue("@REV_ErrorMsg", string.IsNullOrWhiteSpace(revError) ? DBNull.Value : (object)revError);//16
                        cmd.Parameters.AddWithValue("@REV_Expression", string.IsNullOrWhiteSpace(revExpression) ? DBNull.Value : (object)revExpression);//17

                        cmd.Parameters.AddWithValue("@RV_Yesno", rvYesNo);//18
                        cmd.Parameters.AddWithValue("@RV_ErrorMsg", string.IsNullOrWhiteSpace(rvError) ? DBNull.Value : (object)rvError);//19
                        cmd.Parameters.AddWithValue("@RV_MinValue", string.IsNullOrWhiteSpace(rvMin) ? DBNull.Value : (object)rvMin);//20
                        cmd.Parameters.AddWithValue("@RV_MaxValue", string.IsNullOrWhiteSpace(rvMax) ? DBNull.Value : (object)rvMax);//21

                        cmd.Parameters.AddWithValue("@ViewMode", viewMode);//22
                        //cmd.Parameters.AddWithValue("@UserName", user);
                        //cmd.Parameters.AddWithValue("@Remarks", $"Updated from GridView ({recordType})");

                        cmd.Parameters.AddWithValue("@UserName", user);//23
                        cmd.Parameters.AddWithValue("@Remarks", $"Updated from GridView ({recordType})");//24

                        cmd.Parameters.AddWithValue("@ControlType", controltype);//25
                        cmd.Parameters.AddWithValue("@DefaultValue", defaultvalue);//26
                        cmd.Parameters.AddWithValue("@RecordType", recordTypeGV); // Pass SPECIFIC or FALLBACK 27

                        SqlParameter outParam = new SqlParameter("@ResultMessage", SqlDbType.NVarChar, 500); //28

                        outParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(outParam);

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        string resultMessage = outParam.Value.ToString();

                        ScriptManager.RegisterStartupScript(this, this.GetType(), "spMessage",
                            $"alert('{resultMessage.Replace("'", "\\'")}');", true);

                        //ScriptManager.RegisterStartupScript(this, this.GetType(), "updateSuccess", "new PNotify({ title: 'Success', text: 'Field updated successfully.', type: 'success', styling: 'bootstrap3' });", true);
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
            //BindFieldControlGrid(); // Rebind with edit row
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
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
                    UPDATE MST_FormFieldControls SET 
                        IsDeleted = 1,
                        IsLatest = 0,
                        DeletedOn = GETDATE(),
                        DeletedBy = @User,
                        ViewMode = 0,
                        DeletedRemarks = 'Deleted from GridView by the User'
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
            //BindFieldControlGrid(); // Rebind with edit row
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
        }

        protected void GV_Matrix_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Highlight fallback records visually
            //if (e.Row.RowType == DataControlRowType.DataRow)
            //{
            //    // Check if fallback (i.e., all foreign keys are 0 or null)
            //    int plantId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "plant_id") ?? 0);
            //    int lineId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "lineId") ?? 0);
            //    int materialId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "material_id") ?? 0);
            //    int categoryId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "category_id") ?? 0);

            //    bool isFallback = (plantId == 0 && lineId == 0 && materialId == 0 && categoryId == 0);

            //    if (isFallback)
            //    {
            //        e.Row.BackColor = System.Drawing.Color.LightGoldenrodYellow; // subtle fallback highlight

            //        // Optional: Add a fallback label in the first column
            //        if (e.Row.Cells.Count > 0)
            //        {
            //            e.Row.Cells[0].Text = "⚠ Fallback - " + e.Row.Cells[0].Text;
            //        }
            //    }
            //}

            // Existing logic for dropdowns in Edit mode
            if (e.Row.RowType == DataControlRowType.DataRow && e.Row.RowIndex == GV_Matrix.EditIndex)
            {
                // REV_Expression dropdown
                DropDownList ddl = (DropDownList)e.Row.FindControl("ddl_REV_Expression");
                string currentValue = DataBinder.Eval(e.Row.DataItem, "REV_Expression")?.ToString();
                if (ddl != null && !string.IsNullOrEmpty(currentValue))
                {
                    ListItem li = ddl.Items.FindByValue(currentValue);
                    if (li != null)
                        ddl.SelectedValue = currentValue;
                }

                // ControlType dropdown
                DropDownList ddlControlType = (DropDownList)e.Row.FindControl("ddl_ControlType");
                string currentValue1 = DataBinder.Eval(e.Row.DataItem, "ControlType")?.ToString();

                if (ddlControlType != null && !string.IsNullOrEmpty(currentValue1))
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

        protected void btn_SaveFieldPopup_Click(object sender, EventArgs e)
        {
            try
            {
                string recordType = ddl_RecordType.SelectedValue;

                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                string formName = DDL_FormName.SelectedItem.Text;

                //int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                //string plantName = DDL_Plant.SelectedItem.Text;

                //int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                //string lineName = DDL_PlantLine.SelectedItem.Text;

                //int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                //string materialName = DDL_ProductBrand.SelectedItem.Text;

                //int category_id = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                //string category_name = DDL_ProductCategory.SelectedItem.Text;

                int plantId = string.IsNullOrEmpty(DDL_Plant.SelectedValue) ? 0 : Convert.ToInt32(DDL_Plant.SelectedValue);
                string plantName = DDL_Plant.SelectedItem?.Text ?? "";

                int lineId = string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) ? 0 : Convert.ToInt32(DDL_PlantLine.SelectedValue);
                string lineName = DDL_PlantLine.SelectedItem?.Text ?? "";

                int materialId = string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) ? 0 : Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                string materialName = DDL_ProductBrand.SelectedItem?.Text ?? "";

                int category_id = string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) ? 0 : Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                string category_name = DDL_ProductCategory.SelectedItem?.Text ?? "";

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
                    SELECT COUNT(*) FROM MST_FormFieldControls WHERE FormID = @FormID 
                      AND plant_id = @PlantID 
                      AND LineID = @LineID 
                      AND material_id = @MaterialID
                      AND category_id = @CategoryID
                      AND field_name = @FieldName
                      AND IsDeleted = 0
                      AND IsLatest = 1
                      AND RecordType = @RecordType", con))
                    {
                        cmd.Parameters.AddWithValue("@FormID", formId);
                        //cmd.Parameters.AddWithValue("@PlantID", plantId);
                        //cmd.Parameters.AddWithValue("@LineID", lineId);
                        //cmd.Parameters.AddWithValue("@MaterialID", materialId);
                        //cmd.Parameters.AddWithValue("@CategoryID", category_id);
                        cmd.Parameters.AddWithValue("@plant_id", plantId == 0 ? DBNull.Value : (object)plantId);
                        cmd.Parameters.AddWithValue("@plant_name", string.IsNullOrWhiteSpace(plantName) ? DBNull.Value : (object)plantName);
                        cmd.Parameters.AddWithValue("@LineId", lineId == 0 ? DBNull.Value : (object)lineId);
                        cmd.Parameters.AddWithValue("@material_id", materialId == 0 ? DBNull.Value : (object)materialId);
                        cmd.Parameters.AddWithValue("@material_name", string.IsNullOrWhiteSpace(materialName) ? DBNull.Value : (object)materialName);
                        cmd.Parameters.AddWithValue("@category_id", category_id == 0 ? DBNull.Value : (object)category_id);
                        cmd.Parameters.AddWithValue("@category_name", string.IsNullOrWhiteSpace(category_name) ? DBNull.Value : (object)category_name);
                        cmd.Parameters.AddWithValue("@FieldName", fieldName);
                        cmd.Parameters.AddWithValue("@RecordType", recordType);

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
                        //cmd.Parameters.AddWithValue("@material_id", materialId);
                        //cmd.Parameters.AddWithValue("@material_name", materialName);
                        //cmd.Parameters.AddWithValue("@plant_id", plantId);
                        //cmd.Parameters.AddWithValue("@plant_name", plantName);
                        //cmd.Parameters.AddWithValue("@LineId", lineId);

                        //cmd.Parameters.AddWithValue("@category_id", category_id);
                        //cmd.Parameters.AddWithValue("@category_name", category_name);

                        cmd.Parameters.AddWithValue("@plant_id", plantId == 0 ? DBNull.Value : (object)plantId);
                        cmd.Parameters.AddWithValue("@plant_name", string.IsNullOrWhiteSpace(plantName) ? DBNull.Value : (object)plantName);
                        cmd.Parameters.AddWithValue("@LineId", lineId == 0 ? DBNull.Value : (object)lineId);
                        cmd.Parameters.AddWithValue("@material_id", materialId == 0 ? DBNull.Value : (object)materialId);
                        cmd.Parameters.AddWithValue("@material_name", string.IsNullOrWhiteSpace(materialName) ? DBNull.Value : (object)materialName);
                        cmd.Parameters.AddWithValue("@category_id", category_id == 0 ? DBNull.Value : (object)category_id);
                        cmd.Parameters.AddWithValue("@category_name", string.IsNullOrWhiteSpace(category_name) ? DBNull.Value : (object)category_name);

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
                        cmd.Parameters.AddWithValue("@RecordType", recordType);

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

                //BindFieldControlGrid(); // Rebind with edit row
                if (hf_SelectedRecordType.Value.ToString() == "Specific")
                {
                    BindFieldControlGrid();
                }
                else
                {
                    BindFieldControlGrid(isFallback: true);
                }
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
            lbl_docname.Text = "You are viewing Specific/ Standard Form Field Controler for : " + DDL_FormName.SelectedItem.Text.ToString() + ".";

            try
            {
                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
                int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                lbl_DDL_ProductBrand_Value.Text = materialId.ToString();

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    //using (SqlCommand cmd = new SqlCommand(@"SELECT * FROM MST_FormFieldControls WHERE FormID = @FormID AND plant_id = @PlantID AND material_id = @MaterialID AND category_id = @CategoryID AND IsLatest = 1 AND IsDeleted = 0", con))
                    using (SqlCommand cmd = new SqlCommand(@"
                            SELECT * FROM MST_FormFieldControls 
                            WHERE FormID = @FormID 
                              AND IsLatest = 1 
                              AND IsDeleted = 0 
                              AND (
                                    (RecordType = 'Specific' AND plant_id = @PlantID AND material_id = @MaterialID AND category_id = @CategoryID)
                                  )
                        ", con))

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

        protected void Btn_fallback_Click(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0 && DDL_ProductBrand.SelectedIndex != -1)
            {
                int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
                lbl_DDL_ProductBrand_Value.Text = materialId.ToString();
            }
            else
            {
                lbl_DDL_ProductBrand_Value.Text = "N/A";
            }
            hf_SelectedRecordType.Value = "Fallback";
            if (hf_SelectedRecordType.Value.ToString() == "Specific")
            {
                BindFieldControlGrid();
            }
            else
            {
                BindFieldControlGrid(isFallback: true);
            }
            //BindFieldControlGrid(isFallback: true);
            lbl_docname.Text = "You are viewing Fallback Form Field Controler for : " +DDL_FormName.SelectedItem.Text.ToString()+".";
        }

        private void BindFieldControlGrid(bool isFallback = false)
        {
            //int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
            //int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
            //int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
            //int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);

            int plantId = 0, lineId = 0, categoryId = 0, materialId = 0;

            // Try parsing dropdown values
            int.TryParse(DDL_Plant.SelectedValue, out plantId);
            int.TryParse(DDL_PlantLine.SelectedValue, out lineId);
            int.TryParse(DDL_ProductCategory.SelectedValue, out categoryId);
            int.TryParse(DDL_ProductBrand.SelectedValue, out materialId);

            // Nullables to hold final values to be saved
            int? finalPlantId = null;
            int? finalLineId = null;
            int? finalCategoryId = null;
            int? finalMaterialId = null;

            // Check if values are meaningful (not 0 or 1)
            if (plantId > 1)
                finalPlantId = plantId;

            if (lineId > 1)
                finalLineId = lineId;

            if (categoryId > 1)
                finalCategoryId = categoryId;

            if (materialId > 1)
                finalMaterialId = materialId;

            // Now use finalPlantId, finalLineId, etc. in your logic or DB update


            try
            {
                int formId = Convert.ToInt32(DDL_FormName.SelectedValue);

                //string query = @"SELECT id, field_name, Sub_Form, DisplayName, RFV_YesNo, RFV_ErrorMsg, REV_YesNo, REV_ErrorMsg, REV_Expression, RV_Yesno, RV_ErrorMsg, RV_MinValue, RV_MaxValue, ViewMode, ControlType, DefaultValue FROM MST_FormFieldControls WHERE FormID = @FormID AND ISNULL(IsLatest, 1) = 1 AND ISNULL(IsDeleted, 0) = 0";

                //string query = @"SELECT id, field_name, Sub_Form, DisplayName, RFV_YesNo, RFV_ErrorMsg, REV_YesNo, REV_ErrorMsg, REV_Expression, RV_Yesno, RV_ErrorMsg, RV_MinValue, RV_MaxValue, ViewMode, ControlType, DefaultValue, RecordType FROM MST_FormFieldControls WHERE FormID = @FormID AND ISNULL(IsLatest, 1) = 1";

                //if (!isFallback)
                //{
                //    //query += @" AND plant_id = @PlantID AND lineId = @LineID AND material_id = @MaterialID AND category_id = @CategoryID";
                //    //query += @" AND plant_id = @PlantID AND lineId = @LineID AND material_id = @MaterialID AND category_id = @CategoryID";
                //    // Specific match - all fields must match
                //    query += @" AND RecordType = 'Specific'
                //                AND plant_id = @PlantID
                //                AND lineId = @LineID
                //                AND material_id = @MaterialID
                //                AND category_id = @CategoryID";
                //}
                //else
                //{
                //    //query += @" AND ISNULL(plant_id, 0) = 0 
                //    //    AND ISNULL(lineId, 0) = 0 
                //    //    AND ISNULL(material_id, 0) = 0 
                //    //    AND ISNULL(category_id, 0) = 0";


                //    // Fallback match - only one or more fields may match
                //    query += @" AND RecordType = 'Fallback'
                //            AND (
                //                (plant_id IS NULL OR plant_id = @PlantID)
                //                OR (lineId IS NULL OR lineId = @LineID)
                //                OR (material_id IS NULL OR material_id = @MaterialID)
                //                OR (category_id IS NULL OR category_id = @CategoryID)
                //            )";
                //}


                string query = @"SELECT id, field_name, Sub_Form, DisplayName, RFV_YesNo, RFV_ErrorMsg, 
                    REV_YesNo, REV_ErrorMsg, REV_Expression, RV_Yesno, RV_ErrorMsg, 
                    RV_MinValue, RV_MaxValue, ViewMode, ControlType, DefaultValue, RecordType 
                 FROM MST_FormFieldControls 
                 WHERE FormID = @FormID AND ISNULL(IsLatest, 1) = 1 AND RecordType = 'Fallback'";

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@FormID", formId)); // Always mandatory

                if (finalPlantId > 0)
                {
                    query += " AND (plant_id = @PlantID OR plant_id IS NULL)";
                    sqlParams.Add(new SqlParameter("@PlantID", finalPlantId));
                }
                if (finalLineId > 0)
                {
                    query += " AND (lineId = @LineID OR lineId IS NULL)";
                    sqlParams.Add(new SqlParameter("@LineID", finalLineId));
                }
                if (finalMaterialId > 0)
                {
                    query += " AND (material_id = @MaterialID OR material_id IS NULL)";
                    sqlParams.Add(new SqlParameter("@MaterialID", finalMaterialId));
                }
                if (finalCategoryId > 0)
                {
                    query += " AND (category_id = @CategoryID OR category_id IS NULL)";
                    sqlParams.Add(new SqlParameter("@CategoryID", finalCategoryId));
                }


                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    //cmd.Parameters.AddWithValue("@FormID", formId);
                    ////cmd.Parameters.AddWithValue("@RecordType", isFallback ? "Fallback" : "Specific");
                    //if (!isFallback)
                    //{
                    //    cmd.Parameters.AddWithValue("@PlantID", Convert.ToInt32(DDL_Plant.SelectedValue));
                    //    cmd.Parameters.AddWithValue("@LineID", Convert.ToInt32(DDL_PlantLine.SelectedValue));
                    //    cmd.Parameters.AddWithValue("@MaterialID", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                    //    cmd.Parameters.AddWithValue("@CategoryID", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                    //}
                    cmd.Parameters.AddWithValue("@FormID", formId);
                    //cmd.Parameters.AddWithValue("@PlantID", Convert.ToInt32(DDL_Plant.SelectedValue));
                    //cmd.Parameters.AddWithValue("@LineID", Convert.ToInt32(DDL_PlantLine.SelectedValue));
                    //cmd.Parameters.AddWithValue("@MaterialID", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                    //cmd.Parameters.AddWithValue("@CategoryID", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                    // Add parameters with DBNull if value is null
                    cmd.Parameters.AddWithValue("@PlantID", finalPlantId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@LineID", finalLineId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaterialID", finalMaterialId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryID", finalCategoryId ?? (object)DBNull.Value);

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
                            dt.Rows.Add(dt.NewRow());
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
            catch (Exception ex)
            {
                string title = "Notifications :";
                string body = ex.Message.Replace("'", "\\'");
                ClientScript.RegisterStartupScript(this.GetType(), "Popup5", $"ShowPopup('{title}', '{body}');", true);
            }
        }

        protected void btn_CreateFallback_Click(object sender, EventArgs e)
        {

        }

        protected void BTN_CopySpecificToFallback_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_CopySpecificToFallback", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Required Source Filter
                        cmd.Parameters.AddWithValue("@Source_FormID", Convert.ToInt32(DDL_FormName.SelectedValue));

                        // Optional Source Filters
                        cmd.Parameters.AddWithValue("@Source_plant_id", GetNullableInt(DDL_Plant));
                        cmd.Parameters.AddWithValue("@Source_material_id", GetNullableInt(DDL_ProductBrand));
                        cmd.Parameters.AddWithValue("@Source_category_id", GetNullableInt(DDL_ProductCategory));
                        cmd.Parameters.AddWithValue("@Source_LineId", GetNullableInt(DDL_PlantLine));

                        // Target
                        cmd.Parameters.AddWithValue("@Target_plant_id", Convert.ToInt32(DDL_Plant.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_material_id", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_category_id", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_LineId", Convert.ToInt32(DDL_PlantLine.SelectedValue));

                        cmd.Parameters.AddWithValue("@CreatedBy", Session["username"].ToString());

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        ShowPopupNew("Copy Successful", $"{rowsAffected} record(s) copied to Fallback.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPopupNew("Error", ex.Message.Replace("'", "\\'"));
            }
        }

        private object GetNullableInt(DropDownList ddl)
        {
            return string.IsNullOrEmpty(ddl.SelectedValue) || ddl.SelectedValue == "0"
                ? (object)DBNull.Value
                : Convert.ToInt32(ddl.SelectedValue);
        }


        protected void BTN_CopySpecificToFallback_Click_OLD2(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_CopySpecificToFallback", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Source_FormID", Convert.ToInt32(DDL_FormName.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_plant_id", Convert.ToInt32(DDL_Plant.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_material_id", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_category_id", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_LineId", Convert.ToInt32(DDL_PlantLine.SelectedValue));
                        cmd.Parameters.AddWithValue("@CreatedBy", Session["username"].ToString());

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        ShowPopupNew("Copy Successful", $"{rowsAffected} Specific record(s) copied to Fallback.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPopupNew("Error", ex.Message.Replace("'", "\\'"));
            }
        }

        protected void BTN_CopySpecificToFallback_Click_OLD1(object sender, EventArgs e)
        {
            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            int? plantId = string.IsNullOrEmpty(DDL_Plant.SelectedValue) ? (int?)null : Convert.ToInt32(DDL_Plant.SelectedValue);
            int? lineId = string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) ? (int?)null : Convert.ToInt32(DDL_PlantLine.SelectedValue);
            int? materialId = string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) ? (int?)null : Convert.ToInt32(DDL_ProductBrand.SelectedValue);
            int? categoryId = string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) ? (int?)null : Convert.ToInt32(DDL_ProductCategory.SelectedValue);

            try
            {
                CopySpecificToFallback(formId, plantId, lineId, materialId, categoryId);
                //ShowSuccess("Specific controls successfully copied to Fallback!");

                ScriptManager.RegisterStartupScript(this, this.GetType(), "success", @"
                    new PNotify({
                        title: 'Success',
                        text: 'Specific controls successfully copied to Fallback!',
                        type: 'success',
                        styling: 'bootstrap3'
                    });", true);

                BindFieldControlGrid(); // Optional refresh
            }
            catch (Exception ex)
            {
                string title = "Notifications :";
                string body = ex.Message.Replace("'", "\\'"); // prevent JS issues
                ClientScript.RegisterStartupScript(this.GetType(), "Popup55", $"ShowPopup('{title}', '{body}');", true);
                //ShowError("Error copying to Fallback: " + ex.Message);
            }
        }

        public void CopySpecificToFallback(int formId, int? plantId, int? lineId, int? materialId, int? categoryId)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand("usp_CopySpecificToFallback", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FormID", formId);
                cmd.Parameters.AddWithValue("@PlantID", (object)plantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LineID", (object)lineId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaterialID", (object)materialId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoryID", (object)categoryId ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        protected void BTN_CopyFallbackToSpecific_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_CopyFallbackToSpecific", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Required Source
                        cmd.Parameters.AddWithValue("@Source_FormID", Convert.ToInt32(DDL_FormName.SelectedValue));

                        // Optional Source Filters
                        cmd.Parameters.AddWithValue("@Source_plant_id", GetNullableInt(DDL_Plant));
                        cmd.Parameters.AddWithValue("@Source_material_id", GetNullableInt(DDL_ProductBrand));
                        cmd.Parameters.AddWithValue("@Source_category_id", GetNullableInt(DDL_ProductCategory));
                        cmd.Parameters.AddWithValue("@Source_LineId", GetNullableInt(DDL_PlantLine));

                        // Target
                        cmd.Parameters.AddWithValue("@Target_plant_id", Convert.ToInt32(DDL_Plant.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_material_id", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_category_id", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_LineId", Convert.ToInt32(DDL_PlantLine.SelectedValue));

                        // Meta
                        cmd.Parameters.AddWithValue("@CreatedBy", Session["username"].ToString());

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        ShowPopupNew("Copy Successful", $"{rowsAffected} Fallback record(s) copied to Specific.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPopupNew("Error", ex.Message.Replace("'", "\\'"));
            }
        }


        protected void BTN_CopyFallbackToSpecific_Click_OLD2(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_CopyFallbackToSpecific", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Source_FormID", Convert.ToInt32(DDL_FormName.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_plant_id", Convert.ToInt32(DDL_Plant.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_material_id", Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_category_id", Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                        cmd.Parameters.AddWithValue("@Target_LineId", Convert.ToInt32(DDL_PlantLine.SelectedValue));
                        cmd.Parameters.AddWithValue("@CreatedBy", Session["username"].ToString());

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        ShowPopupNew("Copy Successful", $"{rowsAffected} Fallback record(s) copied to Specific.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowPopupNew("Error", ex.Message.Replace("'", "\\'"));
            }
        }

        protected void BTN_CopyFallbackToSpecific_Click_OLD1(object sender, EventArgs e)
        {
            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
            int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
            int materialId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);
            int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);

            try
            {
                CopyFallbackToSpecific(formId, plantId, lineId, materialId, categoryId);
                ShowSuccess("Fallback controls successfully copied to Specific!");
                BindFieldControlGrid(); // Optional refresh
            }
            catch (Exception ex)
            {
                ShowError("Error copying to Specific: " + ex.Message);
            }
        }

        public void CopyFallbackToSpecific(int formId, int plantId, int lineId, int materialId, int categoryId)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand("usp_CopyFallbackToSpecific", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FormID", formId);
                cmd.Parameters.AddWithValue("@PlantID", plantId);
                cmd.Parameters.AddWithValue("@LineID", lineId);
                cmd.Parameters.AddWithValue("@MaterialID", materialId);
                cmd.Parameters.AddWithValue("@CategoryID", categoryId);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }


        private void ShowSuccess(string message)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "popup", $"alert('{message}');", true);
        }

        private void ShowError(string message)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "popup", $"alert('{message.Replace("'", "\\'")}');", true);
        }

        private void ShowPopupNew(string title, string message)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "Popup", $"ShowPopup('{title}', '{message}');", true);
        }


    }
}