using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

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
                    Response.Redirect("login.aspx", false);
                }
                else
                {
                    BindDataToLabels();
                }
            }
        }

        private void BindDataToLabels()
        {
            // Fetch the 7-day array data
            DataTable sparklineData = GetLast7DaysData();

            // EXACT DATE FIX: Extract the actual dates from the Database
            List<string> shortDays = new List<string>();
            List<string> fullDates = new List<string>();

            foreach (DataRow row in sparklineData.Rows)
            {
                DateTime dateVal = Convert.ToDateTime(row["DateVal"]);
                shortDays.Add(dateVal.ToString("ddd")); // "Mon", "Tue"
                fullDates.Add(dateVal.ToString("dddd, MMM dd, yyyy")); // "Monday, Apr 20, 2026"
            }

            // Assign to hidden fields for JS to read
            hfDayLabels.Value = string.Join(",", shortDays);
            hfFullDates.Value = string.Join("|", fullDates);

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Fetch Today/Yesterday summary
            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("GetTodaysRecordCounts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        string tableName = row["TableName"].ToString();
                        double todayCount = Convert.ToDouble(row["TodayCount"]);
                        double yesterdayCount = Convert.ToDouble(row["YesterdayCount"]);

                        string trendText = $"Yesterday: {yesterdayCount}  (Steady)";
                        string trendIcon = "<i class='fa fa-minus'></i>";
                        string colorClass = "text-muted";

                        if (yesterdayCount > 0)
                        {
                            double percentChange = Math.Round(((todayCount - yesterdayCount) / yesterdayCount) * 100, 1);

                            if (percentChange > 0)
                            {
                                trendText = $"Yesterday: {yesterdayCount} &nbsp;(<i class='fa fa-arrow-up'></i> {percentChange}%)";
                                trendIcon = "";
                                colorClass = (tableName == "CRITICAL INCIDENT REPORT") ? "text-danger" : "text-success";
                            }
                            else if (percentChange < 0)
                            {
                                trendText = $"Yesterday: {yesterdayCount} &nbsp;(<i class='fa fa-arrow-down'></i> {Math.Abs(percentChange)}%)";
                                trendIcon = "";
                                colorClass = (tableName == "CRITICAL INCIDENT REPORT") ? "text-success" : "text-danger";
                            }
                        }
                        else if (todayCount > 0 && yesterdayCount == 0)
                        {
                            trendText = $"Yesterday: 0 &nbsp;(<i class='fa fa-arrow-up'></i> 100%)";
                            trendIcon = "";
                            colorClass = (tableName == "CRITICAL INCIDENT REPORT") ? "text-danger" : "text-success";
                        }

                        string html = $"<span class='{colorClass}'>{trendIcon} {trendText}</span>";

                        // Bind Data and Hidden Fields for Chart.js
                        switch (tableName)
                        {
                            case "QUALITY CONTROL INSPECTOR REPORT":
                                lblQCInspector.Text = todayCount.ToString();
                                lblQCInspectorTrend.Text = html;
                                hfQCData.Value = GetSparklineString(sparklineData, "QCInspector");
                                break;

                            case "QA - Process Checking Report":
                                lblProcessChecking.Text = todayCount.ToString();
                                lblProcessTrend.Text = html;
                                hfProcessData.Value = GetSparklineString(sparklineData, "ProcessChecking");
                                break;

                            case "PACKET WEIGHT REPORT":
                                lblPacketWeights.Text = todayCount.ToString();
                                lblPacketTrend.Text = html;
                                hfPacketData.Value = GetSparklineString(sparklineData, "PacketWeight");
                                break;

                            case "ROTARY LINE WT. and OVEN END Variation Report":
                                lblRotaryLine.Text = todayCount.ToString();
                                lblRotaryTrend.Text = html;
                                hfRotaryData.Value = GetSparklineString(sparklineData, "RotaryLine");
                                break;

                            case "CCP Checklist Report":
                                lblCCPChecklist.Text = todayCount.ToString();
                                lblCCPTrend.Text = html;
                                hfCCPData.Value = GetSparklineString(sparklineData, "CCPChecklist");
                                break;

                            case "LEAK/SEAL & SLANTED PACK REPORT":
                                lblLeakSeal.Text = todayCount.ToString();
                                lblLeakTrend.Text = html;
                                hfLeakData.Value = GetSparklineString(sparklineData, "LeakSeal");
                                break;

                            case "PRE DISPATCH CLEARANCE REPORT":
                                lblPreDispatch.Text = todayCount.ToString();
                                lblDispatchTrend.Text = html;
                                hfDispatchData.Value = GetSparklineString(sparklineData, "PreDispatch");
                                break;

                            case "CRITICAL INCIDENT REPORT":
                                lblCriticalIncident.Text = todayCount.ToString();
                                lblCriticalTrend.Text = html;
                                hfCriticalData.Value = GetSparklineString(sparklineData, "CriticalIncident");

                                if (todayCount > 0)
                                    tileCriticalIncident.Attributes["class"] = "tile-stats alert-critical";
                                else
                                    tileCriticalIncident.Attributes["class"] = "tile-stats";
                                break;
                        }
                    }
                }
            }
        }

        private DataTable GetLast7DaysData()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("GetLast7DaysCounts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    ad.Fill(dt);
                    return dt;
                }
            }
        }

        private string GetSparklineString(DataTable dt, string col)
        {
            List<string> vals = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                vals.Add(row[col].ToString());
            }
            return string.Join(",", vals);
        }
    }
}