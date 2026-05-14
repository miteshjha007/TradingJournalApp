namespace TradingJournal.Domain.Entities;

public class BacktestResult : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyDescription { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? InstrumentId { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal TotalPL { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public decimal AverageRRR { get; set; }
    public DateTime? BestDay { get; set; }
    public DateTime? WorstDay { get; set; }
    public string ResultsJson { get; set; } = "[]";

    public User User { get; set; } = null!;
    public Instrument? Instrument { get; set; }
}
