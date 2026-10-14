using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi.Controls
{
    public partial class VisitWindowControl : UserControl
    {
        public void Clear()
        {
            litMarkup.Text = string.Empty;
            Visible = false;
        }

        public void BindHtml(string html)
        {
            litMarkup.Text = html ?? string.Empty;
            Visible = !string.IsNullOrEmpty(litMarkup.Text);
        }

        public void BindVisitorDays(IList<VisitDayLogInfo> days, string approvalStatus)
        {
            if (days == null || days.Count == 0)
            {
                Clear();
                return;
            }

            List<VisitDayScheduleRow> rows = VmsVisitScheduleHelper.BuildVisitorDaySchedule(days, approvalStatus);
            BindHtml(VmsVisitScheduleHelper.BuildVisitorDayScheduleHtml(rows));
        }

        public void BindHostSchedule(
            string scheduleType,
            string approvalStatus,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            DateTime? validFrom,
            DateTime? validTo,
            IList<VisitDayLogInfo> days)
        {
            BindHtml(VmsVisitScheduleHelper.BuildHostScheduleDisplayHtml(
                scheduleType,
                approvalStatus,
                requestedFrom,
                requestedTo,
                validFrom,
                validTo,
                days));
        }

        public static void BindHostFromRow(
            Control row,
            string approvalStatus,
            IDictionary<int, List<VisitDayLogInfo>> dayLogsByVisit)
        {
            GridViewRow gridRow = row as GridViewRow;
            VisitWindowControl uc = gridRow != null
                ? gridRow.FindControl("ucVisitWindow") as VisitWindowControl
                : null;
            if (uc == null)
                return;

            if (gridRow.DataItem == null)
            {
                uc.Clear();
                return;
            }

            object idObj = DataBinder.Eval(gridRow.DataItem, "LogID");
            if (idObj == null || idObj == DBNull.Value)
            {
                uc.Clear();
                return;
            }

            int logId = Convert.ToInt32(idObj);
            List<VisitDayLogInfo> days = null;
            if (dayLogsByVisit != null)
                dayLogsByVisit.TryGetValue(logId, out days);

            uc.BindHostSchedule(
                Convert.ToString(DataBinder.Eval(gridRow.DataItem, "ScheduleType")),
                approvalStatus,
                ReadDate(DataBinder.Eval(gridRow.DataItem, "RequestedValidFrom")),
                ReadDate(DataBinder.Eval(gridRow.DataItem, "RequestedValidUpto")),
                ReadDate(DataBinder.Eval(gridRow.DataItem, "ValidFrom")),
                ReadDate(DataBinder.Eval(gridRow.DataItem, "ValidUpto")),
                days);
        }

        public static void BindHostFromRepeater(
            RepeaterItem item,
            IDictionary<int, List<VisitDayLogInfo>> dayLogsByVisit)
        {
            VisitWindowControl uc = item != null
                ? item.FindControl("ucVisitWindow") as VisitWindowControl
                : null;
            if (uc == null)
                return;

            uc.Clear();
            if (item.DataItem == null)
                return;

            string scheduleType = Convert.ToString(DataBinder.Eval(item.DataItem, "ScheduleType"));
            if (!string.Equals(scheduleType, "MultiDay", StringComparison.OrdinalIgnoreCase))
                return;

            object logIdObj = DataBinder.Eval(item.DataItem, "LogID");
            if (logIdObj == null || logIdObj == DBNull.Value)
                return;

            int logId = Convert.ToInt32(logIdObj);
            List<VisitDayLogInfo> days = null;
            if (dayLogsByVisit != null)
                dayLogsByVisit.TryGetValue(logId, out days);
            if (days == null || days.Count == 0)
                return;

            string approvalStatus = Convert.ToString(DataBinder.Eval(item.DataItem, "ApprovalStatus"));
            uc.BindVisitorDays(days, approvalStatus);
        }

        private static DateTime? ReadDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDateTime(value);
        }
    }
}
