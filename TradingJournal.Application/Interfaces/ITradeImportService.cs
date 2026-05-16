using TradingJournal.Application.DTOs.Import;

namespace TradingJournal.Application.Interfaces;

public interface ITradeImportService
{
    Task<CsvImportPreviewDto> ParseCsvAsync(CsvImportRequestDto dto, Guid userId);
    Task<ImportResultDto> ConfirmCsvImportAsync(CsvImportConfirmDto dto, Guid userId);
    Task<Mt5WebhookConfigDto> GetOrCreateConfigAsync(Guid userId);
    Task<Mt5WebhookConfigDto> UpdateConfigAsync(Guid userId, UpdateMt5ConfigDto dto);
    Task<ImportResultDto> ProcessMt5WebhookAsync(Mt5TradePayloadDto payload, string token);
    Task<List<ImportLogDto>> GetImportHistoryAsync(Guid userId, int page, int pageSize);
}