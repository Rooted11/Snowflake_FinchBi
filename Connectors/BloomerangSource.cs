using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SypherBi.Api.Connectors;

/// <summary>
/// Read-only Bloomerang connector. Pages through constituents and transactions
/// via the Bloomerang v2 REST API and normalises them to the canonical model.
/// Inert until <c>Connectors:Bloomerang:Enabled=true</c> and an API key are set.
/// </summary>
public class BloomerangSource : IDonorSource
{
    private readonly HttpClient _http;
    private readonly BloomerangOptions _opt;
    private readonly ILogger<BloomerangSource> _logger;

    public BloomerangSource(HttpClient http, IOptions<ConnectorOptions> opt, ILogger<BloomerangSource> logger)
    {
        _opt    = opt.Value.Bloomerang;
        _logger = logger;
        _http   = http;
        _http.BaseAddress ??= new Uri(_opt.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-API-KEY", _opt.ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string Name    => "Bloomerang";
    public bool   Enabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.ApiKey);

    public async IAsyncEnumerable<CanonicalDonor> FetchDonorsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (!Enabled) yield break;

        await foreach (var el in PageAsync("constituents", ct))
        {
            var name = Str(el, "FullName") ?? $"{Str(el, "FirstName")} {Str(el, "LastName")}".Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            // TODO: derive segment/lifecycle from Bloomerang giving summary + status.
            yield return new CanonicalDonor(
                Name:      name,
                SegmentId: "grass",
                Lifecycle: "active",
                FirstGift: "Jan 2024",
                MaxGift:   0);
        }
    }

    public async IAsyncEnumerable<CanonicalDonation> FetchDonationsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (!Enabled) yield break;

        await foreach (var el in PageAsync("transactions", ct))
        {
            var id   = Str(el, "Id");
            var name = Str(el, "ConstituentName") ?? "Unknown";   // TODO: resolve via AccountId lookup
            if (id is null || !el.TryGetProperty("Amount", out var amtEl)) continue;

            yield return new CanonicalDonation(
                SourceRef:  $"bloomerang:{id}",
                GiftDate:   Date(el, "Date") ?? DateTime.UtcNow.Date,
                DonorName:  name,
                CampaignId: "spring",   // TODO: map Bloomerang fund/campaign → campaigns(id)
                ChannelId:  "online",   // TODO: map Bloomerang method → channels(id)
                Amount:     amtEl.ValueKind == JsonValueKind.Number ? amtEl.GetDecimal() : 0m,
                Status:     "completed");
        }
    }

    /// <summary>Generic skip/take pager over a Bloomerang collection endpoint.</summary>
    private async IAsyncEnumerable<JsonElement> PageAsync(string path, [EnumeratorCancellation] CancellationToken ct)
    {
        const int take = 50;
        int skip = 0;

        while (!ct.IsCancellationRequested)
        {
            using var resp = await _http.GetAsync($"{path}?skip={skip}&take={take}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bloomerang {Path} returned {Status}", path, (int)resp.StatusCode);
                yield break;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("Results", out var results) ||
                results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                yield break;

            foreach (var item in results.EnumerateArray())
                yield return item.Clone();

            if (results.GetArrayLength() < take) yield break;   // last page
            skip += take;
        }
    }

    private static string? Str(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static DateTime? Date(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
           && DateTime.TryParse(v.GetString(), out var d) ? d : null;
}
