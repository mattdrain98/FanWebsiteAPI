using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.Profile
{
    public class ProfileEditModel
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public IFormFile? ImageUpload { get; set; }
        public string? Bio { get; set; }
    }
}
