using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IPlaybookRepository
{
    Task<List<PlaybookRule>> GetByUserIdAsync(Guid userId);
    Task<PlaybookRule?> GetByIdAsync(Guid id, Guid userId);
    Task<PlaybookRule> CreateAsync(PlaybookRule rule);
    Task<PlaybookRule> UpdateAsync(PlaybookRule rule);
    Task DeleteAsync(Guid id, Guid userId);
    Task ReorderAsync(Guid userId, List<Guid> orderedIds);
}

public interface ITradeChecklistRepository
{
    Task<List<TradeChecklist>> GetByTradeIdAsync(Guid tradeId);
    Task SaveChecklistAsync(Guid tradeId, List<Guid> checkedRuleIds, List<PlaybookRule> allRules);
    Task DeleteByTradeIdAsync(Guid tradeId);
}
