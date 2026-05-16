using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IStrategyTemplateRepository
{
    Task<List<StrategyTemplate>> GetAllAsync(Guid userId, string? instrument);
    Task<StrategyTemplate?> GetByIdAsync(Guid id);
    Task<StrategyTemplate> CreateAsync(StrategyTemplate template);
    Task<StrategyTemplate> UpdateAsync(StrategyTemplate template);
    Task DeleteAsync(Guid id);
}
