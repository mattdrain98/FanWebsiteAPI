using System.ComponentModel.DataAnnotations;

namespace Fan_Website.ViewModel
{
    public class EditProfileDto
    {
        public required string UserName { get; set; }
        public required string ImagePath { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
