using System.ComponentModel.DataAnnotations;

namespace gamepricer.Dtos.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}