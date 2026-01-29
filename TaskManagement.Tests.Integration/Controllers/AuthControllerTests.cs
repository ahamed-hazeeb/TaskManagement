using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TaskManagement.Core.DTOs.Auth;

namespace TaskManagement.Tests.Integration.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_WithValidRequest_ShouldReturn200AndAuthResponse()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = $"test{Guid.NewGuid()}@example.com",
                FullName = "Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authResponse.Should().NotBeNull();
            authResponse!.Email.Should().Be(request.Email);
            authResponse.FullName.Should().Be(request.FullName);
            authResponse.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ShouldReturn400()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "invalid-email",
                FullName = "Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithMismatchedPasswords_ShouldReturn400()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = $"test{Guid.NewGuid()}@example.com",
                FullName = "Test User",
                Password = "Password123",
                ConfirmPassword = "DifferentPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturn200AndAuthResponse()
        {
            // Arrange - First register a user
            var registerRequest = new RegisterRequest
            {
                Email = $"login{Guid.NewGuid()}@example.com",
                FullName = "Login Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            var loginRequest = new LoginRequest
            {
                Email = registerRequest.Email,
                Password = "Password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authResponse.Should().NotBeNull();
            authResponse!.Email.Should().Be(loginRequest.Email);
            authResponse.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturn401()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMe_WithValidToken_ShouldReturn200AndUserDto()
        {
            // Arrange - Register and get token
            var registerRequest = new RegisterRequest
            {
                Email = $"getme{Guid.NewGuid()}@example.com",
                FullName = "GetMe Test User",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

            // Add authorization header
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResponse!.Token}");

            // Act
            var response = await _client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
            userDto.Should().NotBeNull();
            userDto!.Email.Should().Be(registerRequest.Email);
            userDto.FullName.Should().Be(registerRequest.FullName);
        }

        [Fact]
        public async Task GetMe_WithoutToken_ShouldReturn401()
        {
            // Arrange - No token provided

            // Act
            var response = await _client.GetAsync("/api/v1/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
