using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class ClaimsSubmitted : System.Web.UI.Page
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DbConn"]?.ConnectionString ?? "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // require USERID and USERNAME as minimum for a logged-in user
                if (Session["USERID"] != null && Session["USERNAME"] != null)
                {
                    BindRequests();
                }
            }
        }

        private string GetCurrentLoginId()
        {
            string id = HttpContext.Current?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(id))
                id = Session["WORKMAN"] as string;
            return id ?? string.Empty;
        }

        private void BindRequests()
        {
            string user = GetCurrentLoginId();
            if (string.IsNullOrEmpty(user))
            {
                lblMessage.Text = "Unable to determine current user. Please login.";
                gvRequests.DataSource = null;
                gvRequests.DataBind();
                return;
            }

            DataTable dt = new DataTable();
            using (var cn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestsForUser", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RequesterLoginId", user);
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            if (dt.Columns.Contains("RequestDate"))
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (r["RequestDate"] != DBNull.Value)
                        r["RequestDate"] = ConvertUtcToIstString((DateTime)r["RequestDate"]);
                }
            }

            gvRequests.DataSource = dt;
            gvRequests.DataBind();
        }

        protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvRequests.PageIndex = e.NewPageIndex;
            BindRequests();
        }

        protected void gvRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView drv = e.Row.DataItem as DataRowView;
            if (drv == null) return;

            int approvalRequestId = Convert.ToInt32(drv["ApprovalRequestId"]);
            string panelId = "pnlDetails_" + approvalRequestId;

            // build panel (hidden by default)
            Panel pnl = new Panel { ID = panelId, CssClass = "panel-details" };
            pnl.Style[HtmlTextWriterStyle.Display] = "none";

            // 1) Line items (details) - use theme's table classes
            pnl.Controls.Add(new LiteralControl("<h6 class='mb-1'>Line items</h6>"));
            GridView gvDetails = new GridView
            {
                ID = "gvDetails_" + approvalRequestId,
                CssClass = "table table-sm table-bordered",
                AutoGenerateColumns = false
            };

            gvDetails.Columns.Add(new BoundField { HeaderText = "Activity", DataField = "ActivityId" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Distance (KM)", DataField = "Snapshot_TotalDistanceKM", DataFormatString = "{0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Claim Amt", DataField = "Snapshot_CalculatedClaimAmount", DataFormatString = "₹ {0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "DA", DataField = "Snapshot_DAAmount", DataFormatString = "₹ {0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Hotel", DataField = "Snapshot_HotelAmount", DataFormatString = "₹ {0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Travel", DataField = "Snapshot_TravelAmount", DataFormatString = "₹ {0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Expense Total", DataField = "Snapshot_ExpenseTotal", DataFormatString = "₹ {0:N2}" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Status", DataField = "DetailStatus" });
            gvDetails.Columns.Add(new BoundField { HeaderText = "Remarks", DataField = "DetailRemarks" });

            DataTable dtDetails = GetRequestDetails(approvalRequestId);
            if (dtDetails != null)
            {
                if (dtDetails.Columns.Contains("CreatedOn"))
                {
                    foreach (DataRow r in dtDetails.Rows)
                    {
                        if (r["CreatedOn"] != DBNull.Value)
                            r["CreatedOn"] = ConvertUtcToIstString((DateTime)r["CreatedOn"]);
                        if (dtDetails.Columns.Contains("DetailApprovedOn") && r["DetailApprovedOn"] != DBNull.Value)
                            r["DetailApprovedOn"] = ConvertUtcToIstString((DateTime)r["DetailApprovedOn"]);
                    }
                }
                gvDetails.DataSource = dtDetails;
                gvDetails.DataBind();
            }
            pnl.Controls.Add(gvDetails);

            // 2) Approver snapshot (read-only)
            pnl.Controls.Add(new LiteralControl("<hr/><h6 class='mb-1'>Approver stage</h6>"));
            GridView gvApprovers = new GridView
            {
                ID = "gvApprovers_" + approvalRequestId,
                CssClass = "table table-sm table-bordered",
                AutoGenerateColumns = false
            };
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Level", DataField = "ApproverLevel" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Approver", DataField = "ApproverLoginId" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Role", DataField = "ApproverRole" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Required", DataField = "IsRequired" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Status", DataField = "ComputedApproverStatus" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Action By", DataField = "ActionBy" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Action On (IST)", DataField = "ActionDate" });
            gvApprovers.Columns.Add(new BoundField { HeaderText = "Remarks", DataField = "Remarks" });

            DataTable dtApprovers = GetApproversWithComputedState(approvalRequestId);
            if (dtApprovers != null)
            {
                if (dtApprovers.Columns.Contains("ActionDate"))
                {
                    foreach (DataRow r in dtApprovers.Rows)
                    {
                        if (r["ActionDate"] != DBNull.Value)
                            r["ActionDate"] = ConvertUtcToIstString((DateTime)r["ActionDate"]);
                    }
                }
                gvApprovers.DataSource = dtApprovers;
                gvApprovers.DataBind();

                // apply visual hints
                for (int i = 0; i < dtApprovers.Rows.Count; i++)
                {
                    GridViewRow aprow = gvApprovers.Rows[i];
                    DataRow row = dtApprovers.Rows[i];
                    bool isCurrent = row.Table.Columns.Contains("IsCurrentApproverLevel") && Convert.ToBoolean(row["IsCurrentApproverLevel"]);
                    string computedStatus = row.Table.Columns.Contains("ComputedApproverStatus") ? row["ComputedApproverStatus"].ToString() : "";

                    if (isCurrent) aprow.CssClass = "highlight-current";
                    if (ClaimStatuses.Equals(computedStatus, ClaimStatuses.Rejected)) aprow.CssClass = "rejected-row";
                    bool isRequired = row.Table.Columns.Contains("IsRequired") && Convert.ToBoolean(row["IsRequired"]);
                    if (!isRequired && aprow.Cells.Count > 0) aprow.Cells[0].CssClass = "not-required";
                }
            }
            pnl.Controls.Add(gvApprovers);

            // 3) History (read-only)
            pnl.Controls.Add(new LiteralControl("<hr/><h6 class='mb-1'>Approval history</h6>"));
            GridView gvHistory = new GridView
            {
                ID = "gvHistory_" + approvalRequestId,
                CssClass = "table table-sm table-bordered",
                AutoGenerateColumns = false
            };
            gvHistory.Columns.Add(new BoundField { HeaderText = "When (IST)", DataField = "ChangedOn" });
            gvHistory.Columns.Add(new BoundField { HeaderText = "By", DataField = "ChangedBy" });
            gvHistory.Columns.Add(new BoundField { HeaderText = "Old Status", DataField = "OldStatus" });
            gvHistory.Columns.Add(new BoundField { HeaderText = "New Status", DataField = "NewStatus" });
            gvHistory.Columns.Add(new BoundField { HeaderText = "Remarks", DataField = "Remarks" });

            DataTable dtHistory = GetRequestHistory(approvalRequestId);
            if (dtHistory != null)
            {
                if (dtHistory.Columns.Contains("ChangedOn"))
                {
                    foreach (DataRow r in dtHistory.Rows)
                    {
                        if (r["ChangedOn"] != DBNull.Value)
                            r["ChangedOn"] = ConvertUtcToIstString((DateTime)r["ChangedOn"]);
                    }
                }
                gvHistory.DataSource = dtHistory;
                gvHistory.DataBind();
            }
            pnl.Controls.Add(gvHistory);

            // append panel to Actions cell
            TableCell actionCell = e.Row.Cells[e.Row.Cells.Count - 1];
            actionCell.Controls.Add(new LiteralControl("<br/>"));
            actionCell.Controls.Add(pnl);

            // Wire 'Open' link to a read-only view
            HyperLink hlView = e.Row.FindControl("hlView") as HyperLink;
            if (hlView != null)
                hlView.NavigateUrl = ResolveUrl("~/ViewClaim.aspx?ApprovalRequestId=" + approvalRequestId);
        }

        private DataTable GetRequestDetails(int approvalRequestId)
        {
            DataTable dt = new DataTable();
            using (var cn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        private DataTable GetApproversWithComputedState(int approvalRequestId)
        {
            DataTable dt = new DataTable();
            using (var cn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("dbo.usp_GetApprovers_WithComputedState", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        private DataTable GetRequestHistory(int approvalRequestId)
        {
            DataTable dt = new DataTable();
            using (var cn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT HistoryId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks FROM dbo.ApprovalRequestStatusHistory WHERE ApprovalRequestId = @ApprovalRequestId ORDER BY ChangedOn", cn))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        private string ConvertUtcToIstString(DateTime utcDate)
        {
            DateTime utc = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
            try
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime ist = TimeZoneInfo.ConvertTimeFromUtc(utc, istZone);
                return ist.ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
                DateTime ist = utc.AddHours(5).AddMinutes(30);
                return ist.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
}
