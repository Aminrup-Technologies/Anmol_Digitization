using System;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public enum DigitalPassState
    {
        NotApprovedForToday,
        PassNotActiveYet,
        ValidPass,
        VisitorCurrentlyInside,
        VisitCompleted,
        TodaysWindowEnded,
        Expired
    }

    /// <summary>
    /// Deterministic DigitalPass presentation-state calculation.
    /// Facts come from the caller; this type does not access SQL, UI, or DateTime.Now.
    /// </summary>
    public static class DigitalPassStateResolver
    {
        public static DigitalPassState Resolve(
            DigitalPassDetails pass,
            VisitDayLogInfo todayLog,
            DateTime now)
        {
            // SingleDay: preserve prior ValidUpto-only lifecycle (EXPIRED presentation).
            if (!string.Equals(pass.ScheduleType, "MultiDay", StringComparison.OrdinalIgnoreCase))
            {
                if (now > pass.ValidUpto)
                    return DigitalPassState.Expired;

                return DigitalPassState.ValidPass;
            }

            if (todayLog == null || !todayLog.IsDayApproved)
                return DigitalPassState.NotApprovedForToday;

            string dayStatus = string.IsNullOrWhiteSpace(todayLog.VisitStatus)
                ? "Scheduled"
                : todayLog.VisitStatus.Trim();

            if (now < todayLog.DayValidFrom)
                return DigitalPassState.PassNotActiveYet;

            if (string.Equals(dayStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase))
                return DigitalPassState.VisitCompleted;

            if (string.Equals(dayStatus, "Checked-In", StringComparison.OrdinalIgnoreCase))
                return DigitalPassState.VisitorCurrentlyInside;

            if (now > todayLog.DayValidUpto)
                return DigitalPassState.TodaysWindowEnded;

            return DigitalPassState.ValidPass;
        }
    }
}
