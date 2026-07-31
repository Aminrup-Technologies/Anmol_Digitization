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
                cmd.Parameters.AddWithValue("@EmployeeCode", string.IsNullOrWhiteSpace(criteria.EmployeeCode) ? (object)DBNull.Value : criteria.EmployeeCode.Trim());
                cmd.Parameters.AddWithValue("@EmployeeName", string.IsNullOrWhiteSpace(criteria.EmployeeName) ? (object)DBNull.Value : "%" + criteria.EmployeeName.Trim() + "%");
                cmd.Parameters.AddWithValue("@RegionId", criteria.RegionId.HasValue ? (object)criteria.RegionId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchId", criteria.BranchId.HasValue ? (object)criteria.BranchId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@DesignationId", criteria.DesignationId.HasValue ? (object)criteria.DesignationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", criteria.IsActive.HasValue ? (object)criteria.IsActive.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EffectiveDate", criteria.EffectiveDate.HasValue ? (object)criteria.EffectiveDate.Value.Date : DBNull.Value);

                var dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                return dt;
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
    r.Region_Description AS RegionName,
    b.Branch_Description AS BranchName,
    g.Grade_Description AS GradeName,
    d.Designation_Description AS DesignationName
FROM dbo.MST_UserMaster u WITH (NOLOCK)
LEFT JOIN dbo.MST_Region r WITH (NOLOCK) ON TRY_CAST(u.RegionId AS INT) = r.Id
LEFT JOIN dbo.MST_Branch b WITH (NOLOCK) ON TRY_CAST(u.BranchId AS INT) = b.Id
LEFT JOIN dbo.MST_Grade g WITH (NOLOCK) ON TRY_CAST(u.GradeId AS INT) = g.Id
LEFT JOIN dbo.MST_Designation d WITH (NOLOCK) ON TRY_CAST(u.Designation_ID AS INT) = d.Id
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
                        RegionName = reader["RegionName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["RegionName"]),
                        BranchName = reader["BranchName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["BranchName"]),
                        GradeName = reader["GradeName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["GradeName"]),
                        DesignationName = reader["DesignationName"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DesignationName"])
                    };
                }
            }
        }

        public bool HasOverlappingActiveException(string employeeCode, DateTime effectiveFrom, DateTime? effectiveTo, int excludeExceptionId)
        {
            DateTime rangeTo = effectiveTo.HasValue ? effectiveTo.Value.Date : new DateTime(9999, 12, 31);

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.PolicyException WITH (NOLOCK)
WHERE EmployeeCode = @EmployeeCode
  AND IsActive = 1
  AND ExceptionId <> @ExcludeExceptionId
  AND EffectiveFrom <= @RangeTo
  AND ISNULL(EffectiveTo, '9999-12-31') >= @EffectiveFrom", cn))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                cmd.Parameters.AddWithValue("@ExcludeExceptionId", excludeExceptionId);
                cmd.Parameters.AddWithValue("@EffectiveFrom", effectiveFrom.Date);
                cmd.Parameters.AddWithValue("@RangeTo", rangeTo);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public int SavePolicyException(PolicyExceptionRecord record, string currentUser)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(record.ExceptionId > 0 ? "dbo.usp_UpdatePolicyException" : "dbo.usp_InsertPolicyException", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (record.ExceptionId > 0)
                    cmd.Parameters.AddWithValue("@ExceptionId", record.ExceptionId);

                cmd.Parameters.AddWithValue("@EmployeeCode", record.EmployeeCode.Trim());
                cmd.Parameters.AddWithValue("@RegionId", record.RegionId.HasValue ? (object)record.RegionId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchId", record.BranchId.HasValue ? (object)record.BranchId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@GradeId", record.GradeId.HasValue ? (object)record.GradeId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@DesignationId", record.DesignationId.HasValue ? (object)record.DesignationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@PolicyJson", record.PolicyJson ?? "{}");
                cmd.Parameters.AddWithValue("@Priority", record.Priority);
                cmd.Parameters.AddWithValue("@EffectiveFrom", record.EffectiveFrom.Date);
                cmd.Parameters.AddWithValue("@EffectiveTo", record.EffectiveTo.HasValue ? (object)record.EffectiveTo.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", record.IsActive);
                cmd.Parameters.AddWithValue("@User", currentUser ?? "SYSTEM");

                cn.Open();
                if (record.ExceptionId > 0)
                {
                    cmd.ExecuteNonQuery();
                    return record.ExceptionId;
                }

                object scalar = cmd.ExecuteScalar();
                return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
            }
        }

        public void SetPolicyExceptionActiveStatus(int exceptionId, bool isActive, string currentUser)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SetPolicyExceptionActiveStatus", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ExceptionId", exceptionId);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@ModifiedBy", currentUser ?? "SYSTEM");
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable GetMasterList(string masterType)
        {
            string sql;
            switch ((masterType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "region":
                    sql = "SELECT Id, Region_Description AS Description FROM dbo.MST_Region ORDER BY Region_Description";
                    break;
                case "branch":
                    sql = "SELECT Id, Branch_Description AS Description FROM dbo.MST_Branch ORDER BY Branch_Description";
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
                RegionName = HasColumn(reader, "RegionName") && reader["RegionName"] != DBNull.Value ? Convert.ToString(reader["RegionName"]) : string.Empty,
                BranchName = HasColumn(reader, "BranchName") && reader["BranchName"] != DBNull.Value ? Convert.ToString(reader["BranchName"]) : string.Empty,
                GradeName = HasColumn(reader, "GradeName") && reader["GradeName"] != DBNull.Value ? Convert.ToString(reader["GradeName"]) : string.Empty,
                DesignationName = HasColumn(reader, "DesignationName") && reader["DesignationName"] != DBNull.Value ? Convert.ToString(reader["DesignationName"]) : string.Empty,
                PolicyJson = reader["PolicyJson"] == DBNull.Value ? "{}" : Convert.ToString(reader["PolicyJson"]),
                Priority = reader["Priority"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Priority"]),
                EffectiveFrom = reader["EffectiveFrom"] == DBNull.Value ? default(DateTime) : Convert.ToDateTime(reader["EffectiveFrom"]),
                EffectiveTo = reader["EffectiveTo"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EffectiveTo"]),
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                CreatedBy = HasColumn(reader, "CreatedBy") && reader["CreatedBy"] != DBNull.Value ? Convert.ToString(reader["CreatedBy"]) : string.Empty,
                CreatedOn = HasColumn(reader, "CreatedOn") && reader["CreatedOn"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["CreatedOn"]) : null,
                ModifiedBy = HasColumn(reader, "ModifiedBy") && reader["ModifiedBy"] != DBNull.Value ? Convert.ToString(reader["ModifiedBy"]) : string.Empty,
                ModifiedOn = HasColumn(reader, "ModifiedOn") && reader["ModifiedOn"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ModifiedOn"]) : null
            };
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
