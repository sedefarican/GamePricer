namespace gamepricer.Dtos.Favorites
{
    public class FavoriteItemDto
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
        public decimal? LowestPrice { get; set; }
        public DateTime AddedAt { get; set; }
    }
}