using TradingJournal.Application.DTOs.Trades;

namespace TradingJournal.Application.Interfaces;

public interface ITradeService
{
    Task<PagedTradesDto> GetAllAsync(Guid userId, TradeFilterDto filter);
    Task<TradeDto?> GetByIdAsync(Guid id, Guid userId);
    Task<TradeDto> CreateAsync(CreateTradeDto dto, Guid userId);
    Task<TradeDto> UpdateAsync(Guid id, UpdateTradeDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<List<TradeDto>> GetForCalendarAsync(Guid userId, int year, int month);
    Task<byte[]> ExportToCsvAsync(Guid userId, TradeFilterDto filter);
}
