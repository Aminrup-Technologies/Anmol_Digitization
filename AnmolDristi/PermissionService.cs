using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Caching;

namespace AnmolDristi
{
    public class PermissionService
    {
        private readonly string _conn;

        public PermissionService()
        {
            _conn = System.Configuration.ConfigurationManager
                        .ConnectionStrings["DbConn"]
                        .ConnectionString;
        }

        public PermissionService(string connectionString = null)
        {
            _conn = connectionString
                  ?? System.Configuration.ConfigurationManager
                         .ConnectionStrings["DbConn"]
                         .ConnectionString;
        }

        private string Conn => _conn;

        // Get permissions granted to a role
        public List<string> GetPermissionsForRole(int roleId)
        {
            var res = new List<string>();
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT p.Name FROM dbo.RolePermission rp JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId WHERE rp.RoleId = @r", cn))
            {
                cmd.Parameters.AddWithValue("@r", roleId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) res.Add(rdr.GetString(0));
                }
            }
            return res;
        }

        // Assign permission to role (idempotent)
        public void AssignPermissionToRole(int roleId, int permissionId, string changedBy)
        {
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@r AND PermissionId=@p) INSERT INTO dbo.RolePermission(RoleId,PermissionId) VALUES(@r,@p)", cn))
            {
                cmd.Parameters.AddWithValue("@r", roleId);
                cmd.Parameters.AddWithValue("@p", permissionId);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            // invalidate any caches if needed
            // We rely on session-level grant set being rebuilt on user re-login or by explicit endpoint
        }

        // Remove mapping (idempotent)
        public void RemovePermissionFromRole(int roleId, int permissionId)
        {
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("DELETE FROM dbo.RolePermission WHERE RoleId=@r AND PermissionId=@p", cn))
            {
                cmd.Parameters.AddWithValue("@r", roleId);
                cmd.Parameters.AddWithValue("@p", permissionId);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Save UI mapping metadata into Permission row
        public void SavePermissionUiMetadata(int permissionId, string pagePath, string controlId, string permissionType = "UI")
        {
            using (var cn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("UPDATE dbo.Permission SET PagePath=@page, ControlId=@ctrl, PermissionType=@type WHERE PermissionId=@id", cn))
            {
                cmd.Parameters.AddWithValue("@page", (object)pagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ctrl", (object)controlId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@type", (object)permissionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", permissionId);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            PermissionCache.InvalidateAllPermissionsCache();
        }
    }

    public class PermissionDto
    {
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public string PermissionType { get; set; }
        public string PagePath { get; set; }
        public string ControlId { get; set; }
    }

    
}