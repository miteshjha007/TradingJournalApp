using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class TradeImportLog : BaseEntity
{
    public Guid UserId { get; set; }
    public ImportSource Source { get; set; }
    public int TotalReceived { get; set; }
    public int TotalInserted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalFailed { get; set; }
    public string? FileName { get; set; }
    public string? ErrorSummary { get; set; }
    public string? InsertedTradeIds { get; set; }
    public string? SkippedReasons { get; set; }
    public ImportStatus Status { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}