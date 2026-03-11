using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fan_Website.Models;
using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;
using Fan_Website.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fan_Website.Tests
{
    /// <summary>
    /// Unit tests for ApplicationUserService using EF Core InMemory provider.
    /// NuGet packages required:
    ///   - Microsoft.EntityFrameworkCore.InMemory
    ///   - xunit
    ///   - xunit.runner.visualstudio
    /// </summary>
    public class ApplicationUserServiceTests
    {
        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private (AppDbContext ctx, ApplicationUserService svc) Build(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var ctx = new AppDbContext(options);
            return (ctx, new ApplicationUserService(ctx));
        }

        private static ApplicationUser MakeUser(
            string id = "user-1",
            string name = "Matthew",
            int rating = 0,
            string imagePath = "") =>
            new ApplicationUser
            {
                Id = id,
                UserName = name,
                NormalizedUserName = name.ToUpper(),
                Email = $"{name.ToLower()}@example.com",
                Rating = rating,
                ImagePath = imagePath,
                MemberSince = DateTime.UtcNow,
                Follows = new List<Follow>(),
                Followings = new List<Follow>(),
                ProfileComments = new List<ProfileComment>()
            };

        private static ProfileComment MakeComment(
            ApplicationUser commentUser,
            ApplicationUser profileUser,
            string content = "Nice profile!") =>
            new ProfileComment
            {
                Content = content,
                CommentUser = commentUser,
                ProfileUser = profileUser
            };

        // ──────────────────────────────────────────────────────────────
        // GetAll
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_ReturnsAllUsers()
        {
            var (ctx, svc) = Build(nameof(GetAll_ReturnsAllUsers));
            ctx.Users.AddRange(MakeUser("1", "Matthew"), MakeUser("2", "Bob"));
            ctx.SaveChanges();

            Assert.Equal(2, svc.GetAll().Count());
        }

        [Fact]
        public void GetAll_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetAll_EmptyDatabase_ReturnsEmpty));
            Assert.Empty(svc.GetAll());
        }

        // ──────────────────────────────────────────────────────────────
        // GetById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetById_ExistingId_ReturnsUser()
        {
            var (ctx, svc) = Build(nameof(GetById_ExistingId_ReturnsUser));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.SaveChanges();

            var result = svc.GetById(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public void GetById_NonExistentId_ThrowsKeyNotFoundException()
        {
            var (_, svc) = Build(nameof(GetById_NonExistentId_ThrowsKeyNotFoundException));

            Assert.Throws<KeyNotFoundException>(() => svc.GetById("nonexistent"));
        }

        [Fact]
        public void GetById_IncludesFollowsAndProfileComments()
        {
            var (ctx, svc) = Build(nameof(GetById_IncludesFollowsAndProfileComments));
            var user = MakeUser("1", "Matthew");
            var commenter = MakeUser("2", "Bob");
            var comment = MakeComment(commenter, user);

            ctx.Users.AddRange(user, commenter);
            ctx.ProfileComments.Add(comment);
            ctx.SaveChanges();

            var result = svc.GetById(user.Id);

            Assert.NotNull(result.ProfileComments);
            Assert.Single(result.ProfileComments);
        }

        // ──────────────────────────────────────────────────────────────
        // CalculateUserRating
        // ──────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(typeof(Post), 0, 1)]
        [InlineData(typeof(PostReply), 0, 3)]
        [InlineData(typeof(Screenshot), 0, 2)]
        [InlineData(typeof(Forum), 0, 2)]
        [InlineData(typeof(ProfileComment), 0, 3)]
        public async Task UpdateUserRating_CorrectlyIncrementsRating(
            Type type, int initialRating, int expectedRating)
        {
            var dbName = $"{nameof(UpdateUserRating_CorrectlyIncrementsRating)}_{type.Name}";
            var (ctx, svc) = Build(dbName);
            var user = MakeUser(rating: initialRating);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.UpdateUserRating(user.Id, type);

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal(expectedRating, updated!.Rating);
        }

        [Fact]
        public async Task UpdateUserRating_UnknownType_DoesNotChangeRating()
        {
            var (ctx, svc) = Build(nameof(UpdateUserRating_UnknownType_DoesNotChangeRating));
            var user = MakeUser(rating: 5);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.UpdateUserRating(user.Id, typeof(string)); // unknown type

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal(5, updated!.Rating); // unchanged
        }

        [Fact]
        public async Task UpdateUserRating_AccumulatesAcrossMultipleCalls()
        {
            var (ctx, svc) = Build(nameof(UpdateUserRating_AccumulatesAcrossMultipleCalls));
            var user = MakeUser(rating: 0);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.UpdateUserRating(user.Id, typeof(Post));      // +1
            await svc.UpdateUserRating(user.Id, typeof(PostReply)); // +3

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal(4, updated!.Rating);
        }

        // ──────────────────────────────────────────────────────────────
        // SetProfileImage
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task SetProfileImage_UpdatesImagePath()
        {
            var (ctx, svc) = Build(nameof(SetProfileImage_UpdatesImagePath));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var uri = new Uri("https://storage.blob.core.windows.net/images/avatar.png");
            await svc.SetProfileImage(user.Id, uri);

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal(uri.AbsoluteUri, updated!.ImagePath);
        }

        [Fact]
        public async Task SetProfileImage_OverwritesPreviousImage()
        {
            var (ctx, svc) = Build(nameof(SetProfileImage_OverwritesPreviousImage));
            var user = MakeUser(imagePath: "https://old-image.com/old.png");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var newUri = new Uri("https://storage.blob.core.windows.net/images/new.png");
            await svc.SetProfileImage(user.Id, newUri);

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal(newUri.AbsoluteUri, updated!.ImagePath);
        }

        // ──────────────────────────────────────────────────────────────
        // GetLatestUsers
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetLatestUsers_ReturnsNewestFirst()
        {
            var (ctx, svc) = Build(nameof(GetLatestUsers_ReturnsNewestFirst));
            var older = MakeUser("1", "Matthew");
            var newer = MakeUser("2", "Bob");
            older.MemberSince = DateTime.UtcNow.AddDays(-5);
            newer.MemberSince = DateTime.UtcNow;

            ctx.Users.AddRange(older, newer);
            ctx.SaveChanges();

            var result = svc.GetLatestUsers(1).ToList();

            Assert.Single(result);
            Assert.Equal("Bob", result[0].UserName);
        }

        [Fact]
        public void GetLatestUsers_RespectsNLimit()
        {
            var (ctx, svc) = Build(nameof(GetLatestUsers_RespectsNLimit));
            ctx.Users.AddRange(
                MakeUser("1", "A"),
                MakeUser("2", "B"),
                MakeUser("3", "C"));
            ctx.SaveChanges();

            var result = svc.GetLatestUsers(2).ToList();

            Assert.Equal(2, result.Count);
        }

        // ──────────────────────────────────────────────────────────────
        // AddComment / GetCommentById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddComment_PersistsComment()
        {
            var (ctx, svc) = Build(nameof(AddComment_PersistsComment));
            var user = MakeUser("1", "Matthew");
            var commenter = MakeUser("2", "Bob");
            ctx.Users.AddRange(user, commenter);
            await ctx.SaveChangesAsync();

            var comment = MakeComment(commenter, user);
            await svc.AddComment(comment);

            Assert.Equal(1, await ctx.ProfileComments.CountAsync());
        }

        [Fact]
        public async Task GetCommentById_ExistingId_ReturnsComment()
        {
            var (ctx, svc) = Build(nameof(GetCommentById_ExistingId_ReturnsComment));
            var user = MakeUser("1", "Matthew");
            var commenter = MakeUser("2", "Bob");
            var comment = MakeComment(commenter, user, "Hello!");
            ctx.Users.AddRange(user, commenter);
            ctx.ProfileComments.Add(comment);
            await ctx.SaveChangesAsync();

            var result = await svc.GetCommentById(comment.Id);

            Assert.NotNull(result);
            Assert.Equal("Hello!", result.Content);
        }

        [Fact]
        public async Task GetCommentById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetCommentById_NonExistentId_ReturnsNull));

            var result = await svc.GetCommentById(999);

            Assert.Null(result);
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateComment
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateComment_SavesNewContent()
        {
            var (ctx, svc) = Build(nameof(UpdateComment_SavesNewContent));
            var user = MakeUser("1", "Matthew");
            var commenter = MakeUser("2", "Bob");
            var comment = MakeComment(commenter, user, "Original");
            ctx.Users.AddRange(user, commenter);
            ctx.ProfileComments.Add(comment);
            await ctx.SaveChangesAsync();

            comment.Content = "Updated content";
            await svc.UpdateComment(comment);

            var updated = await ctx.ProfileComments.FindAsync(comment.Id);
            Assert.Equal("Updated content", updated!.Content);
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteComment
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteComment_RemovesComment()
        {
            var (ctx, svc) = Build(nameof(DeleteComment_RemovesComment));
            var user = MakeUser("1", "Matthew");
            var commenter = MakeUser("2", "Bob");
            var comment = MakeComment(commenter, user);
            ctx.Users.AddRange(user, commenter);
            ctx.ProfileComments.Add(comment);
            await ctx.SaveChangesAsync();

            await svc.DeleteComment(comment.Id);

            Assert.Equal(0, await ctx.ProfileComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(DeleteComment_NonExistentId_DoesNotThrow));

            var ex = await Record.ExceptionAsync(() => svc.DeleteComment(999));
            Assert.Null(ex);
        }

        // ──────────────────────────────────────────────────────────────
        // EditProfile
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task EditProfile_UpdatesUsernameAndBio()
        {
            var (ctx, svc) = Build(nameof(EditProfile_UpdatesUsernameAndBio));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.EditProfile(user.Id, "My new bio", "NewUsername");

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal("NewUsername", updated!.UserName);
            Assert.Equal("My new bio", updated.Bio);
        }

        [Fact]
        public async Task EditProfile_NormalizesUsername()
        {
            var (ctx, svc) = Build(nameof(EditProfile_NormalizesUsername));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.EditProfile(user.Id, "bio", "newusername");

            var updated = await ctx.Users.FindAsync(user.Id);
            Assert.Equal("NEWUSERNAME", updated!.NormalizedUserName);
        }

        [Fact]
        public async Task EditProfile_DoesNotAffectOtherUsers()
        {
            var (ctx, svc) = Build(nameof(EditProfile_DoesNotAffectOtherUsers));
            var user1 = MakeUser("1", "Matthew");
            var user2 = MakeUser("2", "Bob");
            ctx.Users.AddRange(user1, user2);
            await ctx.SaveChangesAsync();

            await svc.EditProfile(user1.Id, "bio", "UpdatedMatthew");

            var untouched = await ctx.Users.FindAsync(user2.Id);
            Assert.Equal("Bob", untouched!.UserName);
        }

        // ──────────────────────────────────────────────────────────────
        // GetFollowing
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetFollowing_ReturnsFollowsForUser()
        {
            var (ctx, svc) = Build(nameof(GetFollowing_ReturnsFollowsForUser));
            var user = MakeUser("1", "Matthew");
            var follower = MakeUser("2", "Bob");
            var follow = new Follow { Follower = follower, Following = user };

            ctx.Users.AddRange(user, follower);
            ctx.Follows.Add(follow);
            ctx.SaveChanges();

            var result = svc.GetFollowing(user.Id).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void GetFollowing_NoFollowers_ReturnsEmpty()
        {
            var (ctx, svc) = Build(nameof(GetFollowing_NoFollowers_ReturnsEmpty));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.SaveChanges();

            var result = svc.GetFollowing(user.Id).ToList();

            Assert.Empty(result);
        }
    }
}