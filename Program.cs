using SypherBi.Api.Services;
using SypherBi.Api.Connectors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Sypher BI API",
        Version     = "v1",
        Description = "ASP.NET Core 8 + Neon (Postgres) — Donations & Calls analytics backend"
    });
});

builder.Services.AddSingleton<DbService>();
builder.Services.AddScoped<SypherBiService>();
builder.Services.AddHostedService<DataSimulatorService>();
builder.Services.AddMemoryCache();

// ── Connectors (preview) ─────────────────────────────────────────────────────
// Read-only CRM/DMS sources that sync into Neon. Inert unless enabled in config.
builder.Services.Configure<ConnectorOptions>(
    builder.Configuration.GetSection(ConnectorOptions.SectionName));
builder.Services.AddHttpClient<IDonorSource, BloomerangSource>();
builder.Services.AddHostedService<ConnectorSyncService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sypher BI API v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthorization();
app.MapControllers();
app.Run();
