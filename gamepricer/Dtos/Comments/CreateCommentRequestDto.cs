using System.ComponentModel.DataAnnotations;

namespace gamepricer.Dtos.Comments
{
    public class CreateCommentRequestDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(1000)]
        public string Content { get; set; } = null!;
    }
}