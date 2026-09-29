using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AnmolDristi
{
    public partial class RouteDistanceCalculation : System.Web.UI.Page
    {
        // Define connection string at class level for reusability
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            // Load the grid when the page first opens
            BindPendingGrid();
        }

        // Method to fetch pending records and bind to the HTML table
        private void BindPendingGrid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Query fetches records awaiting approval
                    string query = @"SELECT RouteID, OriginName, DestinationName, WaypointsJSON, 
                                            TotalDistanceKM, TravelMode, SubmittedDate 
                                     FROM tbl_TravelExpenseRoutes 
                                     WHERE ApprovalStatus = 'Pending' 
                                     ORDER BY SubmittedDate DESC";

                    using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            rptPendingRoutes.DataSource = dt;
                            rptPendingRoutes.DataBind();
                            lblNoRecords.Visible = false;
                        }
                        else
                        {
                            rptPendingRoutes.DataSource = null;
                            rptPendingRoutes.DataBind();
                            lblNoRecords.Visible = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblServerMessage.Text = "Error loading grid: " + ex.Message;
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
            }
        }

        // --- ADDED 'async' HERE TO RESOLVE CS4033 ---
        protected async void btnSubmitApproval_Click(object sender, EventArgs e)
        {
            string totalDistance = hfTotalDistance.Value;
            string travelMode = hfTravelMode.Value;
            string originName = hfOriginName.Value;
            string destinationName = hfDestinationName.Value;
            string waypointsJson = hfWaypointsJSON.Value;

            int employeeId = 101; // Dummy ID

            if (string.IsNullOrEmpty(totalDistance) || string.IsNullOrEmpty(waypointsJson))
            {
                lblServerMessage.Text = "Error: Route data is incomplete. Please interact with the map.";
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            // --- SERVER-SIDE DISTANCE VERIFICATION ---
            decimal clientDistance = Convert.ToDecimal(totalDistance);
            decimal serverVerifiedDistance = await VerifyDistanceWithOSRM(waypointsJson, travelMode);

            // Allow a small tolerance (e.g., 0.5 KM) for floating point rounding differences
            if (serverVerifiedDistance == -1m || Math.Abs(clientDistance - serverVerifiedDistance) > 0.5m)
            {
                lblServerMessage.Text = "Security Error: Route distance mismatch detected. Submission rejected.";
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }
            // ------------------------------------------

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_Insert_TravelRoute", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                        cmd.Parameters.AddWithValue("@OriginName", originName);
                        cmd.Parameters.AddWithValue("@DestinationName", destinationName);
                        cmd.Parameters.AddWithValue("@WaypointsJSON", waypointsJson);
                        cmd.Parameters.AddWithValue("@TotalDistanceKM", Convert.ToDecimal(totalDistance));
                        cmd.Parameters.AddWithValue("@TravelMode", travelMode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                lblServerMessage.Text = $"Success! Route from {originName} to {destinationName} has been submitted.";
                lblServerMessage.ForeColor = System.Drawing.Color.Green;

                hfTotalDistance.Value = "";
                hfWaypointsJSON.Value = "";

                // Refresh the grid so the newly submitted route appears instantly
                BindPendingGrid();
            }
            catch (Exception ex)
            {
                lblServerMessage.Text = "Database Error: " + ex.Message;
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
            }
        }

        // Helper method to call the API directly from C#
        // Helper method to call the API directly from C# for security validation
        private async Task<decimal> VerifyDistanceWithOSRM(string waypointsJson, string mode)
        {
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                dynamic waypoints = js.Deserialize<dynamic>(waypointsJson);

                System.Collections.Generic.List<string> coords = new System.Collections.Generic.List<string>();
                foreach (var wp in waypoints)
                {
                    coords.Add($"{wp["lon"]},{wp["lat"]}");
                }
                string coordsString = string.Join(";", coords);
                string osrmUrl = $"http://router.project-osrm.org/route/v1/{mode}/{coordsString}?overview=false";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "AnmolDristi_Expense_App");
                    HttpResponseMessage response = await client.GetAsync(osrmUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic parsedJson = js.Deserialize<dynamic>(jsonResponse);
                        if (parsedJson["code"] == "Ok")
                        {
                            decimal meters = parsedJson["routes"][0]["distance"];
                            return meters / 1000m;
                        }
                    }
                }
            }
            catch { /* Log error internally */ }

            return -1m; // Force validation failure if API call fails
        }

        protected void btnSubmitApproval_Click_OLD(object sender, EventArgs e)
        {
            // 1. Retrieve data extracted from the Leaflet map via JS Hidden Fields
            string totalDistance = hfTotalDistance.Value;
            string travelMode = hfTravelMode.Value;
            string originName = hfOriginName.Value;
            string destinationName = hfDestinationName.Value;
            string waypointsJson = hfWaypointsJSON.Value;

            // Optional: Get your logged-in user ID here. Using a dummy value (101) for now.
            int employeeId = 101;

            // 2. Validate that we actually have map data
            if (string.IsNullOrEmpty(totalDistance) || string.IsNullOrEmpty(waypointsJson))
            {
                lblServerMessage.Text = "Error: Route data is incomplete. Please interact with the map.";
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            // 3. Save to SQL Server
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_Insert_TravelRoute", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                        cmd.Parameters.AddWithValue("@OriginName", originName);
                        cmd.Parameters.AddWithValue("@DestinationName", destinationName);
                        cmd.Parameters.AddWithValue("@WaypointsJSON", waypointsJson);
                        cmd.Parameters.AddWithValue("@TotalDistanceKM", Convert.ToDecimal(totalDistance));
                        cmd.Parameters.AddWithValue("@TravelMode", travelMode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                // 4. Success feedback
                lblServerMessage.Text = $"Success! Route from {originName} to {destinationName} ({totalDistance} KM) has been submitted for approval.";
                lblServerMessage.ForeColor = System.Drawing.Color.Green;

                // Clear the hidden fields to prevent accidental resubmission
                hfTotalDistance.Value = "";
                hfWaypointsJSON.Value = "";
            }
            catch (Exception ex)
            {
                lblServerMessage.Text = "Database Error: " + ex.Message;
                lblServerMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
    }
}