using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using AnmolDristi.DAL;
using System.Web;

namespace AnmolDristi
{
    public partial class qaqc_packet_weight_view : System.Web.UI.Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CurrentUserId"] == null)
            {
                Response.Redirect("login.aspx", true);
                return;
            }

            if (!IsPostBack)
            {
                // Default Date Range: Last 7 Days
                txtFromDate.Text = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                txtToDate.Text = DateTime.Today.ToString("yyyy-MM-dd");

                PlantBinder();
                BindGrid(); // Load initial data
            }
        }

        /* --------------------------------------------------------------------------
           CASCADING DROPDOWNS (With 'All' Option Support)
           -------------------------------------------------------------------------- */
        public void PlantBinder()
        {
            string query = "SELECT '0' AS plant_id, '-- All Plants --' AS plant_name UNION ALL SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, "plant_name", "plant_id", out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

            // Trigger cascading reset
            DDL_Plant_SelectedIndexChanged(null, null);
        }

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedValue != "0")
            {
                string query = "SELECT '0' AS line_id, '-- All Lines --' AS line_name UNION ALL SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue AND view_status=1 AND delete_status=0";
                bool recordsBound;
                DatabaseHelper.BindDropDownList(query, DDL_PlantLine, "line_name", "line_id", new SqlParameter("@SelectedPlantValue", DDL_Plant.SelectedValue), out recordsBound);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
            }
            DDL_PlantLine_SelectedIndexChanged(null, null);
        }

        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedValue != "0" && DDL_PlantLine.SelectedValue != "")
            {
                string query = "SELECT '0' AS category_id, '-- All Categories --' AS category_name UNION ALL SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId AND view_status=1 AND delete_status=0";
                SqlParameter[] parameters = {
                    new SqlParameter("@PlantId", DDL_Plant.SelectedValue),
                    new SqlParameter("@LineId", DDL_PlantLine.SelectedValue)
                };
                bool recordsBound;
                DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, "category_name", "category_id", parameters, out recordsBound);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);
            }
            DDL_ProductCategory_SelectedIndexChanged(null, null);
        }

        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductCategory.SelectedValue != "0" && DDL_ProductCategory.SelectedValue != "")
            {
                string query = "SELECT '0' AS brand_id, '-- All Brands --' AS brand_name UNION ALL SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId AND category_id = @CategoryId AND view_status=1 AND delete_status=0";
                SqlParameter[] parameters = {
                    new SqlParameter("@PlantId", DDL_Plant.SelectedValue),
                    new SqlParameter("@LineId", DDL_PlantLine.SelectedValue),
                    new SqlParameter("@CategoryId", DDL_ProductCategory.SelectedValue)
                };
                bool recordsBound;
                DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, "brand_name", "brand_id", parameters, out recordsBound);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);
            }
            DDL_ProductBrand_SelectedIndexChanged(null, null);
        }

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedValue != "0" && DDL_ProductBrand.SelectedValue != "")
            {
                string query = "SELECT '0' AS SKUId, '-- All SKUs --' AS SKU_name UNION ALL SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @BrandId AND ViewMode=1";
                bool recordsBound;
                DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, "SKU_name", "SKUId", new SqlParameter("@BrandId", DDL_ProductBrand.SelectedValue), out recordsBound);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_BrandSKU);
            }
        }

        /* --------------------------------------------------------------------------
           GRID BINDING & SEARCH
           -------------------------------------------------------------------------- */
        protected void lnkBtnSearch_Click(object sender, EventArgs e)
        {
            BindGrid();
        }

        private void BindGrid()
        {
            lblMessage.Text = string.Empty;
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.GetPacketWeightHeaders_ForView", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = string.IsNullOrEmpty(txtFromDate.Text) ? (object)DBNull.Value : Convert.ToDateTime(txtFromDate.Text);
                    cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = string.IsNullOrEmpty(txtToDate.Text) ? (object)DBNull.Value : Convert.ToDateTime(txtToDate.Text);
                    cmd.Parameters.Add("@Plant", SqlDbType.NVarChar, 100).Value = DDL_Plant.SelectedValue == "" ? "0" : DDL_Plant.SelectedValue;
                    cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = DDL_PlantLine.SelectedValue == "" ? "0" : DDL_PlantLine.SelectedValue;
                    cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DDL_ProductCategory.SelectedValue == "" ? "0" : DDL_ProductCategory.SelectedValue;
                    cmd.Parameters.Add("@Brand", SqlDbType.NVarChar, 100).Value = DDL_ProductBrand.SelectedValue == "" ? "0" : DDL_ProductBrand.SelectedValue;
                    cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = DDL_BrandSKU.SelectedValue == "" ? "0" : DDL_BrandSKU.SelectedValue;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                gvHeaders.DataSource = dt;
                gvHeaders.DataBind();
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error fetching records: " + ex.Message;
            }
        }

        protected void gvHeaders_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                int packetHdrId;
                if (int.TryParse(e.CommandArgument.ToString(), out packetHdrId))
                {
                    // 1. Fetch both Summary and Details in a single, high-performance DB call
                    LoadModalDetails(packetHdrId);

                    // 2. Trigger the Javascript to open the modal
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "PopModal", "$('#detailsModal').modal('show');", true);
                }
            }
        }

        private void LoadModalDetails(int packetHdrId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.GetPacketWeightDetails_ForView", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PacketHdrID", packetHdrId);

                con.Open();

                // Use SqlDataReader to read multiple result sets from the Stored Procedure
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    // -------------------------------------------------------------
                    // RESULT SET 1: The Header & Calculation Summary
                    // -------------------------------------------------------------
                    if (dr.Read())
                    {
                        // Top Context Labels
                        lblModalDateShift.Text = dr["EntryDateText"].ToString() + " (Shift " + dr["Shift"].ToString() + ")";
                        lblModalPlantLine.Text = dr["PlantLineText"].ToString();
                        lblModalSKU.Text = dr["SKUNameText"].ToString();

                        // NEW: Bind Variety and Submitter
                        lblModalVariety.Text = dr["Variety"].ToString();
                        lblModalSubmitter.Text = dr["SubmitterName"].ToString();
                        lblModalTimestamp.Text = dr["SubmittedOn"] != DBNull.Value ? Convert.ToDateTime(dr["SubmittedOn"]).ToString("dd-MM-yyyy hh:mm:ss tt") : "N/A";
                        // Retrieve calculation values from DB
                        decimal std = Convert.ToDecimal(dr["StandardWeight"]);
                        decimal grossAvg = Convert.ToDecimal(dr["AvgWeight"]);
                        decimal giveAway = Convert.ToDecimal(dr["GiveAwayPercent"]);
                        decimal tare = dr["TareWeight"] != DBNull.Value ? Convert.ToDecimal(dr["TareWeight"]) : 0;

                        // Calculate Net Average
                        decimal netAvg = grossAvg - tare;

                        // Populate the Bottom Calculation Breakdown Labels
                        lblModalGrossAvg.Text = grossAvg.ToString("0.00");
                        lblModalTare.Text = tare.ToString("0.00");
                        lblModalNetAvg.Text = netAvg.ToString("0.00");
                        lblModalCalcStd.Text = std.ToString("0.00");

                        lblModalCalcGiveAway.Text = giveAway.ToString("0.00") + " %";
                        lblModalCalcGiveAway.ForeColor = giveAway > 0 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                    }

                    // -------------------------------------------------------------
                    // RESULT SET 2: The 20 Packets (4 Rows x 5 Packets)
                    // -------------------------------------------------------------
                    if (dr.NextResult())
                    {
                        gvDetails.DataSource = dr;
                        gvDetails.DataBind();
                    }
                }
            }
        }

        protected void lnkBtnExport_Click(object sender, EventArgs e)
        {
            lblMessage.Text = string.Empty;
            DataTable dt = new DataTable();

            try
            {
                // 1. Fetch the Comprehensive Data
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.GetPacketWeightExport_ForView", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = string.IsNullOrEmpty(txtFromDate.Text) ? (object)DBNull.Value : Convert.ToDateTime(txtFromDate.Text);
                    cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = string.IsNullOrEmpty(txtToDate.Text) ? (object)DBNull.Value : Convert.ToDateTime(txtToDate.Text);
                    cmd.Parameters.Add("@Plant", SqlDbType.NVarChar, 100).Value = DDL_Plant.SelectedValue == "" ? "0" : DDL_Plant.SelectedValue;
                    cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = DDL_PlantLine.SelectedValue == "" ? "0" : DDL_PlantLine.SelectedValue;
                    cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DDL_ProductCategory.SelectedValue == "" ? "0" : DDL_ProductCategory.SelectedValue;
                    cmd.Parameters.Add("@Brand", SqlDbType.NVarChar, 100).Value = DDL_ProductBrand.SelectedValue == "" ? "0" : DDL_ProductBrand.SelectedValue;
                    cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = DDL_BrandSKU.SelectedValue == "" ? "0" : DDL_BrandSKU.SelectedValue;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    string filename = "QC_Packet_Weights_" + DateTime.Now.ToString("ddMMMyyyy_HHmm") + ".xls";

                    // 2. Clear the response completely
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=" + filename);
                    Response.Charset = "";
                    Response.ContentType = "application/vnd.ms-excel";

                    // 3. Build a pure HTML table containing ONLY the data
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("<table border='1' cellpadding='5' cellspacing='0'>");

                    // Generate Header Row
                    sb.Append("<tr style='background-color: #D3D3D3; font-weight: bold;'>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<th>").Append(column.ColumnName).Append("</th>");
                    }
                    sb.Append("</tr>");

                    // Generate Data Rows
                    foreach (DataRow row in dt.Rows)
                    {
                        sb.Append("<tr>");
                        foreach (DataColumn column in dt.Columns)
                        {
                            sb.Append("<td>").Append(row[column].ToString()).Append("</td>");
                        }
                        sb.Append("</tr>");
                    }
                    sb.Append("</table>");

                    // 4. Write the clean table to the Excel file
                    Response.Write(sb.ToString());
                    Response.Flush();

                    // 5. CRITICAL: Suppress the rest of the page from loading into the file!
                    Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    lblMessage.Text = "No records found to export.";
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error during export: " + ex.Message;
            }
        }
    }
}