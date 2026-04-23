using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using static AnmolDristi.ZKTecoManager;

namespace AnmolDristi
{
    public partial class AddMachine : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lbl_msg.Text = "Fill details and click Submit to Save Data!!";
                lbl_msg.CssClass = "text-info";
                BindGrid(); // Load data on first visit
            }
        }

        private void BindGrid()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT MachineID, MachineName, IPAddress, PortNumber, IsActive FROM CMS_MachineMaster";
                using (SqlDataAdapter sda = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    gvMachines.DataSource = dt;
                    gvMachines.DataBind();
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtMachineName.Text) ||
                string.IsNullOrWhiteSpace(txtIPAddress.Text) ||
                string.IsNullOrWhiteSpace(txtPort.Text))
            {
                lbl_msg.Text = "Error: All fields are required.";
                lbl_msg.CssClass = "text-danger font-weight-bold";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    // NEWID() is added to ensure MachineSerialNo gets a unique temporary value
                    string query = @"INSERT INTO CMS_MachineMaster (MachineName, IPAddress, PortNumber, MachineSerialNo, IsActive, CreatedOn) 
                             VALUES (@MachineName, @IPAddress, @PortNumber, CAST(NEWID() AS VARCHAR(100)), 1, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MachineName", txtMachineName.Text.Trim());
                        cmd.Parameters.AddWithValue("@IPAddress", txtIPAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@PortNumber", int.Parse(txtPort.Text.Trim()));

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                lbl_msg.Text = "Device saved successfully!";
                lbl_msg.CssClass = "text-success font-weight-bold";

                txtMachineName.Text = string.Empty;
                txtIPAddress.Text = string.Empty;
                txtPort.Text = "4370";

                BindGrid(); // Refresh grid to show new device
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Database Error: " + ex.Message;
                lbl_msg.CssClass = "text-danger font-weight-bold";
            }
        }

        protected void gvMachines_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);
            int machineId = Convert.ToInt32(gvMachines.DataKeys[index].Values["MachineID"]);
            string ip = gvMachines.DataKeys[index].Values["IPAddress"].ToString();
            int port = Convert.ToInt32(gvMachines.DataKeys[index].Values["PortNumber"]);

            ZKTecoManager zkManager = new ZKTecoManager();
            int errorCode = 0;

            // --- EXISTING TEST CONNECTION LOGIC ---
            if (e.CommandName == "TestConnection")
            {
                bool isConnected = zkManager.TestConnection(ip, port, out errorCode);
                if (isConnected)
                {
                    lbl_msg.Text = $"Successfully connected to {ip}:{port}!";
                    lbl_msg.CssClass = "text-success font-weight-bold";
                }
                else
                {
                    lbl_msg.Text = $"Connection failed for {ip}:{port}. Error Code: {errorCode}";
                    lbl_msg.CssClass = "text-danger font-weight-bold";
                }
            }
            // --- NEW SYNC LOGS LOGIC ---
            else if (e.CommandName == "SyncLogs")
            {
                List<ZKTecoManager.BiometricLog> logs;
                bool isConnected = zkManager.PullLogsSTA(ip, port, out errorCode, out logs);

                if (isConnected)
                {
                    if (logs.Count > 0)
                    {
                        int newRecordsInserted = 0;

                        // Insert into SQL
                        using (SqlConnection conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            foreach (var log in logs)
                            {
                                // Using IF NOT EXISTS to prevent crashing on duplicate logs
                                string insertSql = @"
                            IF NOT EXISTS (SELECT 1 FROM CMS_RawBiometricLogs WHERE MachineID=@MachineID AND EnrollNumber=@EnrollNo AND PunchTime=@PunchTime)
                            BEGIN
                                INSERT INTO CMS_RawBiometricLogs (MachineID, EnrollNumber, VerifyMode, InOutMode, PunchTime)
                                VALUES (@MachineID, @EnrollNo, @VerifyMode, @InOutMode, @PunchTime)
                            END";

                                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@MachineID", machineId);
                                    cmd.Parameters.AddWithValue("@EnrollNo", log.EnrollNumber);
                                    cmd.Parameters.AddWithValue("@VerifyMode", log.VerifyMode);
                                    cmd.Parameters.AddWithValue("@InOutMode", log.InOutMode);
                                    cmd.Parameters.AddWithValue("@PunchTime", log.PunchTime);

                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    if (rowsAffected > 0) newRecordsInserted++;
                                }
                            }
                        }

                        lbl_msg.Text = $"Sync Complete! Found {logs.Count} logs on device. Inserted {newRecordsInserted} new records.";
                        lbl_msg.CssClass = "text-success font-weight-bold";
                    }
                    else
                    {
                        lbl_msg.Text = $"Connected to {ip}, but there are no attendance logs on the device.";
                        lbl_msg.CssClass = "text-info font-weight-bold";
                    }
                }
                else
                {
                    lbl_msg.Text = $"Sync failed. Cannot connect to {ip}:{port}. Error: {errorCode}";
                    lbl_msg.CssClass = "text-danger font-weight-bold";
                }
            }

            else if (e.CommandName == "SyncUsers")
            {
                List<ZKTecoManager.DeviceUser> users;
                bool isConnected = zkManager.PullUsersSTA(ip, port, out errorCode, out users);

                if (isConnected)
                {
                    if (users.Count > 0)
                    {
                        int updated = 0, inserted = 0;

                        using (SqlConnection conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            foreach (var user in users)
                            {
                                // Upsert Logic (Update if exists, otherwise Insert)
                                string upsertSql = @"
                                IF EXISTS (SELECT 1 FROM CMS_DeviceUsers WHERE MachineID=@MachineID AND EnrollNumber=@EnrollNo)
                                BEGIN
                                    UPDATE CMS_DeviceUsers 
                                    SET UserName=@Name, Privilege=@Privilege, IsEnabled=@Enabled, SyncDateTime=GETDATE()
                                    WHERE MachineID=@MachineID AND EnrollNumber=@EnrollNo;
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO CMS_DeviceUsers (MachineID, EnrollNumber, UserName, Privilege, IsEnabled)
                                    VALUES (@MachineID, @EnrollNo, @Name, @Privilege, @Enabled);
                                END";

                                using (SqlCommand cmd = new SqlCommand(upsertSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@MachineID", machineId);
                                    cmd.Parameters.AddWithValue("@EnrollNo", user.EnrollNumber);
                                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrEmpty(user.Name) ? "Unknown" : user.Name);
                                    cmd.Parameters.AddWithValue("@Privilege", user.Privilege);
                                    cmd.Parameters.AddWithValue("@Enabled", user.Enabled);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        lbl_msg.Text = $"User Sync Complete! Processed {users.Count} users from device.";
                        lbl_msg.CssClass = "text-success font-weight-bold";
                    }
                    else
                    {
                        lbl_msg.Text = $"Connected, but no users found on device.";
                        lbl_msg.CssClass = "text-info font-weight-bold";
                    }
                }
                else
                {
                    lbl_msg.Text = $"User Sync failed. Cannot connect. Error: {errorCode}";
                    lbl_msg.CssClass = "text-danger font-weight-bold";
                }
            }
        }


    }
}