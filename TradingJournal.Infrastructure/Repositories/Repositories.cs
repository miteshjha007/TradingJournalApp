using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;
    public UserRepository(ApplicationDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FindAsync(id);

    public async Task<List<User>> GetAllAsync() =>
        await _db.Users.ToListAsync();

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null) { user.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

public class InstrumentRepository : IInstrumentRepository
{
    private readonly ApplicationDbContext _db;
    public InstrumentRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<Instrument>> GetByUserIdAsync(Guid userId) =>
        await _db.Instruments.Where(i => i.UserId == userId).ToListAsync();

    public async Task<Instrument?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.Instruments.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    public async Task<Instrument> CreateAsync(Instrument instrument)
    {
        _db.Instruments.Add(instrument);
        await _db.SaveChangesAsync();
        return instrument;
    }

    public async Task<Instrument> UpdateAsync(Instrument instrument)
    {
        _db.Instruments.Update(instrument);
        await _db.SaveChangesAsync();
        return instrument;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var item = await _db.Instruments.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (item != null) { item.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

public class TradeRepository : ITradeRepository
{
    private readonly ApplicationDbContext _db;
    public TradeRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Trade> trades, int total)> GetPagedAsync(Guid userId, int page, int pageSize,
        DateTime? fromDate, DateTime? toDate, Guid? instrumentId, string? result, string? tradeType)
    {
        var query = _db.Trades.Include(t => t.Instrument)
            .Where(t => t.UserId == userId);

        if (fromDate.HasValue) query = query.Where(t => t.TradeDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(t => t.TradeDate <= toDate.Value);
        if (instrumentId.HasValue) query = query.Where(t => t.InstrumentId == instrumentId.Value);
        if (!string.IsNullOrEmpty(result) && Enum.TryParse<Domain.Enums.TradeResult>(result, true, out var r))
            query = query.Where(t => t.Result == r);
        if (!string.IsNullOrEmpty(tradeType) && Enum.TryParse<Domain.Enums.TradeType>(tradeType, true, out var tt))
            query = query.Where(t => t.TradeType == tt);

        var total = await query.CountAsync();
        var trades = await query.OrderByDescending(t => t.TradeDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (trades, total);
    }

    public async Task<Trade?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.Trades.Include(t => t.Instrument)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    public async Task<List<Trade>> GetByUserIdAsync(Guid userId) =>
        await _db.Trades.Include(t => t.Instrument)
            .Where(t => t.UserId == userId).OrderBy(t => t.TradeDate).ToListAsync();

    public async Task<List<Trade>> GetByDateRangeAsync(Guid userId, DateTime from, DateTime to) =>
        await _db.Trades.Include(t => t.Instrument)
            .Where(t => t.UserId == userId && t.TradeDate >= from && t.TradeDate <= to)
            .OrderBy(t => t.TradeDate).ToListAsync();

    public async Task<Trade> CreateAsync(Trade trade)
    {
        _db.Trades.Add(trade);
        await _db.SaveChangesAsync();
        return trade;
    }

    public async Task<Trade> UpdateAsync(Trade trade)
    {
        _db.Trades.Update(trade);
        await _db.SaveChangesAsync();
        return trade;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var t = await _db.Trades.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (t != null) { t.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

public class NoteRepository : INoteRepository
{
    private readonly ApplicationDbContext _db;
    public NoteRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Note> notes, int total)> GetPagedAsync(Guid userId, int page, int pageSize, string? search)
    {
        var query = _db.Notes.Where(n => n.UserId == userId);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(n => n.Title.Contains(search) || n.Content.Contains(search));

        var total = await query.CountAsync();
        var notes = await query.OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (notes, total);
    }

    public async Task<Note?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

    public async Task<Note> CreateAsync(Note note)
    {
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return note;
    }

    public async Task<Note> UpdateAsync(Note note)
    {
        _db.Notes.Update(note);
        await _db.SaveChangesAsync();
        return note;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (n != null) { n.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}

public class AlertRepository : IAlertRepository
{
    private readonly ApplicationDbContext _db;
    public AlertRepository(ApplicationDbContext db) => _db = db;

    public async Task<Alert?> GetByUserIdAsync(Guid userId) =>
        await _db.Alerts.FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<Alert> UpsertAsync(Alert alert)
    {
        var existing = await _db.Alerts.FirstOrDefaultAsync(a => a.UserId == alert.UserId);
        if (existing == null) _db.Alerts.Add(alert);
        else _db.Alerts.Update(alert);
        await _db.SaveChangesAsync();
        return alert;
    }
}
