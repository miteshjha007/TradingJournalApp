using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class BacktestRepository : IBacktestRepository
{
    private readonly ApplicationDbContext _db;
    public BacktestRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<BacktestResult>> GetByUserIdAsync(Guid userId) =>
        await _db.BacktestResults.Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<BacktestResult?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.BacktestResults.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    public async Task<BacktestResult> CreateAsync(BacktestResult result)
    {
        _db.BacktestResults.Add(result);
        await _db.SaveChangesAsync();
        return result;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var r = await _db.BacktestResults.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (r != null) { r.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}
