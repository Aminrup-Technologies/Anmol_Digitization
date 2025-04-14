using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class qaqc_qahome_rpt : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx",false);
                }
                else
                {
                    BindDataToLabels();
                }
            }
            
        }

        private void BindDataToLabels()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Call the stored procedure
                    using (SqlCommand cmd = new SqlCommand("GetTodaysRecordCounts", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Execute the stored procedure and fetch results into a DataTable
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            // Bind results to labels
                            foreach (DataRow row in dt.Rows)
                            {
                                string tableName = row["TableName"].ToString();
                                string recordCount = row["RecordCount"].ToString();

                                // Find the corresponding label by table name
                                switch (tableName)
                                {
                                    case "QUALITY CONTROL INSPECTOR REPORT":
                                        lblQCInspector.Text = recordCount;
                                        break;

                                    case "QA - Process Checking Report":
                                        lblProcessChecking.Text = recordCount;
                                        break;

                                    case "ROTARY LINE WT. and OVEN END Variation Report":
                                        lblRotaryLine.Text = recordCount;
                                        break;

                                    case "CCP Checklist Report":
                                        lblCCPChecklist.Text = recordCount;
                                        break;

                                    case "LEAK/SEAL & SLANTED PACK REPORT":
                                        lblLeakSeal.Text = recordCount;
                                        break;

                                    case "PRE DISPATCH CLEARANCE REPORT":
                                        lblPreDispatch.Text = recordCount;
                                        break;

                                    case "CRITICAL INCIDENT REPORT":
                                        lblCriticalIncident.Text = recordCount;
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or display the error
                    //lblError.Text = "Error: " + ex.Message;
                }
            }
        }

    }
}