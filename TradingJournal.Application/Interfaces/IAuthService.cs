using TradingJournal.Application.DTOs.Auth;

namespace TradingJournal.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(Guid userId);
    Task<UserInfoDto> AdminCreateUserAsync(AdminCreateUserDto dto);
}
