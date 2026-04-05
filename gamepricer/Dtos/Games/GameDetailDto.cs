namespace gamepricer.Dtos.Games
{
    public class GameDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Developer { get; set; }
        public string? Publisher { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<GamePriceDto> Prices { get; set; } = new();
    }
}