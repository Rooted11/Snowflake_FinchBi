# Sypher BI — Donations & Calls Analytics

Fundraising business-intelligence for nonprofits: a donor + call-center analytics
**API** (ASP.NET Core 8 + [Dapper](https://github.com/DapperLib/Dapper)) on a
**pluggable warehouse — Neon Postgres (default) or Snowflake** — plus a self-contained
**dashboard** (`dashboard.html` / `index.html`, Chart.js) that renders it.

```
git clone https://github.com/Rooted11/sypher-bi-backend.git
```

---

## Architecture

```
┌──────────────────┐    HTTPS/JSON    ┌─────────────────────┐    Npgsql    ┌──────────┐
│  dashboard.html  │ ───────────────▶ │  ASP.NET Core 8 API │ ───────────▶ │  Neon    │
│  (Chart.js SPA)  │ ◀─────────────── │  /api/Analytics/*   │ ◀─────────── │ Postgres │
└──────────────────┘                  └─────────────────────┘              └──────────┘
                                                 ▲
                              ┌──────────────────┴───────────────────┐
                              │  Connectors (preview, off by default) │
                              │  Bloomerang / Salesforce / Blackbaud  │
                              └───────────────────────────────────────┘
```

- **`Controllers/AnalyticsController.cs`** — REST endpoints under `/api/Analytics`.
- **`Services/SypherBiService.cs`** — all analytics SQL (parameterised Dapper queries).
- **`Services/DbService.cs`** — Npgsql/Dapper connection helper.
- **`Services/DataSimulatorService.cs`** — background worker that inserts a few random
  donations & calls each hour so the dashboard always looks "live".
- **`Connectors/`** — read-only CRM/DMS sync layer (see below).
- **`Models/Models.cs`** — response DTOs, wrapped in `ApiResponse<T>`.

---

## Quick start

### 1. Seed Neon
In your Neon project's **SQL Editor** (or via `psql`), run **in order**:

1. `sypher_bi_seed_data.sql` — schema (segments, campaigns, channels, donors, callers,
   donations, calls), lookups, and a base set of rows.
2. `sypher_bi_big_seed.sql` — ~450 more donations and ~260 more calls.

### 2. Configure the connection (see [Configuration & secrets](#configuration--secrets))
Locally, create **`appsettings.Development.json`** (git-ignored):

```json
{
  "ConnectionStrings": {
    "Neon": "Host=<your-endpoint>.neon.tech;Database=neondb;Username=neondb_owner;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### 3. Run
```bash
dotnet run
```
Swagger UI is served at the site root: **http://localhost:5000**.

---

## Configuration & secrets

Connection strings are resolved from `ConnectionStrings:*`, sourced (in order) from
environment, then `appsettings.{Environment}.json`, then `appsettings.json`.

| Where | Use | Committed? |
|-------|-----|-----------|
| `ConnectionStrings__Neon` / `ConnectionStrings__Snowflake` env vars | **Production / Render** | no |
| `appsettings.Development.json` | **Local dev** | no — git-ignored |
| `appsettings.json` | non-secret placeholders + `Database:Provider` | yes |

> **Never put credentials in `appsettings.json`** — it's committed. The tracked file holds
> placeholders; the real values come from env vars or the git-ignored dev file. If a
> credential is ever committed, **rotate it** (removing it from history does not un-leak it).

---

## Warehouse providers (Neon / Snowflake)

The API is warehouse-pluggable. `Database:Provider` selects the backend at startup:

```json
"Database": { "Provider": "Neon" }      // or "Snowflake"
```

How it's wired (all in `Services/`):

- **`IDbConnectionFactory`** → `PostgresConnectionFactory` / `SnowflakeConnectionFactory`
  open connections to the chosen warehouse (Npgsql vs `Snowflake.Data`).
- **`IAnalyticsSql`** → `PostgresAnalyticsSql` / `SnowflakeAnalyticsSql` hold the
  dialect-specific queries (`FILTER` → `CASE`, `::text` → `::varchar`, date math, etc.).
- `SypherBiService` is dialect-agnostic — same endpoints, swappable warehouse.
- `/health` reports the active `provider`, and the dashboard's **data-source badge**
  (`assets/sypher-source.js`) shows it live with round-trip latency.

### Enabling Snowflake
1. Create the schema: run **`snowflake/schema.sql`** in a Snowflake worksheet.
2. Load data from Neon: **`python tools/neon_to_snowflake.py`** (see `tools/requirements.txt`
   and the env vars documented at the top of the script).
3. Set `Database:Provider=Snowflake` and provide `ConnectionStrings__Snowflake`.

> The Snowflake SQL dialect is a best-effort starting point — validate it against a live
> account (esp. `LIMIT` parameter binding) before relying on it in production.

## Live data & 24-hour refresh

`DataSimulatorService` keeps the dashboard live (Neon only):

- **Hourly** incremental top-ups (a few donations + calls).
- A **fresh full rebuild every 24 h** (and once at startup) that purges **only the
  simulated layer** — rows dated on/after `Simulator:LiveSinceUtc` — and lays down a new
  current day. Seed history (before that date) is never touched, so the dataset stays
  fresh and bounded. Turn it off with `Simulator:Enabled=false`.

---

## Endpoints

All routes are prefixed `/api/Analytics` and return
`{ "success": true, "data": …, "error": null, "timestamp": "…" }`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Neon connection check (Postgres version + host) |
| GET | `/overview` | Headline KPIs (raised, donors, calls, pledge conversion) |
| GET | `/donations/summary` | Total raised, gifts, unique donors, avg gift |
| GET | `/donations/monthly` | Revenue & gifts by month |
| GET | `/donations/trend` | One-time vs recurring monthly split |
| GET | `/donations/daily` | Daily revenue, last 30 days |
| GET | `/donations/campaigns` | Raised vs goal per campaign |
| GET | `/donations/channels` | Revenue & share per channel |
| GET | `/donations/list?limit=100` | Recent donations (incl. donor segment) |
| GET | `/donations/export` | Donations as CSV |
| GET | `/calls/summary` | Calls placed, connect rate, pledge conversion, avg duration |
| GET | `/calls/outcomes` | Answered / voicemail / missed breakdown |
| GET | `/calls/leaderboard` | Caller performance leaderboard |
| GET | `/calls/monthly` | Calls placed & connected per month |
| GET | `/calls/heatmap` | Connect rate by hour of day |
| GET | `/calls/list?limit=100` | Recent calls (with donor-quote notes) |
| GET | `/calls/export` | Calls as CSV |
| GET | `/donors/segments` | Donors, total raised & avg gift per segment |
| GET | `/donors/lifecycle` | Active / new / lapsed / reactivated counts |
| GET | `/donors/roster` | Per-donor roster with totals |
| GET | `/donors/at-risk` | Lapsed donors ranked by lifetime value |

---

## The dashboard

`dashboard.html` (identical copy: `index.html`) is a self-contained Chart.js app. It points
at the deployed API via the `API` constant near the bottom of the file:

```js
const API = 'https://<your-service>.onrender.com/api/Analytics';
```

Point it at `http://localhost:5000/api/Analytics` to run against a local API. The donor
**segment / campaign / channel** filters drive every KPI, chart, and table.

---

## Connectors (preview)

Most organisations don't keep donations in a raw Postgres — the data lives in a CRM/DMS.
The connector layer pulls that data into the warehouse so the dashboards light up on real data:

```
[ Bloomerang / Salesforce NPSP / Blackbaud ] ──▶ IDonorSource ──▶ Canonical model ──▶ Neon
```

- **`Connectors/IDonorSource.cs`** — contract: a read-only source streaming donors &
  donations already normalised to the canonical shape.
- **`Connectors/Canonical.cs`** — `CanonicalDonor` / `CanonicalDonation`, the one shape
  every connector maps into.
- **`Connectors/BloomerangSource.cs`** — first integration (Bloomerang v2 REST).
- **`Connectors/ConnectorSyncService.cs`** — scheduled worker that upserts canonical
  records into Neon (idempotent). Idle and cheap when nothing is enabled.

**Off by default.** Enable Bloomerang with:

```bash
Connectors__Bloomerang__Enabled=true
Connectors__Bloomerang__ApiKey=<your-bloomerang-api-key>
```

> The Bloomerang field-mapping (fund→campaign, method→channel, constituent→donor name) is
> stubbed with `TODO`s — wire it to your account before production, and add a
> `UNIQUE source_ref` column for exact idempotency on re-sync.

---

## Deployment

A `Dockerfile` builds and publishes the API (container listens on port **8080**).

```bash
docker build -t sypherbi-api .
docker run -p 8080:8080 -e ConnectionStrings__Neon="<neon-conn-string>" sypherbi-api
```

On Render: set `ConnectionStrings__Neon` (and any `Connectors__*` keys) as **environment
variables**, then deploy. The static dashboard can be served from any static host
(e.g. Cloudflare Pages) as `index.html`.

---

## File layout

```
SypherBi.Api.csproj
Program.cs                       app bootstrap, DI, CORS, Swagger
appsettings.json                 non-secret config + placeholder connection string
Dockerfile                       container build (port 8080)
Controllers/
  AnalyticsController.cs         /api/Analytics endpoints
Models/
  Models.cs                      response DTOs
Services/
  DbService.cs                   provider-agnostic Dapper helper
  IDbConnectionFactory.cs        + PostgresConnectionFactory / SnowflakeConnectionFactory
  IAnalyticsSql.cs               + PostgresAnalyticsSql / SnowflakeAnalyticsSql (dialects)
  SypherBiService.cs             dialect-agnostic analytics read API
  DataSimulatorService.cs        hourly top-ups + 24h fresh rebuild (Neon only)
Connectors/                      CRM/DMS sync layer (preview, off by default)
  IDonorSource.cs                connector contract
  Canonical.cs                   canonical donor/donation model
  ConnectorOptions.cs            bound config
  BloomerangSource.cs            Bloomerang v2 integration
  ConnectorSyncService.cs        scheduled upsert worker
snowflake/
  schema.sql                     Snowflake DDL
tools/
  neon_to_snowflake.py           Neon → Snowflake ETL loader (Python)
  requirements.txt               loader dependencies
assets/
  sypher-source.js               dashboard data-source / health badge (JS)
sypher_bi_seed_data.sql          ① schema + lookups + base rows  (run first)
sypher_bi_big_seed.sql           ② bulk donations & calls        (run second)
dashboard.html / index.html      static Chart.js dashboard
```
