using TradingJournal.Application.DTOs.Auth;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;
using BCrypt.Net;

namespace TradingJournal.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IStreakService _streakService;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IStreakService streakService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _streakService = streakService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.User,
            RefreshToken = _jwtService.GenerateRefreshToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await _userRepository.CreateAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        await _streakService.UpdateStreakOnLoginAsync(user.Id);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return BuildResponse(user);
    }

    public async Task RevokeTokenAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<UserInfoDto> AdminCreateUserAsync(AdminCreateUserDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var role = Enum.TryParse<UserRole>(dto.Role, out var parsedRole) ? parsedRole : UserRole.User;

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            AllowedSections = dto.AllowedSections
        };

        await _userRepository.CreateAsync(user);

        return new UserInfoDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            AccountBalance = user.AccountBalance,
            AllowedSections = user.AllowedSections
        };
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(string idToken)
    {
        try
        {
            var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string> { "138510759100-mv83uftop7vjjea99lldjr1fhnue1d4l.apps.googleusercontent.com" }
            };
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var email = payload.Email.ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    FirstName = payload.GivenName ?? "Google",
                    LastName = payload.FamilyName ?? "User",
                    Email = email,
                    PasswordHash = string.Empty, // No password for Google users
                    Role = UserRole.User,
                    IsActive = true,
                    RefreshToken = _jwtService.GenerateRefreshToken(),
                    RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
                    ProfileImageUrl = payload.Picture
                };
                await _userRepository.CreateAsync(user);
            }
            else
            {
                if (!user.IsActive)
                    throw new UnauthorizedAccessException("Account is deactivated.");

                user.RefreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                if (string.IsNullOrEmpty(user.ProfileImageUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.ProfileImageUrl = payload.Picture;
                }
                await _userRepository.UpdateAsync(user);
            }

            await _streakService.UpdateStreakOnLoginAsync(user.Id);

            return BuildResponse(user);
        }
        catch (Google.Apis.Auth.InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Invalid Google token.");
        }
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken!,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserInfoDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AccountBalance = user.AccountBalance,
                AllowedSections = user.AllowedSections
            }
        };
    }
}
