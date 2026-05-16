using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class Trade : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid InstrumentId { get; set; }
    public decimal LotSize { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal RiskPercentage { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public DateTime TradeDate { get; set; }
    public int TradeDurationMinutes { get; set; }
    public TradeType TradeType { get; set; }
    public TradeResult Result { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public Guid? TradingAccountId { get; set; }
    public decimal? ChecklistCompliancePercent { get; set; }
    public string? ChartImageUrl { get; set; }
    public long? Mt5TicketNumber { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Instrument Instrument { get; set; } = null!;
    public TradingAccount? TradingAccount { get; set; }
    public ICollection<TradeChecklist> Checklists { get; set; } = new List<TradeChecklist>();
}
