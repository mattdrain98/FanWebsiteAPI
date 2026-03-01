using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.ProfileComment
{
    public class ProfileCommentModel
    {
        public int Id { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public int AuthorRating { get; set; }
        public string? AuthorImageUrl { get; set; }
        public DateTime Date { get; set; }
        public required string CommentContent { get; set; }
        public required string UserId { get; set; }
        public required string OtherUserImagePath { get; set; }
        public required string OtherUserName { get; set; }
        public int OtherUserRating { get; set; }
    }
}
