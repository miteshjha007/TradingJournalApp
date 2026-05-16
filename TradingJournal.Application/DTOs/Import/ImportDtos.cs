namespace TradingJournal.Application.DTOs.Import;

public class CsvImportRequestDto
{
    public string CsvContent { get; set; } = string.Empty;
    public Guid? TradingAccountId { get; set; }
    public Guid? ForceInstrumentId { get; set; }
}

public class CsvImportPreviewDto
{
    public List<ParsedTradeDto> ValidTrades { get; set; } = new();
    public List<ParsedTradeDto> DuplicateTrades { get; set; } = new();
    public List<CsvParseErrorDto> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public string CsvFormat { get; set; } = string.Empty;
}

public class ParsedTradeDto
{
    public string Symbol { get; set; } = string.Empty;
    public string MappedInstrumentName { get; set; } = string.Empty;
    public Guid? InstrumentId { get; set; }
    public string TradeType { get; set; } = string.Empty;
    public decimal LotSize { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? Comment { get; set; }
    public bool IsDuplicate { get; set; }
    public string? DuplicateReason { get; set; }
}

public class CsvParseErrorDto
{
    public int RowNumber { get; set; }
    public string RawLine { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class CsvImportConfirmDto
{
    public string CsvContent { get; set; } = string.Empty;
    public Guid? TradingAccountId { get; set; }
    public Guid? ForceInstrumentId { get; set; }
    public bool SkipDuplicates { get; set; } = true;
}

public class ImportResultDto
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> SkippedReasons { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public Guid ImportLogId { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class Mt5WebhookConfigDto
{
    public Guid Id { get; set; }
    public string WebhookToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public Guid? DefaultTradingAccountId { get; set; }
    public string? DefaultTradingAccountName { get; set; }
    public Dictionary<string, string> InstrumentMappings { get; set; } = new();
    public DateTime? LastUsedAt { get; set; }
    public int TotalTradesImported { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
}

public class CreateMt5ConfigDto
{
    public string? Description { get; set; }
    public Guid? DefaultTradingAccountId { get; set; }
    public Dictionary<string, string> InstrumentMappings { get; set; } = new();
}

public class UpdateMt5ConfigDto
{
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public Guid? DefaultTradingAccountId { get; set; }
    public Dictionary<string, string> InstrumentMappings { get; set; } = new();
}

public class Mt5TradePayloadDto
{
    public string Symbol { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal Lots { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal Profit { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public long TicketNumber { get; set; }
    public string MagicNumber { get; set; } = "0";
}

public class ImportLogDto
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public int TotalReceived { get; set; }
    public int TotalInserted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalFailed { get; set; }
    public string? FileName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}