using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class qaqc_qahome : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx", false);
                }
                else
                {
                    BindFormParameters();
                }
            }
        }

        private void BindFormParameters()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string query = @"
                SELECT 
                    fm.FormName, 
                    fm.DocumentNumber, 
                    fm.RevNo, 
                    ft.FrequencyDescription,
                    freq.FrequencyName
                FROM [dbo].[MST_FormsMaster] fm WITH(NOLOCK)
                LEFT JOIN [dbo].[MST_FrequencyMaster] freq WITH(NOLOCK) 
                    ON fm.FrequencyId = freq.FrequencyId
                LEFT JOIN [dbo].[FrequencyTypes] ft WITH(NOLOCK)
                    ON fm.Frequency = ft.FrequencyID";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Convert to uppercase for case-insensitive matching
                            string formName = reader["FormName"].ToString().Trim().ToUpper();
                            string docNo = reader["DocumentNumber"].ToString();
                            string revNo = reader["RevNo"].ToString();

                            string freqDesc = reader["FrequencyDescription"] != DBNull.Value ? reader["FrequencyDescription"].ToString() : "";
                            string freqName = reader["FrequencyName"] != DBNull.Value ? reader["FrequencyName"].ToString() : "";

                            string combinedFrequency = "Not Defined";
                            if (!string.IsNullOrEmpty(freqDesc) && !string.IsNullOrEmpty(freqName))
                                combinedFrequency = $"{freqDesc} ({freqName})";
                            else if (!string.IsNullOrEmpty(freqDesc))
                                combinedFrequency = freqDesc;
                            else if (!string.IsNullOrEmpty(freqName))
                                combinedFrequency = freqName;

                            // Match against the EXACT strings stored in the database
                            switch (formName)
                            {
                                case "QAQC_PROCESS_RPT":
                                    lblDocProcess.Text = docNo;
                                    lblRevProcess.Text = revNo;
                                    lblFreqProcess.Text = combinedFrequency;
                                    break;

                                case "QAQC_INSPECTOR_RPT":
                                    lblDocInspector.Text = docNo;
                                    lblRevInspector.Text = revNo;
                                    lblFreqInspector.Text = combinedFrequency;
                                    break;

                                // Added a fallback just in case packet weights uses a different name
                                case "QAQC_PACKET_WEIGHT":
                                case "QA_QC_FINALCBBWTREPORT":
                                    lblDocPacket.Text = docNo;
                                    lblRevPacket.Text = revNo;
                                    lblFreqPacket.Text = combinedFrequency;
                                    break;

                                case "QAQC_ROTARY_LINE":
                                    lblDocRotary.Text = docNo;
                                    lblRevRotary.Text = revNo;
                                    lblFreqRotary.Text = combinedFrequency;
                                    break;

                                case "CCP_CHECKLIST":
                                    lblDocCCP.Text = docNo;
                                    lblRevCCP.Text = revNo;
                                    lblFreqCCP.Text = combinedFrequency;
                                    break;

                                case "LEAK_TEST":
                                    lblDocLeak.Text = docNo;
                                    lblRevLeak.Text = revNo;
                                    lblFreqLeak.Text = combinedFrequency;
                                    break;

                                case "QAQC_PRE_DISPATCH_QI_REPORT":
                                    lblDocDispatch.Text = docNo;
                                    lblRevDispatch.Text = revNo;
                                    lblFreqDispatch.Text = combinedFrequency;
                                    break;

                                case "CRITICAL_QUALITY_REPORT":
                                    lblDocCritical.Text = docNo;
                                    lblRevCritical.Text = revNo;
                                    lblFreqCritical.Text = combinedFrequency;
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log error
                }
            }
        }
    }
}