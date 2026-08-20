using System;

namespace AnmolDristi
{
    /// <summary>
    /// Deterministic parent ∩ DayLog effective validity window (AR-GS-11).
    /// No database, UI, or authorization side effects.
    /// </summary>
    public static class VmsEffectiveValidity
    {
        /// <summary>
        /// Resolves effectiveFrom = MAX(parentFrom, dayFrom) and
        /// effectiveUpto = MIN(parentUpto, dayUpto).
        /// Returns false when parent ValidFrom/ValidUpto is missing (NULL not unlimited)
        /// or the intersection is empty (effectiveFrom &gt; effectiveUpto).
        /// </summary>
        public static bool TryResolveEffectiveWindow(
            DateTime? parentFrom,
            DateTime? parentUpto,
            DateTime dayFrom,
            DateTime dayUpto,
            out DateTime effectiveFrom,
            out DateTime effectiveUpto)
        {
            effectiveFrom = default(DateTime);
            effectiveUpto = default(DateTime);

            // NULL parent bounds are not open-ended physical access.
            if (!parentFrom.HasValue || !parentUpto.HasValue)
                return false;

            effectiveFrom = parentFrom.Value > dayFrom ? parentFrom.Value : dayFrom;
            effectiveUpto = parentUpto.Value < dayUpto ? parentUpto.Value : dayUpto;

            if (effectiveFrom > effectiveUpto)
                return false;

            return true;
        }
    }
}
