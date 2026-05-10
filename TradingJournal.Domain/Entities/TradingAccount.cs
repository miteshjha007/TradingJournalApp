namespace TradingJournal.Domain.Entities;

public class TradingAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Broker { get; set; }
    public bool IsDefault { get; set; } = false;

    // Prop Firm Configurations
    public bool IsPropFirm { get; set; } = false;
    public string? PropFirmName { get; set; }   // e.g. "Funding Pips", "FTMO", "The5ers"
    public string? PropFirmPlan { get; set; }   // e.g. "$10,000 — 2 Step"
    public int MinTradingDays { get; set; } = 0; // Minimum days required to pass challenge
    public bool NewsTradeAllowed { get; set; } = true;
    public bool WeekendHoldingAllowed { get; set; } = false;
    public decimal DailyDrawdownLimitPct { get; set; } = 3.0m;
    public decimal MaxOverallLossPct { get; set; } = 6.0m;
    public decimal ProfitTargetPct { get; set; } = 10.0m;
    public decimal ProfitSplitPct { get; set; } = 80.0m;
    public decimal MaxRiskPerTradePctOfDailyLimit { get; set; } = 40.0m; // The 40% rule
    public decimal MaxAllowedLotSize { get; set; } = 5.0m;
    public bool UseDynamicEquity { get; set; } = true;
    public bool Has5xLotRule { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
