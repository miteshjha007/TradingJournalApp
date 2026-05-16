using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface ITradeImportRepository
{
    Task<Mt5WebhookConfig?> GetConfigByUserIdAsync(Guid userId);
    Task<Mt5WebhookConfig?> GetConfigByTokenAsync(string token);
    Task<Mt5WebhookConfig> CreateConfigAsync(Mt5WebhookConfig config);
    Task<Mt5WebhookConfig> UpdateConfigAsync(Mt5WebhookConfig config);
    Task<TradeImportLog> CreateLogAsync(TradeImportLog log);
    Task<List<TradeImportLog>> GetLogsByUserIdAsync(Guid userId, int page, int pageSize);
    Task<bool> IsTicketAlreadyImportedAsync(Guid userId, long ticketNumber);
}