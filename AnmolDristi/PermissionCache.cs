using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AnmolDristi
{
    public class PermissionCache
    {
        private const string AllPermsKey = "App_AllPermissions"; // app cache
        private static TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public static Dictionary<int, PermissionDto> GetAllPermissions(string connectionString)
        {
            var cache = HttpContext.Current?.Application;
            if (cache == null) // fallback: read directly
                return LoadAllPermissions(connectionString);

            var obj = cache[AllPermsKey] as Dictionary<int, PermissionDto>;
            if (obj != null) return obj;

            obj = LoadAllPermissions(connectionString);
            cache[AllPermsKey] = obj;
            return obj;
        }

        public static void InvalidateAllPermissionsCache()
        {
            var cache = HttpContext.Current?.Application;
            if (cache != null) cache.Remove(AllPermsKey);
        }

        private static Dictionary<int, PermissionDto> LoadAllPermissions(string conn)
        {
            var dict = new Dictionary<int, PermissionDto>();
            using (var cn = new SqlConnection(conn))
            using (var cmd = new SqlCommand("SELECT PermissionId, PermissionName, PermissionType, PagePath, ControlId FROM dbo.Permission WHERE IsObsolete = 0", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var p = new PermissionDto
                        {
                            PermissionId = rdr.GetInt32(0),
                            PermissionName = rdr.GetString(1),
                            PermissionType = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                            PagePath = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                            ControlId = rdr.IsDBNull(4) ? null : rdr.GetString(4)
                        };
                        dict[p.PermissionId] = p;
                    }
                }
            }
            return dict;
        }

        public class PermissionDto
        {
            public int PermissionId { get; set; }
            public string PermissionName { get; set; }
            public string PermissionType { get; set; }
            public string PagePath { get; set; }
            public string ControlId { get; set; }
        }
    }
}