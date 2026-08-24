// ========================================================================
// When: 2026-08-22
// What: Approval configuration admin page — request type flags + IT Head.
// Why: Prompt 1.6 — IT Head approver is administratively configurable.
//      No inline SQL. No System.Web in BLL. Session gatekeeping per convention.
// ========================================================================

using System;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    public partial class ApprovalConfig : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["WORKMAN"] == null
                || Session["USERID"] == null
                || Session["BRANCH"] == null)
            {
                Response.Redirect("~/login.aspx", true);
                return;
            }

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

            ClearMessage();

            int requestTypeId;
            if (!int.TryParse(e.CommandArgument.ToString(), out requestTypeId))
            {
                return;
            }

            // Find the row to read checkbox state.
            int rowIndex = ((GridViewRow)((Control)e.CommandSource).NamingContainer).RowIndex;
            var row = gvRequestTypes.Rows[rowIndex];

            var chkManager = row.FindControl("chkManager") as CheckBox;
            var chkItHead = row.FindControl("chkItHead") as CheckBox;

            if (chkManager == null || chkItHead == null)
            {
                return;
            }

            try
            {
                var service = new ApprovalConfigurationService();
                service.UpdateRequestTypeApproval(
                    requestTypeId,
                    chkManager.Checked,
                    chkItHead.Checked);

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
                    Session["WORKMAN"].ToString());

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

            // Current active IT Head.
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

            // Employee dropdown.
            ddlItHead.Items.Clear();
            ddlItHead.AppendDataBoundItems = true;
            ddlItHead.Items.Add(new ListItem("-- Select Employee --", string.Empty));
            ddlItHead.DataSource = service.GetActiveEmployees();
            ddlItHead.DataTextField = "EmployeeName";
            ddlItHead.DataValueField = "EmployeeCode";
            ddlItHead.DataBind();

            // Configuration history.
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
