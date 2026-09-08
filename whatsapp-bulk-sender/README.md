# WhatsApp Bulk Sender

Web UI for bulk WhatsApp messaging using **approved templates** — the web version of your
`Bulk_Whatsapp_Ready.csv` Python script, with provider flexibility beyond AiSensy.

Two deployment options, same UI:

| Option | Location | Runtime | Best for |
|---|---|---|---|
| **ASP.NET-integrated** (recommended) | `AnmolDristi/WhatsAppBulk/` | Your existing IIS app — no Node, no separate env | Using your existing platform |
| Standalone Node server | `whatsapp-bulk-sender/` | `node serve.js` | Quick local run / Freebuff preview |

## Option A — ASP.NET-integrated (inside your existing app)

Files live in `AnmolDristi/WhatsAppBulk/` and are registered in `AnmolDristi.csproj`
(`WhatsAppSend.ashx` as Content, `WhatsAppSend.ashx.cs` as Compile).

1. Build & deploy the app as usual (IIS, .NET Framework 4.8).
2. Open `https://your-app/WhatsAppBulk/` (signed in — the handler requires a valid session).
3. In the UI, keep **Route = Server-side via WhatsAppSend.ashx** (default).

`WhatsAppSend.ashx` relays `POST` JSON to the provider from the server, so there are **no CORS
restrictions** and **no Node process**. It only forwards to an allowlist
(`api-wa.co`, `graph.facebook.com`, `graph.meta.com` — edit `AllowedHosts` in
`WhatsAppSend.ashx.cs` to extend). The handler requires an authenticated session
(`Request.IsAuthenticated` + `Session["LoginToken"]`), matching the app's `BasePage` flow.

## Option B — standalone Node server

```bash
cd whatsapp-bulk-sender
node serve.js        # http://localhost:8080
```

No dependencies to install. Node 18+ (uses global `fetch`).

## What it does

| Your Python script | This tool |
|---|---|
| `API_KEY` / `CAMPAIGN_NAME` constants | Entered once in the UI, stored in browser localStorage |
| `csv.DictReader` with fixed columns | Any CSV — you map columns to template params visually |
| `templateParams: [achievement, growth]` | Unlimited positional params `{{1}}…{{n}}` via Add parameter |
| `time.sleep(2)` fixed | Configurable delay + batch size + batch pause |
| Console SUCCESS/FAILED tally | Live table with per-recipient HTTP status + response body, exportable to CSV |

## Providers

1. **AiSensy** (your current setup) — campaign name + project API URL (`backend.api-wa.co/campaign/<project>/api/v2`). `userName` gets the mapped Name column, `templateParams` get your mapped columns in order.
2. **Meta WhatsApp Cloud API** — phone number ID + template name + language. Builds the `graph.facebook.com/<ver>/<phoneId>/messages` payload with a Bearer token.
3. **Custom endpoint** — any POST-JSON API; payload shape mirrors AiSensy (`destination`, `userName`, `templateParams`).

## Why a relay at all?

Browsers block cross-origin calls to `backend.api-wa.co` / `graph.facebook.com` unless the provider
sends CORS headers (they don't). Both options provide a narrow server-side relay:

- ASP.NET: `WhatsAppSend.ashx` (session-protected, allowlisted hosts)
- Node: `serve.js` `POST /api/send` (allowlisted via `WA_ALLOW_HOSTS`)

API keys are typed in the page, kept in `localStorage`, and relayed per-request — never logged or stored server-side.

## CSV format

Header row required. `Phone` is the only mandatory column; any other column can be mapped to a
template parameter. Phone numbers are normalized (non-digits stripped, `+` removed, bare 10-digit
numbers get `91` prefixed — matching your script's India assumption; adjust if needed).

Duplicates are flagged and sent once (first occurrence wins).

## Safety notes

- **Throttling**: default 2 s between messages, pause every 25 — tune to your provider's rate limits
  (Meta Cloud API tiers: 250/day → 100k/day; AiSensy sets per-account limits).
- **Opt-in**: only message recipients who opted in; template sends outside the 24-h customer service
  window must use approved templates (that's what this tool does).
- **Meta quality rating**: high failure/unknown-contact rates degrade your number's quality tier.
- Meta Cloud API tokens should be a **system-user token** with `whatsapp_business_messaging` scope,
  not a personal token.

## Files

- `AnmolDristi/WhatsAppBulk/index.html` — UI hosted inside the ASP.NET app
- `AnmolDristi/WhatsAppBulk/WhatsAppSend.ashx(.cs)` — server-side relay (session-protected, allowlisted)
- `whatsapp-bulk-sender/index.html` — same UI for the standalone Node option
- `whatsapp-bulk-sender/serve.js` — static hosting + allowlisted `/api/send` proxy (zero dependencies)
- `whatsapp-bulk-sender/package.json` — `npm start` convenience
