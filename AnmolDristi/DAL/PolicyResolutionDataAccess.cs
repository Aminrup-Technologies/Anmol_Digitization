using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    public class PolicyResolutionDataAccess
    {
        private readonly string _connectionString;

        public PolicyResolutionDataAccess()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        /// <summary>
        /// Resolves the standard (matrix) policy using the existing database procedure.
        /// </summary>
        public ResolvedPolicyDto GetStandardPolicy(
            string employeeCode,
            int? regionId,
            int? branchId,
            int? gradeId,
            int? designationId)
        {
            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetResolvedPolicy", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@EmployeeCode", SqlDbType.VarChar, 50).Value = (object)employeeCode ?? DBNull.Value;
                cmd.Parameters.Add("@RegionIdParam", SqlDbType.VarChar, 200).Value = regionId.HasValue ? (object)regionId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@BranchIdParam", SqlDbType.VarChar, 200).Value = branchId.HasValue ? (object)branchId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@GradeIdParam", SqlDbType.VarChar, 200).Value = gradeId.HasValue ? (object)gradeId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@DesignationIdParam", SqlDbType.VarChar, 200).Value = designationId.HasValue ? (object)designationId.Value.ToString() : DBNull.Value;

                cn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (!reader.Read()) return null;
                    return MapStandardPolicy(reader);
                }
            }
        }

        private static ResolvedPolicyDto MapStandardPolicy(SqlDataReader reader)
        {
            var dto = new ResolvedPolicyDto();

            if (ColumnExists(reader, "Source")) dto.Source = reader["Source"] as string;
            if (ColumnExists(reader, "PolicyId")) dto.RowId = reader["PolicyId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PolicyId"]) : null;
            if (ColumnExists(reader, "EmployeeCode")) dto.EmployeeCode = reader["EmployeeCode"] != DBNull.Value ? reader["EmployeeCode"].ToString() : null;
            if (ColumnExists(reader, "RegionId")) dto.RegionId = reader["RegionId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["RegionId"]) : null;
            if (ColumnExists(reader, "BranchId")) dto.BranchId = reader["BranchId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BranchId"]) : null;
            if (ColumnExists(reader, "GradeId")) dto.GradeId = reader["GradeId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["GradeId"]) : null;
            if (ColumnExists(reader, "DesignationId")) dto.DesignationId = reader["DesignationId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["DesignationId"]) : null;
            if (ColumnExists(reader, "PolicyJson")) dto.PolicyJson = reader["PolicyJson"] != DBNull.Value ? reader["PolicyJson"].ToString() : null;
            if (ColumnExists(reader, "Priority")) dto.Priority = reader["Priority"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Priority"]) : null;
            if (ColumnExists(reader, "EffectiveFrom")) dto.EffectiveFrom = reader["EffectiveFrom"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveFrom"]) : null;
            if (ColumnExists(reader, "EffectiveTo")) dto.EffectiveTo = reader["EffectiveTo"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveTo"]) : null;
            if (ColumnExists(reader, "IsActive")) dto.IsActive = reader["IsActive"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["IsActive"]) : null;
            if (ColumnExists(reader, "Specificity")) dto.Specificity = reader["Specificity"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Specificity"]) : null;
            if (ColumnExists(reader, "Resolved_MaxKM")) dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
            if (ColumnExists(reader, "Resolved_RatePerKm")) dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
            if (ColumnExists(reader, "Resolved_DA")) dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
            if (ColumnExists(reader, "Resolved_HotelCap")) dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;
            if (ColumnExists(reader, "Resolved_OutstationDA")) dto.Resolved_OutstationDA = reader["Resolved_OutstationDA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OutstationDA"]) : null;
            if (ColumnExists(reader, "Resolved_OwnAllowance")) dto.Resolved_OwnAllowance = reader["Resolved_OwnAllowance"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OwnAllowance"]) : null;
            if (ColumnExists(reader, "Resolved_Remarks")) dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

            if (!HasAnyResolvedValue(dto) && !string.IsNullOrEmpty(dto.PolicyJson))
                PolicyExceptionSerializer.ApplyToResolvedPolicy(dto, dto.PolicyJson);

            return dto;
        }

        private static bool HasAnyResolvedValue(ResolvedPolicyDto dto)
        {
            return dto.Resolved_MaxKM.HasValue
                || dto.Resolved_RatePerKm.HasValue
                || dto.Resolved_DA.HasValue
                || dto.Resolved_OutstationDA.HasValue
                || dto.Resolved_OwnAllowance.HasValue;
        }

        private static bool ColumnExists(SqlDataReader reader, string columnName)
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
