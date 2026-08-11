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
            // SingleDay: presentation matches Gate effective window (parent ∩ DayLog).
            if (!string.Equals(pass.ScheduleType, "MultiDay", StringComparison.OrdinalIgnoreCase))
            {
                if (todayLog == null || !todayLog.IsDayApproved)
                    return DigitalPassState.NotApprovedForToday;

                DateTime effectiveFrom;
                DateTime effectiveUpto;
                if (!VmsEffectiveValidity.TryResolveEffectiveWindow(
                        pass.ValidFrom,
                        pass.ValidUpto,
                        todayLog.DayValidFrom,
                        todayLog.DayValidUpto,
                        out effectiveFrom,
                        out effectiveUpto))
                {
                    // NULL parent ValidUpto/ValidFrom or empty intersection → not valid.
                    return DigitalPassState.Expired;
                }

                if (now < effectiveFrom)
                    return DigitalPassState.PassNotActiveYet;

                if (now > effectiveUpto)
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
