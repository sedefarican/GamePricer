namespace gamepricer.Entities
{
    public class GamePrice
    {
        public Guid Id { get; set; }

        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;

        public Guid PlatformId { get; set; }
        public Platform Platform { get; set; } = null!;

        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public decimal? DiscountRate { get; set; }
        public string? ProductUrl { get; set; }
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
        public bool IsAvailable { get; set; } = true;
    }
}