using System;
using System.Collections.Generic;
using System.Web.UI;

namespace AnmolDristi.Controls
{
    public partial class ApprovalSummaryControl : UserControl
    {
        public void Clear()
        {
            ucVisitWindow.Clear();
            pnlStatus.Visible = false;
            Visible = false;
        }

        public void BindHostSchedule(
            string scheduleType,
            string approvalStatus,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            DateTime? validFrom,
            DateTime? validTo,
            IList<VisitDayLogInfo> days,
            bool showStatus = false)
        {
            ucVisitWindow.BindHostSchedule(
                scheduleType,
                approvalStatus,
                requestedFrom,
                requestedTo,
                validFrom,
                validTo,
                days);

            if (showStatus && !string.IsNullOrEmpty(approvalStatus))
            {
                ucStatus.Bind(approvalStatus);
                pnlStatus.Visible = true;
            }
            else
            {
                pnlStatus.Visible = false;
            }

            Visible = ucVisitWindow.Visible;
        }
    }
}
