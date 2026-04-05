using System.ComponentModel.DataAnnotations;

namespace gamepricer.Dtos.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}