using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    /// <summary>
    /// Persistence access for VMS visitor registration and visit records.
    /// </summary>
    public class VmsVisitorRepository
    {
        private readonly string _connectionString;

        public VmsVisitorRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public int CreateVisitorVisit(
            string category,
            string fullName,
            string firstName,
            string lastName,
            string mobile,
            string email,
            string company,
            string designation,
            string photoPath,
            int branchId,
            string hostCode,
            string purpose,
            string approvalStatus,
            DateTime validFrom,
            DateTime validUpto,
            object qrToken,
            string visitStatus,
            object actualInTime,
            object approvedOn,
            VisitScheduleRequest visitSchedule)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = @"

                                INSERT INTO VMS_VisitLog
                                (
                                PassCategory,
                                VisitorName,
                                VisitorFirstName,
                                VisitorLastName,
                                VisitorMobile,
                                VisitorEmail,
                                VisitorCompany,
                                VisitorDesignation,
                                PhotoPath,
                                TargetBranchID,
                                HostEmployeeCode,
                                PurposeOfVisit,
                                ApprovalStatus,
                                ValidFrom,
                                ValidUpto,
                                QRToken,
                                VisitStatus,
                                ActualInTime,
                                ApprovedOn,
                                ScheduleType,
                                RequestedValidFrom,
                                RequestedValidUpto,
                                DailyTimeFrom,
                                DailyTimeTo,
                                ApprovedValidFrom,
                                ApprovedValidUpto
                                )
                                VALUES
                                (
                                @Category,
                                @FullName,
                                @FirstName,
                                @LastName,
                                @Mobile,
                                @Email,
                                @Company,
                                @Designation,
                                @PhotoPath,
                                @BranchID,
                                @HostCode,
                                @Purpose,
                                @ApprovalStatus,
                                @ValidFrom,
                                @ValidUpto,
                                @QRToken,
                                @VisitStatus,
                                @ActualInTime,
                                @ApprovedOn,
                                @ScheduleType,
                                @RequestedValidFrom,
                                @RequestedValidUpto,
                                @DailyTimeFrom,
                                @DailyTimeTo,
                                @ApprovedValidFrom,
                                @ApprovedValidUpto
                                );

                                SELECT SCOPE_IDENTITY();
                                ";

                        int newVisitId;
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@Category", category);
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            cmd.Parameters.AddWithValue("@FirstName", firstName);
                            cmd.Parameters.AddWithValue("@LastName", lastName);
                            cmd.Parameters.AddWithValue("@Mobile", mobile);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@Company", company);
                            cmd.Parameters.AddWithValue("@Designation", designation);
                            cmd.Parameters.AddWithValue("@PhotoPath", photoPath);
                            cmd.Parameters.AddWithValue("@BranchID", branchId);
                            cmd.Parameters.AddWithValue("@HostCode", hostCode);
                            cmd.Parameters.AddWithValue("@Purpose", purpose);
                            cmd.Parameters.AddWithValue("@ApprovalStatus", approvalStatus);
                            cmd.Parameters.AddWithValue("@ValidFrom", validFrom);
                            cmd.Parameters.AddWithValue("@ValidUpto", validUpto);
                            cmd.Parameters.AddWithValue("@QRToken", qrToken);
                            cmd.Parameters.AddWithValue("@VisitStatus", visitStatus);
                            cmd.Parameters.AddWithValue("@ActualInTime", actualInTime);
                            cmd.Parameters.AddWithValue("@ApprovedOn", approvedOn);
                            cmd.Parameters.AddWithValue("@ScheduleType", visitSchedule.ScheduleType);
                            cmd.Parameters.AddWithValue("@RequestedValidFrom", visitSchedule.RequestedValidFrom);
                            cmd.Parameters.AddWithValue("@RequestedValidUpto", visitSchedule.RequestedValidUpto);
                            cmd.Parameters.AddWithValue("@DailyTimeFrom", visitSchedule.TimeFrom);
                            cmd.Parameters.AddWithValue("@DailyTimeTo", visitSchedule.TimeTo);
                            cmd.Parameters.AddWithValue("@ApprovedValidFrom",
                                approvalStatus == "Approved" ? (object)validFrom : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ApprovedValidUpto",
                                approvalStatus == "Approved" ? (object)validUpto : DBNull.Value);

                            newVisitId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        VmsVisitScheduleHelper.CreateRequestedDayLogs(
                            con,
                            trans,
                            newVisitId,
                            visitSchedule,
                            approvalStatus == "Approved");

                        trans.Commit();
                        return newVisitId;
                    }
                    catch
                    {
                        try
                        {
                            trans.Rollback();
                        }
                        catch
                        {
                            // Ignore rollback exceptions
                        }

                        throw;
                    }
                }
            }
        }

        public DataTable GetVisitorHistory(string mobile, string email)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        v.LogID,
                        v.CreatedDate,
                        v.PurposeOfVisit,
                        v.ApprovalStatus,
                        v.VisitStatus,
                        v.ScheduleType,
                        v.RequestedValidFrom,
                        v.RequestedValidUpto,
                        v.ValidFrom,
                        v.ValidUpto,
                        v.QRToken,
                        ISNULL(o.AliasName, b.Branch_Description) AS AliasName,
                        u.EmployeeName AS HostName
                    FROM 
                        VMS_VisitLog v
                    LEFT JOIN 
                        VMS_OfficeSettings o ON v.TargetBranchID = o.Branch_ID
                    LEFT JOIN 
                        MST_Branch b ON v.TargetBranchID = b.Branch_ID
                    LEFT JOIN 
                        MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    WHERE 
                        (
                            (@Mobile <> '' AND v.VisitorMobile = @Mobile)
                            OR (@Email <> '' AND @Email <> 'N/A' AND v.VisitorEmail = @Email)
                        )
                        AND v.CreatedDate >= DATEADD(month, -6, GETDATE())
                    ORDER BY 
                        v.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", VmsHelper.NormalizeMobile(mobile));
                    cmd.Parameters.AddWithValue("@Email", VmsHelper.NormalizeEmail(email));

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
}
