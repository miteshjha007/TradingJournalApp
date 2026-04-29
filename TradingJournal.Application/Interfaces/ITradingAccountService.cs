using TradingJournal.Application.DTOs.Accounts;

namespace TradingJournal.Application.Interfaces;

public interface ITradingAccountService
{
    Task<List<TradingAccountDto>> GetUserAccountsAsync(Guid userId);
    Task<TradingAccountDto?> GetAccountByIdAsync(Guid id, Guid userId);
    Task<TradingAccountDto> CreateAccountAsync(CreateTradingAccountDto dto, Guid userId);
    Task<TradingAccountDto> UpdateAccountAsync(UpdateTradingAccountDto dto, Guid userId);
    Task DeleteAccountAsync(Guid id, Guid userId);
    Task<TradingAccountDto?> GetDefaultAccountAsync(Guid userId);
}
