using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    public class PolicyExceptionService
    {
        public const string HistoryPermissionKey = "UI.PolicyException.History.View";
        public const string DefaultCreateReason = "Initial Policy Creation";
        public const string DefaultActivateReason = "Policy Activated";
        public const string DefaultDeactivateReason = "Policy Deactivated";

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
            out string errorMessage,
            string changeReason = null)
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

            bool isUpdate = record.ExceptionId > 0;
            string resolvedReason = ResolveChangeReason(isUpdate ? "Update" : "Create", changeReason, out errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
                return 0;

            record.PolicyJson = PolicyExceptionSerializer.Serialize(policyDto);

            try
            {
                if (isUpdate)
                {
                    _dataAccess.UpdatePolicyException(record, currentUser, resolvedReason);
                    return record.ExceptionId;
                }

                return _dataAccess.InsertPolicyException(record, currentUser, resolvedReason);
            }
            catch (SqlException ex)
            {
                errorMessage = ex.Message;
                return 0;
            }
        }

        public void SetActiveStatus(
            int exceptionId,
            bool isActive,
            string currentUser,
            out string errorMessage,
            string changeReason = null)
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

            string resolvedReason = ResolveChangeReason(isActive ? "Activate" : "Deactivate", changeReason, out errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
                return;

            try
            {
                _dataAccess.SetPolicyExceptionActiveStatus(exceptionId, isActive, currentUser, resolvedReason);
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

        public List<PolicyExceptionHistoryCompareRow> CompareHistoryVersions(int exceptionId, int versionA, int versionB)
        {
            var rows = new List<PolicyExceptionHistoryCompareRow>();
            PolicyExceptionHistoryRecord a = GetHistoryVersion(exceptionId, versionA);
            PolicyExceptionHistoryRecord b = GetHistoryVersion(exceptionId, versionB);
            if (a == null || b == null) return rows;

            PolicyExceptionDto dtoA = PolicyExceptionSerializer.Deserialize(a.PolicyJson);
            PolicyExceptionDto dtoB = PolicyExceptionSerializer.Deserialize(b.PolicyJson);

            AddCompare(rows, "HQ/Ex HQ DA", FormatDecimal(dtoA.DA), FormatDecimal(dtoB.DA));
            AddCompare(rows, "Outstation DA", FormatDecimal(dtoA.OutstationDA), FormatDecimal(dtoB.OutstationDA));
            AddCompare(rows, "Hotel Cap", FormatDecimal(dtoA.HotelCap), FormatDecimal(dtoB.HotelCap));
            AddCompare(rows, "Own Arrangement", FormatDecimal(dtoA.OwnAllowance), FormatDecimal(dtoB.OwnAllowance));
            AddCompare(rows, "Bike Rate", FormatDecimal(dtoA.RatePerKm), FormatDecimal(dtoB.RatePerKm));
            AddCompare(rows, "Max KM", FormatDecimal(dtoA.MaxKM), FormatDecimal(dtoB.MaxKM));
            AddCompare(rows, "Remarks", FormatText(dtoA.Remarks), FormatText(dtoB.Remarks));
            AddCompare(rows, "Priority", a.Priority.ToString(CultureInfo.InvariantCulture), b.Priority.ToString(CultureInfo.InvariantCulture));
            AddCompare(rows, "Effective From", FormatDate(a.EffectiveFrom), FormatDate(b.EffectiveFrom));
            AddCompare(rows, "Effective To", FormatDate(a.EffectiveTo), FormatDate(b.EffectiveTo));
            AddCompare(rows, "Active Status", a.IsActive ? "Active" : "Inactive", b.IsActive ? "Active" : "Inactive");
            return rows;
        }

        public DataTable BuildHistoryExportTable(int exceptionId)
        {
            var export = new DataTable();
            export.Columns.Add("Version", typeof(int));
            export.Columns.Add("Action", typeof(string));
            export.Columns.Add("Changed By", typeof(string));
            export.Columns.Add("Changed On", typeof(string));
            export.Columns.Add("Reason", typeof(string));
            export.Columns.Add("Priority", typeof(int));
            export.Columns.Add("Effective From", typeof(string));
            export.Columns.Add("Effective To", typeof(string));
            export.Columns.Add("Policy Values", typeof(string));

            DataTable history = GetHistory(exceptionId);
            if (history == null) return export;

            // Export chronological (oldest first) from stored snapshots only.
            DataView view = history.DefaultView;
            view.Sort = "VersionNo ASC";
            foreach (DataRowView rowView in view)
            {
                DataRow row = rowView.Row;
                string policyJson = row["PolicyJson"] == DBNull.Value ? "{}" : Convert.ToString(row["PolicyJson"]);
                export.Rows.Add(
                    Convert.ToInt32(row["VersionNo"]),
                    Convert.ToString(row["ActionType"]),
                    row["ChangedBy"] == DBNull.Value ? string.Empty : Convert.ToString(row["ChangedBy"]),
                    row["ChangedOn"] == DBNull.Value
                        ? string.Empty
                        : Convert.ToDateTime(row["ChangedOn"]).ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture),
                    row["ChangeReason"] == DBNull.Value ? string.Empty : Convert.ToString(row["ChangeReason"]),
                    row["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(row["Priority"]),
                    row["EffectiveFrom"] == DBNull.Value
                        ? string.Empty
                        : Convert.ToDateTime(row["EffectiveFrom"]).ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),
                    row["EffectiveTo"] == DBNull.Value
                        ? string.Empty
                        : Convert.ToDateTime(row["EffectiveTo"]).ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),
                    FormatPolicyValues(policyJson));
            }

            return export;
        }

        private static string ResolveChangeReason(string actionType, string changeReason, out string errorMessage)
        {
            errorMessage = null;
            string trimmed = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim();

            if (string.Equals(actionType, "Update", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    errorMessage = "Reason for Change is required when updating a policy exception.";
                    return null;
                }
                return trimmed;
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;

            if (string.Equals(actionType, "Create", StringComparison.OrdinalIgnoreCase))
                return DefaultCreateReason;
            if (string.Equals(actionType, "Activate", StringComparison.OrdinalIgnoreCase))
                return DefaultActivateReason;
            if (string.Equals(actionType, "Deactivate", StringComparison.OrdinalIgnoreCase))
                return DefaultDeactivateReason;

            return trimmed;
        }

        private static void AddCompare(List<PolicyExceptionHistoryCompareRow> rows, string field, string a, string b)
        {
            rows.Add(new PolicyExceptionHistoryCompareRow
            {
                Field = field,
                VersionAValue = a,
                VersionBValue = b,
                IsChanged = !string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal)
            });
        }

        private static string FormatPolicyValues(string policyJson)
        {
            PolicyExceptionDto dto = PolicyExceptionSerializer.Deserialize(policyJson);
            var sb = new StringBuilder();
            AppendPolicyPart(sb, "HQ/Ex HQ DA", FormatDecimal(dto.DA));
            AppendPolicyPart(sb, "Outstation DA", FormatDecimal(dto.OutstationDA));
            AppendPolicyPart(sb, "Hotel Cap", FormatDecimal(dto.HotelCap));
            AppendPolicyPart(sb, "Own Arrangement", FormatDecimal(dto.OwnAllowance));
            AppendPolicyPart(sb, "Bike Rate", FormatDecimal(dto.RatePerKm));
            AppendPolicyPart(sb, "Max KM", FormatDecimal(dto.MaxKM));
            AppendPolicyPart(sb, "Remarks", FormatText(dto.Remarks));
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static void AppendPolicyPart(StringBuilder sb, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "—") return;
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(label).Append("=").Append(value);
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : "NULL";
        }

        private static string FormatDate(DateTime value)
        {
            return value == default(DateTime) ? "NULL" : value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        }

        private static string FormatText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }
    }
}
