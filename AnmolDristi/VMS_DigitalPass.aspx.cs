using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using QRCoder;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_DigitalPass : System.Web.UI.Page
    {
        private readonly VmsVisitorManagementRepository _repository = new VmsVisitorManagementRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["token"] != null)
                {
                    LoadPassDetails(Request.QueryString["token"]);
                }
                else
                {
                    ShowError("Invalid Link. No security token provided.");
                }
            }
        }

        private void LoadPassDetails(string tokenStr)
        {
            Guid token;
            if (!Guid.TryParse(tokenStr, out token))
            {
                ShowError("Corrupted security token.");
                return;
            }

            DigitalPassDetails pass = _repository.GetDigitalPassDetails(token);
            if (pass == null)
            {
                ShowError("Pass not found in the system.");
                return;
            }

            // 1. Verify Approval Status
            if (pass.ApprovalStatus != "Approved")
            {
                ShowError("This pass has not been approved or has been revoked.");
                return;
            }

            // 2. Bind Data to UI
            lblVisitorName.Text = pass.VisitorName;
            lblCompany.Text = pass.VisitorCompany;
            lblHostName.Text = pass.HostName;
            lblPlantAlias.Text = pass.PlantAlias;

            // Format Policy text to keep line breaks
            litPolicyText.Text = pass.PolicyText.Replace("\n", "<br/>");

            // Handle absolute vs relative image paths
            string photoUrl = pass.PhotoPath;
            imgVisitorPhoto.ImageUrl = string.IsNullOrEmpty(photoUrl) ? "~/images/user.png" : photoUrl;

            // 3. Classify today's presentation state (read-only; Gate Scanner remains authoritative)
            VisitDayLogInfo todayLog = null;
            DigitalPassState state = ResolvePresentationState(pass, out todayLog);
            BindValidityDisplay(pass, todayLog, state);
            ApplyPresentationState(state, tokenStr);

            pnlPass.Visible = true;
            BindApprovedDates(pass.LogId, pass.ScheduleType, pass.ApprovalStatus);
        }

        private DigitalPassState ResolvePresentationState(DigitalPassDetails pass, out VisitDayLogInfo todayLog)
        {
            // SingleDay and MultiDay both need today's DayLog for presentation under AR-GS-11.
            todayLog = _repository.GetTodayVisitDayLog(pass.LogId);

            return DigitalPassStateResolver.Resolve(
                pass,
                todayLog,
                DateTime.Now);
        }

        private void BindValidityDisplay(DigitalPassDetails pass, VisitDayLogInfo todayLog, DigitalPassState state)
        {
            DateTime? displayFrom = pass.ValidFrom;
            DateTime? displayUpto = pass.ValidUpto;

            if (todayLog != null && todayLog.IsDayApproved)
            {
                DateTime effectiveFrom;
                DateTime effectiveUpto;
                if (VmsEffectiveValidity.TryResolveEffectiveWindow(
                        pass.ValidFrom,
                        pass.ValidUpto,
                        todayLog.DayValidFrom,
                        todayLog.DayValidUpto,
                        out effectiveFrom,
                        out effectiveUpto))
                {
                    if (!string.Equals(pass.ScheduleType, "MultiDay", StringComparison.OrdinalIgnoreCase))
                    {
                        displayFrom = effectiveFrom;
                        displayUpto = effectiveUpto;
                    }
                }

                divTodayWindow.Visible = true;
                lblTodayWindow.Text = VmsVisitScheduleHelper.FormatClockTime(todayLog.DayValidFrom.TimeOfDay) +
                    " – " +
                    VmsVisitScheduleHelper.FormatClockTime(todayLog.DayValidUpto.TimeOfDay);
            }
            else
            {
                divTodayWindow.Visible = false;
                lblTodayWindow.Text = "";
            }

            lblValidFrom.Text = displayFrom.HasValue
                ? displayFrom.Value.ToString("dd MMM yyyy, hh:mm tt")
                : "Not set";
            lblValidUpto.Text = displayUpto.HasValue
                ? displayUpto.Value.ToString("dd MMM yyyy, hh:mm tt")
                : "Not set";
        }

        private void ApplyPresentationState(DigitalPassState state, string tokenStr)
        {
            string badgeText;
            string badgeCss;
            string message;

            switch (state)
            {
                case DigitalPassState.NotApprovedForToday:
                    badgeText = "NOT APPROVED FOR TODAY";
                    badgeCss = "status-badge status-expired";
                    message = "This pass is not approved for entry today.";
                    break;
                case DigitalPassState.PassNotActiveYet:
                    badgeText = "PASS NOT ACTIVE YET";
                    badgeCss = "status-badge status-pending";
                    message = "Your approved visit window has not started.";
                    break;
                case DigitalPassState.VisitorCurrentlyInside:
                    badgeText = "VISITOR CURRENTLY INSIDE";
                    badgeCss = "status-badge status-inside";
                    message = "You have already checked in.";
                    break;
                case DigitalPassState.VisitCompleted:
                    badgeText = "VISIT COMPLETED";
                    badgeCss = "status-badge status-completed";
                    message = "You have already checked out for today.";
                    break;
                case DigitalPassState.TodaysWindowEnded:
                    badgeText = "TODAY'S WINDOW ENDED";
                    badgeCss = "status-badge status-expired";
                    message = "Today's approved visiting window has ended.";
                    break;
                case DigitalPassState.Expired:
                    badgeText = "<i class='fa fa-times-circle'></i> EXPIRED";
                    badgeCss = "status-badge status-expired";
                    message = "";
                    break;
                case DigitalPassState.ValidPass:
                default:
                    badgeText = "VALID PASS";
                    badgeCss = "status-badge status-valid";
                    message = "Show this QR code at the security gate.";
                    break;
            }

            lblStatusBadge.Text = "<span class='" + badgeCss + "'>" + badgeText + "</span>";
            lblStatusMessage.Text = message;

            if (state == DigitalPassState.ValidPass)
            {
                divQrBox.Visible = true;
                imgScanQR.Visible = true;
                GenerateScannableQR(tokenStr);
            }
            else
            {
                divQrBox.Visible = false;
                imgScanQR.Visible = false;
            }
        }

        private void BindApprovedDates(int logId, string scheduleType, string approvalStatus)
        {
            List<VisitDayLogInfo> days = _repository.GetVisitDayLogs(logId);
            bool isMultiDay = string.Equals(scheduleType, "MultiDay", StringComparison.OrdinalIgnoreCase)
                || (days != null && days.Count > 1);

            if (isMultiDay && days != null && days.Count > 0)
            {
                List<VisitDayScheduleRow> rows = VmsVisitScheduleHelper.BuildVisitorDaySchedule(days, approvalStatus);
                litVisitDays.Text = VmsVisitScheduleHelper.BuildVisitorDayScheduleHtml(rows);
                pnlVisitDays.Visible = true;
                divApprovedDates.Visible = false;
                lblApprovedDates.Text = "";
                return;
            }

            pnlVisitDays.Visible = false;
            litVisitDays.Text = "";

            string approvedDatesText = _repository.GetMultiDayApprovedDatesText(logId);
            if (string.IsNullOrEmpty(approvedDatesText))
            {
                divApprovedDates.Visible = false;
                lblApprovedDates.Text = "";
                return;
            }

            lblApprovedDates.Text = approvedDatesText;
            divApprovedDates.Visible = true;
        }

        private void GenerateScannableQR(string tokenData)
        {
            // The Guard's scanner will read this raw text to Check-In the visitor
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(tokenData, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);

            using (Bitmap bitMap = qrCode.GetGraphic(10))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] byteImage = ms.ToArray();
                    imgScanQR.ImageUrl = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                }
            }
        }

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            pnlPass.Visible = false;
            lblError.Text = message;
        }
    }
}
