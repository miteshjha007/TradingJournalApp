using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class TradingAccountRepository : ITradingAccountRepository
{
    private readonly ApplicationDbContext _context;

    public TradingAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TradingAccount>> GetByUserIdAsync(Guid userId)
    {
        return await _context.TradingAccounts
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<TradingAccount?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.TradingAccounts
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);
    }

    public async Task<TradingAccount> CreateAsync(TradingAccount account)
    {
        if (account.IsDefault)
        {
            await ClearDefaultAsync(account.UserId);
        }

        _context.TradingAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<TradingAccount> UpdateAsync(TradingAccount account)
    {
        if (account.IsDefault)
        {
            await ClearDefaultAsync(account.UserId, account.Id);
        }

        account.UpdatedAt = DateTime.UtcNow;
        _context.TradingAccounts.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var account = await GetByIdAsync(id, userId);
        if (account != null)
        {
            account.IsDeleted = true;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TradingAccount?> GetDefaultByUserIdAsync(Guid userId)
    {
        var account = await _context.TradingAccounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault && !x.IsDeleted);

        if (account == null)
        {
            // Fallback to first available account if no default is explicitly set
            account = await _context.TradingAccounts
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        return account;
    }

    private async Task ClearDefaultAsync(Guid userId, Guid? excludeId = null)
    {
        var query = _context.TradingAccounts
            .Where(x => x.UserId == userId && x.IsDefault && !x.IsDeleted);
            
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        var defaults = await query.ToListAsync();
        foreach (var def in defaults)
        {
            def.IsDefault = false;
            def.UpdatedAt = DateTime.UtcNow;
        }
    }
}
