using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.Services;
using System.Web.UI.WebControls;
using System.Web.UI;
using AnmolDristi.DAL;
using AnmolDristi.Controls;

namespace AnmolDristi
{
    public partial class VMS_MyVisits : System.Web.UI.Page
    {
        private readonly VmsVisitorRepository _repository = new VmsVisitorRepository();
        private Dictionary<int, List<VisitDayLogInfo>> _dayLogsByVisit;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!ValidateVisitorSession())
                    return;

                LoadVisitorHistory();
                BindAnotherHostLink();
            }
        }

        /// <summary>
        /// Shows a Registration link when the plant BranchId is known (query or session)
        /// so a visitor with a pass for Host A can still request Host B.
        /// </summary>
        private void BindAnotherHostLink()
        {
            pnlAnotherHost.Visible = false;

            int branchId = 0;
            string queryBranch = Request.QueryString["BranchId"];
            if (!string.IsNullOrEmpty(queryBranch) && int.TryParse(queryBranch, out branchId) && branchId > 0)
                Session["VmsBranchId"] = branchId;
            else if (Session["VmsBranchId"] != null)
                int.TryParse(Session["VmsBranchId"].ToString(), out branchId);

            if (branchId <= 0)
                return;

            pnlAnotherHost.Visible = true;
            lnkAnotherHost.NavigateUrl = $"VMS_Registration.aspx?BranchId={branchId}";
        }

        private bool ValidateVisitorSession()
        {
            // Security Check: Look for the NEW Dual-Auth session keys
            if (Session["VerifiedMobile"] == null ||
                string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
            {
                // Safe Redirect Pattern
                Response.Redirect("VMS_SelfLogin.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            return true;
        }

        private void LoadVisitorHistory()
        {
            string mobile = Session["VerifiedMobile"] != null
                ? Session["VerifiedMobile"].ToString()
                : string.Empty;
            string email = Session["VerifiedEmail"] != null
                ? Session["VerifiedEmail"].ToString()
                : string.Empty;

            DataTable dt = _repository.GetVisitorHistory(mobile, email);
            LoadDayLogs(dt);
            BindStatusTabs(dt);
        }

        private void LoadDayLogs(DataTable dt)
        {
            var logIds = new List<int>();
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row["LogID"] == DBNull.Value)
                        continue;
                    int logId = Convert.ToInt32(row["LogID"]);
                    if (logId > 0 && !logIds.Contains(logId))
                        logIds.Add(logId);
                }
            }

            _dayLogsByVisit = logIds.Count == 0
                ? new Dictionary<int, List<VisitDayLogInfo>>()
                : _repository.GetDayLogsForVisits(logIds);
        }

        private void BindStatusTabs(DataTable dt)
        {
            DateTime now = DateTime.Now;
            DataTable active = dt.Clone();
            DataTable pending = dt.Clone();
            DataTable rejected = dt.Clone();
            DataTable expired = dt.Clone();

            foreach (DataRow row in dt.Rows)
            {
                string status = row["ApprovalStatus"] == DBNull.Value
                    ? string.Empty
                    : row["ApprovalStatus"].ToString().Trim();

                if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    pending.ImportRow(row);
                }
                else if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    rejected.ImportRow(row);
                }
                else if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    // NULL ValidUpto: keep under Active Passes (do not treat missing date as expired).
                    if (row["ValidUpto"] == DBNull.Value || row["ValidUpto"] == null)
                    {
                        active.ImportRow(row);
                    }
                    else
                    {
                        DateTime validUpto = Convert.ToDateTime(row["ValidUpto"]);
                        if (validUpto >= now)
                            active.ImportRow(row);
                        else
                            expired.ImportRow(row);
                    }
                }
            }

            BindCategoryRepeater(rptVisitsActivePasses, lblEmptyActivePasses, active);
            BindCategoryRepeater(rptVisitsPending, lblEmptyPending, pending);
            BindCategoryRepeater(rptVisitsRejected, lblEmptyRejected, rejected);
            BindCategoryRepeater(rptVisitsExpiredPasses, lblEmptyExpiredPasses, expired);
        }

        private void BindCategoryRepeater(Repeater repeater, Label emptyLabel, DataTable categoryRows)
        {
            repeater.DataSource = categoryRows;
            repeater.DataBind();
            emptyLabel.Visible = categoryRows.Rows.Count == 0;
        }

        protected string FormatVisitSchedule(
            object scheduleTypeObj,
            object fromObj,
            object toObj,
            object validFromObj = null,
            object validToObj = null,
            object approvalStatusObj = null)
        {
            if (fromObj == null || fromObj == DBNull.Value || toObj == null || toObj == DBNull.Value)
                return "Not specified";

            DateTime from = Convert.ToDateTime(fromObj);
            DateTime to = Convert.ToDateTime(toObj);
            string scheduleType = scheduleTypeObj?.ToString() ?? "SingleDay";
            string approvalStatus = approvalStatusObj == null || approvalStatusObj == DBNull.Value
                ? string.Empty
                : approvalStatusObj.ToString();

            if (string.Equals(approvalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                && validFromObj != null && validFromObj != DBNull.Value
                && validToObj != null && validToObj != DBNull.Value)
            {
                from = Convert.ToDateTime(validFromObj);
                to = Convert.ToDateTime(validToObj);
            }

            string timeFrom = VmsVisitScheduleHelper.FormatClockTime(from.TimeOfDay);
            string timeTo = VmsVisitScheduleHelper.FormatClockTime(to.TimeOfDay);

            if (scheduleType == "MultiDay")
                return $"{from:dd MMM yyyy} to {to:dd MMM yyyy}, daily {timeFrom} - {timeTo}";

            return $"{from:dd MMM yyyy}, {timeFrom} - {timeTo}";
        }

        protected void rptVisits_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                StatusBadgeControl ucStatus = (StatusBadgeControl)e.Item.FindControl("ucStatus");
                if (ucStatus != null)
                    ucStatus.Bind(Convert.ToString(DataBinder.Eval(e.Item.DataItem, "ApprovalStatus")));

                string status = Convert.ToString(DataBinder.Eval(e.Item.DataItem, "ApprovalStatus")).ToLower();

                VisitWindowControl.BindHostFromRepeater(e.Item, _dayLogsByVisit);

                // 2. Process Digital Pass Footer
                Panel pnlFooter = (Panel)e.Item.FindControl("pnlFooter");

                if (status == "approved")
                {
                    // Full Screen Pass only for Active Passes — never for Expired Passes.
                    if (!ReferenceEquals(sender, rptVisitsExpiredPasses))
                    {
                        object tokenObj = DataBinder.Eval(e.Item.DataItem, "QRToken");

                        if (tokenObj != DBNull.Value && tokenObj != null && !string.IsNullOrEmpty(tokenObj.ToString()))
                        {
                            pnlFooter.Visible = true;

                            Panel pnlPassLink = (Panel)e.Item.FindControl("pnlPassLink");
                            if (pnlPassLink != null)
                                pnlPassLink.Visible = true;
                        }
                    }
                }
                else
                {
                    if (pnlFooter != null)
                        pnlFooter.Visible = true;

                    Panel pnlPassLink = (Panel)e.Item.FindControl("pnlPassLink");
                    if (pnlPassLink != null)
                        pnlPassLink.Visible = false;

                    Label lblQRMessage = (Label)e.Item.FindControl("lblQRMessage");
                    if (lblQRMessage != null)
                    {
                        if (status == "pending")
                        {
                            lblQRMessage.Text = "Waiting for host approval. This page will show your gate pass once approved.";
                            lblQRMessage.Visible = true;
                        }
                        else if (status == "rejected")
                        {
                            lblQRMessage.Text = "This visit request was rejected by the host.";
                            lblQRMessage.Visible = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sliding session ping for the visitor MyVisits page only.
        /// DigitalPass is token-authenticated and must not use this.
        /// </summary>
        [WebMethod(EnableSession = true)]
        public static bool KeepAlive()
        {
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            if (session == null)
                return false;

            object mobile = session["VerifiedMobile"];
            return mobile != null && !string.IsNullOrEmpty(mobile.ToString());
        }
    }
}
