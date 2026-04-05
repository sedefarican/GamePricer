namespace gamepricer.Entities
{
    public class GameCategory
    {
        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}