namespace Fan_Website.ViewModel
{
    public class SearchResultDto
    {
        public IEnumerable<PostDto>? Posts { get; set; }
        public string? SearchQuery { get; set; }
        public bool EmptySearchResults { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalPosts { get; set; }
    }
}