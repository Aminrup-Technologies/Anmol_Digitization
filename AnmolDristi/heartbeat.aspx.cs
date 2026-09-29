using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Script.Services;

namespace AnmolDristi
{
    public partial class heartbeat : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        // WebMethod to update LastHeartbeat for an active session.
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object Heartbeat(int activeSessionId)
        {
            // Optional: check caller's session / authentication — you can add checks here.
            try
            {
                // (1) Basic validation
                if (activeSessionId <= 0) return new { ok = false, msg = "invalid id" };

                string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("UPDATE ActiveUserSession SET LastHeartbeat = GETDATE() WHERE ActiveSessionId = @id AND IsActive = 1", con))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = activeSessionId;
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    // Optionally return rows updated
                    return new { ok = rows > 0, updated = rows };
                }
            }
            catch (Exception ex)
            {
                // log error server-side as needed
                return new { ok = false, msg = ex.Message };
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object Heartbeat(string loginToken)
        {
            try
            {
                if (string.IsNullOrEmpty(loginToken)) return new { ok = false, msg = "no token" };
                Guid token;
                if (!Guid.TryParse(loginToken, out token)) return new { ok = false, msg = "invalid token" };

                bool updated = SessionHelper.UpdateHeartbeat(token); // add method below
                if (!updated) return new { ok = false, msg = "inactive or not found" };

                // Optionally check HttpContext.Current.Session["USERID"] matches DB row inside UpdateHeartbeat
                return new { ok = true };
            }
            catch (Exception ex)
            {
                // log exception if you have a logger
                return new { ok = false, msg = ex.Message };
            }
        }

    }
}