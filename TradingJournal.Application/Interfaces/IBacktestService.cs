using TradingJournal.Application.DTOs.Backtest;

namespace TradingJournal.Application.Interfaces;

public interface IBacktestService
{
    Task<BacktestResultDto> RunBacktestAsync(Guid userId, BacktestRequestDto dto);
    Task<List<BacktestResultDto>> GetBacktestHistoryAsync(Guid userId);
    Task<BacktestResultDto?> GetBacktestByIdAsync(Guid id, Guid userId);
    Task DeleteBacktestAsync(Guid id, Guid userId);
}

public interface IBacktestRepository
{
    Task<List<Domain.Entities.BacktestResult>> GetByUserIdAsync(Guid userId);
    Task<Domain.Entities.BacktestResult?> GetByIdAsync(Guid id, Guid userId);
    Task<Domain.Entities.BacktestResult> CreateAsync(Domain.Entities.BacktestResult result);
    Task DeleteAsync(Guid id, Guid userId);
}
