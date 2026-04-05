using gamepricer.Dtos.Itad;

namespace gamepricer.Services
{
    public interface IItadApiClient
    {
        Task<IReadOnlyList<ItadSearchItemDto>> SearchGamesAsync(string title, int results, CancellationToken cancellationToken = default);

        Task<ItadSearchItemDto?> LookupGameByTitleAsync(string title, CancellationToken cancellationToken = default);

        /// <summary>
        /// Önce dar lookup, olmazsa arama ile en olası ana oyun kaydını döndürür.
        /// </summary>
        Task<ItadSearchItemDto?> ResolveGameByTitleAsync(string title, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ItadPricesByGameDto>> GetPricesForGamesAsync(
            IReadOnlyList<string> itadGameIds,
            string country,
            CancellationToken cancellationToken = default);

        Task<ItadGameInfoDto?> GetGameInfoAsync(string itadGameId, CancellationToken cancellationToken = default);
    }
}
