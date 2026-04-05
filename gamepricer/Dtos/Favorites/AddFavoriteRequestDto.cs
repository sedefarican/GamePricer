using System.ComponentModel.DataAnnotations;

namespace gamepricer.Dtos.Favorites
{
    public class AddFavoriteRequestDto
    {
        [Required]
        public Guid GameId { get; set; }
    }
}