using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using AnmolDristi.DAL;
using static AnmolDristi.DAL.ShiftManager;

namespace AnmolDristi
{
    public partial class qaqc_packet_weight : System.Web.UI.Page
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        private int CurrentUserId
        {
            get
            {
                if (Session["CurrentUserId"] == null)
                {
                    Response.Redirect("login.aspx", true);
                }
                int userId;
                if (!int.TryParse(Session["CurrentUserId"].ToString(), out userId))
                {
                    throw new InvalidOperationException("Invalid user session value.");
                }
                return userId;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CurrentUserId"] == null)
            {
                Response.Redirect("login.aspx", true);
                return;
            }

            if (!IsPostBack)
            {
                ResolveCurrentShift();
                PlantBinder();
            }
        }

        /* --------------------------------------------------------------------------
           CASCADING DROPDOWNS LOGIC
           -------------------------------------------------------------------------- */
        public void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, "plant_name", "plant_id", out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
        }

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetEntryPanel();
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue;
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                PlantLinesBinder(selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
            }
        }

        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue and view_status=1 and delete_status=0 order by plant_id";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_PlantLine, "line_name", "line_id", new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
        }

        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetEntryPanel();
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue;
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue;
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);
            }
        }

        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId and view_status=1 and delete_status=0 order by category_id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, "category_name", "category_id", parameters, out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);
        }

        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetEntryPanel();
            if (DDL_ProductCategory.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue;
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue;
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue;
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;
                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);
            }
        }

        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId and view_status=1 and delete_status=0 order by brand_id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, "brand_name", "brand_id", parameters, out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);
        }

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetEntryPanel();
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue;
                lbl_DDL_ProductBrand_Value.Text = selectedProductBrandValue;
                BrandSKUBinder(selectedProductBrandValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_BrandSKU);
            }
        }

        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue and ViewMode=1 order by SKUId";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, "SKU_name", "SKUId", new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_BrandSKU);
        }

        private void ResetEntryPanel()
        {
            pnlPacketEntry.Visible = false;
            divMetadata.Visible = false;
            lblError.Text = string.Empty;
        }

        /* --------------------------------------------------------------------------
           FINAL SELECTION: Fetch Standard Weight & Intervals Independently
           -------------------------------------------------------------------------- */
        protected void DDL_BrandSKU_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (DDL_Plant.SelectedIndex <= 0 || DDL_PlantLine.SelectedIndex <= 0 ||
                DDL_ProductCategory.SelectedIndex <= 0 || DDL_ProductBrand.SelectedIndex <= 0 ||
                string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) || DDL_BrandSKU.SelectedIndex <= 0)
            {
                ResetEntryPanel();
                return;
            }

            string skuId = DDL_BrandSKU.SelectedValue;

            // 1. Fetch the Standard Weight Directly from Master
            LoadSKUMetadata(skuId);

            // 2. Fetch Intervals based on the Context/Latest Record in the Packet Weight Table
            BindIntervalsByContext(
                DDL_Plant.SelectedValue,
                DDL_PlantLine.SelectedValue,
                DDL_ProductCategory.SelectedValue,
                DDL_ProductBrand.SelectedValue,
                skuId
            );

            divMetadata.Visible = true;
            pnlPacketEntry.Visible = true;
        }

        private void LoadSKUMetadata(string skuId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT SKU_name FROM MST_Brand_SKU WHERE SKUId = @SKUId", con))
            {
                cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = skuId;
                con.Open();
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    // Assuming SKU_name holds the target Standard Weight as text
                    decimal standardWeight;
                    if (decimal.TryParse(result.ToString(), out standardWeight))
                    {
                        hdnStdWeight.Value = standardWeight.ToString("0.00");
                        lblStandardWeightDisplay.Text = standardWeight.ToString("0.00");
                    }
                }
            }
        }

        private void BindIntervalsByContext(string plant, string line, string category, string brand, string skuId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.GetNextPacketWeightIntervals_ByContext", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = CurrentUserId;
                cmd.Parameters.Add("@Plant", SqlDbType.NVarChar, 100).Value = plant;
                cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = line;
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = category;
                cmd.Parameters.Add("@Brand", SqlDbType.NVarChar, 100).Value = brand;
                cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = skuId;

                // Pass current Date, Shift Name, and Shift Start Time for Time-Gating
                cmd.Parameters.Add("@EntryDate", SqlDbType.Date).Value = DateTime.Today;
                cmd.Parameters.Add("@Shift", SqlDbType.VarChar, 50).Value = lblShift.Text;

                // Ensure Session["SHIFT_START"] exists, otherwise default to midnight
                string shiftStart = Session["SHIFT_START"] != null ? Session["SHIFT_START"].ToString() : "00:00";
                cmd.Parameters.Add("@ShiftStartTime", SqlDbType.VarChar, 20).Value = shiftStart;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            rptIntervals.DataSource = dt;
            rptIntervals.DataBind();

            if (dt.Rows.Count > 0)
            {
                lblWindow.Text = $"{dt.Rows[0]["IntervalStartTime"]} – {dt.Rows[dt.Rows.Count - 1]["IntervalEndTime"]}";
            }
            else
            {
                // If 0 rows are returned, they are either done, or they are too early!
                lblWindow.Text = "Not Available";
                lblError.Text = "Notice: You have either completed all 4 intervals, or the 2-hour window for the next interval has not opened yet.";
                pnlPacketEntry.Visible = false; // Safely hide the entry grid
            }
        }

        /* --------------------------------------------------------------------------
           SUBMISSION LOGIC
           -------------------------------------------------------------------------- */
        protected void btnSubmitPacket_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable packetDetails = BuildPacketDetailTable();
                if (packetDetails.Rows.Count == 0)
                {
                    lblError.Text = "No packet data entered.";
                    return;
                }

                CalculateSummary(packetDetails);
                InsertPacketWeight(packetDetails);

                pnlPacketEntry.Visible = false;
                divMetadata.Visible = false;
                pnlSuccess.Visible = true;
            }
            catch (Exception ex)
            {
                lblError.Text = "Error: " + ex.Message;
            }
        }

        private DataTable BuildPacketDetailTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IntervalStart", typeof(TimeSpan));
            dt.Columns.Add("IntervalEnd", typeof(TimeSpan));
            dt.Columns.Add("PacketNo", typeof(int));
            dt.Columns.Add("PacketWeight", typeof(decimal));

            foreach (RepeaterItem item in rptIntervals.Items)
            {
                HiddenField hfStart = item.FindControl("hfStart") as HiddenField;
                HiddenField hfEnd = item.FindControl("hfEnd") as HiddenField;
                if (hfStart == null || hfEnd == null) continue;

                TimeSpan start = TimeSpan.Parse(hfStart.Value);
                TimeSpan end = TimeSpan.Parse(hfEnd.Value);

                for (int i = 1; i <= 5; i++)
                {
                    TextBox txt = item.FindControl($"txtPacket{i}") as TextBox;
                    if (txt != null && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        decimal weight;
                        if (decimal.TryParse(txt.Text.Trim(), out weight))
                        {
                            dt.Rows.Add(start, end, i, weight);
                        }
                        else
                        {
                            throw new Exception($"Invalid weight format at {start}-{end}, Packet {i}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Missing weight at {start}-{end}, Packet {i}");
                    }
                }
            }
            return dt;
        }

        private void InsertPacketWeight(DataTable packetDetails)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            // IMPORTANT: Update this SP to accept Context parameters instead of QCInspectorID
            using (SqlCommand cmd = new SqlCommand("dbo.InsertPacketWeightData_Independent", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                decimal stdWeight = 0;
                decimal.TryParse(hdnStdWeight.Value, out stdWeight);

                cmd.Parameters.AddWithValue("@Plant", DDL_Plant.SelectedValue);
                cmd.Parameters.AddWithValue("@Line", DDL_PlantLine.SelectedValue);
                cmd.Parameters.AddWithValue("@Category", DDL_ProductCategory.SelectedValue);
                cmd.Parameters.AddWithValue("@Brand", DDL_ProductBrand.SelectedValue);
                cmd.Parameters.AddWithValue("@SKUId", DDL_BrandSKU.SelectedValue);
                cmd.Parameters.AddWithValue("@StandardWeight", stdWeight);
                cmd.Parameters.AddWithValue("@Shift", lblShift.Text);
                cmd.Parameters.AddWithValue("@SubmittedById", CurrentUserId);

                SqlParameter tvp = cmd.Parameters.AddWithValue("@PacketDetails", packetDetails);
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "dbo.PacketWeightDetailType";

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void ResolveCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            ShiftInfo shift = shiftManager.GetCurrentShiftInfo();
            lblShift.Text = shift.ShiftName;
            Session["SHIFT_NAME"] = shift.ShiftName;
        }

        private void CalculateSummary(DataTable packetDetails)
        {
            var weights = packetDetails.AsEnumerable().Select(r => r.Field<decimal>("PacketWeight")).ToList();
            if (weights.Count == 0) return;

            decimal stdWeight;
            if (!decimal.TryParse(hdnStdWeight.Value, out stdWeight) || stdWeight <= 0) return;

            decimal min = weights.Min();
            decimal max = weights.Max();
            decimal averageWeight = weights.Average();
            decimal giveAwayPercent = ((averageWeight - stdWeight) / stdWeight) * 100;

            lblMinWeight.Text = min.ToString("0.00");
            lblMaxWeight.Text = max.ToString("0.00");
            lblAverage.Text = averageWeight.ToString("0.00");
            lblGiveAway.Text = giveAwayPercent.ToString("0.00") + " %";
        }
    }
}