using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token);
    Guid GetUserIdFromToken(string token);
}
