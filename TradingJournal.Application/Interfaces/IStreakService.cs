using TradingJournal.Application.DTOs.Dashboard;

namespace TradingJournal.Application.Interfaces;

public interface IStreakService
{
    Task<StreakDto> GetStreakAsync(Guid userId);
    Task UpdateStreakOnLoginAsync(Guid userId);
}
