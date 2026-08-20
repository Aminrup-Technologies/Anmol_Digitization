using System;
using System.Data;
using System.Web.UI.WebControls;
using System.Web.UI;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_MyVisits : System.Web.UI.Page
    {
        private readonly VmsVisitorRepository _repository = new VmsVisitorRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!ValidateVisitorSession())
                    return;

                LoadVisitorHistory();
            }
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
            BindStatusTabs(dt);
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

        protected string FormatVisitSchedule(object scheduleTypeObj, object fromObj, object toObj)
        {
            if (fromObj == null || fromObj == DBNull.Value || toObj == null || toObj == DBNull.Value)
                return "Not specified";

            DateTime from = Convert.ToDateTime(fromObj);
            DateTime to = Convert.ToDateTime(toObj);
            string scheduleType = scheduleTypeObj?.ToString() ?? "SingleDay";

            if (scheduleType == "MultiDay")
                return $"{from:dd MMM yyyy} to {to:dd MMM yyyy}, daily {from:HH:mm} - {to:HH:mm}";

            return $"{from:dd MMM yyyy}, {from:HH:mm} - {to:HH:mm}";
        }

        protected void rptVisits_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                // 1. Style the Status Badge dynamically
                Label lblStatus = (Label)e.Item.FindControl("lblStatus");
                string status = lblStatus.Text.ToLower();

                if (status == "approved")
                    lblStatus.CssClass = "badge badge-approved";
                else if (status == "rejected")
                    lblStatus.CssClass = "badge badge-rejected";
                else
                    lblStatus.CssClass = "badge badge-pending";

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
    }
}
