namespace Fan_Website.Infrastructure
{
    public interface IPostSearchIndex
    {
        void Add(Post post);
        void Update(Post post);
        void Remove(int postId);

        IReadOnlyList<int> Search(string query);

        // Full rebuild, used once at startup. Replaces the entire index contents.
        void Load(IEnumerable<Post> posts);
    }
}
