using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    public class PolicyExceptionDataAccess
    {
        private readonly string _connectionString;

        public PolicyExceptionDataAccess()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public DataTable SearchPolicyExceptions(PolicyExceptionSearchCriteria criteria)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SearchPolicyExceptions", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@EmployeeCode", ToDbString(criteria.EmployeeCode));
                cmd.Parameters.AddWithValue("@EmployeeName", ToDbString(criteria.EmployeeName));
                cmd.Parameters.AddWithValue("@RegionId", criteria.RegionId.HasValue ? (object)criteria.RegionId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchId", criteria.BranchId.HasValue ? (object)criteria.BranchId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@GradeId", criteria.GradeId.HasValue ? (object)criteria.GradeId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@DesignationId", criteria.DesignationId.HasValue ? (object)criteria.DesignationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", criteria.IsActive.HasValue ? (object)criteria.IsActive.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EffectiveDate", criteria.EffectiveDate.HasValue ? (object)criteria.EffectiveDate.Value.Date : DBNull.Value);

                var dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                NormalizeSearchResultColumns(dt);
                return dt;
            }
        }

        /// <summary>
        /// Returns the highest-priority active policy exception effective on the claim date.
        /// </summary>
        public PolicyExceptionRecord GetApplicablePolicyException(string employeeCode, DateTime claimDate)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return null;

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP (1)
      PE.ExceptionId
    , PE.EmployeeCode
    , UM.EmployeeName
    , PE.RegionId
    , PE.BranchId
    , PE.GradeId
    , PE.DesignationId
    , PE.PolicyJson
    , PE.Priority
    , PE.EffectiveFrom
    , PE.EffectiveTo
    , PE.IsActive
    , PE.CreatedBy
    , PE.CreatedOn
    , PE.ModifiedBy
    , PE.ModifiedOn
FROM dbo.PolicyException PE WITH (NOLOCK)
LEFT JOIN dbo.MST_UserMaster UM WITH (NOLOCK) ON UM.EmployeeCode = PE.EmployeeCode
WHERE PE.EmployeeCode = @EmployeeCode
  AND PE.IsActive = 1
  AND PE.EffectiveFrom <= @ClaimDate
  AND (PE.EffectiveTo IS NULL OR PE.EffectiveTo >= @ClaimDate)
ORDER BY PE.Priority ASC", cn))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                cmd.Parameters.AddWithValue("@ClaimDate", claimDate.Date);
                cn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return MapRecord(reader);
                }
            }
        }

        public PolicyExceptionRecord GetPolicyExceptionById(int exceptionId)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetPolicyExceptionById", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", exceptionId);
                cn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return MapRecord(reader);
                }
            }
        }

        public EmployeePolicyContext GetEmployeePolicyContext(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return null;

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 1
    u.EmployeeCode,
    u.EmployeeName,
    TRY_CAST(u.RegionId AS INT) AS RegionId,
    TRY_CAST(u.BranchId AS INT) AS BranchId,
    TRY_CAST(u.GradeId AS INT) AS GradeId,
    TRY_CAST(u.Designation_ID AS INT) AS DesignationId,
    ISNULL(r.Region_Description, '') AS RegionName,
    ISNULL(b.Branch_Description, '') AS BranchName,
    ISNULL(g.Grade_Description, '') AS GradeName,
    ISNULL(d.Designation_Description, '') AS DesignationName
FROM dbo.MST_UserMaster u WITH (NOLOCK)
LEFT JOIN dbo.MST_Region r WITH (NOLOCK) ON CAST(u.RegionId AS VARCHAR(50)) = CAST(r.Region_ID AS VARCHAR(50))
LEFT JOIN dbo.MST_Branch b WITH (NOLOCK) ON CAST(u.BranchId AS VARCHAR(50)) = CAST(b.Branch_ID AS VARCHAR(50))
LEFT JOIN dbo.MST_Grade g WITH (NOLOCK) ON CAST(u.GradeId AS VARCHAR(50)) = CAST(g.Grade_ID AS VARCHAR(50))
LEFT JOIN dbo.MST_Designation d WITH (NOLOCK) ON u.Designation_ID = d.Id
WHERE u.EmployeeCode = @EmployeeCode", cn))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                cn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new EmployeePolicyContext
                    {
                        EmployeeCode = Convert.ToString(reader["EmployeeCode"]),
                        EmployeeName = reader["EmployeeName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["EmployeeName"]),
                        RegionId = reader["RegionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RegionId"]),
                        BranchId = reader["BranchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BranchId"]),
                        GradeId = reader["GradeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["GradeId"]),
                        DesignationId = reader["DesignationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DesignationId"]),
                        RegionName = Convert.ToString(reader["RegionName"]),
                        BranchName = Convert.ToString(reader["BranchName"]),
                        GradeName = Convert.ToString(reader["GradeName"]),
                        DesignationName = Convert.ToString(reader["DesignationName"])
                    };
                }
            }
        }

        public bool HasOverlappingActiveException(string employeeCode, DateTime effectiveFrom, DateTime? effectiveTo, int excludeExceptionId)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.PolicyException WITH (NOLOCK)
WHERE EmployeeCode = @EmployeeCode
  AND IsActive = 1
  AND ExceptionId <> @ExcludeExceptionId
  AND @EffectiveFrom <= ISNULL(EffectiveTo, '99991231')
  AND ISNULL(@EffectiveTo, '99991231') >= EffectiveFrom", cn))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                cmd.Parameters.AddWithValue("@ExcludeExceptionId", excludeExceptionId);
                cmd.Parameters.AddWithValue("@EffectiveFrom", effectiveFrom.Date);
                cmd.Parameters.AddWithValue("@EffectiveTo", effectiveTo.HasValue ? (object)effectiveTo.Value.Date : DBNull.Value);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public int InsertPolicyException(PolicyExceptionRecord record, string createdBy, string changeReason = null)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_InsertPolicyException", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@EmployeeCode", record.EmployeeCode.Trim());
                cmd.Parameters.AddWithValue("@PolicyJson", record.PolicyJson ?? "{}");
                cmd.Parameters.AddWithValue("@Priority", record.Priority);
                cmd.Parameters.AddWithValue("@EffectiveFrom", record.EffectiveFrom.Date);
                cmd.Parameters.AddWithValue("@EffectiveTo", record.EffectiveTo.HasValue ? (object)record.EffectiveTo.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy ?? "SYSTEM");
                cmd.Parameters.AddWithValue("@ChangeReason", ToDbString(changeReason));
                cn.Open();
                object scalar = cmd.ExecuteScalar();
                return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
            }
        }

        public void UpdatePolicyException(PolicyExceptionRecord record, string modifiedBy, string changeReason = null)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_UpdatePolicyException", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", record.ExceptionId);
                cmd.Parameters.AddWithValue("@EmployeeCode", record.EmployeeCode.Trim());
                cmd.Parameters.AddWithValue("@PolicyJson", record.PolicyJson ?? "{}");
                cmd.Parameters.AddWithValue("@Priority", record.Priority);
                cmd.Parameters.AddWithValue("@EffectiveFrom", record.EffectiveFrom.Date);
                cmd.Parameters.AddWithValue("@EffectiveTo", record.EffectiveTo.HasValue ? (object)record.EffectiveTo.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy ?? "SYSTEM");
                cmd.Parameters.AddWithValue("@ChangeReason", ToDbString(changeReason));
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SetPolicyExceptionActiveStatus(int exceptionId, bool isActive, string currentUser, string changeReason = null)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SetPolicyExceptionActiveStatus", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", exceptionId);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@ModifiedBy", currentUser ?? "SYSTEM");
                cmd.Parameters.AddWithValue("@ChangeReason", ToDbString(changeReason));
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetPolicyExceptionHistory(int exceptionId)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetPolicyExceptionHistory", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", exceptionId);
                var dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
            }
        }

        public PolicyExceptionHistoryRecord GetPolicyExceptionHistoryVersion(int exceptionId, int versionNo)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetPolicyExceptionHistoryVersion", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", exceptionId);
                cmd.Parameters.AddWithValue("@VersionNo", versionNo);
                cn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return MapHistoryRecord(reader);
                }
            }
        }

        /// <summary>
        /// Lightweight employee suggestions for Policy Exception search autocomplete.
        /// Reuses MST_UserMaster (same source as GetEmployeePolicyContext). No SP changes.
        /// </summary>
        public DataTable SuggestEmployees(string term, int maxRows = 10)
        {
            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(term)) return dt;

            string like = "%" + term.Trim() + "%";
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP (@MaxRows)
    u.EmployeeCode,
    ISNULL(u.EmployeeName, '') AS EmployeeName
FROM dbo.MST_UserMaster u WITH (NOLOCK)
WHERE u.EmployeeCode LIKE @Term
   OR u.EmployeeName LIKE @Term
ORDER BY
    CASE WHEN u.EmployeeCode LIKE @Prefix THEN 0 ELSE 1 END,
    u.EmployeeCode", cn))
            {
                cmd.Parameters.AddWithValue("@MaxRows", maxRows <= 0 ? 10 : maxRows);
                cmd.Parameters.AddWithValue("@Term", like);
                cmd.Parameters.AddWithValue("@Prefix", term.Trim() + "%");
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
            return dt;
        }

        public DataTable GetMasterList(string masterType)
        {
            string sql;
            switch ((masterType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "region":
                    sql = "SELECT Region_ID AS Id, Region_Description AS Description FROM dbo.MST_Region ORDER BY Region_Description";
                    break;
                case "branch":
                    sql = "SELECT Branch_ID AS Id, Branch_Description AS Description FROM dbo.MST_Branch ORDER BY Branch_Description";
                    break;
                case "grade":
                    sql = "SELECT Grade_ID AS Id, Grade_Description AS Description FROM dbo.MST_Grade ORDER BY Grade_Description";
                    break;
                case "designation":
                    sql = "SELECT Id, Designation_Description AS Description FROM dbo.MST_Designation ORDER BY Designation_Description";
                    break;
                default:
                    return new DataTable();
            }

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private static object ToDbString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static void NormalizeSearchResultColumns(DataTable dt)
        {
            RenameColumnIfExists(dt, "Region_Description", "RegionName");
            RenameColumnIfExists(dt, "Branch_Description", "BranchName");
            RenameColumnIfExists(dt, "Grade_Description", "GradeName");
            RenameColumnIfExists(dt, "Designation_Description", "DesignationName");
        }

        private static void RenameColumnIfExists(DataTable dt, string oldName, string newName)
        {
            if (dt == null || !dt.Columns.Contains(oldName) || dt.Columns.Contains(newName)) return;
            dt.Columns[oldName].ColumnName = newName;
        }

        private static PolicyExceptionHistoryRecord MapHistoryRecord(SqlDataReader reader)
        {
            return new PolicyExceptionHistoryRecord
            {
                HistoryId = Convert.ToInt32(reader["HistoryId"]),
                ExceptionId = Convert.ToInt32(reader["ExceptionId"]),
                VersionNo = Convert.ToInt32(reader["VersionNo"]),
                ActionType = Convert.ToString(reader["ActionType"]),
                PolicyJson = reader["PolicyJson"] == DBNull.Value ? "{}" : Convert.ToString(reader["PolicyJson"]),
                Priority = reader["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Priority"]),
                EffectiveFrom = reader["EffectiveFrom"] == DBNull.Value ? default(DateTime) : Convert.ToDateTime(reader["EffectiveFrom"]),
                EffectiveTo = reader["EffectiveTo"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EffectiveTo"]),
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                ChangedBy = reader["ChangedBy"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ChangedBy"]),
                ChangedOn = reader["ChangedOn"] == DBNull.Value ? default(DateTime) : Convert.ToDateTime(reader["ChangedOn"]),
                ChangeReason = reader["ChangeReason"] == DBNull.Value ? null : Convert.ToString(reader["ChangeReason"])
            };
        }

        private static PolicyExceptionRecord MapRecord(SqlDataReader reader)
        {
            return new PolicyExceptionRecord
            {
                ExceptionId = Convert.ToInt32(reader["ExceptionId"]),
                EmployeeCode = Convert.ToString(reader["EmployeeCode"]),
                EmployeeName = reader["EmployeeName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["EmployeeName"]),
                RegionId = reader["RegionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RegionId"]),
                BranchId = reader["BranchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BranchId"]),
                GradeId = reader["GradeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["GradeId"]),
                DesignationId = reader["DesignationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DesignationId"]),
                RegionName = ReadString(reader, "RegionName"),
                BranchName = ReadString(reader, "BranchName"),
                GradeName = ReadString(reader, "GradeName"),
                DesignationName = ReadString(reader, "DesignationName"),
                PolicyJson = reader["PolicyJson"] == DBNull.Value ? "{}" : Convert.ToString(reader["PolicyJson"]),
                Priority = reader["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Priority"]),
                EffectiveFrom = reader["EffectiveFrom"] == DBNull.Value ? default(DateTime) : Convert.ToDateTime(reader["EffectiveFrom"]),
                EffectiveTo = reader["EffectiveTo"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EffectiveTo"]),
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                CreatedBy = ReadString(reader, "CreatedBy"),
                CreatedOn = HasColumn(reader, "CreatedOn") && reader["CreatedOn"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["CreatedOn"]) : null,
                ModifiedBy = ReadString(reader, "ModifiedBy"),
                ModifiedOn = HasColumn(reader, "ModifiedOn") && reader["ModifiedOn"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ModifiedOn"]) : null
            };
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            return HasColumn(reader, columnName) && reader[columnName] != DBNull.Value
                ? Convert.ToString(reader[columnName])
                : string.Empty;
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
