using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class StrategyTemplateRepository : IStrategyTemplateRepository
{
    private readonly ApplicationDbContext _db;
    public StrategyTemplateRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<StrategyTemplate>> GetAllAsync(Guid userId, string? instrument)
    {
        var query = _db.StrategyTemplates
            .Where(s => s.IsSystemTemplate || s.UserId == userId);

        if (!string.IsNullOrEmpty(instrument))
            query = query.Where(s => s.Instrument == instrument);

        return await query.ToListAsync();
    }

    public async Task<StrategyTemplate?> GetByIdAsync(Guid id) =>
        await _db.StrategyTemplates.FindAsync(id);

    public async Task<StrategyTemplate> CreateAsync(StrategyTemplate template)
    {
        _db.StrategyTemplates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }

    public async Task<StrategyTemplate> UpdateAsync(StrategyTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _db.StrategyTemplates.Update(template);
        await _db.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _db.StrategyTemplates.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
