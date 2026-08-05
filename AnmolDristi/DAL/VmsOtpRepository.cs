using System;
using System.Configuration;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    /// <summary>
    /// Persistence access for VMS OTP tracking records.
    /// </summary>
    public class VmsOtpRepository
    {
        private readonly string _connectionString;

        public VmsOtpRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public void CreateOtp(string mobile, string otp, DateTime expiresAt)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    INSERT INTO VMS_OTPTracking
                    (MobileNo, EmailAddress, MobileOTP, EmailOTP, ExpiresAt)
                    VALUES
                    (@Mobile, '', @MobileOTP, 'N/A', @ExpiresAt)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@MobileOTP", otp);
                    cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
