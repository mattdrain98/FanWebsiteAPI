namespace Fan_Website.ViewModel
{
    public class EditProfileDto
    {
        public required string UserId { get; set; }
        public string? UserName { get; set; }
        public string? Bio { get; set; }
    }
}