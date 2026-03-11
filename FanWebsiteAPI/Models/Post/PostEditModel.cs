using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.Post
{
    public class PostEditModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Content { get; set; }
        public required string ForumName { get; set; }
        public int ForumId { get; set; }
        public DateTime Created { get; set; }
    }
}
