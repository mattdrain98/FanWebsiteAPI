using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.ProfileComment
{
    public class ProfileCommentModel
    {
        public int Id { get; set; }
        public required string ProfileUserId { get; set; }
        public required string ProfileUserName { get; set; }
        public int ProfileUserRating { get; set; }
        public string? ProfileUserImageUrl { get; set; }
        public DateTime Date { get; set; }
        public required string CommentContent { get; set; }
        public required string CommentUserId { get; set; }
        public required string CommentUserImagePath { get; set; }
        public required string CommentUserName { get; set; }
        public int CommentUserRating { get; set; }
    }
}
