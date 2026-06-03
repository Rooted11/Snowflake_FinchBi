# FinchBi Sales API — ASP.NET Core 8 + Supabase

## Setup (3 steps)

### 1. Run the SQL in Supabase
- Go to your Supabase project → **SQL Editor** → **New query**
- Paste the contents of `supabase_setup.sql` and click **Run**
- This creates all tables and seeds sample data

### 2. Run the API
```bash
dotnet run
```
Swagger opens at `http://localhost:5000`

### 3. Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| GET | `/api/analytics/health` | Postgres connection check |
| GET | `/api/analytics/revenue/summary?year=2024` | Revenue KPIs |
| GET | `/api/analytics/revenue/monthly?year=2024` | Monthly breakdown |
| GET | `/api/analytics/products/top?n=10` | Top products |
| GET | `/api/analytics/customers/segments` | RFM segmentation |
| GET | `/api/analytics/regions` | Revenue by region |
| GET | `/api/analytics/inventory/low-stock?threshold=50` | Low stock alert |

## File layout
```
FinchBi.Api.csproj
Program.cs
appsettings.json
Controllers/AnalyticsController.cs
Models/Models.cs
Services/
  DbService.cs
  SalesAnalyticsService.cs
supabase_setup.sql   ← run this in Supabase SQL Editor first
```
