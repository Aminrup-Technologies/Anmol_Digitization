using System;

namespace AnmolDristi
{
    /// <summary>
    /// Interprets MST_EmployeeTolerance.MinAllowedKM / MaxAllowedKM as tolerance values
    /// (reduction / increase) around the current saved KM baseline.
    /// </summary>
    public static class KmToleranceHelper
    {
        public const int DefaultMaxModificationCount = 2;

        public static void ComputeAllowedRange(
            decimal currentKm,
            decimal? reductionTolerance,
            decimal? increaseTolerance,
            out decimal? allowedMin,
            out decimal? allowedMax)
        {
            allowedMin = reductionTolerance.HasValue
                ? Math.Max(0m, currentKm - reductionTolerance.Value)
                : (decimal?)null;

            allowedMax = increaseTolerance.HasValue
                ? currentKm + increaseTolerance.Value
                : (decimal?)null;
        }

        public static bool IsKmWithinTolerance(
            decimal newKm,
            decimal currentKm,
            decimal? reductionTolerance,
            decimal? increaseTolerance)
        {
            decimal? allowedMin;
            decimal? allowedMax;
            ComputeAllowedRange(currentKm, reductionTolerance, increaseTolerance, out allowedMin, out allowedMax);

            if (allowedMin.HasValue && newKm < allowedMin.Value - 0.0001m) return false;
            if (allowedMax.HasValue && newKm > allowedMax.Value + 0.0001m) return false;
            return true;
        }

        public static string FormatRange(decimal? allowedMin, decimal? allowedMax)
        {
            string minText = allowedMin.HasValue ? allowedMin.Value.ToString("0.##") : "—";
            string maxText = allowedMax.HasValue ? allowedMax.Value.ToString("0.##") : "—";
            return string.Format("{0} – {1}", minText, maxText);
        }

        public static string FormatRemaining(int usedCount, int maxCount)
        {
            int remaining = Math.Max(0, maxCount - usedCount);
            return string.Format("{0} of {1}", remaining, maxCount);
        }
    }

    public sealed class EmployeeKmToleranceConfig
    {
        public decimal? ReductionTolerance { get; set; }
        public decimal? IncreaseTolerance { get; set; }
        public int MaxModificationCount { get; set; } = KmToleranceHelper.DefaultMaxModificationCount;
    }
}
