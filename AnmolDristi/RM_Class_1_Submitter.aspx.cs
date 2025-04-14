using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;

namespace AnmolDristi
{
    public partial class RM_Class_1_Submitter : System.Web.UI.Page
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

                    lbl_docname.Text = "Search Filter for QC RM Class 1 Report";
                    lbl_viewname.Text = "View and Search for Detailed View || ";

                    MaterialBinder();
                    loadAlldata();
                }

            }
        }

        private void MaterialBinder()
        {
            string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 1";
            string textField = "Material_Name";
            string valueField = "Material_Id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Material, textField, valueField, out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Material);

                string MaterialBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionBinderErrorNotification", MaterialBinder_Error_script, false);
            }
        }

        protected void DDL_Material_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Material.SelectedIndex != 0)
            {
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
                PlantBinder();

                // Call method to show relevant controls based on selected material
                //DivBinders(selectedMaterialValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

                string DDL_Material_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionInvalidErrorNotification", DDL_Material_Error_script, false);
            }
        }


        private void PlantBinder()
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

            }
            else
            {
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



        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }

        protected void ReportbtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("RM_Class_1_Submitter.aspx");
        }

        protected void ReportbtnSubmit_Click(object sender, EventArgs e)
        {
            DataLoader();
        }


        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                    c.ID AS DBID,
                    c.FormID as FormID,
                    'NA' as RecordID,
	                m.Material_Name AS MaterialName,
                    p.plant_name AS PlantName,
                    c.SubmittedByEmployeeCode as EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate as SDate,
                    c.SubmittedTime as STime,
                    c.Approver1EmployeeCode as L1,
                    c.Approver1_Status,
                    c.Approver1_TimeStamp,
                    c.Approver2EmployeeCode as L2,
                    c.Approver2_Status,
                    c.Approver2_TimeStamp,
                    c.DottedLineApproverEmployeeCode as L3,
                    c.DottedApprover_Status,
                    c.DottedApprover_TimeStamp
                    FROM 
                        TRN_RM_CLASS_1 c
                    LEFT JOIN
                        RM_MATERIAL m ON c.MaterialName = m.Material_Id
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantName = p.plant_id
                    LEFT JOIN
                        MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                    WHERE 
                        1 = 1
                    ORDER BY 
                        c.[SubmittedDate] DESC, 
                        c.[SubmittedTime] DESC;
                ";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GridView1.DataSource = dt;
                        GridView1.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
            }
        }

        private void DataLoader()
        {
            DateTime? dateFrom = string.IsNullOrEmpty(TB_Date_From.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_From.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TB_Date_To.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_To.Text, "yyyy-MM-dd", null);

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT 
                    c.ID AS DBID,
                    c.FormID AS FormID,
	                m.Material_Name AS MaterialName,
                    p.plant_name AS PlantName,
                    c.SubmittedByEmployeeCode AS EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate AS SDate,
                    c.SubmittedTime AS STime,
                    'No Comment' AS Remarks,
                    c.Approver1EmployeeCode AS L1,
                    c.Approver1_Status,
                    c.Approver1_TimeStamp,
                    c.Approver2EmployeeCode AS L2,
                    c.Approver2_Status,
                    c.Approver2_TimeStamp,
                    c.DottedLineApproverEmployeeCode AS L3,
                    c.DottedApprover_Status,
                    c.DottedApprover_TimeStamp
                FROM TRN_RM_CLASS_1 c
                LEFT JOIN RM_MATERIAL m ON c.MaterialName = m.Material_Id
                LEFT JOIN MST_PlantDetails p ON c.PlantName = p.plant_id
                LEFT JOIN MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                WHERE 1 = 1");

            var parameters = new List<SqlParameter>();

            // Add filters for date range
            if (dateFrom.HasValue)
            {
                queryBuilder.Append(" AND c.SubmittedDate >= @DateFrom");
                parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
            }

            if (dateTo.HasValue)
            {
                queryBuilder.Append(" AND c.SubmittedDate <= @DateTo");
                parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
            }

            if (!string.IsNullOrEmpty(DDL_Material.SelectedValue) && DDL_Material.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.MaterialName = @Material_Id");
                parameters.Add(new SqlParameter("@Material_Id", SqlDbType.Int) { Value = DDL_Material.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.PlantName = @PlantId");
                parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
            }

            //if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.Line = @LineName");
            //    parameters.Add(new SqlParameter("@LineName", SqlDbType.Int) { Value = DDL_PlantLine.SelectedValue });
            //}

            //if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.ProductCategory = @ProductCategory");
            //    parameters.Add(new SqlParameter("@ProductCategory", SqlDbType.Int) { Value = DDL_ProductCategory.SelectedValue });
            //}

            //if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.ProductBrand = @ProductBrand");
            //    parameters.Add(new SqlParameter("@ProductBrand", SqlDbType.Int) { Value = DDL_ProductBrand.SelectedValue });
            //}

            queryBuilder.Append(" ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC");

            try
            {
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());
                GridView1.DataSource = filteredData;
                GridView1.DataBind();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                string FilterDataerrorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "FilteredDataerror", FilterDataerrorScript, true);
            }
        }

        private DataTable GetDataFromTable(string query, SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddRange(parameters);
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dt);
                    }
                }
            }
            return dt;
        }

        protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                // Get the DBID from the CommandArgument.
                int id = Convert.ToInt32(e.CommandArgument);

                if (id > 0)
                {
                    // Redirect with the correct DBID.
                    Response.Redirect("RM_Class_1_Detailed.aspx?Id=" + id + "&source=submitter");
                }
                else
                {
                    // Handle cases where DBID is not valid.
                    // Show an error message or log the issue.
                    System.Diagnostics.Debug.WriteLine("Invalid DBID passed: " + e.CommandArgument);
                }
            }
        }

    }
}