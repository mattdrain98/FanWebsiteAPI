using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    public required string UserName { get; set; }
    public required string Email { get; set; }

    [MinLength(6)]
    public required string Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }

    public IFormFile? ProfileImage { get; set; }
}