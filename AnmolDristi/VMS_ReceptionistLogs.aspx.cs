using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using System.Collections.Generic;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_ReceptionistLogs : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private readonly VmsVisitorManagementRepository _visitorManagementRepository =
            new VmsVisitorManagementRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindBranches();

                // Month-wise report: first day through last day of the current month
                DateTime today = DateTime.Now.Date;
                DateTime monthStart = new DateTime(today.Year, today.Month, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
                txtFromDate.Text = monthStart.ToString("yyyy-MM-dd");
                txtToDate.Text = monthEnd.ToString("yyyy-MM-dd");

                LoadDashboardData();
            }
        }

        private void BindBranches()
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT Branch_ID, Branch_Description FROM MST_Branch where VMS_Menu=1 ORDER BY Branch_Description";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ddlBranch.DataSource = dt;
                        ddlBranch.DataTextField = "Branch_Description";
                        ddlBranch.DataValueField = "Branch_ID";
                        ddlBranch.DataBind();
                        ddlBranch.Items.Insert(0, new ListItem("-- All Branches --", "0"));
                    }
                }
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            BindGrid();
            BindChart();
        }

        private void BindGrid()
        {
            DateTime fromDate;
            DateTime toDate;
            if (!TryGetInclusiveReportRange(out fromDate, out toDate))
                return;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT 
                v.LogID, 
                v.VisitorName, 
                v.VisitorCompany, 
                v.PassCategory, 
                v.PhotoPath,
                v.VisitorMobile,
                v.VisitorEmail,
                v.ApprovalStatus,
                v.HostEmployeeCode, 
                ISNULL(u.EmployeeName, 'Unknown Host') AS HostName,
                ISNULL(u.Mobile, 'N/A') AS HostMobile, 
                ISNULL(u.Email, 'N/A') AS HostEmail,
                v.VisitStatus, 
                v.CreatedDate,
                v.ApprovedOn,
                CASE WHEN d.DayLogID IS NOT NULL THEN d.ActualInTime ELSE v.ActualInTime END AS ActualInTime,
                CASE WHEN d.DayLogID IS NOT NULL THEN d.ActualOutTime ELSE v.ActualOutTime END AS ActualOutTime,
                b.Branch_ID, 
                b.Branch_Description
            FROM VMS_VisitLog v
            LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
            LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
            LEFT JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                AND d.VisitDate = CAST(GETDATE() AS date)
            WHERE CAST(v.CreatedDate as DATE) >= @FromDate 
              AND CAST(v.CreatedDate as DATE) <= @ToDate ";

                // Apply Branch Filter
                if (ddlBranch.SelectedValue != "0") query += " AND v.TargetBranchID = @BranchID ";

                // Apply Status Filter
                if (ddlStatus.SelectedValue == "ActiveDefault")
                {
                    query += " AND ISNULL(v.VisitStatus, '') NOT IN ('Cancelled', 'Rejected') AND ISNULL(v.ApprovalStatus, '') != 'Rejected' ";
                }
                else if (ddlStatus.SelectedValue == "Cancelled")
                {
                    query += " AND (ISNULL(v.VisitStatus, '') IN ('Cancelled', 'Rejected') OR ISNULL(v.ApprovalStatus, '') = 'Rejected') ";
                }
                else if (ddlStatus.SelectedValue != "All")
                {
                    query += " AND ISNULL(v.VisitStatus, '') = @Status ";
                }

                query += " ORDER BY v.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    if (ddlBranch.SelectedValue != "0") cmd.Parameters.AddWithValue("@BranchID", ddlBranch.SelectedValue);
                    if (ddlStatus.SelectedValue != "ActiveDefault" && ddlStatus.SelectedValue != "Cancelled" && ddlStatus.SelectedValue != "All")
                    {
                        cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                    }

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvVisitorLogs.DataSource = dt;
                        gvVisitorLogs.DataBind();

                        ViewState["ExportData"] = dt;
                    }
                }
            }
        }

        private void BindChart()
        {
            DateTime fromDate;
            DateTime toDate;
            if (!TryGetInclusiveReportRange(out fromDate, out toDate))
                return;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT 
                CAST(v.CreatedDate as DATE) AS VisitDate,
                SUM(CASE WHEN v.PassCategory = 'I Work Here' THEN 1 ELSE 0 END) AS InternalCount,
                SUM(CASE WHEN v.PassCategory = 'Meeting Appointment' THEN 1 ELSE 0 END) AS MeetingCount,
                SUM(CASE WHEN v.PassCategory = 'Delivery' THEN 1 ELSE 0 END) AS DeliveryCount,
                SUM(CASE WHEN v.PassCategory = 'Service' THEN 1 ELSE 0 END) AS ServiceCount,
                SUM(CASE WHEN v.PassCategory = 'Interview' THEN 1 ELSE 0 END) AS InterviewCount
            FROM VMS_VisitLog v
            WHERE CAST(v.CreatedDate as DATE) >= @FromDate 
              AND CAST(v.CreatedDate as DATE) <= @ToDate ";

                // Apply Branch Filter
                if (ddlBranch.SelectedValue != "0") query += " AND v.TargetBranchID = @BranchID ";

                // Apply Status Filter to chart so numbers match the grid exactly
                if (ddlStatus.SelectedValue == "ActiveDefault")
                {
                    query += " AND ISNULL(v.VisitStatus, '') NOT IN ('Cancelled', 'Rejected') AND ISNULL(v.ApprovalStatus, '') != 'Rejected' ";
                }
                else if (ddlStatus.SelectedValue == "Cancelled")
                {
                    query += " AND (ISNULL(v.VisitStatus, '') IN ('Cancelled', 'Rejected') OR ISNULL(v.ApprovalStatus, '') = 'Rejected') ";
                }
                else if (ddlStatus.SelectedValue != "All")
                {
                    query += " AND ISNULL(v.VisitStatus, '') = @Status ";
                }

                query += " GROUP BY CAST(v.CreatedDate as DATE) ORDER BY CAST(v.CreatedDate as DATE) ASC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    if (ddlBranch.SelectedValue != "0") cmd.Parameters.AddWithValue("@BranchID", ddlBranch.SelectedValue);
                    if (ddlStatus.SelectedValue != "ActiveDefault" && ddlStatus.SelectedValue != "Cancelled" && ddlStatus.SelectedValue != "All")
                    {
                        cmd.Parameters.AddWithValue("@Status", ddlStatus.SelectedValue);
                    }

                    Dictionary<DateTime, int[]> countsByDate = new Dictionary<DateTime, int[]>();

                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            DateTime visitDate = Convert.ToDateTime(sdr["VisitDate"]).Date;
                            countsByDate[visitDate] = new int[]
                            {
                                Convert.ToInt32(sdr["InternalCount"]),
                                Convert.ToInt32(sdr["MeetingCount"]),
                                Convert.ToInt32(sdr["DeliveryCount"]),
                                Convert.ToInt32(sdr["ServiceCount"]),
                                Convert.ToInt32(sdr["InterviewCount"])
                            };
                        }
                    }
                    con.Close();

                    List<string> labels = new List<string>();
                    List<int> internalData = new List<int>();
                    List<int> meetingData = new List<int>();
                    List<int> deliveryData = new List<int>();
                    List<int> serviceData = new List<int>();
                    List<int> interviewData = new List<int>();

                    // Inclusive first-through-last day of the selected range, including zero-visit days.
                    for (DateTime day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
                    {
                        labels.Add(day.ToString("dd-MMM", CultureInfo.InvariantCulture));
                        int[] counts;
                        if (!countsByDate.TryGetValue(day, out counts))
                            counts = new int[] { 0, 0, 0, 0, 0 };

                        internalData.Add(counts[0]);
                        meetingData.Add(counts[1]);
                        deliveryData.Add(counts[2]);
                        serviceData.Add(counts[3]);
                        interviewData.Add(counts[4]);
                    }

                    string jsonLabels = labels.Count > 0 ? "[\"" + string.Join("\",\"", labels) + "\"]" : "[]";
                    string jsonInternal = "[" + string.Join(",", internalData) + "]";
                    string jsonMeeting = "[" + string.Join(",", meetingData) + "]";
                    string jsonDelivery = "[" + string.Join(",", deliveryData) + "]";
                    string jsonService = "[" + string.Join(",", serviceData) + "]";
                    string jsonInterview = "[" + string.Join(",", interviewData) + "]";

                    ScriptManager.RegisterStartupScript(UpdatePanelGrid, UpdatePanelGrid.GetType(), "renderChart",
                        $"renderChart('{jsonLabels}', '{jsonInternal}', '{jsonMeeting}', '{jsonDelivery}', '{jsonService}', '{jsonInterview}');", true);
                }
            }
        }

        private bool TryGetInclusiveReportRange(out DateTime fromDate, out DateTime toDate)
        {
            DateTime today = DateTime.Now.Date;
            DateTime monthStart = new DateTime(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            if (!DateTime.TryParse(txtFromDate.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate))
                fromDate = monthStart;
            if (!DateTime.TryParse(txtToDate.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
                toDate = monthEnd;

            fromDate = fromDate.Date;
            toDate = toDate.Date;
            if (toDate < fromDate)
                toDate = fromDate;

            return true;
        }

        protected void gvVisitorLogs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int logId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "ViewDetails")
            {
                PopulateModal(logId);
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowModal", "showModal();", true);
            }
            else if (e.CommandName == "ApproveVisit")
            {
                ExecuteAction(logId, "Approve");
            }
            else if (e.CommandName == "CheckInVisit")
            {
                ExecuteAction(logId, "CheckIn");
            }
            else if (e.CommandName == "CheckOutVisit")
            {
                ExecuteAction(logId, "CheckOut");
            }
        }

        private void ExecuteAction(int logId, string actionType)
        {
            if (actionType == "Approve")
            {
                // AR-VMS-02: shared VisitLog+DayLog approval (QR, RequestedValid*, pass notify, Pending guard, tx).
                _visitorManagementRepository.ApproveVisitByReceptionist(
                    new VisitorApprovalRequest
                    {
                        LogId = logId,
                        ApprovalRemarksSuffix =
                            "[Reception Override] Force Approved on " + DateTime.Now.ToString("dd-MMM-yy HH:mm")
                    });

                LoadDashboardData();
                return;
            }

            if (actionType == "CheckIn")
            {
                // AR-VMS-03B: DayLog-aware desk Check-In (CheckInDay when today approved).
                ReceptionistAttendanceResult result = _visitorManagementRepository.ReceptionistManualCheckIn(logId);
                if (!result.Succeeded)
                {
                    RegisterAttendanceFailureScript(FormatReceptionistAttendanceFailure(result.FailureReason, isCheckIn: true));
                }

                LoadDashboardData();
                return;
            }

            if (actionType == "CheckOut")
            {
                // AR-VMS-03B: DayLog-aware desk Check-Out (CheckOutDay / MultiDay Parent Scheduled).
                ReceptionistAttendanceResult result = _visitorManagementRepository.ReceptionistManualCheckOut(logId);
                if (!result.Succeeded)
                {
                    RegisterAttendanceFailureScript(FormatReceptionistAttendanceFailure(result.FailureReason, isCheckIn: false));
                }

                LoadDashboardData();
                return;
            }
        }

        private void RegisterAttendanceFailureScript(string message)
        {
            string safe = (message ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(
                UpdatePanelGrid,
                UpdatePanelGrid.GetType(),
                "ReceptionistAttendanceFail",
                "alert('" + safe + "');",
                true);
        }

        private static string FormatReceptionistAttendanceFailure(
            ReceptionistAttendanceFailureReason reason,
            bool isCheckIn)
        {
            switch (reason)
            {
                case ReceptionistAttendanceFailureReason.TodayNotApproved:
                    return isCheckIn
                        ? "Unable to Check-In. Today is not approved for this visit."
                        : "Unable to Check-Out. Today is not approved for this visit.";
                case ReceptionistAttendanceFailureReason.StateChanged:
                    return isCheckIn
                        ? "Unable to Check-In. Status may have changed or the visitor is already checked in."
                        : "Unable to Check-Out. Status may have changed or the visitor is not checked in.";
                default:
                    return isCheckIn
                        ? "Unable to Check-In. Please refresh and try again."
                        : "Unable to Check-Out. Please refresh and try again.";
            }
        }

        private void PopulateModal(int logId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"SELECT * FROM VMS_VisitLog WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            lblMdlName.Text = sdr["VisitorName"].ToString();
                            lblMdlMobile.Text = sdr["VisitorMobile"].ToString();
                            lblMdlEmail.Text = sdr["VisitorEmail"].ToString();
                            lblMdlCompany.Text = sdr["VisitorCompany"].ToString();
                            lblMdlCategory.Text = sdr["PassCategory"].ToString();
                            lblMdlHost.Text = sdr["HostEmployeeCode"].ToString();
                            lblMdlPurpose.Text = sdr["PurposeOfVisit"].ToString();
                            lblMdlValid.Text = Convert.ToDateTime(sdr["ValidUpto"]).ToString("dd-MMM-yyyy HH:mm");
                            lblMdlStatus.Text = sdr["VisitStatus"].ToString();
                            lblMdlStatusBadge.Text = sdr["VisitStatus"].ToString(); // Feeds the nice badge under the photo
                            lblMdlMeal.Text = Convert.ToBoolean(sdr["IsMealRequired"]) ? "Yes" : "No";

                            // Handle the Photo Binding
                            string photoPath = sdr["PhotoPath"].ToString();
                            if (string.IsNullOrEmpty(photoPath))
                            {
                                // Fallback image if the visitor didn't snap a photo
                                imgMdlVisitor.ImageUrl = "WebData/No_Image.jpg";
                            }
                            else
                            {
                                // Bind the actual physical image path or base64 data
                                imgMdlVisitor.ImageUrl = photoPath;
                            }
                        }
                    }
                    con.Close();
                }
            }
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            if (ViewState["ExportData"] != null)
            {
                DataTable dt = (DataTable)ViewState["ExportData"];
                string attachment = "attachment; filename=VisitorLogs_" + DateTime.Now.ToString("yyyyMMdd") + ".xls";
                Response.ClearContent();
                Response.AddHeader("content-disposition", attachment);
                Response.ContentType = "application/vnd.ms-excel";

                string tab = "";
                foreach (DataColumn dc in dt.Columns)
                {
                    Response.Write(tab + dc.ColumnName);
                    tab = "\t";
                }
                Response.Write("\n");

                foreach (DataRow dr in dt.Rows)
                {
                    tab = "";
                    foreach (DataColumn dc in dt.Columns)
                    {
                        Response.Write(tab + dr[dc.ColumnName].ToString().Replace("\t", ""));
                        tab = "\t";
                    }
                    Response.Write("\n");
                }
                Response.End();
            }
        }
    }
}