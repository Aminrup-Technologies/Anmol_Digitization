// ========================================================================
// When: 2026-08-22
// What: Enforce ITSM approval-configuration permission on page load and
//       every mutation handler.
// Why: Session authentication alone must not grant approval-policy control.
//      Configuration changes are privileged administrative operations.
// ========================================================================

using System;
using System.Web.UI;
using System.Drawing;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    public partial class ApprovalConfig : BasePage
    {
        private const string ManagePermission = "ITSM.ApprovalConfig.Manage";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["USERID"] == null || Session["BRANCH"] == null)
            {
                RedirectToLogin();
                return;
            }

            RequirePermission(ManagePermission);

            if (!IsPostBack)
            {
                BindRequestTypes();
                BindItHeadSection();
            }
        }

        protected void gvRequestTypes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "SaveFlags")
            {
                return;
            }

            RequirePermission(ManagePermission);
            ClearMessage();

            int requestTypeId;
            if (!int.TryParse(e.CommandArgument.ToString(), out requestTypeId))
            {
                ShowMessage("Invalid request type.", false);
                return;
            }

            int rowIndex = ((GridViewRow)((Control)e.CommandSource).NamingContainer).RowIndex;
            var row = gvRequestTypes.Rows[rowIndex];
            var chkManager = row.FindControl("chkManager") as CheckBox;
            var chkItHead = row.FindControl("chkItHead") as CheckBox;

            if (chkManager == null || chkItHead == null)
            {
                ShowMessage("Unable to read approval settings.", false);
                return;
            }

            try
            {
                var service = new ApprovalConfigurationService();
                service.UpdateRequestTypeApproval(
                    requestTypeId,
                    chkManager.Checked,
                    chkItHead.Checked,
                    CurrentEmployeeCode);

                ShowMessage("Approval policy updated.", true);
                BindRequestTypes();
            }
            catch (Exception ex)
            {
                ShowMessage("Error updating approval policy: " + ex.Message, false);
            }
        }

        protected void btnSetItHead_Click(object sender, EventArgs e)
        {
            RequirePermission(ManagePermission);
            ClearMessage();

            if (string.IsNullOrWhiteSpace(ddlItHead.SelectedValue))
            {
                return;
            }

            try
            {
                var service = new ApprovalConfigurationService();
                service.SetActiveItHead(
                    ddlItHead.SelectedValue,
                    CurrentEmployeeCode);

                ShowMessage("IT Head approver updated successfully.", true);
                BindItHeadSection();
            }
            catch (Exception ex)
            {
                ShowMessage("Error setting IT Head: " + ex.Message, false);
            }
        }

        private void BindRequestTypes()
        {
            var service = new ApprovalConfigurationService();
            gvRequestTypes.DataSource = service.GetRequestTypes();
            gvRequestTypes.DataBind();
        }

        private void BindItHeadSection()
        {
            var service = new ApprovalConfigurationService();
            var current = service.GetActiveItHead();

            if (current != null)
            {
                pnlCurrentItHead.Visible = true;
                pnlNoItHead.Visible = false;
                lblCurrentItHead.Text = current.ItHeadApproverCode
                    + " — " + current.ItHeadApproverName;
                lblCurrentItHeadStatus.Text = current.ItHeadApproverStatus;
            }
            else
            {
                pnlCurrentItHead.Visible = false;
                pnlNoItHead.Visible = true;
            }

            ddlItHead.Items.Clear();
            ddlItHead.AppendDataBoundItems = true;
            ddlItHead.Items.Add(new ListItem("-- Select Employee --", string.Empty));
            ddlItHead.DataSource = service.GetActiveEmployees();
            ddlItHead.DataTextField = "EmployeeName";
            ddlItHead.DataValueField = "EmployeeCode";
            ddlItHead.DataBind();

            gvItHeadHistory.DataSource = service.GetAllItHeadConfigs();
            gvItHeadHistory.DataBind();
        }

        private void ClearMessage()
        {
            lblMessage.Text = string.Empty;
        }

        private void ShowMessage(string message, bool success)
        {
            lblMessage.Text = message;
            lblMessage.ForeColor = success ? Color.Green : Color.Red;
        }
    }
}
