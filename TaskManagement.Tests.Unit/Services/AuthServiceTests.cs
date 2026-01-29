using Moq;
using FluentAssertions;
using TaskManagement.Application.Services;
using TaskManagement.Core.DTOs.Auth;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Interfaces;
using System.Linq.Expressions;

namespace TaskManagement.Tests.Unit.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _jwtServiceMock = new Mock<IJwtService>();
            _userRepositoryMock = new Mock<IRepository<User>>();
            
            _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
            
            _authService = new AuthService(_unitOfWorkMock.Object, _jwtServiceMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithValidRequest_ShouldCreateUserAndReturnAuthResponse()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                FullName = "Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            _userRepositoryMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) => user);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            _jwtServiceMock
                .Setup(j => j.GenerateToken(It.IsAny<User>()))
                .Returns("test-jwt-token");

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(request.Email);
            result.FullName.Should().Be(request.FullName);
            result.Role.Should().Be(UserRole.User);
            result.Token.Should().Be("test-jwt-token");
            
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "existing@example.com",
                FullName = "Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var existingUser = new User
            {
                Id = 1,
                Email = "existing@example.com",
                FullName = "Existing User",
                PasswordHash = "hashed",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            _userRepositoryMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(existingUser);

            // Act
            Func<Task> act = async () => await _authService.RegisterAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email already registered");
            
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            _userRepositoryMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            _jwtServiceMock
                .Setup(j => j.GenerateToken(It.IsAny<User>()))
                .Returns("test-jwt-token");

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.FullName.Should().Be(user.FullName);
            result.Token.Should().Be("test-jwt-token");
            
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            _userRepositoryMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password");
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUserDto()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = "hashed",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.FullName.Should().Be(user.FullName);
            result.Role.Should().Be(user.Role);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userId = 999;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.GetUserByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }
    }
}
