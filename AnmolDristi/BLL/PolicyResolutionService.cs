using System;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    public class PolicyResolutionService
    {
        private readonly PolicyResolutionDataAccess _resolutionDataAccess = new PolicyResolutionDataAccess();
        private readonly PolicyExceptionDataAccess _exceptionDataAccess = new PolicyExceptionDataAccess();

        public ResolvedPolicyDto ResolvePolicy(
            string employeeCode,
            DateTime claimDate,
            int? regionId = null,
            int? branchId = null,
            int? gradeId = null,
            int? designationId = null)
        {
            ResolvedPolicyDto standard = _resolutionDataAccess.GetStandardPolicy(
                employeeCode,
                regionId,
                branchId,
                gradeId,
                designationId);

            PolicyExceptionRecord exception = _exceptionDataAccess.GetApplicablePolicyException(employeeCode, claimDate);
            if (exception != null)
            {
                if (standard == null)
                    standard = new ResolvedPolicyDto { EmployeeCode = employeeCode };
                return ApplyExceptionOverride(standard, exception);
            }

            if (standard == null)
                return null;

            standard.IsPolicyExceptionApplied = false;
            standard.PolicySource = "Standard";
            if (string.IsNullOrWhiteSpace(standard.Source))
                standard.Source = "MATRIX";
            return standard;
        }

        private static ResolvedPolicyDto ApplyExceptionOverride(ResolvedPolicyDto standard, PolicyExceptionRecord exception)
        {
            standard.IsPolicyExceptionApplied = true;
            standard.PolicyExceptionId = exception.ExceptionId;
            standard.PolicySource = "Employee Exception";
            standard.AppliedPriority = exception.Priority;
            standard.Source = "EX";

            PolicyOverrideMergeHelper.ApplyPartialOverrides(standard, exception.PolicyJson);
            return standard;
        }
    }
}
