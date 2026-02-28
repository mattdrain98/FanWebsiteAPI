namespace FanWebsiteAPI.DTOs
{
    public class FollowDto
    {
        public required string Id { get; set; }          
        public required string UserName { get; set; }     
        public string? ImagePath { get; set; }  
        public int Rating { get; set; }         
        public required string MemberSince { get; set; }  
    }
}
