using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Core.DTOs.Auth;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            var existingUser = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                throw new InvalidOperationException("Email already registered");

            // Create new user
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate token
            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by email
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password");

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate token
            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Token = token,
                ExpiresAt = expiresAt
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
