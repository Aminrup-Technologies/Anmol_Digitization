using System;

namespace AnmolDristi
{
    /// <summary>
    /// Final resolved policy values consumed by SalesMonthlyReview and other claim screens.
    /// </summary>
    public class ResolvedPolicyDto
    {
        public string Source { get; set; }
        public int? RowId { get; set; }
        public string EmployeeCode { get; set; }
        public int? RegionId { get; set; }
        public int? BranchId { get; set; }
        public int? GradeId { get; set; }
        public int? DesignationId { get; set; }
        public string PolicyJson { get; set; }
        public int? Priority { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool? IsActive { get; set; }
        public int? Specificity { get; set; }

        public decimal? Resolved_MaxKM { get; set; }
        public decimal? Resolved_RatePerKm { get; set; }
        public decimal? Resolved_DA { get; set; }
        public decimal? Resolved_HotelCap { get; set; }
        public decimal? Resolved_OutstationDA { get; set; }
        public decimal? Resolved_OwnAllowance { get; set; }
        public string Resolved_Remarks { get; set; }
        public decimal? FixedTravelAllowance { get; set; }

        public bool IsPolicyExceptionApplied { get; set; }
        public int? PolicyExceptionId { get; set; }
        public string PolicySource { get; set; }
        public int? AppliedPriority { get; set; }
    }
}
