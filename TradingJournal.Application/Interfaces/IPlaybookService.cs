using TradingJournal.Application.DTOs.Playbook;

namespace TradingJournal.Application.Interfaces;

public interface IPlaybookService
{
    Task<List<PlaybookRuleDto>> GetRulesAsync(Guid userId);
    Task<PlaybookRuleDto> CreateRuleAsync(Guid userId, CreatePlaybookRuleDto dto);
    Task<PlaybookRuleDto> UpdateRuleAsync(Guid id, Guid userId, UpdatePlaybookRuleDto dto);
    Task DeleteRuleAsync(Guid id, Guid userId);
    Task ReorderRulesAsync(Guid userId, List<Guid> orderedIds);
    Task<List<TradeChecklistDto>> GetChecklistForTradeAsync(Guid tradeId, Guid userId);
    Task<decimal> SaveChecklistAsync(SaveChecklistDto dto, Guid userId);
}
