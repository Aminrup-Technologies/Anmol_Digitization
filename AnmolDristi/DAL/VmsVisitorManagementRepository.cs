using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    /// <summary>
    /// Persistence access for VMS host My Visitors operations.
    /// </summary>
    public class VmsVisitorManagementRepository
    {
        private readonly string _connectionString;

        public VmsVisitorManagementRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public DataTable GetPendingVisits(string empCode)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string pendingQuery =
                    "SELECT * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus = 'Pending' ORDER BY CreatedDate DESC";

                using (SqlDataAdapter sda = new SqlDataAdapter(pendingQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtPending = new DataTable();
                    sda.Fill(dtPending);
                    return dtPending;
                }
            }
        }

        public DataTable GetHostVisitHistory(string empCode)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string historyQuery =
                    "SELECT TOP 50 * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus IN ('Approved', 'Rejected') ORDER BY CreatedDate DESC";

                using (SqlDataAdapter sda = new SqlDataAdapter(historyQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtHistory = new DataTable();
                    sda.Fill(dtHistory);
                    return dtHistory;
                }
            }
        }

        public void RejectVisitor(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string updateQuery =
                    "UPDATE VMS_VisitLog SET ApprovalStatus = 'Rejected', VisitStatus = 'Rejected', ApprovedOn = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public void ManualCheckIn(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // FIXED: Changed CheckInTime to ActualInTime
                string updateQuery =
                    "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-In', ActualInTime = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public void ManualCheckOut(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // FIXED: Changed CheckOutTime to ActualOutTime
                string updateQuery =
                    "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-Out', ActualOutTime = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }
    }
}
