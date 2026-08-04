using System;
using System.Data.SqlClient;
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

            try
            {
                if (record.ExceptionId > 0)
                {
                    _dataAccess.UpdatePolicyException(record, currentUser);
                    return record.ExceptionId;
                }

                return _dataAccess.InsertPolicyException(record, currentUser);
            }
            catch (SqlException ex)
            {
                errorMessage = ex.Message;
                return 0;
            }
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

            try
            {
                _dataAccess.SetPolicyExceptionActiveStatus(exceptionId, isActive, currentUser);
            }
            catch (SqlException ex)
            {
                errorMessage = ex.Message;
            }
        }

        public System.Data.DataTable GetMasterList(string masterType)
        {
            return _dataAccess.GetMasterList(masterType);
        }

        public System.Data.DataTable GetHistory(int exceptionId)
        {
            if (exceptionId <= 0) return new System.Data.DataTable();
            return _dataAccess.GetPolicyExceptionHistory(exceptionId);
        }

        public PolicyExceptionHistoryRecord GetHistoryVersion(int exceptionId, int versionNo)
        {
            if (exceptionId <= 0 || versionNo <= 0) return null;
            return _dataAccess.GetPolicyExceptionHistoryVersion(exceptionId, versionNo);
        }
    }
}
