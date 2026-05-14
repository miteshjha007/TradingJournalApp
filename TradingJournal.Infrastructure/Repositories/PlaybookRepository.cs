using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class PlaybookRepository : IPlaybookRepository
{
    private readonly ApplicationDbContext _db;
    public PlaybookRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<PlaybookRule>> GetByUserIdAsync(Guid userId) =>
        await _db.PlaybookRules.Where(r => r.UserId == userId)
            .OrderBy(r => r.OrderIndex).ToListAsync();

    public async Task<PlaybookRule?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.PlaybookRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    public async Task<PlaybookRule> CreateAsync(PlaybookRule rule)
    {
        _db.PlaybookRules.Add(rule);
        await _db.SaveChangesAsync();
        return rule;
    }

    public async Task<PlaybookRule> UpdateAsync(PlaybookRule rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _db.PlaybookRules.Update(rule);
        await _db.SaveChangesAsync();
        return rule;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var rule = await _db.PlaybookRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (rule != null) { rule.IsDeleted = true; await _db.SaveChangesAsync(); }
    }

    public async Task ReorderAsync(Guid userId, List<Guid> orderedIds)
    {
        var rules = await _db.PlaybookRules.Where(r => r.UserId == userId).ToListAsync();
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var rule = rules.FirstOrDefault(r => r.Id == orderedIds[i]);
            if (rule != null) { rule.OrderIndex = i; rule.UpdatedAt = DateTime.UtcNow; }
        }
        await _db.SaveChangesAsync();
    }
}

public class TradeChecklistRepository : ITradeChecklistRepository
{
    private readonly ApplicationDbContext _db;
    public TradeChecklistRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<TradeChecklist>> GetByTradeIdAsync(Guid tradeId) =>
        await _db.TradeChecklists.Where(c => c.TradeId == tradeId).ToListAsync();

    public async Task SaveChecklistAsync(Guid tradeId, List<Guid> checkedRuleIds, List<PlaybookRule> allRules)
    {
        var existing = await _db.TradeChecklists.Where(c => c.TradeId == tradeId).ToListAsync();
        _db.TradeChecklists.RemoveRange(existing);

        var newEntries = allRules.Select(r => new TradeChecklist
        {
            TradeId = tradeId,
            RuleId = r.Id,
            IsChecked = checkedRuleIds.Contains(r.Id)
        });
        _db.TradeChecklists.AddRange(newEntries);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteByTradeIdAsync(Guid tradeId)
    {
        var items = await _db.TradeChecklists.Where(c => c.TradeId == tradeId).ToListAsync();
        _db.TradeChecklists.RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}
