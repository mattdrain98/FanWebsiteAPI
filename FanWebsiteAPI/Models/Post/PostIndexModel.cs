using Fan_Website.Models.Reply;
using FanWebsiteAPI.DTOs;
using System;
using System.Collections.Generic;


namespace Fan_Website.Models.Post
{
    public class PostIndexModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }

        public required string AuthorName { get; set; }
        public required string AuthorId { get; set; }
        public int AuthorRating { get; set; }
        public string? AuthorImageUrl { get; set; }

        public DateTime Date { get; set; }
        public string? PostContent { get; set; }

        public IEnumerable<PostReplyModel>? Replies { get; set; }

        public int TotalLikes { get; set; }

        public IEnumerable<LikeDto>? Likes { get; set; }

        public int ForumId { get; set; }
        public required string ForumName { get; set; }

        public bool UserHasLiked { get; set; }
    }
}
