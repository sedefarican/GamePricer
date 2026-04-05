using System.ComponentModel.DataAnnotations;

namespace gamepricer.Dtos.Users
{
    public class UpdateUserRequestDto
    {
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Username { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }
    }
}