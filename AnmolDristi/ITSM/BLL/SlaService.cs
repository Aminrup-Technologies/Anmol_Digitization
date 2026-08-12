using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// SLA date math using plant working hours (<c>dbo.VMS_PlantTimings</c>)
    /// and business calendar weekends (<c>dbo.MST_Calendar</c>).
    /// </summary>
    public class SlaService
    {
        /// <summary>
        /// Calculates SLA start (deferred to next OpenTime when outside working hours)
        /// and due date by adding <paramref name="resolutionHours"/> while skipping
        /// calendar days where <c>MST_Calendar.IsWeekend == true</c>.
        /// </summary>
        public (DateTime SlaStartTime, DateTime DueDate) CalculateSla(
            int branchId,
            DateTime requestTime,
            int resolutionHours)
        {
            if (resolutionHours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionHours));
            }

            using (var context = new ItsmDbContext())
            {
                var slaStartTime = ResolveSlaStartTime(context, branchId, requestTime);
                var dueDate = AddBusinessHoursSkippingWeekends(
                    context, slaStartTime, resolutionHours);
                return (slaStartTime, dueDate);
            }
        }

        private static DateTime ResolveSlaStartTime(
            ItsmDbContext context,
            int branchId,
            DateTime requestTime)
        {
            var dayName = requestTime.ToString("dddd", CultureInfo.InvariantCulture);
            var timing = GetPlantTiming(context, branchId, dayName);

            // No timing configured — treat as always open (same stance as VmsHelper).
            if (timing == null || !timing.OpenTime.HasValue)
            {
                return requestTime;
            }

            var openToday = requestTime.Date.Add(timing.OpenTime.Value);
            var closeToday = timing.CloseTime.HasValue
                ? requestTime.Date.Add(timing.CloseTime.Value)
                : (DateTime?)null;

            // Before open → start at today's OpenTime.
            if (requestTime < openToday)
            {
                return openToday;
            }

            // During working hours → start immediately.
            if (!closeToday.HasValue || requestTime < closeToday.Value)
            {
                return requestTime;
            }

            // After close → defer to the next valid OpenTime on a non-weekend day.
            return FindNextOpenTime(context, branchId, requestTime.Date.AddDays(1));
        }

        private static DateTime FindNextOpenTime(
            ItsmDbContext context,
            int branchId,
            DateTime fromDate)
        {
            // Bound the search to avoid infinite loops if timings/calendar are sparse.
            for (var offset = 0; offset < 14; offset++)
            {
                var candidateDate = fromDate.Date.AddDays(offset);
                if (IsWeekend(context, candidateDate))
                {
                    continue;
                }

                var dayName = candidateDate.ToString("dddd", CultureInfo.InvariantCulture);
                var timing = GetPlantTiming(context, branchId, dayName);
                if (timing != null && timing.OpenTime.HasValue)
                {
                    return candidateDate.Add(timing.OpenTime.Value);
                }

                // Non-weekend day with no timing row — start at midnight of that day.
                if (timing == null)
                {
                    return candidateDate;
                }
            }

            // Fallback: next calendar day at the original request's time-of-day intent.
            return fromDate.Date.AddDays(1);
        }

        private static DateTime AddBusinessHoursSkippingWeekends(
            ItsmDbContext context,
            DateTime slaStartTime,
            int resolutionHours)
        {
            if (resolutionHours == 0)
            {
                return slaStartTime;
            }

            var cursor = slaStartTime;
            var remaining = (double)resolutionHours;

            // Safety: prevent runaway loops on malformed calendar data.
            var guard = 0;
            while (remaining > 0.0001 && guard < 10000)
            {
                guard++;

                if (IsWeekend(context, cursor.Date))
                {
                    // Skip the entire weekend day; resume at next midnight.
                    cursor = cursor.Date.AddDays(1);
                    continue;
                }

                var endOfDay = cursor.Date.AddDays(1);
                var hoursUntilMidnight = (endOfDay - cursor).TotalHours;
                if (hoursUntilMidnight <= 0)
                {
                    cursor = endOfDay;
                    continue;
                }

                var consume = Math.Min(remaining, hoursUntilMidnight);
                cursor = cursor.AddHours(consume);
                remaining -= consume;
            }

            return cursor;
        }

        private static PlantTimings GetPlantTiming(
            ItsmDbContext context,
            int branchId,
            string dayOfWeek)
        {
            return context.PlantTimings
                .AsNoTracking()
                .FirstOrDefault(t =>
                    t.Branch_ID == branchId
                    && t.DayOfWeek == dayOfWeek);
        }

        private static bool IsWeekend(ItsmDbContext context, DateTime date)
        {
            var day = date.Date;
            var calendar = context.Calendars
                .AsNoTracking()
                .FirstOrDefault(c => c.CalendarDate == day);

            if (calendar != null)
            {
                return calendar.IsWeekend;
            }

            // Fallback when MST_Calendar has no row for this date.
            return day.DayOfWeek == DayOfWeek.Saturday
                || day.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}
