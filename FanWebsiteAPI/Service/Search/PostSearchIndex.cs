using Fan_Website.Infrastructure;
using System.Text.RegularExpressions;

namespace Fan_Website.Service.Search
{
    public class PostSearchIndex : IPostSearchIndex, IDisposable
    {
        private const int MinTokenLength = 2;

        private static readonly Regex ImagePlaceholderPattern =
            new(@"\[Image-\d+-\d+\]", RegexOptions.Compiled);

        private static readonly Regex TokenPattern =
            new(@"\w+", RegexOptions.Compiled);

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        // term -> (postId -> term frequency in that post)
        private readonly Dictionary<string, Dictionary<int, int>> _index = new();

        // postId -> total token count, for normalized term frequency
        private readonly Dictionary<int, int> _documentLengths = new();

        // postId -> the distinct terms it contributed, so Remove/Update can clean
        // up _index without re-tokenizing whatever the post's old content was.
        private readonly Dictionary<int, HashSet<string>> _termsByPostId = new();

        // Computed, not a separately-tracked counter — a counter can drift out of
        // sync if any path double-adds or under-removes; this can't.
        private int TotalDocuments => _termsByPostId.Count;

        public void Add(Post post)
        {
            _lock.EnterWriteLock();
            try
            {
                AddNoLock(post);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Remove(int postId)
        {
            _lock.EnterWriteLock();
            try
            {
                RemoveNoLock(postId);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Update(Post post)
        {
            // One critical section for the remove+re-add — otherwise a concurrent
            // Search (read lock) could momentarily see the post as absent entirely.
            _lock.EnterWriteLock();
            try
            {
                RemoveNoLock(post.PostId);
                AddNoLock(post);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Load(IEnumerable<Post> posts)
        {
            _lock.EnterWriteLock();
            try
            {
                _index.Clear();
                _documentLengths.Clear();
                _termsByPostId.Clear();

                foreach (var post in posts)
                    AddNoLock(post);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IReadOnlyList<int> Search(string query)
        {
            _lock.EnterReadLock();
            try
            {
                var queryTerms = Tokenize(query).Distinct().ToList();
                if (queryTerms.Count == 0 || TotalDocuments == 0)
                    return Array.Empty<int>();

                var scores = new Dictionary<int, double>();

                foreach (var term in queryTerms)
                {
                    if (!_index.TryGetValue(term, out var postings) || postings.Count == 0)
                        continue;

                    // Smoothed IDF — the naive log(N/df) goes negative once a term
                    // appears in more than ~37% of documents (and always for a term
                    // present in every document), which would penalize a genuine
                    // match. This variant stays >= 1 for a term present everywhere,
                    // while still ranking rarer terms higher.
                    var idf = Math.Log((double)(TotalDocuments + 1) / (postings.Count + 1)) + 1.0;

                    foreach (var (postId, termFrequency) in postings)
                    {
                        var documentLength = _documentLengths[postId];
                        var tf = (double)termFrequency / documentLength;

                        scores.TryGetValue(postId, out var existing);
                        scores[postId] = existing + tf * idf;
                    }
                }

                return scores
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void AddNoLock(Post post)
        {
            // Idempotent: safe to call twice for the same post without double
            // counting, and lets Load reuse this directly for a full rebuild.
            RemoveNoLock(post.PostId);

            var text = ImagePlaceholderPattern.Replace($"{post.Title} {post.Content}", " ");
            var terms = Tokenize(text).ToList();

            var termSet = new HashSet<string>();
            foreach (var term in terms)
            {
                if (!_index.TryGetValue(term, out var postings))
                {
                    postings = new Dictionary<int, int>();
                    _index[term] = postings;
                }

                postings.TryGetValue(post.PostId, out var count);
                postings[post.PostId] = count + 1;

                termSet.Add(term);
            }

            // Registered unconditionally, even for a post with no indexable terms —
            // otherwise TotalDocuments undercounts and every other term's IDF is
            // subtly wrong. A doc with 0 terms simply never matches any search.
            _termsByPostId[post.PostId] = termSet;
            _documentLengths[post.PostId] = terms.Count;
        }

        private void RemoveNoLock(int postId)
        {
            if (!_termsByPostId.TryGetValue(postId, out var terms))
                return;

            foreach (var term in terms)
            {
                if (!_index.TryGetValue(term, out var postings))
                    continue;

                postings.Remove(postId);
                if (postings.Count == 0)
                    _index.Remove(term);
            }

            _termsByPostId.Remove(postId);
            _documentLengths.Remove(postId);
        }

        private static IEnumerable<string> Tokenize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            foreach (Match match in TokenPattern.Matches(text.ToLowerInvariant()))
            {
                if (match.Value.Length >= MinTokenLength)
                    yield return match.Value;
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
