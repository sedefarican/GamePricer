namespace gamepricer.Entities
{
    public class Platform
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? WebsiteUrl { get; set; }

        public ICollection<GamePrice> GamePrices { get; set; } = new List<GamePrice>();
    }
}