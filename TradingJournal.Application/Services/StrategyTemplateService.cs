using TradingJournal.Application.DTOs.StrategyTemplates;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Services;

public class StrategyTemplateService : IStrategyTemplateService
{
    private readonly IStrategyTemplateRepository _repository;

    public StrategyTemplateService(IStrategyTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<StrategyTemplateDto>> GetTemplatesAsync(Guid userId, string? instrument)
    {
        var entities = await _repository.GetAllAsync(userId, instrument);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<StrategyTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<StrategyTemplateDto> CreateCustomTemplateAsync(Guid userId, CreateStrategyTemplateDto dto)
    {
        var entity = new StrategyTemplate
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Methodology = dto.Methodology,
            Instrument = dto.Instrument,
            Rules = dto.Rules,
            DefaultFilters = dto.DefaultFilters,
            SessionBadge = dto.SessionBadge,
            TimeframeBadge = dto.TimeframeBadge,
            MinRRR = dto.MinRRR,
            IsSystemTemplate = false,
            IsActive = true
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task UpdateCustomTemplateAsync(Guid userId, Guid id, CreateStrategyTemplateDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId || existing.IsSystemTemplate)
            throw new UnauthorizedAccessException("Cannot modify this template.");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Methodology = dto.Methodology;
        existing.Instrument = dto.Instrument;
        existing.Rules = dto.Rules;
        existing.DefaultFilters = dto.DefaultFilters;
        existing.SessionBadge = dto.SessionBadge;
        existing.TimeframeBadge = dto.TimeframeBadge;
        existing.MinRRR = dto.MinRRR;

        await _repository.UpdateAsync(existing);
    }

    public async Task DeleteCustomTemplateAsync(Guid userId, Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId || existing.IsSystemTemplate)
            throw new UnauthorizedAccessException("Cannot delete this template.");

        await _repository.DeleteAsync(id);
    }

    private static StrategyTemplateDto MapToDto(StrategyTemplate s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        Methodology = s.Methodology,
        Instrument = s.Instrument,
        Rules = s.Rules,
        DefaultFilters = s.DefaultFilters,
        SessionBadge = s.SessionBadge,
        TimeframeBadge = s.TimeframeBadge,
        MinRRR = s.MinRRR,
        IsSystemTemplate = s.IsSystemTemplate,
        IsActive = s.IsActive
    };
}
