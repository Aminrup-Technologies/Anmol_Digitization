using System;

namespace AnmolDristi
{
    /// <summary>
    /// Strongly typed policy override values serialized into PolicyException.PolicyJson.
    /// Field names align with existing policy resolution JSON keys.
    /// </summary>
    public class PolicyExceptionDto
    {
        public decimal? MaxKM { get; set; }
        public decimal? RatePerKm { get; set; }
        public decimal? DA { get; set; }
        public decimal? HotelCap { get; set; }
        public decimal? OutstationDA { get; set; }
        public decimal? OwnAllowance { get; set; }
        public string Remarks { get; set; }

        public bool HasAnyOverride()
        {
            return MaxKM.HasValue
                || RatePerKm.HasValue
                || DA.HasValue
                || HotelCap.HasValue
                || OutstationDA.HasValue
                || OwnAllowance.HasValue
                || !string.IsNullOrWhiteSpace(Remarks);
        }
    }

    public class PolicyExceptionRecord
    {
        public int ExceptionId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? RegionId { get; set; }
        public int? BranchId { get; set; }
        public int? GradeId { get; set; }
        public int? DesignationId { get; set; }
        public string RegionName { get; set; }
        public string BranchName { get; set; }
        public string GradeName { get; set; }
        public string DesignationName { get; set; }
        public string PolicyJson { get; set; }
        public int Priority { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

    public class PolicyExceptionSearchCriteria
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? RegionId { get; set; }
        public int? BranchId { get; set; }
        public int? GradeId { get; set; }
        public int? DesignationId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }

    public class EmployeePolicyContext
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? RegionId { get; set; }
        public int? BranchId { get; set; }
        public int? GradeId { get; set; }
        public int? DesignationId { get; set; }
        public string RegionName { get; set; }
        public string BranchName { get; set; }
        public string GradeName { get; set; }
        public string DesignationName { get; set; }
    }

    /// <summary>
    /// Immutable history snapshot for a PolicyException version.
    /// </summary>
    public class PolicyExceptionHistoryRecord
    {
        public int HistoryId { get; set; }
        public int ExceptionId { get; set; }
        public int VersionNo { get; set; }
        public string ActionType { get; set; }
        public string PolicyJson { get; set; }
        public int Priority { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangeReason { get; set; }
    }

    /// <summary>
    /// One field row in a historical version comparison.
    /// </summary>
    public class PolicyExceptionHistoryCompareRow
    {
        public string Field { get; set; }
        public string VersionAValue { get; set; }
        public string VersionBValue { get; set; }
        public bool IsChanged { get; set; }

        public string RowCssClass
        {
            get { return IsChanged ? "history-changed" : "history-unchanged"; }
        }
    }
}
