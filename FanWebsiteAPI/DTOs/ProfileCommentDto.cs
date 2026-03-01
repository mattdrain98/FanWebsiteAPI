namespace FanWebsiteAPI.DTOs
{
    public class ProfileCommentDto
    {
        public int Id { get; set; }                     
        public required string ProfileUserId { get; set; }           
        public required string ProfileUserName { get; set; }       
        public string? ProfileUserImageUrl { get; set; }  
        public int ProfileUserRating { get; set; }       
        public string? Date { get; set; }              
        public string? CommentContent { get; set; }     
        public required string CommentUserId { get; set; }             
        public string? CommentUserName { get; set; }     
        public string? CommentUserImagePath { get; set; }
        public int? CommentUserRating { get; set; }     
    }
}
