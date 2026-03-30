using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fan_Website.Tests
{
    public class PostServiceTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private (AppDbContext ctx, PostService svc) Build(string dbName)
        {
            var ctx = CreateContext(dbName);
            return (ctx, new PostService(ctx));
        }

        private static ApplicationUser MakeUser(string id = "user-1", string name = "Alice") =>
            new ApplicationUser { Id = id, UserName = name, Rating = 10 };

        private static Forum MakeForum(ApplicationUser owner, int id = 1) =>
            new Forum
            {
                ForumId = id,
                PostTitle = "General",
                Description = "General discussion",
                User = owner,
                Posts = new List<Post>()
            };

        private static Post MakePost(ApplicationUser author, Forum forum, int id = 1) =>
            new Post
            {
                PostId = id,
                Title = "Hello World",
                Content = "Some content",
                CreatedOn = DateTime.UtcNow,
                User = author,
                Forum = forum,
                Replies = new List<PostReply>(),
                Likes = new List<Like>()
            };

        public void Dispose() { }

        // ──────────────────────────────────────────────────────────────
        // Add
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Add_PersistsPostToDatabase()
        {
            var (ctx, svc) = Build(nameof(Add_PersistsPostToDatabase));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            var post = MakePost(user, forum);
            await svc.Add(post);

            Assert.Equal(1, await ctx.Posts.CountAsync());
        }

        // ──────────────────────────────────────────────────────────────
        // AddReply
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddReply_PersistsReplyToDatabase()
        {
            var (ctx, svc) = Build(nameof(AddReply_PersistsReplyToDatabase));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            var reply = new PostReply
            {
                ReplyContent = "Nice post!",
                CreateOn = DateTime.UtcNow,
                User = user,
                Post = post
            };
            await svc.AddReply(reply);

            Assert.Equal(1, await ctx.Replies.CountAsync());
        }

        // ──────────────────────────────────────────────────────────────
        // Delete
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_RemovesPostAndCascades()
        {
            var (ctx, svc) = Build(nameof(Delete_RemovesPostAndCascades));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);

            var reply = new PostReply { ReplyContent = "reply", CreateOn = DateTime.UtcNow, User = user, Post = post };
            var like = new Like { User = user, Post = post };

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Replies.Add(reply);
            ctx.Likes.Add(like);
            await ctx.SaveChangesAsync();

            await svc.Delete(post.PostId);

            Assert.Equal(0, await ctx.Posts.CountAsync());
            Assert.Equal(0, await ctx.Replies.CountAsync());
            Assert.Equal(0, await ctx.Likes.CountAsync());
        }

        [Fact]
        public async Task Delete_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(Delete_NonExistentId_DoesNotThrow));
            var exception = await Record.ExceptionAsync(() => svc.Delete(999));
            Assert.Null(exception);
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteReply
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteReply_RemovesReply()
        {
            var (ctx, svc) = Build(nameof(DeleteReply_RemovesReply));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            var reply = new PostReply { ReplyContent = "bye", CreateOn = DateTime.UtcNow, User = user, Post = post };

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Replies.Add(reply);
            await ctx.SaveChangesAsync();

            await svc.DeleteReply(reply.Id);

            Assert.Equal(0, await ctx.Replies.CountAsync());
        }

        [Fact]
        public async Task DeleteReply_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(DeleteReply_NonExistentId_DoesNotThrow));
            var exception = await Record.ExceptionAsync(() => svc.DeleteReply(999));
            Assert.Null(exception);
        }

        // ──────────────────────────────────────────────────────────────
        // EditPost
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task EditPost_UpdatesTitleAndContent()
        {
            var (ctx, svc) = Build(nameof(EditPost_UpdatesTitleAndContent));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            await svc.EditPost(post.PostId, "New content", "New title");

            var updated = await ctx.Posts.FindAsync(post.PostId);
            Assert.Equal("New content", updated!.Content);
            Assert.Equal("New title", updated.Title);
        }

        [Fact]
        public async Task EditPost_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(EditPost_NonExistentId_DoesNotThrow));
            var exception = await Record.ExceptionAsync(() => svc.EditPost(999, "x", "y"));
            Assert.Null(exception);
        }

        // ──────────────────────────────────────────────────────────────
        // EditReply
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task EditReply_UpdatesContent()
        {
            var (ctx, svc) = Build(nameof(EditReply_UpdatesContent));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            var reply = new PostReply { ReplyContent = "old", CreateOn = DateTime.UtcNow, User = user, Post = post };

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Replies.Add(reply);
            await ctx.SaveChangesAsync();

            await svc.EditReply(reply.Id, "updated content");

            var updated = await ctx.Replies.FindAsync(reply.Id);
            Assert.Equal("updated content", updated!.ReplyContent);
        }

        // ──────────────────────────────────────────────────────────────
        // GetAll
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsAllPosts()
        {
            var (ctx, svc) = Build(nameof(GetAll_ReturnsAllPosts));
            var user = MakeUser();
            var forum = MakeForum(user);

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(MakePost(user, forum, 1), MakePost(user, forum, 2));
            await ctx.SaveChangesAsync();

            Assert.Equal(2, (await svc.GetAll()).Count());
        }

        [Fact]
        public async Task GetAll_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetAll_EmptyDatabase_ReturnsEmpty));
            Assert.Empty(await svc.GetAll());
        }

        // ──────────────────────────────────────────────────────────────
        // GetById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingId_ReturnsPost()
        {
            var (ctx, svc) = Build(nameof(GetById_ExistingId_ReturnsPost));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            var result = await svc.GetById(post.PostId);

            Assert.NotNull(result);
            Assert.Equal(post.PostId, result!.PostId);
        }

        [Fact]
        public async Task GetById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetById_NonExistentId_ReturnsNull));
            Assert.Null(await svc.GetById(999));
        }

        // ──────────────────────────────────────────────────────────────
        // GetFilteredPosts (string overload)
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetFilteredPosts_MatchingTitle_ReturnsCorrectPosts()
        {
            var (ctx, svc) = Build(nameof(GetFilteredPosts_MatchingTitle_ReturnsCorrectPosts));
            var user = MakeUser();
            var forum = MakeForum(user);
            var p1 = MakePost(user, forum, 1);
            var p2 = new Post
            {
                PostId = 2,
                Title = "Another",
                Content = "stuff",
                CreatedOn = DateTime.UtcNow,
                User = user,
                Forum = forum,
                Replies = new List<PostReply>(),
                Likes = new List<Like>()
            };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var results = (await svc.GetFilteredPosts("hello")).ToList();

            Assert.Single(results);
            Assert.Equal("Hello World", results[0].Title);
        }

        [Fact]
        public async Task GetFilteredPosts_NoMatch_ReturnsEmpty()
        {
            var (ctx, svc) = Build(nameof(GetFilteredPosts_NoMatch_ReturnsEmpty));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(MakePost(user, forum));
            await ctx.SaveChangesAsync();

            Assert.Empty(await svc.GetFilteredPosts("zzznomatch"));
        }

        // ──────────────────────────────────────────────────────────────
        // GetFilteredPosts (Forum overload)
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetFilteredPosts_ForumOverload_EmptyQuery_ReturnsAll()
        {
            var (ctx, svc) = Build(nameof(GetFilteredPosts_ForumOverload_EmptyQuery_ReturnsAll));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post1 = MakePost(user, forum, 1);
            var post2 = MakePost(user, forum, 2);

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(post1, post2);
            await ctx.SaveChangesAsync();

            var forumWithPosts = await ctx.Forums
                .Include(f => f.Posts)
                .FirstAsync(f => f.ForumId == forum.ForumId);

            var results = (await svc.GetFilteredPosts(forumWithPosts, string.Empty)).ToList();
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GetFilteredPosts_ForumOverload_WithQuery_FiltersCorrectly()
        {
            var (ctx, svc) = Build(nameof(GetFilteredPosts_ForumOverload_WithQuery_FiltersCorrectly));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post1 = MakePost(user, forum, 1);
            var post2 = new Post
            {
                PostId = 2,
                Title = "Secret",
                Content = "hidden",
                CreatedOn = DateTime.UtcNow,
                User = user,
                Forum = forum,
                Replies = new List<PostReply>(),
                Likes = new List<Like>()
            };

            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(post1, post2);
            await ctx.SaveChangesAsync();

            var results = (await svc.GetFilteredPosts(forum, "secret")).ToList();
            Assert.Single(results);
            Assert.Equal("Secret", results[0].Title);
        }

        // ──────────────────────────────────────────────────────────────
        // SearchPostsAsync
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task SearchPostsAsync_MatchingTitle_ReturnsMappedModel()
        {
            var (ctx, svc) = Build(nameof(SearchPostsAsync_MatchingTitle_ReturnsMappedModel));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            var results = (await svc.SearchPostsAsync("hello")).ToList();

            Assert.Single(results);
            Assert.Equal(post.PostId, results[0].Id);
            Assert.Equal(post.Title, results[0].Title);
            Assert.Equal(user.UserName, results[0].AuthorName);
        }

        [Theory]
        [InlineData("hello world", true)]
        [InlineData("hello", true)]
        [InlineData("Hello World", false)]
        [InlineData("HELLO WORLD", false)]
        [InlineData("HELLO", false)]
        public async Task SearchPostsAsync_CaseInsensitive_AlwaysMatches(string query, bool shouldMatch)
        {
            var dbName = $"{nameof(SearchPostsAsync_CaseInsensitive_AlwaysMatches)}_{query}";
            var (ctx, svc) = Build(dbName);
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(MakePost(user, forum));
            await ctx.SaveChangesAsync();

            var results = await svc.SearchPostsAsync(query);

            if (shouldMatch)
                Assert.NotEmpty(results);
            else
                Assert.Empty(results);
        }

        [Fact]
        public async Task SearchPostsAsync_NoMatch_ReturnsEmpty()
        {
            var (ctx, svc) = Build(nameof(SearchPostsAsync_NoMatch_ReturnsEmpty));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(MakePost(user, forum));
            await ctx.SaveChangesAsync();

            var results = await svc.SearchPostsAsync("zzznomatch");
            Assert.Empty(results);
        }

        // ──────────────────────────────────────────────────────────────
        // GetLatestPosts
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetLatestPosts_ReturnsNewestFirst()
        {
            var (ctx, svc) = Build(nameof(GetLatestPosts_ReturnsNewestFirst));
            var user = MakeUser();
            var forum = MakeForum(user);
            var older = new Post { PostId = 1, Title = "Older", Content = "x", CreatedOn = DateTime.UtcNow.AddDays(-2), User = user, Forum = forum, Replies = new List<PostReply>(), Likes = new() };
            var newer = new Post { PostId = 2, Title = "Newer", Content = "y", CreatedOn = DateTime.UtcNow, User = user, Forum = forum, Replies = new List<PostReply>(), Likes = new() };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(older, newer);
            await ctx.SaveChangesAsync();

            var result = (await svc.GetLatestPosts(1)).ToList();

            Assert.Single(result);
            Assert.Equal("Newer", result[0].Title);
        }

        // ──────────────────────────────────────────────────────────────
        // GetTopPosts
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTopPosts_ReturnsMostLikedFirst()
        {
            var (ctx, svc) = Build(nameof(GetTopPosts_ReturnsMostLikedFirst));
            var user = MakeUser();
            var forum = MakeForum(user);
            var lowLikes = new Post { PostId = 1, Title = "Low", Content = "x", TotalLikes = 1, CreatedOn = DateTime.UtcNow, User = user, Forum = forum, Replies = new List<PostReply>(), Likes = new() };
            var highLikes = new Post { PostId = 2, Title = "High", Content = "y", TotalLikes = 9, CreatedOn = DateTime.UtcNow, User = user, Forum = forum, Replies = new List<PostReply>(), Likes = new() };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.AddRange(lowLikes, highLikes);
            await ctx.SaveChangesAsync();

            var result = (await svc.GetTopPosts(1)).ToList();

            Assert.Single(result);
            Assert.Equal("High", result[0].Title);
        }

        // ──────────────────────────────────────────────────────────────
        // UpdatePostLikes
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdatePostLikes_IncrementsLikesByOne()
        {
            var (ctx, svc) = Build(nameof(UpdatePostLikes_IncrementsLikesByOne));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            post.TotalLikes = 5;
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            await svc.UpdatePostLikes(post.PostId);

            var updated = await ctx.Posts.FindAsync(post.PostId);
            Assert.Equal(6, updated!.TotalLikes);
        }

        // ──────────────────────────────────────────────────────────────
        // CalculatePostLikes
        // ──────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(0, 1)]
        [InlineData(5, 6)]
        [InlineData(99, 100)]
        public void CalculatePostLikes_AlwaysAddsOne(int input, int expected)
        {
            var (_, svc) = Build(nameof(CalculatePostLikes_AlwaysAddsOne) + input);
            Assert.Equal(expected, svc.CalculatePostLikes(input));
        }

        // ──────────────────────────────────────────────────────────────
        // GetLikeById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetLikeById_ExistingId_ReturnsLike()
        {
            var (ctx, svc) = Build(nameof(GetLikeById_ExistingId_ReturnsLike));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            var like = new Like { User = user, Post = post };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Likes.Add(like);
            await ctx.SaveChangesAsync();

            var result = await svc.GetLikeById(like.Id);

            Assert.NotNull(result);
            Assert.Equal(like.Id, result!.Id);
        }

        [Fact]
        public async Task GetLikeById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetLikeById_NonExistentId_ReturnsNull));
            Assert.Null(await svc.GetLikeById(999));
        }

        // ──────────────────────────────────────────────────────────────
        // GetAllLikes
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllLikes_ReturnsLikesForPost()
        {
            var (ctx, svc) = Build(nameof(GetAllLikes_ReturnsLikesForPost));
            var user = MakeUser();
            var user2 = MakeUser("user-2", "Bob");
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.AddRange(user, user2);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Likes.AddRange(new Like { User = user, Post = post }, new Like { User = user2, Post = post });
            await ctx.SaveChangesAsync();

            var likes = (await svc.GetAllLikes(post.PostId)).ToList();
            Assert.Equal(2, likes.Count);
        }

        [Fact]
        public async Task GetAllLikes_NonExistentPost_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetAllLikes_NonExistentPost_ReturnsEmpty));
            Assert.Empty(await svc.GetAllLikes(999));
        }

        // ──────────────────────────────────────────────────────────────
        // GetReplyById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetReplyById_ExistingId_ReturnsReply()
        {
            var (ctx, svc) = Build(nameof(GetReplyById_ExistingId_ReturnsReply));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            var reply = new PostReply { ReplyContent = "hi", CreateOn = DateTime.UtcNow, User = user, Post = post };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Replies.Add(reply);
            await ctx.SaveChangesAsync();

            var result = await svc.GetReplyByIdAsync(reply.Id);

            Assert.NotNull(result);
            Assert.Equal("hi", result!.ReplyContent);
        }

        [Fact]
        public async Task GetReplyById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetReplyById_NonExistentId_ReturnsNull));
            Assert.Null(await svc.GetReplyByIdAsync(999));
        }
    }
}