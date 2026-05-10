namespace TradingJournal.Domain.Entities;

/// <summary>
/// Seeded reference data for well-known prop firm rule configurations.
/// Users select a firm + plan to auto-fill their TradingAccount settings.
/// Seeded in DbSeeder.cs — add new firms there without needing migrations.
/// </summary>
public class PropFirmPreset : BaseEntity
{
    public string FirmName { get; set; } = string.Empty;    // "Funding Pips", "FTMO"
    public string PlanName { get; set; } = string.Empty;    // "$10,000 — 2 Step"
    public decimal AccountSize { get; set; }

    // Core Rules
    public decimal DailyDrawdownLimitPct { get; set; }      // e.g. 4 = 4%
    public decimal MaxOverallLossPct { get; set; }          // e.g. 8 = 8%
    public decimal ProfitTargetPct { get; set; }            // e.g. 8 = 8%
    public decimal ProfitSplitPct { get; set; }             // e.g. 80 = 80%
    public int MinTradingDays { get; set; }                 // 0 = no requirement

    // Lot Rules
    public decimal MaxAllowedLotSize { get; set; }
    public bool Has5xLotRule { get; set; }
    public decimal MaxRiskPerTradePctOfDailyLimit { get; set; } // 40 = the 40% rule

    // Account Rules
    public bool UseDynamicEquity { get; set; }
    public bool NewsTradeAllowed { get; set; }
    public bool WeekendHoldingAllowed { get; set; }

    public bool IsActive { get; set; } = true;
}
