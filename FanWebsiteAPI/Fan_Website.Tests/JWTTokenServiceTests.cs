using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Fan_Website;
using FanWebsiteAPI.Service;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Fan_Website.Tests
{
    public class JwtTokenServiceTests
    {
        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private static IConfiguration BuildConfig(
            string secretKey = "ThisIsAVeryLongSecretKeyForTesting123!",
            string issuer = "TestIssuer",
            string audience = "TestAudience")
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Jwt:SecretKey", secretKey },
                { "Jwt:Issuer",    issuer    },
                { "Jwt:Audience",  audience  }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private static ApplicationUser MakeUser(
            string id = "user-1",
            string email = "matthew@example.com",
            string userName = "Matthew") =>
            new ApplicationUser
            {
                Id = id,
                Email = email,
                UserName = userName
            };

        private JwtSecurityToken ParseToken(string token)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(token);
        }

        // ──────────────────────────────────────────────────────────────
        // Token Generation
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_ReturnsNonEmptyString()
        {
            var svc = new JwtTokenService(BuildConfig());
            var token = svc.GenerateToken(MakeUser());

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_ReturnsValidJwtFormat()
        {
            var svc = new JwtTokenService(BuildConfig());
            var token = svc.GenerateToken(MakeUser());

            // A JWT has exactly 3 parts separated by dots
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
        }

        // ──────────────────────────────────────────────────────────────
        // Claims
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_ContainsNameIdentifierClaim()
        {
            var user = MakeUser();
            var svc = new JwtTokenService(BuildConfig());
            var token = ParseToken(svc.GenerateToken(user));

            var claim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Assert.NotNull(claim);
            Assert.Equal(user.Id, claim!.Value);
        }

        [Fact]
        public void GenerateToken_ContainsEmailClaim()
        {
            var user = MakeUser();
            var svc = new JwtTokenService(BuildConfig());
            var token = ParseToken(svc.GenerateToken(user));

            var claim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            Assert.NotNull(claim);
            Assert.Equal(user.Email, claim!.Value);
        }

        [Fact]
        public void GenerateToken_ContainsNameClaim()
        {
            var user = MakeUser();
            var svc = new JwtTokenService(BuildConfig());
            var token = ParseToken(svc.GenerateToken(user));

            var claim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            Assert.NotNull(claim);
            Assert.Equal(user.UserName, claim!.Value);
        }

        [Fact]
        public void GenerateToken_ClaimsMatchDifferentUsers()
        {
            var svc = new JwtTokenService(BuildConfig());
            var user1 = MakeUser("id-1", "matthew@example.com", "Matthew");
            var user2 = MakeUser("id-2", "bob@example.com", "Bob");

            var token1 = ParseToken(svc.GenerateToken(user1));
            var token2 = ParseToken(svc.GenerateToken(user2));

            var email1 = token1.Claims.First(c => c.Type == ClaimTypes.Email).Value;
            var email2 = token2.Claims.First(c => c.Type == ClaimTypes.Email).Value;

            Assert.NotEqual(email1, email2);
        }

        // ──────────────────────────────────────────────────────────────
        // Issuer & Audience
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_HasCorrectIssuer()
        {
            var config = BuildConfig(issuer: "MyIssuer");
            var svc = new JwtTokenService(config);
            var token = ParseToken(svc.GenerateToken(MakeUser()));

            Assert.Equal("MyIssuer", token.Issuer);
        }

        [Fact]
        public void GenerateToken_HasCorrectAudience()
        {
            var config = BuildConfig(audience: "MyAudience");
            var svc = new JwtTokenService(config);
            var token = ParseToken(svc.GenerateToken(MakeUser()));

            Assert.Equal("MyAudience", token.Audiences.First());
        }

        // ──────────────────────────────────────────────────────────────
        // Expiry
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_ExpiresInApproximately24Hours()
        {
            var svc = new JwtTokenService(BuildConfig());
            var before = DateTime.UtcNow;
            var token = ParseToken(svc.GenerateToken(MakeUser()));
            var after = DateTime.UtcNow;

            var expectedExpiry = before.AddHours(24);

            // Allow a few seconds of tolerance
            Assert.True(token.ValidTo >= expectedExpiry.AddSeconds(-5));
            Assert.True(token.ValidTo <= after.AddHours(24).AddSeconds(5));
        }

        [Fact]
        public void GenerateToken_IsNotExpiredImmediately()
        {
            var svc = new JwtTokenService(BuildConfig());
            var token = ParseToken(svc.GenerateToken(MakeUser()));

            Assert.True(token.ValidTo > DateTime.UtcNow);
        }

        // ──────────────────────────────────────────────────────────────
        // Algorithm
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_UsesHmacSha256Algorithm()
        {
            var svc = new JwtTokenService(BuildConfig());
            var token = ParseToken(svc.GenerateToken(MakeUser()));

            Assert.Equal("HS256", token.Header.Alg);
        }

        // ──────────────────────────────────────────────────────────────
        // Different Config Values
        // ──────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("Issuer1", "Audience1")]
        [InlineData("Issuer2", "Audience2")]
        [InlineData("ProdIssuer", "ProdAudience")]
        public void GenerateToken_RespectsConfigValues(string issuer, string audience)
        {
            var config = BuildConfig(issuer: issuer, audience: audience);
            var svc = new JwtTokenService(config);
            var token = ParseToken(svc.GenerateToken(MakeUser()));

            Assert.Equal(issuer, token.Issuer);
            Assert.Equal(audience, token.Audiences.First());
        }

        // ──────────────────────────────────────────────────────────────
        // Two tokens for same user should differ (issued at different times)
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public void GenerateToken_TwoCallsSameUser_ProduceDifferentTokens()
        {
            var svc = new JwtTokenService(BuildConfig());
            var user = MakeUser();

            var token1 = svc.GenerateToken(user);
            Thread.Sleep(1000); // ensure different IssuedAt
            var token2 = svc.GenerateToken(user);

            Assert.NotEqual(token1, token2);
        }
    }
}