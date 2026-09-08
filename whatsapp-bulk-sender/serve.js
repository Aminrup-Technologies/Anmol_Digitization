// WhatsApp Bulk Sender — static hosting + CORS-safe API proxy
// Zero dependencies, Node 18+ (uses global fetch). Run: node serve.js
// Proxy forwards POST /api/send only to hosts in the allowlist,
// so the page never exposes an open relay.
const http = require("http");
const fs = require("fs");
const path = require("path");

const PORT = Number(process.env.PORT) || 8080;
const HOST = process.env.HOST || "0.0.0.0";
const ROOT = __dirname;

const DEFAULT_ALLOW = ["api-wa.co", "aisensy.com", "graph.facebook.com", "graph.meta.com"];
const ALLOW_HOSTS = (process.env.WA_ALLOW_HOSTS || DEFAULT_ALLOW.join(","))
  .split(",")
  .map((s) => s.trim().toLowerCase())
  .filter(Boolean);

function hostAllowed(raw) {
  try {
    const u = new URL(raw);
    if (u.protocol !== "https:" && u.protocol !== "http:") return false;
    const h = u.hostname.toLowerCase();
    return ALLOW_HOSTS.some((a) => h === a || h.endsWith("." + a));
  } catch {
    return false;
  }
}

const MIME = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".ico": "image/x-icon",
  ".csv": "text/csv; charset=utf-8",
};

function sendJSON(res, code, obj) {
  res.writeHead(code, { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" });
  res.end(JSON.stringify(obj));
}

const server = http.createServer((req, res) => {
  const url = new URL(req.url, `http://${req.headers.host || "localhost"}`);

  if (url.pathname === "/api/health" && req.method === "GET") {
    return sendJSON(res, 200, { ok: true });
  }

  if (url.pathname === "/api/send" && req.method === "POST") {
    let data = "";
    req.on("data", (c) => {
      data += c;
      if (data.length > 1e6) req.destroy();
    });
    req.on("end", async () => {
      try {
        const { url: target, payload, headers } = JSON.parse(data || "{}");
        if (!target || !hostAllowed(target)) {
          return sendJSON(res, 400, { error: "Destination host is not allowed. Add it to the WA_ALLOW_HOSTS list." });
        }
        const ac = new AbortController();
        const t = setTimeout(() => ac.abort(), 30000);
        const r = await fetch(target, {
          method: "POST",
          headers: Object.assign({ "Content-Type": "application/json" }, headers || {}),
          body: JSON.stringify(payload),
          signal: ac.signal,
        });
        clearTimeout(t);
        const text = await r.text();
        sendJSON(res, 200, { status: r.status, body: text.slice(0, 4000) });
      } catch (e) {
        sendJSON(res, 502, { error: String((e && e.message) || e) });
      }
    });
    return;
  }

  // Static files
  let p = decodeURIComponent(url.pathname);
  if (p === "/") p = "/index.html";
  const file = path.normalize(path.join(ROOT, p));
  if (!file.startsWith(ROOT)) {
    res.writeHead(403, { "Content-Type": "text/plain" });
    return res.end("Forbidden");
  }
  fs.readFile(file, (err, buf) => {
    if (err) {
      res.writeHead(404, { "Content-Type": "text/plain" });
      return res.end("Not found");
    }
    res.writeHead(200, { "Content-Type": MIME[path.extname(file).toLowerCase()] || "application/octet-stream" });
    res.end(buf);
  });
});

server.listen(PORT, HOST, () => {
  console.log(`WhatsApp Bulk Sender running at http://${HOST}:${PORT}`);
});