namespace gamepricer.Entities
{
    public class Favorite
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}