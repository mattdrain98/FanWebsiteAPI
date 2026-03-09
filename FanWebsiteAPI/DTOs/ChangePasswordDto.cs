using System.ComponentModel.DataAnnotations;

namespace Fan_Website.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        public required string Password { get; set; }

        [Required]
        public required string NewPassword { get; set; }
    }
}
