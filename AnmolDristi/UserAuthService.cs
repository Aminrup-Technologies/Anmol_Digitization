using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Configuration;

namespace AnmolDristi
{
    public class UserAuthService
    {
        private readonly DB_Utility_OH4Y dbcl;

        public UserAuthService(DB_Utility_OH4Y db)
        {
            if (db == null)
                throw new ArgumentNullException("db");

            dbcl = db;
        }

        //private readonly DB_Utility_OH4Y dbcl;
        // Authenticate (normal path) - uses new hash

        public DataRow Authenticate(string employeeCode, string password)
        {
            if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrEmpty(password))
                return null;

            string query = "SELECT TOP 1 Id, EmployeeCode, EmployeeName, EmployeeStatus, PasswordHash, PasswordSalt, PasswordIterations, RegionId, BranchId, PlantId, Module, DepartmentId, SubDepartmentId, GradeId, Designation_ID FROM MST_UserMaster WHERE EmployeeCode=@EmployeeCode";
            SqlParameter[] pram = { new SqlParameter("@EmployeeCode", employeeCode) };
            DataTable dt = dbcl.SPreturn_dt(query, pram);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];

            // If there's no PasswordHash (legacy) return null so caller can do migration logic
            var storedHash = row["PasswordHash"] == DBNull.Value ? null : row["PasswordHash"].ToString();
            var storedSalt = row["PasswordSalt"] == DBNull.Value ? null : row["PasswordSalt"].ToString();
            var iterations = row["PasswordIterations"] == DBNull.Value ? PasswordHasher.DefaultIterations : Convert.ToInt32(row["PasswordIterations"]);

            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return null; // treat as not verifiable, caller may handle migration

            bool ok = PasswordHasher.Verify(password, storedHash, storedSalt, iterations);
            return ok ? row : null;
        }

        // Authenticate and migrate if user has plaintext password stored in legacy 'Password' column
        public DataRow AuthenticateAndMigrateIfNeeded(string employeeCode, string password)
        {
            if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrEmpty(password))
                return null;

            string query = "SELECT TOP 1 * FROM MST_UserMaster WHERE EmployeeCode=@EmployeeCode";
            SqlParameter[] pram = { new SqlParameter("@EmployeeCode", employeeCode) };
            DataTable dt = dbcl.SPreturn_dt(query, pram);
            if (dt == null || dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];

            var storedHash = row["PasswordHash"] == DBNull.Value ? null : row["PasswordHash"].ToString();
            var storedSalt = row["PasswordSalt"] == DBNull.Value ? null : row["PasswordSalt"].ToString();

            if (!string.IsNullOrEmpty(storedHash) && !string.IsNullOrEmpty(storedSalt))
            {
                int iterations = row["PasswordIterations"] == DBNull.Value ? PasswordHasher.DefaultIterations : Convert.ToInt32(row["PasswordIterations"]);
                bool ok = PasswordHasher.Verify(password, storedHash, storedSalt, iterations);
                return ok ? row : null;
            }
            else
            {
                var legacy = row["LegacyPassword"] == DBNull.Value ? null : row["LegacyPassword"].ToString();
                if (!string.IsNullOrEmpty(legacy) && legacy == password)
                {
                    // Migrate to hash+salt
                    var tup = PasswordHasher.CreateHash(password, PasswordHasher.DefaultIterations);
                    string newHash = tup.Item1;
                    string newSalt = tup.Item2;
                    int iterations = tup.Item3;

                    string upq = "UPDATE MST_UserMaster SET PasswordHash=@PasswordHash, PasswordSalt=@PasswordSalt, PasswordIterations=@Iterations, Password=NULL WHERE Id=@Id";
                    SqlParameter[] upParams = {
                    new SqlParameter("@PasswordHash", newHash),
                    new SqlParameter("@PasswordSalt", newSalt),
                    new SqlParameter("@Iterations", iterations),
                    new SqlParameter("@Id", row["Id"])
                };

                    dbcl.ExecuteNonQuery(upq, upParams);

                    // update in-memory row values so caller can use them
                    row["PasswordHash"] = newHash;
                    row["PasswordSalt"] = newSalt;
                    row["PasswordIterations"] = iterations;
                    return row;
                }
                else
                {
                    return null;
                }
            }
        }

        // Register new user (sets hash+salt)
        public int RegisterUser(string employeeCode, string fullName, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) throw new ArgumentNullException(nameof(employeeCode));
            if (string.IsNullOrEmpty(plainPassword)) throw new ArgumentNullException(nameof(plainPassword));

            var tup = PasswordHasher.CreateHash(plainPassword, PasswordHasher.DefaultIterations);
            string hash = tup.Item1;
            string salt = tup.Item2;
            int iterations = tup.Item3;

            string insert = @"INSERT INTO MST_UserMaster (EmployeeCode, EmployeeName, PasswordHash, PasswordSalt, PasswordIterations, EmployeeStatus)
                          VALUES (@EmployeeCode, @EmployeeName, @PasswordHash, @PasswordSalt, @Iterations, @Status);
                          SELECT SCOPE_IDENTITY();";

            SqlParameter[] pram = {
            new SqlParameter("@EmployeeCode", employeeCode),
            new SqlParameter("@EmployeeName", (object)fullName ?? DBNull.Value),
            new SqlParameter("@PasswordHash", hash),
            new SqlParameter("@PasswordSalt", salt),
            new SqlParameter("@Iterations", iterations),
            new SqlParameter("@Status", "Active")
        };

            object result = dbcl.ExecuteScalar(insert, pram);
            return Convert.ToInt32(result);
        }

        // Change password (requires current password verification)
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            string q = "SELECT PasswordHash, PasswordSalt, PasswordIterations, Password AS LegacyPassword FROM MST_UserMaster WHERE Id=@Id";
            SqlParameter[] p = { new SqlParameter("@Id", userId) };
            DataTable dt = dbcl.SPreturn_dt(q, p);
            if (dt == null || dt.Rows.Count == 0) return false;

            var row = dt.Rows[0];
            var storedHash = row["PasswordHash"] == DBNull.Value ? null : row["PasswordHash"].ToString();
            var storedSalt = row["PasswordSalt"] == DBNull.Value ? null : row["PasswordSalt"].ToString();

            bool verified = false;
            if (!string.IsNullOrEmpty(storedHash) && !string.IsNullOrEmpty(storedSalt))
            {
                int iterations = row["PasswordIterations"] == DBNull.Value ? PasswordHasher.DefaultIterations : Convert.ToInt32(row["PasswordIterations"]);
                verified = PasswordHasher.Verify(currentPassword, storedHash, storedSalt, iterations);
            }
            else
            {
                var legacy = row["LegacyPassword"] == DBNull.Value ? null : row["LegacyPassword"].ToString();
                verified = (legacy != null && legacy == currentPassword);
            }

            if (!verified) return false;

            var tup = PasswordHasher.CreateHash(newPassword, PasswordHasher.DefaultIterations);
            string newHash = tup.Item1;
            string newSalt = tup.Item2;
            int newIterations = tup.Item3;

            string up = "UPDATE MST_UserMaster SET PasswordHash=@Hash, PasswordSalt=@Salt, PasswordIterations=@Iterations, Password=NULL WHERE Id=@Id";
            SqlParameter[] upParams = {
            new SqlParameter("@Hash", newHash),
            new SqlParameter("@Salt", newSalt),
            new SqlParameter("@Iterations", newIterations),
            new SqlParameter("@Id", userId)
        };
            dbcl.ExecuteNonQuery(up, upParams);
            return true;
        }
    }
}