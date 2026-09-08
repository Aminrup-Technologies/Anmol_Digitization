# WhatsApp Bulk Sender

Web UI for bulk WhatsApp messaging using **approved templates** — the web version of your
`Bulk_Whatsapp_Ready.csv` Python script.

## Deployment Options

| Option | Location | Runtime | API Target | Best for |
|---|---|---|---|---|
| **Phase 1 WebForms** (recommended) | `AnmolDristi/WhatsApp_BulkSend.aspx` | Your IIS app + .NET Framework | CEOITBOX bulk CSV endpoint | Production use |
| ASP.NET ashx relay | `AnmolDristi/WhatsAppBulk/` | Your IIS app | AiSensy per-recipient | Quick integration |
| Standalone Node server | `whatsapp-bulk-sender/` | `node serve.js` | AiSensy / Meta / Custom | Local dev / preview |

## Phase 1 — WhatsApp_BulkSend.aspx (Recommended)

The primary integration for your ASP.NET WebForms app. Upload Excel, preview, validate,
map columns, and submit to CEOITBOX's bulk CSV campaign endpoint in one flow.

**Files:**
- `AnmolDristi/WhatsApp_BulkSend.aspx` + `.aspx.cs` + `.aspx.designer.cs` — UI + code-behind
- `AnmolDristi/WhatsAppService.cs` — service layer (Excel parse, CSV convert, API call)
- `AnmolDristi/WhatsAppModels.cs` — DTOs (`CEOITBOXConfig`, `CampaignConfig`, `SendResult`)

**Setup:**
1. Add CEOITBOX credentials to `Web.config`:
   ```xml
   <add key="CEOITBOX.BaseUrl" value="https://official.ceoitbox.com" />
   <add key="CEOITBOX.ApiKey" value="YOUR_API_KEY" />
   <add key="CEOITBOX.PhoneId" value="" />
   <add key="CEOITBOX.UseBasicAuthentication" value="false" />
   <add key="CEOITBOX.Username" value="" />
   <add key="CEOITBOX.Password" value="" />
   ```
2. Build in Visual Studio (no new NuGet needed — uses EPPlus 4.5 + System.Net.Http).
3. Navigate to `WhatsApp_BulkSend.aspx`.

**API endpoint:** `POST /api/v1/whatsapp/sendMessageByCampaign1` (multipart/form-data)
- `jsonData`: `{ campaignType: "CSV", campaignName, message: { templateName, language } }`
- `csvFile`: UTF-8 CSV with phone + variable columns
- Headers: `x-api-key`, `x-phone-id`, optional `Authorization: Basic`

## Standalone Node Server

```bash
cd whatsapp-bulk-sender
node serve.js        # http://localhost:8080
```

Zero dependencies. Node 18+ (uses global `fetch`).

**Providers:**
1. **AiSensy** — campaign name + project API URL (`backend.api-wa.co/campaign/<project>/api/v2`).
2. **Meta WhatsApp Cloud API** — phone number ID + template name + language.
3. **Custom endpoint** — any POST-JSON API.

## What it does

| Your Python script | This tool |
|---|---|
| `API_KEY` / `CAMPAIGN_NAME` constants | Entered once in the UI, stored in browser localStorage |
| `csv.DictReader` with fixed columns | Any CSV — you map columns to template params visually |
| `templateParams: [achievement, growth]` | Unlimited positional params `{{1}}…{{n}}` via Add parameter |
| `time.sleep(2)` fixed | Configurable delay + batch size + batch pause |
| Console SUCCESS/FAILED tally | Live table with per-recipient HTTP status + response body, exportable to CSV |

## Why a relay at all?

Browsers block cross-origin calls to `backend.api-wa.co` / `graph.facebook.com` unless the provider
sends CORS headers (they don't). Options provide a narrow server-side relay:

- **Phase 1**: `WhatsAppService.cs` calls CEOITBOX directly from the server (no browser CORS)
- **ashx relay**: `WhatsAppSend.ashx` (session-protected, allowlisted hosts)
- **Node**: `serve.js` `POST /api/send` (allowlisted via `WA_ALLOW_HOSTS`)

## CSV format

Header row required. `Phone` is the only mandatory column; any other column can be mapped to a
template parameter. Phone numbers are normalized (preserves `+` country code; bare 10-digit
numbers get `91` prefixed — matching your script's India assumption).

Duplicates are flagged and sent once (first occurrence wins).

## Safety notes

- **Throttling**: tune to your provider's rate limits.
- **Opt-in**: only message recipients who opted in; template sends must use approved templates.
- **Credentials**: Phase 1 reads from `Web.config` appSettings — never exposed in UI or source.

## All Files

**Phase 1 WebForms:**
- `AnmolDristi/WhatsApp_BulkSend.aspx(.cs,.designer.cs)` — upload/preview/map/send UI
- `AnmolDristi/WhatsAppService.cs` — Excel→DataTable→CSV→CEOITBOX API
- `AnmolDristi/WhatsAppModels.cs` — DTOs

**ASP.NET ashx relay:**
- `AnmolDristi/WhatsAppBulk/index.html` — UI hosted inside the ASP.NET app
- `AnmolDristi/WhatsAppBulk/WhatsAppSend.ashx(.cs)` — session-protected relay

**Standalone Node:**
- `whatsapp-bulk-sender/index.html` — full UI (CSV upload, mapping, send, export)
- `whatsapp-bulk-sender/serve.js` — static hosting + allowlisted `/api/send` proxy
- `whatsapp-bulk-sender/package.json` — `npm start` convenience
- `whatsapp-bulk-sender/README.md` — this file
