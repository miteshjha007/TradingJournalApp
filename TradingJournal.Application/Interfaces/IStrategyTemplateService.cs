using TradingJournal.Application.DTOs.StrategyTemplates;

namespace TradingJournal.Application.Interfaces;

public interface IStrategyTemplateService
{
    Task<List<StrategyTemplateDto>> GetTemplatesAsync(Guid userId, string? instrument);
    Task<StrategyTemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<StrategyTemplateDto> CreateCustomTemplateAsync(Guid userId, CreateStrategyTemplateDto dto);
    Task UpdateCustomTemplateAsync(Guid userId, Guid id, CreateStrategyTemplateDto dto);
    Task DeleteCustomTemplateAsync(Guid userId, Guid id);
}
