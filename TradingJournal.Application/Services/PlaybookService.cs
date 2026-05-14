using Microsoft.Extensions.Logging;
using TradingJournal.Application.DTOs.Playbook;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class PlaybookService : IPlaybookService
{
    private readonly IPlaybookRepository _playbookRepo;
    private readonly ITradeChecklistRepository _checklistRepo;
    private readonly ITradeRepository _tradeRepo;
    private readonly ILogger<PlaybookService> _logger;

    public PlaybookService(
        IPlaybookRepository playbookRepo,
        ITradeChecklistRepository checklistRepo,
        ITradeRepository tradeRepo,
        ILogger<PlaybookService> logger)
    {
        _playbookRepo = playbookRepo;
        _checklistRepo = checklistRepo;
        _tradeRepo = tradeRepo;
        _logger = logger;
    }

    public async Task<List<PlaybookRuleDto>> GetRulesAsync(Guid userId)
    {
        var rules = await _playbookRepo.GetByUserIdAsync(userId);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<PlaybookRuleDto> CreateRuleAsync(Guid userId, CreatePlaybookRuleDto dto)
    {
        var rule = new PlaybookRule
        {
            UserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            OrderIndex = dto.OrderIndex,
            IsActive = true
        };

        var created = await _playbookRepo.CreateAsync(rule);
        _logger.LogInformation("Playbook rule {RuleId} created for user {UserId}", created.Id, userId);
        return MapToDto(created);
    }

    public async Task<PlaybookRuleDto> UpdateRuleAsync(Guid id, Guid userId, UpdatePlaybookRuleDto dto)
    {
        var rule = await _playbookRepo.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException($"Rule {id} not found.");

        rule.Title = dto.Title;
        rule.Description = dto.Description;
        rule.Category = dto.Category;
        rule.IsActive = dto.IsActive;
        rule.OrderIndex = dto.OrderIndex;
        rule.UpdatedAt = DateTime.UtcNow;

        var updated = await _playbookRepo.UpdateAsync(rule);
        _logger.LogInformation("Playbook rule {RuleId} updated for user {UserId}", id, userId);
        return MapToDto(updated);
    }

    public async Task DeleteRuleAsync(Guid id, Guid userId)
    {
        await _playbookRepo.DeleteAsync(id, userId);
        _logger.LogInformation("Playbook rule {RuleId} deleted for user {UserId}", id, userId);
    }

    public async Task ReorderRulesAsync(Guid userId, List<Guid> orderedIds)
    {
        await _playbookRepo.ReorderAsync(userId, orderedIds);
        _logger.LogInformation("Playbook rules reordered for user {UserId}", userId);
    }

    public async Task<List<TradeChecklistDto>> GetChecklistForTradeAsync(Guid tradeId, Guid userId)
    {
        var rules = await _playbookRepo.GetByUserIdAsync(userId);
        var checklist = await _checklistRepo.GetByTradeIdAsync(tradeId);
        var checkedIds = checklist.Where(c => c.IsChecked).Select(c => c.RuleId).ToHashSet();

        return rules.Where(r => r.IsActive).Select(r => new TradeChecklistDto
        {
            RuleId = r.Id,
            Title = r.Title,
            Category = r.Category,
            CategoryName = r.Category.ToString(),
            IsChecked = checkedIds.Contains(r.Id)
        }).ToList();
    }

    public async Task<decimal> SaveChecklistAsync(SaveChecklistDto dto, Guid userId)
    {
        var rules = await _playbookRepo.GetByUserIdAsync(userId);
        var activeRules = rules.Where(r => r.IsActive).ToList();

        await _checklistRepo.SaveChecklistAsync(dto.TradeId, dto.CheckedRuleIds, activeRules);

        var compliance = activeRules.Count > 0
            ? Math.Round((decimal)dto.CheckedRuleIds.Count / activeRules.Count * 100, 2)
            : 0;

        var trade = await _tradeRepo.GetByIdAsync(dto.TradeId, userId);
        if (trade != null)
        {
            trade.ChecklistCompliancePercent = compliance;
            trade.UpdatedAt = DateTime.UtcNow;
            await _tradeRepo.UpdateAsync(trade);
        }

        _logger.LogInformation("Checklist saved for trade {TradeId}, compliance {Pct}%", dto.TradeId, compliance);
        return compliance;
    }

    private static PlaybookRuleDto MapToDto(PlaybookRule r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        Category = r.Category,
        CategoryName = r.Category.ToString(),
        IsActive = r.IsActive,
        OrderIndex = r.OrderIndex,
        CreatedAt = r.CreatedAt
    };
}
