# Sypher BI API — ASP.NET Core 8 + Neon (Postgres)

Donations & calls analytics backend for the **Sypher BI** dashboard. Built on
ASP.NET Core 8 + [Dapper](https://github.com/DapperLib/Dapper) + [Npgsql](https://www.npgsql.org/),
backed by a serverless **Neon** Postgres database. A static, single-file dashboard
(`dashboard.html` / `index.html`) consumes the API and renders the charts.

---

## Architecture

```
┌──────────────────┐      HTTPS/JSON      ┌─────────────────────┐      Npgsql      ┌────────────┐
│  dashboard.html  │  ───────────────────▶│  ASP.NET Core 8 API │ ───────────────▶ │  Neon      │
│  (Chart.js SPA)  │ ◀─────────────────── │  /api/Analytics/*   │ ◀─────────────── │  Postgres  │
└──────────────────┘                      └─────────────────────┘                  └────────────┘
```

- **`Controllers/AnalyticsController.cs`** — REST endpoints under `/api/Analytics`.
- **`Services/SypherBiService.cs`** — all SQL (parameterised Dapper queries).
- **`Services/DbService.cs`** — thin Npgsql connection/query helper.
- **`Services/DataSimulatorService.cs`** — `BackgroundService` that inserts a few
  random donations & calls **every hour** so the dashboard always looks "live".
- **`Models/Models.cs`** — response DTOs (`ApiResponse<T>` envelope).

---

## Setup (3 steps)

### 1. Seed the Neon database
Open your Neon project → **SQL Editor** (or connect with `psql`) and run, **in order**:

1. `sypher_bi_seed_data.sql` — creates the schema (segments, campaigns, channels,
   donors, callers, donations, calls), lookup data, and a base set of rows.
2. `sypher_bi_big_seed.sql` — adds ~450 donations and ~260 calls for a fuller dataset.

### 2. Point the API at your Neon database
The connection string lives in `appsettings.json` under `ConnectionStrings:Neon`:

```json
"ConnectionStrings": {
  "Neon": "Host=<your-endpoint>.neon.tech;Database=neondb;Username=neondb_owner;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
}
```

> In production, override it with the `ConnectionStrings__Neon` environment variable
> instead of committing secrets.

### 3. Run the API
```bash
dotnet run
```
Swagger UI is served at the site root: **http://localhost:5000**.

---

## Endpoints

All routes are prefixed with `/api/Analytics` and return a JSON envelope:
`{ "success": true, "data": …, "error": null, "timestamp": "…" }`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health` | Neon connection check (Postgres version + host) |
| GET | `/overview` | Headline KPIs (raised, donors, calls, pledge conversion) |
| GET | `/donations/summary` | Total raised, gifts, unique donors, avg gift |
| GET | `/donations/monthly` | Revenue & gifts grouped by month |
| GET | `/donations/trend` | One-time vs recurring monthly split |
| GET | `/donations/daily` | Daily revenue, last 30 days |
| GET | `/donations/campaigns` | Raised vs goal per campaign |
| GET | `/donations/channels` | Revenue & share per channel |
| GET | `/donations/list?limit=100` | Recent donations |
| GET | `/donations/export` | Donations as CSV download |
| GET | `/calls/summary` | Calls placed, connect rate, pledge conversion, avg duration |
| GET | `/calls/outcomes` | Answered / voicemail / missed breakdown |
| GET | `/calls/leaderboard` | Caller performance leaderboard |
| GET | `/calls/monthly` | Calls placed & connected per month |
| GET | `/calls/heatmap` | Connect rate by hour of day |
| GET | `/calls/list?limit=100` | Recent calls (with donor-quote notes) |
| GET | `/calls/export` | Calls as CSV download |
| GET | `/donors/segments` | Donors, total raised & avg gift per segment |
| GET | `/donors/lifecycle` | Active / new / lapsed / reactivated counts |
| GET | `/donors/roster` | Per-donor roster with totals |
| GET | `/donors/at-risk` | Lapsed donors ranked by lifetime value |

---

## The dashboard

`dashboard.html` (identical copy: `index.html`) is a self-contained Chart.js app.
It points at the deployed API via the `API` constant near the bottom of the file:

```js
const API = 'https://snowflake-finchbi.onrender.com/api/Analytics';
```

Change that to `http://localhost:5000/api/Analytics` to run it against a local API.

---

## Connectors (preview)

Most organisations don't keep donations in a raw Postgres — the data lives in a
CRM/DMS. Sypher's connector layer pulls that data into the warehouse so the
dashboards light up on real data:

```
[ Bloomerang / Salesforce NPSP / Blackbaud ] ──▶ IDonorSource ──▶ Canonical model ──▶ Neon
```

- **`Connectors/IDonorSource.cs`** — the contract: a read-only source that streams
  donors & donations already normalised to the canonical shape.
- **`Connectors/Canonical.cs`** — `CanonicalDonor` / `CanonicalDonation`, the one
  shape every connector maps into.
- **`Connectors/BloomerangSource.cs`** — first integration (Bloomerang v2 REST).
- **`Connectors/ConnectorSyncService.cs`** — scheduled worker that upserts canonical
  records into Neon (idempotent). Idle and cheap when nothing is enabled.

The layer is **off by default**. Enable Bloomerang by setting:

```bash
Connectors__Bloomerang__Enabled=true
Connectors__Bloomerang__ApiKey=<your-bloomerang-api-key>
```

> The Bloomerang field-mapping (fund→campaign, method→channel, constituent→donor
> name) is stubbed with `TODO`s — wire it to your account's funds/appeals before
> turning it on in production, and add a `UNIQUE source_ref` column for exact
> idempotency on re-sync.

## Deployment

A `Dockerfile` builds and publishes the API (container listens on port **8080**).
The hosted instance runs on Render; the static dashboard can be served from any
static host (e.g. Cloudflare Pages) as `index.html`.

```bash
docker build -t sypherbi-api .
docker run -p 8080:8080 -e ConnectionStrings__Neon="<neon-conn-string>" sypherbi-api
```

---

## File layout

```
SypherBi.Api.csproj
Program.cs                       app bootstrap, DI, CORS, Swagger
appsettings.json                 ConnectionStrings:Neon
Dockerfile                       container build (port 8080)
Controllers/
  AnalyticsController.cs         /api/Analytics endpoints
Models/
  Models.cs                      response DTOs
Services/
  DbService.cs                   Npgsql/Dapper helper
  SypherBiService.cs             all analytics SQL
  DataSimulatorService.cs        hourly "live" data generator
Connectors/                      CRM/DMS sync layer (preview, off by default)
  IDonorSource.cs                connector contract
  Canonical.cs                   canonical donor/donation model
  ConnectorOptions.cs            bound config
  BloomerangSource.cs            Bloomerang v2 integration
  ConnectorSyncService.cs        scheduled upsert worker
sypher_bi_seed_data.sql          ① schema + lookups + base rows  (run first)
sypher_bi_big_seed.sql           ② bulk donations & calls        (run second)
dashboard.html / index.html      static Chart.js dashboard
```
