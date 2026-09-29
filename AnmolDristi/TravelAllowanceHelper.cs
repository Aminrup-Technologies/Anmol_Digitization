using System;

namespace AnmolDristi
{
    public static class TravelAllowanceHelper
    {
        public static decimal CalculateAmount(decimal totalTravelKm, decimal ratePerKm, decimal? fixedTravelAllowance)
        {
            if (totalTravelKm <= 0m) return 0m;

            if (fixedTravelAllowance.HasValue)
                return Math.Round(fixedTravelAllowance.Value, 2, MidpointRounding.AwayFromZero);

            return Math.Round(ratePerKm * totalTravelKm, 2, MidpointRounding.AwayFromZero);
        }
    }
}
