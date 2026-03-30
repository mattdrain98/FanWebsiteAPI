using Fan_Website.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fan_Website.Tests
{
    public class ForumServiceTests
    {
        private (AppDbContext ctx, ForumService svc) Build(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var ctx = new AppDbContext(options);
            return (ctx, new ForumService(ctx));
        }

        private static ApplicationUser MakeUser(string id = "user-1", string name = "Matthew") =>
            new ApplicationUser { Id = id, UserName = name, Rating = 10 };

        private static Forum MakeForum(ApplicationUser owner, int id = 1,
            string title = "General", string description = "General discussion") =>
            new Forum
            {
                ForumId = id,
                PostTitle = title,
                Description = description,
                User = owner,
                Posts = new List<Post>()
            };

        private static Post MakePost(ApplicationUser author, Forum forum, int id = 1) =>
            new Post
            {
                PostId = id,
                Title = "Test Post",
                Content = "Test content",
                UpdatedOn = DateTime.UtcNow,
                User = author,
                Forum = forum,
                Replies = new List<PostReply>(),
                Likes = new List<Like>()
            };

        // ──────────────────────────────────────────────────────────────
        // Create
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_PersistsForumToDatabase()
        {
            var (ctx, svc) = Build(nameof(Create_PersistsForumToDatabase));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.Create(forum);

            Assert.Equal(1, await ctx.Forums.CountAsync());
        }

        [Fact]
        public async Task Create_SavesCorrectTitle()
        {
            var (ctx, svc) = Build(nameof(Create_SavesCorrectTitle));
            var user = MakeUser();
            var forum = MakeForum(user, title: "My Forum");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.Create(forum);

            var saved = await ctx.Forums.FirstAsync();
            Assert.Equal("My Forum", saved.PostTitle);
        }

        // ──────────────────────────────────────────────────────────────
        // Delete
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_RemovesForumFromDatabase()
        {
            var (ctx, svc) = Build(nameof(Delete_RemovesForumFromDatabase));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            await svc.Delete(forum.ForumId);

            Assert.Equal(0, await ctx.Forums.CountAsync());
        }

        [Fact]
        public async Task Delete_OnlyRemovesTargetForum()
        {
            var (ctx, svc) = Build(nameof(Delete_OnlyRemovesTargetForum));
            var user = MakeUser();
            var forum1 = MakeForum(user, 1, "Forum 1");
            var forum2 = MakeForum(user, 2, "Forum 2");
            ctx.Users.Add(user);
            ctx.Forums.AddRange(forum1, forum2);
            await ctx.SaveChangesAsync();

            await svc.Delete(forum1.ForumId);

            var remaining = await ctx.Forums.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("Forum 2", remaining[0].PostTitle);
        }

        // ──────────────────────────────────────────────────────────────
        // GetAll
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsAllForums()
        {
            var (ctx, svc) = Build(nameof(GetAll_ReturnsAllForums));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.Forums.AddRange(MakeForum(user, 1), MakeForum(user, 2), MakeForum(user, 3));
            await ctx.SaveChangesAsync();

            var results = (await svc.GetAll()).ToList();

            Assert.Equal(3, results.Count);
        }

        [Fact]
        public async Task GetAll_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetAll_EmptyDatabase_ReturnsEmpty));

            Assert.Empty(await svc.GetAll());
        }

        [Fact]
        public async Task GetAll_IncludesUser()
        {
            var (ctx, svc) = Build(nameof(GetAll_IncludesUser));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            var result = await svc.GetAll();

            var userResult = result.First();

            Assert.NotNull(userResult.User);
            Assert.Equal("Matthew", userResult.User.UserName);
        }

        [Fact]
        public async Task GetAll_IncludesPosts()
        {
            var (ctx, svc) = Build(nameof(GetAll_IncludesPosts));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            await ctx.SaveChangesAsync();

            var result = (await svc.GetAll()).First();

            Assert.NotNull(result.Posts);
            Assert.Single(result.Posts);
        }

        // ──────────────────────────────────────────────────────────────
        // GetById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsForum()
        {
            var (ctx, svc) = Build(nameof(GetByIdAsync_ExistingId_ReturnsForum));
            var user = MakeUser();
            var forum = MakeForum(user);
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            var result = await svc.GetByIdAsync(forum.ForumId);

            Assert.NotNull(result);
            Assert.Equal(forum.ForumId, result.ForumId);
        }

        [Fact]
        public async Task GetById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetById_NonExistentId_ReturnsNull));

            Assert.Null(await svc.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetById_IncludesPostsWithReplies()
        {
            var (ctx, svc) = Build(nameof(GetById_IncludesPostsWithReplies));
            var user = MakeUser();
            var forum = MakeForum(user);
            var post = MakePost(user, forum);
            var reply = new PostReply
            {
                ReplyContent = "Nice!",
                UpdatedOn = DateTime.UtcNow,
                User = user,
                Post = post
            };
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            ctx.Posts.Add(post);
            ctx.Replies.Add(reply);
            await ctx.SaveChangesAsync();

            var result = await svc.GetByIdAsync(forum.ForumId);

            Assert.Single(result.Posts);
            Assert.Single(result.Posts.First().Replies);
        }

        // ──────────────────────────────────────────────────────────────
        // GetTopForums
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTopForums_ReturnsMostPostsFirst()
        {
            var (ctx, svc) = Build(nameof(GetTopForums_ReturnsMostPostsFirst));
            var user = MakeUser();
            var forum1 = MakeForum(user, 1, "Low");
            var forum2 = MakeForum(user, 2, "High");

            var posts = Enumerable.Range(1, 5)
                .Select(i => MakePost(user, forum2, i))
                .ToList();
            posts.Add(MakePost(user, forum1, 6));

            ctx.Users.Add(user);
            ctx.Forums.AddRange(forum1, forum2);
            ctx.Posts.AddRange(posts);
            await ctx.SaveChangesAsync();

            var result = (await svc.GetTopForums(1)).ToList();

            Assert.Single(result);
            Assert.Equal("High", result[0].PostTitle);
        }

        [Fact]
        public async Task GetTopForums_RespectsNLimit()
        {
            var (ctx, svc) = Build(nameof(GetTopForums_RespectsNLimit));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.Forums.AddRange(
                MakeForum(user, 1, "A"),
                MakeForum(user, 2, "B"),
                MakeForum(user, 3, "C"));
            await ctx.SaveChangesAsync();

            var result = (await svc.GetTopForums(2)).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTopForums_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetTopForums_EmptyDatabase_ReturnsEmpty));

            Assert.Empty(await svc.GetTopForums(5));
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateForumDescription
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateForumDescription_UpdatesCorrectly()
        {
            var (ctx, svc) = Build(nameof(UpdateForumDescription_UpdatesCorrectly));
            var user = MakeUser();
            var forum = MakeForum(user, description: "Old description");
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            await svc.UpdateForumDescription(forum.ForumId, "New description");

            var updated = await ctx.Forums.FindAsync(forum.ForumId);
            Assert.Equal("New description", updated!.Description);
        }

        [Fact]
        public async Task UpdateForumDescription_DoesNotChangeTitle()
        {
            var (ctx, svc) = Build(nameof(UpdateForumDescription_DoesNotChangeTitle));
            var user = MakeUser();
            var forum = MakeForum(user, title: "Original Title", description: "Old");
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            await svc.UpdateForumDescription(forum.ForumId, "New description");

            var updated = await ctx.Forums.FindAsync(forum.ForumId);
            Assert.Equal("Original Title", updated!.PostTitle);
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateForumTitle
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateForumTitle_UpdatesCorrectly()
        {
            var (ctx, svc) = Build(nameof(UpdateForumTitle_UpdatesCorrectly));
            var user = MakeUser();
            var forum = MakeForum(user, title: "Old Title");
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            await svc.UpdateForumTitle(forum.ForumId, "New Title");

            var updated = await ctx.Forums.FindAsync(forum.ForumId);
            Assert.Equal("New Title", updated!.PostTitle);
        }

        [Fact]
        public async Task UpdateForumTitle_DoesNotChangeDescription()
        {
            var (ctx, svc) = Build(nameof(UpdateForumTitle_DoesNotChangeDescription));
            var user = MakeUser();
            var forum = MakeForum(user, title: "Old", description: "Original Description");
            ctx.Users.Add(user);
            ctx.Forums.Add(forum);
            await ctx.SaveChangesAsync();

            await svc.UpdateForumTitle(forum.ForumId, "New Title");

            var updated = await ctx.Forums.FindAsync(forum.ForumId);
            Assert.Equal("Original Description", updated!.Description);
        }

        [Fact]
        public async Task UpdateForumTitle_OnlyUpdatesTargetForum()
        {
            var (ctx, svc) = Build(nameof(UpdateForumTitle_OnlyUpdatesTargetForum));
            var user = MakeUser();
            var forum1 = MakeForum(user, 1, "Forum 1");
            var forum2 = MakeForum(user, 2, "Forum 2");
            ctx.Users.Add(user);
            ctx.Forums.AddRange(forum1, forum2);
            await ctx.SaveChangesAsync();

            await svc.UpdateForumTitle(forum1.ForumId, "Updated Forum 1");

            var untouched = await ctx.Forums.FindAsync(forum2.ForumId);
            Assert.Equal("Forum 2", untouched!.PostTitle);
        }
    }
}