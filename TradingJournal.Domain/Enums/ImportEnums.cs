namespace TradingJournal.Domain.Enums;

public enum ImportSource
{
    CsvUpload = 1,
    Mt5Webhook = 2
}

public enum ImportStatus
{
    Success = 1,
    PartialSuccess = 2,
    Failed = 3
}