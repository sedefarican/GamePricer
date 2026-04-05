namespace gamepricer.Dtos.Games
{
    public class GamePriceDto
    {
        public string PlatformName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public decimal? DiscountRate { get; set; }
        public string? ProductUrl { get; set; }
    }
}