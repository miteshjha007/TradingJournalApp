using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllAsync();
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<List<User>> GetAllExceptAsync(Guid currentUserId);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}

public interface IInstrumentRepository
{
    Task<List<Instrument>> GetByUserIdAsync(Guid userId);
    Task<Instrument?> GetByIdAsync(Guid id, Guid userId);
    Task<Instrument> CreateAsync(Instrument instrument);
    Task<Instrument> UpdateAsync(Instrument instrument);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ITradeRepository
{
    Task<(List<Trade> trades, int total)> GetPagedAsync(Guid userId, int page, int pageSize,
        DateTime? fromDate, DateTime? toDate, Guid? instrumentId, string? result, string? tradeType);
    Task<Trade?> GetByIdAsync(Guid id, Guid userId);
    Task<List<Trade>> GetByUserIdAsync(Guid userId);
    Task<List<Trade>> GetByDateRangeAsync(Guid userId, DateTime from, DateTime to);
    Task<Trade> CreateAsync(Trade trade);
    Task<Trade> UpdateAsync(Trade trade);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface INoteRepository
{
    Task<(List<Note> notes, int total)> GetPagedAsync(Guid userId, int page, int pageSize, string? search);
    Task<Note?> GetByIdAsync(Guid id, Guid userId);
    Task<Note> CreateAsync(Note note);
    Task<Note> UpdateAsync(Note note);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IAlertRepository
{
    Task<Alert?> GetByUserIdAsync(Guid userId);
    Task<Alert> UpsertAsync(Alert alert);
}

public interface ITradingAccountRepository
{
    Task<List<TradingAccount>> GetByUserIdAsync(Guid userId);
    Task<TradingAccount?> GetByIdAsync(Guid id, Guid userId);
    Task<TradingAccount> CreateAsync(TradingAccount account);
    Task<TradingAccount> UpdateAsync(TradingAccount account);
    Task DeleteAsync(Guid id, Guid userId);
    Task<TradingAccount?> GetDefaultByUserIdAsync(Guid userId);
}
