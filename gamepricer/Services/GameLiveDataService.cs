using gamepricer.Configuration;
using gamepricer.Dtos.Games;
using gamepricer.Dtos.Itad;
using Microsoft.Extensions.Options;

namespace gamepricer.Services
{
    public class GameLiveDataService
    {
        private readonly IItadApiClient _itad;
        private readonly ItadOptions _options;

        public GameLiveDataService(IItadApiClient itad, IOptions<ItadOptions> options)
        {
            _itad = itad;
            _options = options.Value;
        }

        public async Task EnrichListItemsAsync(
            IList<GameListItemDto> games,
            string? country,
            CancellationToken cancellationToken = default)
        {
            if (games.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                return;

            var cc = string.IsNullOrWhiteSpace(country) ? _options.DefaultCountry : country;

            var sem = new SemaphoreSlim(5, 5);
            var lookupTasks = games.Select(async g =>
            {
                await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var hit = await _itad.ResolveGameByTitleAsync(g.Name, cancellationToken).ConfigureAwait(false);
                    return (Game: g, Hit: hit);
                }
                finally
                {
                    sem.Release();
                }
            });

            var lookupRows = await Task.WhenAll(lookupTasks).ConfigureAwait(false);

            var itadByLocalId = new Dictionary<Guid, string>();
            foreach (var (game, hit) in lookupRows)
            {
                if (hit != null && !string.IsNullOrWhiteSpace(hit.Id))
                    itadByLocalId[game.Id] = hit.Id;
            }

            if (itadByLocalId.Count == 0)
                return;

            var distinctItadIds = itadByLocalId.Values.Distinct(StringComparer.Ordinal).ToList();

            IReadOnlyList<ItadPricesByGameDto> priceBlocks;
            try
            {
                priceBlocks = await _itad.GetPricesForGamesAsync(distinctItadIds, cc, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                priceBlocks = Array.Empty<ItadPricesByGameDto>();
            }

            var priceDict = priceBlocks
                .Where(b => !string.IsNullOrWhiteSpace(b.ItadGameId))
                .ToDictionary(b => b.ItadGameId, b => b.Deals, StringComparer.Ordinal);

            var infoSem = new SemaphoreSlim(5, 5);
            var infoTasks = distinctItadIds.Select(async itadId =>
            {
                await infoSem.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var info = await _itad.GetGameInfoAsync(itadId, cancellationToken).ConfigureAwait(false);
                    return (itadId, info);
                }
                finally
                {
                    infoSem.Release();
                }
            });
            var infoRows = await Task.WhenAll(infoTasks).ConfigureAwait(false);
            var infoDict = infoRows
                .Where(x => x.info != null && !string.IsNullOrWhiteSpace(x.info.BoxartUrl))
                .ToDictionary(x => x.itadId, x => x.info!.BoxartUrl!, StringComparer.Ordinal);

            foreach (var g in games)
            {
                if (!itadByLocalId.TryGetValue(g.Id, out var itadId))
                    continue;

                if (infoDict.TryGetValue(itadId, out var art))
                    g.CoverImageUrl = art;

                if (!priceDict.TryGetValue(itadId, out var deals) || deals.Count == 0)
                    continue;

                var best = deals.OrderBy(d => d.Price).First();
                g.LowestPrice = best.Price;
                g.PriceCurrency = string.IsNullOrWhiteSpace(best.Currency) ? cc : best.Currency;
                g.BestDiscountPercent = deals.Max(d => d.CutPercent);
                if (g.BestDiscountPercent <= 0)
                    g.BestDiscountPercent = null;
            }
        }

        public async Task EnrichDetailCoverAsync(GameDetailDto game, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                return;

            ItadSearchItemDto? hit;
            try
            {
                hit = await _itad.ResolveGameByTitleAsync(game.Name, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (hit == null)
                return;

            ItadGameInfoDto? info;
            try
            {
                info = await _itad.GetGameInfoAsync(hit.Id, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (info != null && !string.IsNullOrWhiteSpace(info.BoxartUrl))
                game.CoverImageUrl = info.BoxartUrl;
        }
    }
}
