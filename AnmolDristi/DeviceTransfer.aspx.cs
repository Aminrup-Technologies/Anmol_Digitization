using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace AnmolDristi
{
    public partial class DeviceTransfer : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadDevices();
            }
        }

        private void LoadDevices()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT MachineID, MachineName + ' (' + IPAddress + ')' AS DisplayName FROM CMS_MachineMaster WHERE IsActive = 1";
                using (SqlDataAdapter sda = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);

                    ddlSourceDevice.DataSource = dt;
                    ddlSourceDevice.DataTextField = "DisplayName";
                    ddlSourceDevice.DataValueField = "MachineID";
                    ddlSourceDevice.DataBind();
                    ddlSourceDevice.Items.Insert(0, new ListItem("-- Select Source --", "0"));

                    ddlTargetDevice.DataSource = dt;
                    ddlTargetDevice.DataTextField = "DisplayName";
                    ddlTargetDevice.DataValueField = "MachineID";
                    ddlTargetDevice.DataBind();
                    ddlTargetDevice.Items.Insert(0, new ListItem("-- Select Target --", "0"));
                }
            }
        }

        protected void ddlSourceDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlSourceDevice.SelectedValue != "0")
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    // ADDED: 'Not Compared' AS SyncStatus and 'Source Only' AS LocationDetails 
                    // to match the GridView's expected columns!
                    string query = @"
                        SELECT 
                            EnrollNumber, 
                            UserName, 
                            Privilege, 
                            'Not Compared' AS SyncStatus, 
                            'Source Only' AS LocationDetails 
                        FROM CMS_DeviceUsers 
                        WHERE MachineID = @MachineID 
                        ORDER BY EnrollNumber";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MachineID", ddlSourceDevice.SelectedValue);
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvUsers.DataSource = dt;
                            gvUsers.DataBind();
                        }
                    }
                }
                lbl_msg.Text = $"Loaded {gvUsers.Rows.Count} users from Source device. Select users to transfer or click Compare.";
                lbl_msg.CssClass = "text-info font-weight-bold";
            }
            else
            {
                gvUsers.DataSource = null;
                gvUsers.DataBind();
            }
        }

        protected void btnTransfer_Click(object sender, EventArgs e)
        {
            if (ddlSourceDevice.SelectedValue == "0" || ddlTargetDevice.SelectedValue == "0" ||
                ddlSourceDevice.SelectedValue == ddlTargetDevice.SelectedValue)
            {
                lbl_msg.Text = "Please select valid, distinct Source and Target devices.";
                lbl_msg.CssClass = "text-danger font-weight-bold";
                return;
            }

            // Gather selected users
            List<string> selectedUsers = new List<string>();
            foreach (GridViewRow row in gvUsers.Rows)
            {
                CheckBox chk = (CheckBox)row.FindControl("chkSelect");
                if (chk != null && chk.Checked)
                {
                    selectedUsers.Add(gvUsers.DataKeys[row.RowIndex].Value.ToString());
                }
            }

            if (selectedUsers.Count == 0)
            {
                lbl_msg.Text = "Please check at least one user from the grid to transfer.";
                lbl_msg.CssClass = "text-warning font-weight-bold";
                return;
            }

            // Fetch IPs for Source and Target
            string sourceIp = "", targetIp = "";
            int sourcePort = 4370, targetPort = 4370;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT MachineID, IPAddress, PortNumber FROM CMS_MachineMaster WHERE MachineID IN (@Src, @Tgt)", conn))
                {
                    cmd.Parameters.AddWithValue("@Src", ddlSourceDevice.SelectedValue);
                    cmd.Parameters.AddWithValue("@Tgt", ddlTargetDevice.SelectedValue);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["MachineID"].ToString() == ddlSourceDevice.SelectedValue)
                            {
                                sourceIp = reader["IPAddress"].ToString();
                                sourcePort = Convert.ToInt32(reader["PortNumber"]);
                            }
                            else
                            {
                                targetIp = reader["IPAddress"].ToString();
                                targetPort = Convert.ToInt32(reader["PortNumber"]);
                            }
                        }
                    }
                }
            }

            // Execute the Transfer via ZKTecoManager
            ZKTecoManager zkManager = new ZKTecoManager();
            int successCount = 0;
            string errMessage = "";

            lbl_msg.Text = "Transferring... Please wait.";

            bool transferComplete = zkManager.TransferSpecificUsersSTA(sourceIp, sourcePort, targetIp, targetPort, selectedUsers, out successCount, out errMessage);

            if (transferComplete)
            {
                lbl_msg.Text = $"Transfer Successful! {successCount} out of {selectedUsers.Count} users copied to Target Device.";
                lbl_msg.CssClass = "text-success font-weight-bold";
            }
            else
            {
                lbl_msg.Text = $"Transfer Failed. Ensure both devices are online. {errMessage}";
                lbl_msg.CssClass = "text-danger font-weight-bold";
            }
        }

        protected void btnCompare_Click(object sender, EventArgs e)
        {
            if (ddlSourceDevice.SelectedValue == "0" || ddlTargetDevice.SelectedValue == "0")
            {
                lbl_msg.Text = "Please select BOTH a Source and a Target device to compare.";
                lbl_msg.CssClass = "text-danger font-weight-bold";
                return;
            }

            if (ddlSourceDevice.SelectedValue == ddlTargetDevice.SelectedValue)
            {
                lbl_msg.Text = "Source and Target cannot be the same machine.";
                lbl_msg.CssClass = "text-danger font-weight-bold";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                // FULL OUTER JOIN to find who is missing from where
                string query = @"
            SELECT 
                ISNULL(S.EnrollNumber, T.EnrollNumber) AS EnrollNumber,
                ISNULL(S.UserName, T.UserName) AS UserName,
                CASE 
                    WHEN S.EnrollNumber IS NOT NULL AND T.EnrollNumber IS NOT NULL THEN 'Synced'
                    WHEN S.EnrollNumber IS NOT NULL AND T.EnrollNumber IS NULL THEN 'Missing in Target'
                    WHEN S.EnrollNumber IS NULL AND T.EnrollNumber IS NOT NULL THEN 'Missing in Source'
                END AS SyncStatus,
                CASE 
                    WHEN S.EnrollNumber IS NOT NULL AND T.EnrollNumber IS NOT NULL THEN 'Both Machines'
                    WHEN S.EnrollNumber IS NOT NULL AND T.EnrollNumber IS NULL THEN 'Source Only'
                    WHEN S.EnrollNumber IS NULL AND T.EnrollNumber IS NOT NULL THEN 'Target Only'
                END AS LocationDetails
            FROM 
                (SELECT EnrollNumber, UserName FROM CMS_DeviceUsers WHERE MachineID = @SourceID) S
            FULL OUTER JOIN 
                (SELECT EnrollNumber, UserName FROM CMS_DeviceUsers WHERE MachineID = @TargetID) T
            ON S.EnrollNumber = T.EnrollNumber
            ORDER BY SyncStatus, EnrollNumber";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SourceID", ddlSourceDevice.SelectedValue);
                    cmd.Parameters.AddWithValue("@TargetID", ddlTargetDevice.SelectedValue);

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvUsers.DataSource = dt;
                        gvUsers.DataBind();

                        lbl_msg.Text = $"Comparison Complete. Found {dt.Rows.Count} total unique users across both devices.";
                        lbl_msg.CssClass = "text-info font-weight-bold";
                    }
                }
            }
        }

        // Visual formatting for the badges
        protected void gvUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                Label lblSyncStatus = (Label)e.Row.FindControl("lblSyncStatus");
                CheckBox chkSelect = (CheckBox)e.Row.FindControl("chkSelect");

                string status = lblSyncStatus.Text;

                if (status == "Not Compared")
                {
                    lblSyncStatus.CssClass = "badge badge-secondary"; // Standard grey badge
                }
                else if (status == "Synced")
                {
                    lblSyncStatus.CssClass = "badge badge-success";
                    chkSelect.Enabled = false; // Prevent transferring someone who is already synced
                }
                else if (status == "Missing in Target")
                {
                    lblSyncStatus.CssClass = "badge badge-warning"; // Highlight as action needed
                }
                else if (status == "Missing in Source")
                {
                    lblSyncStatus.CssClass = "badge badge-danger";
                    chkSelect.Enabled = false; // Cannot transfer TO target if they only exist IN target
                }
            }
        }
    }
}