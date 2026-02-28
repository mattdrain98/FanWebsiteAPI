using Fan_Website.Models.Reply;
using FanWebsiteAPI.DTOs;
using System;
using System.Collections.Generic;


namespace Fan_Website.Models.Post
{
    public class PostIndexModel
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string AuthorName { get; set; }
        public string AuthorId { get; set; }
        public int AuthorRating { get; set; }
        public string AuthorImageUrl { get; set; }

        public DateTime Date { get; set; }
        public string PostContent { get; set; }

        public IEnumerable<PostReplyModel> Replies { get; set; }

        public int TotalLikes { get; set; }

        public IEnumerable<LikeDto> Likes { get; set; }

        public int ForumId { get; set; }
        public string ForumName { get; set; }

        public bool UserHasLiked { get; set; }
    }
}
