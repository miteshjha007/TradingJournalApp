namespace TradingJournal.Application.Interfaces;

public interface IStorageService
{
    Task<string> SaveChartAsync(Stream imageStream, string contentType, Guid userId, Guid tradeId);
    Task DeleteChartAsync(Guid userId, Guid tradeId);
    bool ChartExists(Guid userId, Guid tradeId);
    string GetChartUrl(Guid userId, Guid tradeId);
}
