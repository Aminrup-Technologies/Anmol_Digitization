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
        public void PlantBinder_OLD()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, "plant_name", "plant_id", out recordsBound);
            if (!recordsBound) DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
        }

        public void PlantBinder()
        {
            // 1. Bind the DropDownList with all available ERP plants
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_Plant, "plant_name", "plant_id", out recordsBound);

            // If binding fails, set default and exit early
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                lbl_DDL_Plant_Value.Text = "Error";
                DDL_Plant.Enabled = false; // Disable on error
                return;
            }

            // Grab both the Local Plant ID and the Mapped ERP Plant ID from Session
            string localPlantId = Session["PLANTID"] != null ? Session["PLANTID"].ToString() : "";
            string sessionErpPlantId = Session["ERPPlantId"] != null ? Session["ERPPlantId"].ToString() : "0";

            // 2. Handle the "Common" (PAN INDIA) Edge Case
            if (localPlantId == "1089")
            {
                lbl_DDL_Plant_Value.Text = "PAN INDIA";

                DDL_Plant.ClearSelection();
                ListItem defaultItem = DDL_Plant.Items.FindByValue("0");
                if (defaultItem != null)
                {
                    defaultItem.Selected = true;
                }

                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                // --- NEW: Keep the dropdown unlocked so PAN INDIA can change plants ---
                DDL_Plant.Enabled = true;
            }
            // 3. Handle standard mapped employees
            else if (sessionErpPlantId != "0")
            {
                DDL_Plant.ClearSelection();
                ListItem itemToSelect = DDL_Plant.Items.FindByValue(sessionErpPlantId);

                if (itemToSelect != null)
                {
                    itemToSelect.Selected = true;
                    lbl_DDL_Plant_Value.Text = itemToSelect.Value;
                    ResetEntryPanel();
                    PlantLinesBinder(itemToSelect.Value);

                    // --- NEW: Lock the dropdown so standard users cannot change it ---
                    DDL_Plant.Enabled = false;
                }
                else
                {
                    lbl_DDL_Plant_Value.Text = "Not Found";
                    DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                    DDL_Plant.Enabled = false; // Lock on missing mapping
                }
            }
            // 4. Fallback for unmapped or broken records
            else
            {
                lbl_DDL_Plant_Value.Text = "N/A";
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                DDL_Plant.Enabled = false; // Lock on error
            }
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

        private void LoadSKUMetadata_OLD(string skuId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            // Fetch SKU_details1 (Tare Weight) along with SKU_name (Standard Weight)
            using (SqlCommand cmd = new SqlCommand("SELECT SKU_name, ISNULL(SKU_details1, '0') AS TareWeight FROM MST_Brand_SKU WHERE SKUId = @SKUId", con))
            {
                cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = skuId;
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        decimal standardWeight, tareWeight;

                        if (decimal.TryParse(dr["SKU_name"].ToString(), out standardWeight))
                        {
                            hdnStdWeight.Value = standardWeight.ToString("0.00");
                            lblStandardWeightDisplay.Text = standardWeight.ToString("0.00");
                        }

                        if (decimal.TryParse(dr["TareWeight"].ToString(), out tareWeight))
                        {
                            hdnTareWeight.Value = tareWeight.ToString("0.00");
                        }
                    }
                }
            }
        }

        private void LoadSKUMetadata(string skuId)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT SKU_name, ISNULL(SKU_details1, '0') AS TareWeight FROM MST_Brand_SKU WHERE SKUId = @SKUId", con))
            {
                cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = skuId;
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        decimal standardWeight, tareWeight;

                        if (decimal.TryParse(dr["SKU_name"].ToString(), out standardWeight))
                        {
                            hdnStdWeight.Value = standardWeight.ToString("0.00");
                            lblStandardWeightDisplay.Text = standardWeight.ToString("0.00");
                        }

                        if (decimal.TryParse(dr["TareWeight"].ToString(), out tareWeight))
                        {
                            hdnTareWeight.Value = tareWeight.ToString("0.00");
                            hdnOldTareWeight.Value = tareWeight.ToString("0.00"); // NEW: Track the old weight for the log
                            lblTareDisplay.Text = tareWeight.ToString("0.00");

                            // Trigger the popup if the value is 0
                            if (tareWeight == 0)
                            {
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowTareModal", "setTimeout(function() { showTareWeightModal(); }, 200);", true);
                            }
                        }
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
                // 1. Find both start and end hidden fields
                HiddenField hfStart = item.FindControl("hfStart") as HiddenField;
                HiddenField hfEnd = item.FindControl("hfEnd") as HiddenField;

                if (hfStart == null || hfEnd == null) continue;

                // 2. Dynamically parse the actual 30-minute timestamps (e.g., "09:00:00" and "09:30:00")
                TimeSpan start = TimeSpan.Zero;
                TimeSpan end = TimeSpan.Zero;

                TimeSpan.TryParse(hfStart.Value, out start);
                TimeSpan.TryParse(hfEnd.Value, out end);

                // 3. Loop through the 5 packets for this specific 30-minute interval
                for (int i = 1; i <= 5; i++)
                {
                    TextBox txt = item.FindControl("txtPacket" + i) as TextBox;
                    if (txt != null && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        decimal weight;
                        if (decimal.TryParse(txt.Text.Trim(), out weight))
                        {
                            dt.Rows.Add(start, end, i, weight);
                        }
                        else
                        {
                            // STOP: Prevent saving if they typed letters instead of numbers
                            throw new Exception($"Invalid weight format for Packet {i} in the {start:hh\\:mm} interval.");
                        }
                    }
                    else
                    {
                        // STOP: Prevent saving if a box is completely empty
                        throw new Exception($"Missing weight for Packet {i} in the {start:hh\\:mm} interval.");
                    }
                }
            }

            return dt;
        }

        private void InsertPacketWeight_OLD(DataTable packetDetails)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.InsertPacketWeightData_Independent", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                decimal stdWeight = 0;
                decimal tareWeight = 0;
                decimal.TryParse(hdnStdWeight.Value, out stdWeight);
                decimal.TryParse(hdnTareWeight.Value, out tareWeight); // Fetch from hidden field

                cmd.Parameters.AddWithValue("@Plant", DDL_Plant.SelectedValue);
                cmd.Parameters.AddWithValue("@Line", DDL_PlantLine.SelectedValue);
                cmd.Parameters.AddWithValue("@Category", DDL_ProductCategory.SelectedValue);
                cmd.Parameters.AddWithValue("@Brand", DDL_ProductBrand.SelectedValue);
                cmd.Parameters.AddWithValue("@SKUId", DDL_BrandSKU.SelectedValue);

                cmd.Parameters.AddWithValue("@StandardWeight", stdWeight);
                cmd.Parameters.AddWithValue("@TareWeight", tareWeight); // NEW PARAMETER
                cmd.Parameters.AddWithValue("@Shift", lblShift.Text);
                cmd.Parameters.AddWithValue("@SubmittedById", CurrentUserId);

                SqlParameter tvp = cmd.Parameters.AddWithValue("@PacketDetails", packetDetails);
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "dbo.PacketWeightDetailType";

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertPacketWeight(DataTable packetDetails)
        {
            // 1. Calculate the values needed for the database
            var weights = packetDetails.AsEnumerable().Select(r => r.Field<decimal>("PacketWeight")).ToList();

            decimal stdWeight = 0;
            decimal tareWeight = 0;
            decimal.TryParse(hdnStdWeight.Value, out stdWeight);
            decimal.TryParse(hdnTareWeight.Value, out tareWeight);

            // Gross Calculations
            decimal grossMin = weights.Min();
            decimal grossMax = weights.Max();
            decimal grossAverage = weights.Average();

            // Net Calculations
            decimal netMin = grossMin - tareWeight;
            decimal netMax = grossMax - tareWeight;
            decimal netAverage = grossAverage - tareWeight;

            decimal giveAwayPercent = 0;
            bool isUnderweight = false;
            decimal underweightDeficit = 0;

            // Evaluate the Underweight Rule vs Give-Away
            if (netAverage < stdWeight)
            {
                isUnderweight = true;
                underweightDeficit = stdWeight - netAverage;
                giveAwayPercent = 0; // No give away, it's a deficit
            }
            else if (netAverage > stdWeight && stdWeight > 0)
            {
                giveAwayPercent = ((netAverage - stdWeight) / stdWeight) * 100;
            }

            // 2. Send everything to the database
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.InsertPacketWeightData_Independent", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Existing Parameters
                cmd.Parameters.AddWithValue("@Plant", DDL_Plant.SelectedValue);
                cmd.Parameters.AddWithValue("@Line", DDL_PlantLine.SelectedValue);
                cmd.Parameters.AddWithValue("@Category", DDL_ProductCategory.SelectedValue);
                cmd.Parameters.AddWithValue("@Brand", DDL_ProductBrand.SelectedValue);
                cmd.Parameters.AddWithValue("@SKUId", DDL_BrandSKU.SelectedValue);
                cmd.Parameters.AddWithValue("@StandardWeight", stdWeight);
                cmd.Parameters.AddWithValue("@TareWeight", tareWeight);
                cmd.Parameters.AddWithValue("@Shift", lblShift.Text);
                cmd.Parameters.AddWithValue("@SubmittedById", CurrentUserId);

                // Summary Parameters matching SP
                cmd.Parameters.AddWithValue("@GrossMin", grossMin);
                cmd.Parameters.AddWithValue("@GrossMax", grossMax);
                cmd.Parameters.AddWithValue("@GrossAvg", grossAverage);
                cmd.Parameters.AddWithValue("@NetMin", netMin);
                cmd.Parameters.AddWithValue("@NetMax", netMax);
                cmd.Parameters.AddWithValue("@NetAvg", netAverage);
                cmd.Parameters.AddWithValue("@GiveAwayPercent", giveAwayPercent);
                cmd.Parameters.AddWithValue("@IsUnderweight", isUnderweight);
                cmd.Parameters.AddWithValue("@UnderweightDeficit", underweightDeficit);

                // Table-Valued Parameter for individual packets
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

        private void CalculateSummary_OLD(DataTable packetDetails)
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

        private void CalculateSummary(DataTable packetDetails)
        {
            var weights = packetDetails.AsEnumerable().Select(r => r.Field<decimal>("PacketWeight")).ToList();
            if (weights.Count == 0) return;

            decimal stdWeight, tareWeight;
            if (!decimal.TryParse(hdnStdWeight.Value, out stdWeight) || stdWeight <= 0) return;

            decimal.TryParse(hdnTareWeight.Value, out tareWeight);

            decimal grossMin = weights.Min();
            decimal grossMax = weights.Max();
            decimal grossAverage = weights.Average();

            // Calculate Net Values
            decimal netMin = grossMin - tareWeight;
            decimal netMax = grossMax - tareWeight;
            decimal netAvg = grossAverage - tareWeight;

            // UI Binding
            lblMinWeight.Text = netMin.ToString("0.00");
            lblMaxWeight.Text = netMax.ToString("0.00");
            lblAverage.Text = grossAverage.ToString("0.00");
            lblTareDisplay.Text = tareWeight.ToString("0.00");
            lblNetAvgDisplay.Text = netAvg.ToString("0.00");

            // Evaluate Give-Away vs Underweight Flag
            if (netAvg < stdWeight)
            {
                decimal deficit = stdWeight - netAvg;
                lblGiveAway.Text = $"Flag: Underweight (-{deficit:0.00}g)";
                lblGiveAway.ForeColor = System.Drawing.Color.FromName("#d9534f"); // Red
                lblNetAvgDisplay.ForeColor = System.Drawing.Color.FromName("#d9534f"); // Red
            }
            else if (netAvg == stdWeight)
            {
                lblGiveAway.Text = "0.00 %";
                lblGiveAway.ForeColor = System.Drawing.Color.Black;
                lblNetAvgDisplay.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                decimal giveAwayPercent = ((netAvg - stdWeight) / stdWeight) * 100;
                lblGiveAway.Text = giveAwayPercent.ToString("0.00") + " %";
                lblGiveAway.ForeColor = System.Drawing.Color.Black;
                lblNetAvgDisplay.ForeColor = System.Drawing.Color.Green;
            }
        }

        protected void btnApplyTare_Click(object sender, EventArgs e)
        {
            decimal oldWeight = 0;
            decimal.TryParse(hdnOldTareWeight.Value, out oldWeight);

            decimal newWeight = 0;
            if (decimal.TryParse(txtModalTareWeight.Text, out newWeight))
            {
                // 1. Update the Database and Log the Revision
                UpdateTareWeightInDatabase(DDL_BrandSKU.SelectedValue, oldWeight, newWeight);

                // 2. Update the UI and Hidden Fields with the new confirmed weight
                hdnTareWeight.Value = newWeight.ToString("0.00");
                hdnOldTareWeight.Value = newWeight.ToString("0.00"); // Reset old to new for future edits
                lblTareDisplay.Text = newWeight.ToString("0.00");

                // 3. Clear the textbox, hide the modal, and recalculate the Net preview
                txtModalTareWeight.Text = "";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "HideTareModal", "$('#tareWeightModal').modal('hide'); recalculatePreview();", true);
            }
        }

        private void UpdateTareWeightInDatabase(string skuId, decimal oldWeight, decimal newWeight)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.UpdateTareWeightAndLog", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SKUId", skuId);
                cmd.Parameters.AddWithValue("@OldWeight", oldWeight);
                cmd.Parameters.AddWithValue("@NewWeight", newWeight);
                cmd.Parameters.AddWithValue("@UserId", CurrentUserId);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}