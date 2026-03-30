using System.ComponentModel.DataAnnotations;

namespace FanWebsiteAPI.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        public required string Password { get; set; }

        [Required]
        public required string NewPassword { get; set; }
    }
}
