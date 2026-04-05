namespace gamepricer.Dtos.Comments
{
    public class CommentItemDto
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}