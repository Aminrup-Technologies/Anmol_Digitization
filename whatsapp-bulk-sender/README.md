# WhatsApp Bulk Sender

Web UI for bulk WhatsApp messaging using **approved templates** — the web version of your
`Bulk_Whatsapp_Ready.csv` Python script, with provider flexibility beyond AiSensy.

## Run

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

## Why the bundled server?

Browsers block cross-origin calls to `backend.api-wa.co` / `graph.facebook.com` unless the provider
sends CORS headers (they don't). `serve.js` exposes **one** endpoint, `POST /api/send`, which
forwards only to hosts in the allowlist (`api-wa.co`, `graph.facebook.com`, `graph.meta.com` —
extend via `WA_ALLOW_HOSTS` env var, comma-separated). It is a narrow relay, not an open proxy.
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

- `index.html` — the entire UI (no build step, no framework)
- `serve.js` — static hosting + allowlisted `/api/send` proxy (zero dependencies)
- `package.json` — `npm start` convenience
