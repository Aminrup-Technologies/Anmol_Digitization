using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace AnmolDristi
{
    /// <summary>
    /// Server-side relay for the WhatsApp Bulk Sender UI.
    /// The browser cannot call backend.api-wa.co / graph.facebook.com directly (CORS),
    /// so the page posts to this handler, which forwards the request to the provider.
    /// Only hosts in the allowlist below are accepted. No credentials are logged or stored.
    /// </summary>
    public class WhatsAppSend : IHttpHandler, System.Web.SessionState.IReadOnlySessionState
    {
        private static readonly string[] AllowedHosts =
        {
            "api-wa.co",              // AiSensy
            "graph.facebook.com",     // Meta WhatsApp Cloud API
            "graph.meta.com"          // Meta WhatsApp Cloud API (alt)
        };

        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            if (!HttpContext.Current.Request.IsAuthenticated ||
                HttpContext.Current.Session == null ||
                HttpContext.Current.Session["LoginToken"] == null)
            {
                context.Response.StatusCode = 401;
                WriteJson(context.Response, new { error = "Session expired. Please sign in again." });
                return;
            }

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 405;
                WriteJson(context.Response, new { error = "POST required." });
                return;
            }

            string body = new StreamReader(context.Request.InputStream, Encoding.UTF8).ReadToEnd();
            if (string.IsNullOrWhiteSpace(body))
            {
                context.Response.StatusCode = 400;
                WriteJson(context.Response, new { error = "Empty request body." });
                return;
            }

            var json = Json.Deserialize<Dictionary<string, object>>(body ?? "{}");
            if (json == null)
            {
                context.Response.StatusCode = 400;
                WriteJson(context.Response, new { error = "Invalid JSON body." });
                return;
            }

            string target = json.ContainsKey("url") ? Convert.ToString(json["url"]) : null;
            if (string.IsNullOrWhiteSpace(target) || !IsHostAllowed(target))
            {
                context.Response.StatusCode = 400;
                WriteJson(context.Response, new { error = "Destination host is not allowed. Add it to the handler's AllowedHosts list.", target = target });
                return;
            }

            Dictionary<string, object> payload = null;
            if (json.ContainsKey("payload") && json["payload"] is Dictionary<string, object>)
                payload = (Dictionary<string, object>)json["payload"];

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (json.ContainsKey("headers") && json["headers"] is Dictionary<string, object>)
            {
                foreach (var kv in (Dictionary<string, object>)json["headers"])
                    headers[kv.Key] = Convert.ToString(kv.Value);
            }

            Forward(context, target, headers, payload ?? new Dictionary<string, object>());
        }

        private static void Forward(HttpContext context, string target, Dictionary<string, string> headers, Dictionary<string, object> payload)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(target);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 30000;
                request.ReadWriteTimeout = 30000;
                foreach (var h in headers)
                {
                    if (string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                        request.ContentType = h.Value;
                    else
                        request.Headers[h.Key] = h.Value;
                }

                byte[] data = Encoding.UTF8.GetBytes(Json.Serialize(payload));
                using (var stream = request.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string text = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    context.Response.StatusCode = (int)response.StatusCode;
                    WriteJson(context.Response, new { status = (int)response.StatusCode, body = Truncate(text, 4000) });
                }
            }
            catch (WebException wex)
            {
                int status = 0;
                string text = wex.Message;
                if (wex.Response != null)
                {
                    using (HttpWebResponse er = (HttpWebResponse)wex.Response)
                    {
                        status = (int)er.StatusCode;
                        using (var sr = new StreamReader(er.GetResponseStream(), Encoding.UTF8))
                            text = sr.ReadToEnd();
                    }
                }
                try { context.Response.StatusCode = status > 0 ? status : 502; } catch { context.Response.StatusCode = 502; }
                WriteJson(context.Response, new { status = status, body = Truncate(text, 4000) });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 502;
                WriteJson(context.Response, new { status = 0, body = Truncate(ex.Message, 4000) });
            }
        }

        private static bool IsHostAllowed(string url)
        {
            try
            {
                Uri u = new Uri(url);
                if (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp) return false;
                string host = u.Host.ToLowerInvariant();
                return AllowedHosts.Any(a => host == a || host.EndsWith("." + a, StringComparison.Ordinal));
            }
            catch
            {
                return false;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max);
        }

        private static void WriteJson(HttpResponse response, object obj)
        {
            response.Write(Json.Serialize(obj));
        }

        public bool IsReusable => false;
    }
}