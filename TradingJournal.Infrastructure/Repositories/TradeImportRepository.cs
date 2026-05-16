using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class TradeImportRepository : ITradeImportRepository
{
    private readonly ApplicationDbContext _db;

    public TradeImportRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Mt5WebhookConfig?> GetConfigByUserIdAsync(Guid userId)
    {
        return await _db.Mt5WebhookConfigs
            .Include(c => c.DefaultTradingAccount)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Mt5WebhookConfig?> GetConfigByTokenAsync(string token)
    {
        return await _db.Mt5WebhookConfigs
            .Include(c => c.DefaultTradingAccount)
            .FirstOrDefaultAsync(c => c.WebhookToken == token);
    }

    public async Task<Mt5WebhookConfig> CreateConfigAsync(Mt5WebhookConfig config)
    {
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        _db.Mt5WebhookConfigs.Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    public async Task<Mt5WebhookConfig> UpdateConfigAsync(Mt5WebhookConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _db.Mt5WebhookConfigs.Update(config);
        await _db.SaveChangesAsync();
        return config;
    }

    public async Task<TradeImportLog> CreateLogAsync(TradeImportLog log)
    {
        log.Id = Guid.NewGuid();
        log.CreatedAt = DateTime.UtcNow;
        log.UpdatedAt = DateTime.UtcNow;
        _db.TradeImportLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<List<TradeImportLog>> GetLogsByUserIdAsync(Guid userId, int page, int pageSize)
    {
        return await _db.TradeImportLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> IsTicketAlreadyImportedAsync(Guid userId, long ticketNumber)
    {
        return await _db.Trades
            .AnyAsync(t => t.UserId == userId && t.Mt5TicketNumber == ticketNumber);
    }
}