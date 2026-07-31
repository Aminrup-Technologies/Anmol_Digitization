using System;
using System.Collections.Generic;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    public class PolicyExceptionService
    {
        private readonly PolicyExceptionDataAccess _dataAccess = new PolicyExceptionDataAccess();

        public System.Data.DataTable Search(PolicyExceptionSearchCriteria criteria)
        {
            return _dataAccess.SearchPolicyExceptions(criteria);
        }

        public PolicyExceptionRecord GetById(int exceptionId)
        {
            return _dataAccess.GetPolicyExceptionById(exceptionId);
        }

        public EmployeePolicyContext GetEmployeeContext(string employeeCode)
        {
            return _dataAccess.GetEmployeePolicyContext(employeeCode);
        }

        public int Save(
            PolicyExceptionRecord record,
            PolicyExceptionDto policyDto,
            string currentUser,
            out string errorMessage)
        {
            errorMessage = null;
            if (record == null)
            {
                errorMessage = "Record is required.";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(record.EmployeeCode))
            {
                errorMessage = "Employee is mandatory.";
                return 0;
            }

            if (record.Priority <= 0)
            {
                errorMessage = "Priority must be greater than zero.";
                return 0;
            }

            if (record.EffectiveFrom == default(DateTime))
            {
                errorMessage = "Effective From is mandatory.";
                return 0;
            }

            if (policyDto == null || !policyDto.HasAnyOverride())
            {
                errorMessage = "At least one policy override value is required.";
                return 0;
            }

            if (record.EffectiveTo.HasValue && record.EffectiveTo.Value < record.EffectiveFrom.Date)
            {
                errorMessage = "Effective To cannot be before Effective From.";
                return 0;
            }

            record.PolicyJson = PolicyExceptionSerializer.Serialize(policyDto);

            if (_dataAccess.HasOverlappingActiveException(
                record.EmployeeCode.Trim(),
                record.EffectiveFrom.Date,
                record.EffectiveTo,
                record.ExceptionId))
            {
                errorMessage = "An active policy exception already exists for this employee during the selected effective period.";
                return 0;
            }

            return _dataAccess.SavePolicyException(record, currentUser);
        }

        public void SetActiveStatus(int exceptionId, bool isActive, string currentUser, out string errorMessage)
        {
            errorMessage = null;
            if (exceptionId <= 0)
            {
                errorMessage = "Invalid exception id.";
                return;
            }

            PolicyExceptionRecord existing = _dataAccess.GetPolicyExceptionById(exceptionId);
            if (existing == null)
            {
                errorMessage = "Policy exception not found.";
                return;
            }

            if (isActive && _dataAccess.HasOverlappingActiveException(
                existing.EmployeeCode,
                existing.EffectiveFrom.Date,
                existing.EffectiveTo,
                exceptionId))
            {
                errorMessage = "Cannot activate: another active exception overlaps this effective period.";
                return;
            }

            _dataAccess.SetPolicyExceptionActiveStatus(exceptionId, isActive, currentUser);
        }

        public System.Data.DataTable GetMasterList(string masterType)
        {
            return _dataAccess.GetMasterList(masterType);
        }
    }
}
