using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using gamepricer.Configuration;
using gamepricer.Dtos.Itad;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace gamepricer.Services
{
    public class ItadApiClient : IItadApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http;
        private readonly ItadOptions _options;

        public ItadApiClient(HttpClient http, IOptions<ItadOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<ItadSearchItemDto>> SearchGamesAsync(string title, int results, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            results = Math.Clamp(results, 1, 100);

            var url = QueryHelpers.AddQueryString(
                Combine("games/search/v1"),
                new Dictionary<string, string?>
                {
                    ["key"] = _options.ApiKey,
                    ["title"] = title,
                    ["results"] = results.ToString()
                });

            var list = await _http.GetFromJsonAsync<List<ItadSearchItemDto>>(url, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (list == null)
                return new List<ItadSearchItemDto>();
            return list;
        }

        public async Task<ItadSearchItemDto?> LookupGameByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var url = QueryHelpers.AddQueryString(
                Combine("games/lookup/v1"),
                new Dictionary<string, string?>
                {
                    ["key"] = _options.ApiKey,
                    ["title"] = title
                });

            var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("found", out var foundEl) || !foundEl.GetBoolean())
                return null;

            if (!doc.RootElement.TryGetProperty("game", out var gameEl))
                return null;

            return JsonSerializer.Deserialize<ItadSearchItemDto>(gameEl.GetRawText(), JsonOptions);
        }

        public async Task<ItadSearchItemDto?> ResolveGameByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            var t = title.Trim();

            var fromLookup = await LookupGameByTitleAsync(t, cancellationToken).ConfigureAwait(false);
            if (fromLookup != null)
                return fromLookup;

            var search = await SearchGamesAsync(t, 30, cancellationToken).ConfigureAwait(false);
            if (search.Count == 0)
                return null;

            static bool IsGame(string? ty) =>
                string.Equals(ty, "game", StringComparison.OrdinalIgnoreCase);

            var exactGame = search.FirstOrDefault(x =>
                IsGame(x.Type) && string.Equals(x.Title, t, StringComparison.OrdinalIgnoreCase));
            if (exactGame != null)
                return exactGame;

            var exactAny = search.FirstOrDefault(x =>
                string.Equals(x.Title, t, StringComparison.OrdinalIgnoreCase));
            if (exactAny != null)
                return exactAny;

            foreach (var x in search)
            {
                if (!IsGame(x.Type))
                    continue;
                if (!x.Title.StartsWith(t, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (x.Title.Length == t.Length)
                    return x;
                var next = x.Title[t.Length];
                if (char.IsWhiteSpace(next) || next is ':' or '-' or '–' or '—' or '&' or '(')
                    return x;
            }

            var firstGame = search.FirstOrDefault(x => IsGame(x.Type));
            if (firstGame != null)
                return firstGame;

            return search[0];
        }

        public async Task<IReadOnlyList<ItadPricesByGameDto>> GetPricesForGamesAsync(
            IReadOnlyList<string> itadGameIds,
            string country,
            CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            if (itadGameIds.Count == 0)
                return Array.Empty<ItadPricesByGameDto>();

            if (itadGameIds.Count > 200)
                throw new ArgumentException("En fazla 200 oyun kimliği gönderilebilir.", nameof(itadGameIds));

            var url = QueryHelpers.AddQueryString(
                Combine("games/prices/v3"),
                new Dictionary<string, string?>
                {
                    ["key"] = _options.ApiKey,
                    ["country"] = string.IsNullOrWhiteSpace(country) ? _options.DefaultCountry : country
                });

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(itadGameIds)
            };

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return Array.Empty<ItadPricesByGameDto>();

            var byGame = new List<ItadPricesByGameDto>();
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var id = entry.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                    ? idEl.GetString() ?? ""
                    : "";

                var offers = new List<ItadDealOfferDto>();
                if (entry.TryGetProperty("deals", out var dealsEl) && dealsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var deal in dealsEl.EnumerateArray())
                        offers.Add(ParseDeal(deal));
                }

                byGame.Add(new ItadPricesByGameDto { ItadGameId = id, Deals = offers });
            }

            return byGame;
        }

        public async Task<ItadGameInfoDto?> GetGameInfoAsync(string itadGameId, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var url = QueryHelpers.AddQueryString(
                Combine("games/info/v2"),
                new Dictionary<string, string?>
                {
                    ["key"] = _options.ApiKey,
                    ["id"] = itadGameId
                });

            var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = doc.RootElement;

            string? boxart = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Object &&
                assets.TryGetProperty("boxart", out var box) && box.ValueKind == JsonValueKind.String)
            {
                boxart = box.GetString();
            }

            return string.IsNullOrWhiteSpace(boxart) ? new ItadGameInfoDto() : new ItadGameInfoDto { BoxartUrl = boxart };
        }

        private static ItadDealOfferDto ParseDeal(JsonElement deal)
        {
            var shopName = deal.TryGetProperty("shop", out var shop) && shop.TryGetProperty("name", out var nameEl)
                ? nameEl.GetString() ?? ""
                : "";

            decimal amount = 0;
            var currency = "";
            if (deal.TryGetProperty("price", out var priceObj) && priceObj.ValueKind == JsonValueKind.Object)
            {
                if (priceObj.TryGetProperty("amount", out var amt))
                    amount = amt.GetDecimal();
                if (priceObj.TryGetProperty("currency", out var cur))
                    currency = cur.GetString() ?? "";
            }

            var cut = deal.TryGetProperty("cut", out var cutEl) ? cutEl.GetInt32() : 0;
            var link = deal.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;

            return new ItadDealOfferDto
            {
                ShopName = shopName,
                Price = amount,
                Currency = currency,
                CutPercent = cut,
                ProductUrl = link
            };
        }

        private string Combine(string relativePath)
        {
            var baseUri = _options.BaseUrl.TrimEnd('/');
            return $"{baseUri}/{relativePath.TrimStart('/')}";
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("ITAD:ApiKey yapılandırması eksik.");
        }
    }
}
