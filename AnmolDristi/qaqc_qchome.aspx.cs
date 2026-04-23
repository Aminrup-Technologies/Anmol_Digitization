using System;
using System.Configuration;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class qaqc_qchome : BasePage
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

                            // Match against the exact FormName strings in your DB
                            switch (formName)
                            {
                                // --- PACKING MATERIALS ---
                                case "PM_LAMINATETESTING":
                                    lblDocPMLaminate.Text = docNo;
                                    lblRevPMLaminate.Text = revNo;
                                    lblFreqPMLaminate.Text = combinedFrequency;
                                    break;

                                case "PM_OVERWRAPREPORT":
                                    lblDocPMOverwrap.Text = docNo;
                                    lblRevPMOverwrap.Text = revNo;
                                    lblFreqPMOverwrap.Text = combinedFrequency;
                                    break;

                                case "PM_CORRUGATEDBOARDBOXREPORT":
                                    lblDocPMCBB.Text = docNo;
                                    lblRevPMCBB.Text = revNo;
                                    lblFreqPMCBB.Text = combinedFrequency;
                                    break;

                                case "PM_BOPPTAPEREPORT":
                                    lblDocPMTape.Text = docNo;
                                    lblRevPMTape.Text = revNo;
                                    lblFreqPMTape.Text = combinedFrequency;
                                    break;

                                case "PM_PVCTRAYREPORT":
                                    lblDocPMTray.Text = docNo;
                                    lblRevPMTray.Text = revNo;
                                    lblFreqPMTray.Text = combinedFrequency;
                                    break;

                                // --- RAW MATERIALS ---
                                case "RM_AATA_MAIDA":
                                case "AATA_MAIDA_FORM":
                                    lblDocRMAtta.Text = docNo;
                                    lblRevRMAtta.Text = revNo;
                                    lblFreqRMAtta.Text = combinedFrequency;
                                    break;

                                case "RM_CLASS_1":
                                    lblDocRM1.Text = docNo;
                                    lblRevRM1.Text = revNo;
                                    lblFreqRM1.Text = combinedFrequency;
                                    break;

                                case "RM_CLASS_2":
                                    lblDocRM2.Text = docNo;
                                    lblRevRM2.Text = revNo;
                                    lblFreqRM2.Text = combinedFrequency;
                                    break;

                                case "RM_CLASS_3":
                                    lblDocRM3.Text = docNo;
                                    lblRevRM3.Text = revNo;
                                    lblFreqRM3.Text = combinedFrequency;
                                    break;

                                case "RM_CLASS_4":
                                    lblDocRM4.Text = docNo;
                                    lblRevRM4.Text = revNo;
                                    lblFreqRM4.Text = combinedFrequency;
                                    break;

                                case "RM_CLASS_5":
                                    lblDocRM5.Text = docNo;
                                    lblRevRM5.Text = revNo;
                                    lblFreqRM5.Text = combinedFrequency;
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error if needed
                }
            }
        }
    }
}