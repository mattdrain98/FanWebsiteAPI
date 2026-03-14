using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.Forum
{
    public class ForumListingModel
    {
        public int ForumId { get; set; }
        public required string ForumTitle { get; set; }
        public required string Description { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorId { get; set; }
        public int AuthorRating { get; set; }    
        public string? AuthorUrl { get; set; } 
    }
}
