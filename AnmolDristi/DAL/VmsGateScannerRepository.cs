using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    /// <summary>
    /// Persistence access for VMS Gate Scanner read operations.
    /// </summary>
    public class VmsGateScannerRepository
    {
        private readonly string _connectionString;

        public VmsGateScannerRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public string GetGateLocation(int branchId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT ISNULL(s.AliasName, b.Branch_Description) AS LocationName
                    FROM MST_Branch b
                    LEFT JOIN VMS_OfficeSettings s ON b.Branch_ID = s.Branch_ID
                    WHERE b.Branch_ID = @BranchID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : null;
                }
            }
        }

        public DataTable GetInsideVisitors(int? branchId)
        {
            string query = @"
                SELECT 
                    v.LogID,
                    ISNULL(d.DayLogID, 0) AS DayLogID,
                    v.VisitorName, 
                    v.VisitorCompany, 
                    ISNULL(NULLIF(v.VisitorMobile, ''), v.VisitorEmail) AS ContactInfo,
                    ISNULL(u.EmployeeName, 'Unknown') AS HostName,
                    COALESCE(d.ActualInTime, v.ActualInTime) AS ActualInTime,
                    COALESCE(d.DayValidUpto, v.ValidUpto) AS ValidUpto,
                    v.PhotoPath
                FROM VMS_VisitLog v
                LEFT JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                    AND d.VisitDate = CAST(GETDATE() AS date)
                    AND d.VisitStatus = 'Checked-In'
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE (
                    (d.DayLogID IS NOT NULL)
                    OR (v.VisitStatus = 'Checked-In' AND NOT EXISTS (
                        SELECT 1 FROM VMS_VisitDayLog dx WHERE dx.LogID = v.LogID
                    ))
                )";

            if (branchId.HasValue)
            {
                query += " AND v.TargetBranchID = @BranchId";
            }

            query += " ORDER BY v.ActualInTime DESC";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (branchId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId.Value);
                }

                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetScanVisit(Guid token)
        {
            string query = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail, 
                    v.ApprovalStatus, v.VisitStatus, v.ValidFrom, v.ValidUpto, v.PhotoPath, v.ActualInTime, v.PassCategory,
                    v.TargetBranchID,
                    u.EmployeeName AS HostName, u.Email AS HostEmail,
                    ISNULL(dept.Department_Description, 'General') AS HostDepartment,
                    ISNULL(b.Branch_Description, 'Unknown Branch') AS VisitBranchName,
                    d.DayLogID, d.DayValidFrom, d.DayValidUpto, d.VisitStatus AS DayVisitStatus,
                    d.ActualInTime AS DayActualInTime, d.IsDayApproved
                FROM VMS_VisitLog v
                INNER JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department dept ON CAST(u.DepartmentId AS VARCHAR) = dept.Department_ID
                LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
                WHERE v.QRToken = @Token
                  AND d.VisitDate = CAST(GETDATE() AS DATE)
                  AND d.IsDayApproved = 1";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetVisitByToken(Guid token)
        {
            string query = @"
                SELECT 
                    v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail,
                    v.PhotoPath, v.PassCategory, v.ValidUpto,
                    u.EmployeeName AS HostName,
                    ISNULL(dept.Department_Description, 'General') AS HostDepartment
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department dept ON CAST(u.DepartmentId AS VARCHAR) = dept.Department_ID
                WHERE v.QRToken = @Token";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetCheckoutVisitor(int logId)
        {
            string query = @"
                SELECT v.VisitorName, v.VisitorCompany, u.EmployeeName AS HostName, u.Email AS HostEmail 
                FROM VMS_VisitLog v 
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode 
                WHERE v.LogID = @LogID";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }
    }
}
