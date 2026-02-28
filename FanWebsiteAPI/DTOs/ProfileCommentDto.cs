namespace FanWebsiteAPI.DTOs
{
    public class ProfileCommentDto
    {
        public int Id { get; set; }                     
        public required string AuthorId { get; set; }           
        public required string AuthorName { get; set; }       
        public string? AuthorImageUrl { get; set; }  
        public int AuthorRating { get; set; }       
        public string? Date { get; set; }              
        public required string CommentContent { get; set; }     
        public required string UserId { get; set; }             
        public string? OtherUserName { get; set; }     
        public string? OtherUserImagePath { get; set; }
        public int? OtherUserRating { get; set; }     
    }
}
