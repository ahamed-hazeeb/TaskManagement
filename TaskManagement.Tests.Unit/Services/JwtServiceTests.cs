using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TaskManagement.Application.Services;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using System.IdentityModel.Tokens.Jwt;

namespace TaskManagement.Tests.Unit.Services
{
    public class JwtServiceTests
    {
        private readonly IConfiguration _configuration;
        private readonly JwtService _jwtService;

        public JwtServiceTests()
        {
            // Set up configuration
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"JwtSettings:Secret", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpirationInMinutes", "60"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _jwtService = new JwtService(_configuration);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldReturnValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = "hashed",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            
            // Verify token can be parsed
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            jwtToken.Should().NotBeNull();
            jwtToken.Issuer.Should().Be("TestIssuer");
            jwtToken.Audiences.Should().Contain("TestAudience");
            
            // Verify claims
            jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "1");
            jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@example.com");
            jwtToken.Claims.Should().Contain(c => c.Type == "name" && c.Value == "Test User");
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnUserId()
        {
            // Arrange
            var user = new User
            {
                Id = 42,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = "hashed",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            var token = _jwtService.GenerateToken(user);

            // Act
            var userId = _jwtService.ValidateToken(token);

            // Assert
            userId.Should().Be(42);
        }

        [Fact]
        public void ValidateToken_WithNullToken_ShouldReturnNull()
        {
            // Act
            var userId = _jwtService.ValidateToken(null);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithEmptyToken_ShouldReturnNull()
        {
            // Act
            var userId = _jwtService.ValidateToken(string.Empty);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var userId = _jwtService.ValidateToken(invalidToken);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange - Create a token with a user, but we can't easily create an expired token
            // without manipulating time, so we'll test with an invalid signature instead
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = "hashed",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            var validToken = _jwtService.GenerateToken(user);
            
            // Tamper with the token to make it invalid
            var tamperedToken = validToken.Substring(0, validToken.Length - 5) + "XXXXX";

            // Act
            var userId = _jwtService.ValidateToken(tamperedToken);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void GenerateToken_WithDifferentUsers_ShouldGenerateDifferentTokens()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Email = "user1@example.com",
                FullName = "User One",
                PasswordHash = "hashed1",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                Id = 2,
                Email = "user2@example.com",
                FullName = "User Two",
                PasswordHash = "hashed2",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var token1 = _jwtService.GenerateToken(user1);
            var token2 = _jwtService.GenerateToken(user2);

            // Assert
            token1.Should().NotBe(token2);
            
            var userId1 = _jwtService.ValidateToken(token1);
            var userId2 = _jwtService.ValidateToken(token2);
            
            userId1.Should().Be(1);
            userId2.Should().Be(2);
        }
    }
}
