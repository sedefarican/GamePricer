namespace gamepricer.Dtos.Itad
{
    public class ItadSearchItemDto
    {
        public string Id { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool Mature { get; set; }
    }

    public class ItadDealOfferDto
    {
        public string ShopName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = null!;
        public int CutPercent { get; set; }
        public string? ProductUrl { get; set; }
    }

    public class GameItadPricesResponseDto
    {
        public Guid GameId { get; set; }
        public string MatchedTitle { get; set; } = null!;
        public string ItadGameId { get; set; } = null!;
        public IReadOnlyList<ItadDealOfferDto> Deals { get; set; } = Array.Empty<ItadDealOfferDto>();
    }

    public class ItadPricesByGameDto
    {
        public string ItadGameId { get; set; } = null!;
        public IReadOnlyList<ItadDealOfferDto> Deals { get; set; } = Array.Empty<ItadDealOfferDto>();
    }

    public class ItadGameInfoDto
    {
        public string? BoxartUrl { get; set; }
    }
}
