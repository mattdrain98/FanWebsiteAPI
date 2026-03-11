using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fan_Website.Models;
using Fan_Website.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fan_Website.Tests
{
    /// <summary>
    /// Unit tests for ScreenshotService using EF Core InMemory provider.
    /// NuGet packages required:
    ///   - Microsoft.EntityFrameworkCore.InMemory
    ///   - xunit
    ///   - xunit.runner.visualstudio
    /// </summary>
    public class ScreenshotServiceTests
    {
        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private (AppDbContext ctx, ScreenshotService svc) Build(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var ctx = new AppDbContext(options);
            return (ctx, new ScreenshotService(ctx));
        }

        private static ApplicationUser MakeUser(string id = "user-1", string name = "Alice") =>
            new ApplicationUser
            {
                Id = id,
                UserName = name,
                Email = $"{name.ToLower()}@example.com"
            };

        private static Screenshot MakeScreenshot(ApplicationUser user, int id = 1,
            string title = "Test Screenshot", string imagePath = "https://blob.core.windows.net/img/test.png", string description = "test description") =>
            new Screenshot
            {
                ScreenshotId = id,
                ScreenshotTitle = title,
                ScreenshotDescription = description,
                ImagePath = imagePath,
                CreatedOn = DateTime.UtcNow,
                User = user
            };

        // ──────────────────────────────────────────────────────────────
        // Add
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Add_PersistsScreenshotToDatabase()
        {
            var (ctx, svc) = Build(nameof(Add_PersistsScreenshotToDatabase));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.Add(MakeScreenshot(user));

            Assert.Equal(1, await ctx.Screenshots.CountAsync());
        }

        [Fact]
        public async Task Add_SavesCorrectImagePath()
        {
            var (ctx, svc) = Build(nameof(Add_SavesCorrectImagePath));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var screenshot = MakeScreenshot(user, imagePath: "https://blob.core.windows.net/img/shot.png");
            await svc.Add(screenshot);

            var saved = await ctx.Screenshots.FirstAsync();
            Assert.Equal("https://blob.core.windows.net/img/shot.png", saved.ImagePath);
        }

        [Fact]
        public async Task Add_MultipleScreenshots_AllPersisted()
        {
            var (ctx, svc) = Build(nameof(Add_MultipleScreenshots_AllPersisted));
            var user = MakeUser();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            await svc.Add(MakeScreenshot(user, 1, "Shot 1"));
            await svc.Add(MakeScreenshot(user, 2, "Shot 2"));
            await svc.Add(MakeScreenshot(user, 3, "Shot 3"));

            Assert.Equal(3, await ctx.Screenshots.CountAsync());
        }

        // ──────────────────────────────────────────────────────────────
        // Delete
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_RemovesScreenshot()
        {
            var (ctx, svc) = Build(nameof(Delete_RemovesScreenshot));
            var user = MakeUser();
            var screenshot = MakeScreenshot(user);
            ctx.Users.Add(user);
            ctx.Screenshots.Add(screenshot);
            await ctx.SaveChangesAsync();

            await svc.Delete(screenshot.ScreenshotId);

            Assert.Equal(0, await ctx.Screenshots.CountAsync());
        }

        [Fact]
        public async Task Delete_OnlyRemovesTargetScreenshot()
        {
            var (ctx, svc) = Build(nameof(Delete_OnlyRemovesTargetScreenshot));
            var user = MakeUser();
            var shot1 = MakeScreenshot(user, 1, "Shot 1");
            var shot2 = MakeScreenshot(user, 2, "Shot 2");
            ctx.Users.Add(user);
            ctx.Screenshots.AddRange(shot1, shot2);
            await ctx.SaveChangesAsync();

            await svc.Delete(shot1.ScreenshotId);

            var remaining = await ctx.Screenshots.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("Shot 2", remaining[0].ScreenshotTitle);
        }

        [Fact]
        public async Task Delete_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(Delete_NonExistentId_DoesNotThrow));

            var ex = await Record.ExceptionAsync(() => svc.Delete(999));
            Assert.Null(ex);
        }

        // ──────────────────────────────────────────────────────────────
        // EditScreenshotContext
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task EditScreenshotContext_ThrowsNotImplementedException()
        {
            var (_, svc) = Build(nameof(EditScreenshotContext_ThrowsNotImplementedException));

            await Assert.ThrowsAsync<NotImplementedException>(() => svc.EditScreenshotContext(1, "new content"));
        }

        // ──────────────────────────────────────────────────────────────
        // GetAll
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_ReturnsAllScreenshots()
        {
            var (ctx, svc) = Build(nameof(GetAll_ReturnsAllScreenshots));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.Screenshots.AddRange(
                MakeScreenshot(user, 1),
                MakeScreenshot(user, 2),
                MakeScreenshot(user, 3));
            ctx.SaveChanges();

            Assert.Equal(3, svc.GetAll().Count());
        }

        [Fact]
        public void GetAll_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetAll_EmptyDatabase_ReturnsEmpty));

            Assert.Empty(svc.GetAll());
        }

        [Fact]
        public void GetAll_IncludesUser()
        {
            var (ctx, svc) = Build(nameof(GetAll_IncludesUser));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.Screenshots.Add(MakeScreenshot(user));
            ctx.SaveChanges();

            var result = svc.GetAll().First();

            Assert.NotNull(result.User);
            Assert.Equal("Alice", result.User.UserName);
        }

        // ──────────────────────────────────────────────────────────────
        // GetById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetById_ExistingId_ReturnsScreenshot()
        {
            var (ctx, svc) = Build(nameof(GetById_ExistingId_ReturnsScreenshot));
            var user = MakeUser();
            var screenshot = MakeScreenshot(user);
            ctx.Users.Add(user);
            ctx.Screenshots.Add(screenshot);
            ctx.SaveChanges();

            var result = svc.GetById(screenshot.ScreenshotId);

            Assert.NotNull(result);
            Assert.Equal(screenshot.ScreenshotId, result!.ScreenshotId);
        }

        [Fact]
        public void GetById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetById_NonExistentId_ReturnsNull));

            Assert.Null(svc.GetById(999));
        }

        [Fact]
        public void GetById_IncludesUser()
        {
            var (ctx, svc) = Build(nameof(GetById_IncludesUser));
            var user = MakeUser();
            var screenshot = MakeScreenshot(user);
            ctx.Users.Add(user);
            ctx.Screenshots.Add(screenshot);
            ctx.SaveChanges();

            var result = svc.GetById(screenshot.ScreenshotId);

            Assert.NotNull(result!.User);
            Assert.Equal("Alice", result.User.UserName);
        }

        // ──────────────────────────────────────────────────────────────
        // GetLatestScreenshots
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetLatestScreenshots_ReturnsNewestFirst()
        {
            var (ctx, svc) = Build(nameof(GetLatestScreenshots_ReturnsNewestFirst));
            var user = MakeUser();
            var older = MakeScreenshot(user, 1, "Older");
            var newer = MakeScreenshot(user, 2, "Newer");
            older.CreatedOn = DateTime.UtcNow.AddDays(-5);
            newer.CreatedOn = DateTime.UtcNow;

            ctx.Users.Add(user);
            ctx.Screenshots.AddRange(older, newer);
            ctx.SaveChanges();

            var result = svc.GetLatestScreenshots(1).ToList();

            Assert.Single(result);
            Assert.Equal("Newer", result[0].ScreenshotTitle);
        }

        [Fact]
        public void GetLatestScreenshots_RespectsNLimit()
        {
            var (ctx, svc) = Build(nameof(GetLatestScreenshots_RespectsNLimit));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.Screenshots.AddRange(
                MakeScreenshot(user, 1),
                MakeScreenshot(user, 2),
                MakeScreenshot(user, 3),
                MakeScreenshot(user, 4));
            ctx.SaveChanges();

            var result = svc.GetLatestScreenshots(2).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetLatestScreenshots_EmptyDatabase_ReturnsEmpty()
        {
            var (_, svc) = Build(nameof(GetLatestScreenshots_EmptyDatabase_ReturnsEmpty));

            Assert.Empty(svc.GetLatestScreenshots(5));
        }

        // ──────────────────────────────────────────────────────────────
        // GetAllUsers / GetUserById
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetAllUsers_ReturnsAllUsers()
        {
            var (ctx, svc) = Build(nameof(GetAllUsers_ReturnsAllUsers));
            ctx.Users.AddRange(MakeUser("1", "Alice"), MakeUser("2", "Bob"));
            ctx.SaveChanges();

            Assert.Equal(2, svc.GetAllUsers().Count());
        }

        [Fact]
        public void GetUserById_ExistingId_ReturnsUser()
        {
            var (ctx, svc) = Build(nameof(GetUserById_ExistingId_ReturnsUser));
            var user = MakeUser();
            ctx.Users.Add(user);
            ctx.SaveChanges();

            var result = svc.GetUserById(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public void GetUserById_NonExistentId_ReturnsNull()
        {
            var (_, svc) = Build(nameof(GetUserById_NonExistentId_ReturnsNull));

            Assert.Null(svc.GetUserById("nonexistent"));
        }

        // ──────────────────────────────────────────────────────────────
        // SetScreenshotImage
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task SetScreenshotImage_UpdatesImagePath()
        {
            var (ctx, svc) = Build(nameof(SetScreenshotImage_UpdatesImagePath));
            var user = MakeUser();
            var screenshot = MakeScreenshot(user);
            ctx.Users.Add(user);
            ctx.Screenshots.Add(screenshot);
            await ctx.SaveChangesAsync();

            var newUri = new Uri("https://storage.blob.core.windows.net/images/new.png");
            await svc.SetScreenshotImage(screenshot.ScreenshotId, newUri);

            var updated = await ctx.Screenshots.FindAsync(screenshot.ScreenshotId);
            Assert.Equal(newUri.AbsoluteUri, updated!.ImagePath);
        }

        [Fact]
        public async Task SetScreenshotImage_OverwritesPreviousImage()
        {
            var (ctx, svc) = Build(nameof(SetScreenshotImage_OverwritesPreviousImage));
            var user = MakeUser();
            var screenshot = MakeScreenshot(user, imagePath: "https://old.com/old.png");
            ctx.Users.Add(user);
            ctx.Screenshots.Add(screenshot);
            await ctx.SaveChangesAsync();

            var newUri = new Uri("https://storage.blob.core.windows.net/images/updated.png");
            await svc.SetScreenshotImage(screenshot.ScreenshotId, newUri);

            var updated = await ctx.Screenshots.FindAsync(screenshot.ScreenshotId);
            Assert.Equal(newUri.AbsoluteUri, updated!.ImagePath);
        }

        [Fact]
        public async Task SetScreenshotImage_NonExistentId_DoesNotThrow()
        {
            var (_, svc) = Build(nameof(SetScreenshotImage_NonExistentId_DoesNotThrow));

            var ex = await Record.ExceptionAsync(() =>
                svc.SetScreenshotImage(999, new Uri("https://storage.blob.core.windows.net/images/x.png")));

            Assert.Null(ex);
        }
    }
}