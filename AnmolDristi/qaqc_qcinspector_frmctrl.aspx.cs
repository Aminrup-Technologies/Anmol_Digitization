using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class qaqc_qcinspector_frmctrl : System.Web.UI.Page
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
                    PlantBinder();
                }
            }
        }

        public void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            string textField = "plant_name";
            string valueField = "plant_id";

            bool recordsBound;

            // Bind the DropDownList and get the flag indicating whether records were bound
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
                string PlantBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);

            }
        }

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                PlantLinesBinder(selectedPlantValue);
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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_Plant_Error_script, false);
            }
        }

        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
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


        private void BindGrid()
        {
            string brand_id = DDL_ProductBrand.SelectedValue.ToString();
            lbl_DDL_ProductBrand_Value.Text = brand_id;
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("GetBrandFieldsControl", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@brand_id", brand_id);
                    conn.Open();
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    GridView1.DataSource = dt;
                    GridView1.DataBind();
                }
            }
        }

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView1.EditIndex = e.NewEditIndex;
            BindGrid();
        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridView1.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Values[0]);
            string displayname = (row.FindControl("txtDisplayName") as TextBox)?.Text;
            // Fetching checkbox values and converting them to 0 or 1
            int viewMode = (row.FindControl("ViewMode") as CheckBox)?.Checked == true ? 1 : 0;
            int deleteMode = (row.FindControl("DeleteMode") as CheckBox)?.Checked == true ? 1 : 0;
            int rfvYesNo = (row.FindControl("RFV_YesNo") as CheckBox)?.Checked == true ? 1 : 0;
            string rfvErrorMsg = (row.FindControl("txtRFVErrorMsg") as TextBox)?.Text;
            int revYesNo = (row.FindControl("REV_YesNo") as CheckBox)?.Checked == true ? 1 : 0;
            string revErrorMsg = (row.FindControl("txtREVErrorMsg") as TextBox)?.Text;
            string revExpression = (row.FindControl("txtREVExpression") as TextBox)?.Text;
            int rvYesNo = (row.FindControl("RV_Yesno") as CheckBox)?.Checked == true ? 1 : 0;
            string rvErrorMsg = (row.FindControl("txtRVErrorMsg") as TextBox)?.Text;
            string rvMinValue = (row.FindControl("txtRVMinValue") as TextBox)?.Text;
            string rvMaxValue = (row.FindControl("txtRVMaxValue") as TextBox)?.Text;
            string modifiedRemarks = "Modified by "+ Session["USERID"].ToString() + " from UI/UX";

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("UpdateBrandFieldsControl", conn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@DisplayName", displayname);
                        cmd.Parameters.AddWithValue("@RFV_YesNo", rfvYesNo);
                        cmd.Parameters.AddWithValue("@RFV_ErrorMsg", rfvErrorMsg);
                        cmd.Parameters.AddWithValue("@REV_YesNo", revYesNo);
                        cmd.Parameters.AddWithValue("@REV_ErrorMsg", revErrorMsg);
                        cmd.Parameters.AddWithValue("@REV_Expression", revExpression);
                        cmd.Parameters.AddWithValue("@RV_Yesno", rvYesNo);
                        cmd.Parameters.AddWithValue("@RV_ErrorMsg", rvErrorMsg);
                        cmd.Parameters.AddWithValue("@RV_MinValue", rvMinValue);
                        cmd.Parameters.AddWithValue("@RV_MaxValue", rvMaxValue);
                        cmd.Parameters.AddWithValue("@ViewMode", viewMode);
                        cmd.Parameters.AddWithValue("@DeleteMode", deleteMode);
                        cmd.Parameters.AddWithValue("@ModifiedOn", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@ModifiedBy", Session["USERID"].ToString());
                        cmd.Parameters.AddWithValue("@ModifiedRemarks", modifiedRemarks);
                        conn.Open();
                        cmd.ExecuteNonQuery();

                        string Update_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Update Success',
                                text: 'Record Updated Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                        // RegisterStartupScript adds the JavaScript code to the page
                        ClientScript.RegisterStartupScript(this.GetType(), "UpdateSuccessNotification", Update_SuccessScript, false);
                    }
                    catch (Exception ex)
                    {
                        DisplayUpdatingErrorNotification(ex.Message);
                    }
                }
            }
            GridView1.EditIndex = -1;
            BindGrid();
        }

        private void DisplayUpdatingErrorNotification(string errorMessage)
        {
            string errorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{errorMessage.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "DisplayUpdatingErrorNotification", errorScript, false);
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView1.EditIndex = -1;
            BindGrid();
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            GridViewRow row = GridView1.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Values[0]);
            //bool ViewMode = (row.FindControl("ViewMode") as CheckBox).Checked;
            //bool DeleteMode = (row.FindControl("DeleteMode") as CheckBox).Checked;
            string DeletedRemarks = "Deleted by the "+ Session["USERID"].ToString() + " from UI/UX";

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("DeleteBrandFieldsControl", conn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@ViewMode", 0);
                        cmd.Parameters.AddWithValue("@DeleteMode", 1);
                        cmd.Parameters.AddWithValue("@DeleteOn", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@DeletedBy", Session["USERID"].ToString());
                        cmd.Parameters.AddWithValue("@DeletedRemarks", DeletedRemarks);
                        conn.Open();
                        cmd.ExecuteNonQuery();

                        string Delete_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Delete Success',
                                text: 'Record Deleted Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                        // RegisterStartupScript adds the JavaScript code to the page
                        ClientScript.RegisterStartupScript(this.GetType(), "DeleteSuccessNotification", Delete_SuccessScript, false);
                    }
                    catch (Exception ex)
                    {
                        DisplayDeletingErrorNotification(ex.Message);
                    }
                }
            }
            BindGrid();
        }

        private void DisplayDeletingErrorNotification(string errorMessage)
        {
            string errorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{errorMessage.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "DisplayDeletingErrorNotification", errorScript, false);
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                if (DDL_PlantLine.SelectedIndex != 0)
                {
                    if (DDL_ProductCategory.SelectedIndex != 0)
                    {
                        if (DDL_ProductBrand.SelectedIndex != 0)
                        {
                            BindGrid();
                        }
                    }
                }
            }
        }

        protected void btn_insert_Click(object sender, EventArgs e)
        {
            try
            {
                string brandid = DDL_ProductBrand.SelectedValue.ToString();
                int brand_Id = Convert.ToInt32(brandid);
                string brandName = DDL_ProductBrand.SelectedItem.Text.ToString();

                int result = InsertQCInspectorRpt_BrandFieldsControl(brand_Id, brandName);

                if (result == 1)
                {
                    // Record already exists
                    string existsScript = "<script type='text/javascript'>\n" +
                                          "new PNotify({\n" +
                                          "    title: 'Warning',\n" +
                                          "    text: 'Record already exists for this Brand ID and Name.',\n" +
                                          "    type: 'warning',\n" +
                                          "    styling: 'bootstrap3'\n" +
                                          "});\n" +
                                          "</script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "RecordExistsNotification", existsScript, false);
                }
                else
                {
                    // Record inserted successfully
                    string successScript = "<script type='text/javascript'>\n" +
                                           "new PNotify({\n" +
                                           "    title: 'Success',\n" +
                                           "    text: 'Record inserted successfully!',\n" +
                                           "    type: 'success',\n" +
                                           "    styling: 'bootstrap3'\n" +
                                           "});\n" +
                                           "</script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "SuccessNotification", successScript, false);
                }
            }
            catch (Exception ex)
            {
                string catcherrorScript = "<script type='text/javascript'>\n" +
                         $"new PNotify({{\n" +
                         "    title: 'Error',\n" +
                         $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                         "    type: 'error',\n" +
                         "    styling: 'bootstrap3'\n" +
                         "});\n" +
                         "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ExceptionErrorNotification", catcherrorScript, false);
            }
        }

        public void InsertQCInspectorRpt_BrandFieldsControl_old(int brandId, string brandName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCInspectorRpt_BrandFieldsControl", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@brand_id", brandId);
                    cmd.Parameters.AddWithValue("@brand_name", brandName);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        string sqlerrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "SqlExceptionNotification", sqlerrorScript, false);
                    }
                    catch (Exception ex)
                    {
                        string catcherrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ExceptionErrorNotification", catcherrorScript, false);
                    }
                }
            }
        }


        public int InsertQCInspectorRpt_BrandFieldsControl(int brandId, string brandName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCInspectorRpt_BrandFieldsControl", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@brand_id", brandId);
                    cmd.Parameters.AddWithValue("@brand_name", brandName);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnValue);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        return (int)returnValue.Value;
                    }
                    catch (SqlException ex)
                    {
                        string sqlerrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "SqlExceptionNotification", sqlerrorScript, false);
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        string catcherrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ExceptionErrorNotification", catcherrorScript, false);
                        return -1;
                    }
                }
            }
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}