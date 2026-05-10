namespace TradingJournal.Application.DTOs.Dashboard;

/// <summary>
/// Real-time Prop Firm rule compliance status for the dashboard card.
/// Powers the live 🟢🟡🔴 status indicator.
/// </summary>
public class PropFirmStatusDto
{
    // Account info
    public string FirmName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal AccountBalance { get; set; }

    // Daily Drawdown
    public decimal DailyLossUsed { get; set; }         // $ lost today (positive value)
    public decimal DailyLossLimit { get; set; }         // $ max daily loss allowed
    public decimal DailyLossUsedPct { get; set; }       // % of daily limit used (0–100)
    public decimal RemainingDailyBudget { get; set; }   // $ still can lose today

    // Overall Drawdown
    public decimal TotalDrawdown { get; set; }          // $ total drawdown from peak
    public decimal MaxDrawdownLimit { get; set; }       // $ max overall loss allowed
    public decimal TotalDrawdownPct { get; set; }       // % of overall limit used (0–100)
    public decimal RemainingOverallBudget { get; set; } // $ still can lose overall

    // Profit Target
    public decimal ProfitEarned { get; set; }           // $ total P&L earned
    public decimal ProfitTarget { get; set; }           // $ profit target
    public decimal ProfitEarnedPct { get; set; }        // % of target achieved (0–100+)
    public decimal EstimatedPayout { get; set; }        // $ payout at current profit * split %

    // Trading Days (challenge requirement)
    public int TradingDaysCompleted { get; set; }
    public int MinTradingDaysRequired { get; set; }

    // Rule Flags
    public bool NewsTradeAllowed { get; set; }
    public bool WeekendHoldingAllowed { get; set; }
    public bool Has5xLotRule { get; set; }
    public bool UseDynamicEquity { get; set; }

    // Status
    /// <summary>SAFE | WARNING | CRITICAL | BREACHED_DAILY | BREACHED_OVERALL | PASSED</summary>
    public string AccountStatus { get; set; } = "SAFE";

    public bool DailyLimitBreached { get; set; }
    public bool OverallLimitBreached { get; set; }
    public bool ProfitTargetReached { get; set; }

    /// <summary>Human-readable warnings to display on the card</summary>
    public List<string> ActiveWarnings { get; set; } = new();

    /// <summary>Hex color for status badge: green / amber / orange / red</summary>
    public string StatusColor { get; set; } = "#10b981";
}

/// <summary>
/// Static prop firm preset — returned from /api/propfirm/presets
/// Lets the frontend auto-fill account settings based on firm + plan selection.
/// </summary>
public class PropFirmPresetDto
{
    public string FirmName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal AccountSize { get; set; }
    public decimal DailyDrawdownLimitPct { get; set; }
    public decimal MaxOverallLossPct { get; set; }
    public decimal ProfitTargetPct { get; set; }
    public decimal ProfitSplitPct { get; set; }
    public int MinTradingDays { get; set; }
    public decimal MaxAllowedLotSize { get; set; }
    public bool Has5xLotRule { get; set; }
    public bool UseDynamicEquity { get; set; }
    public bool NewsTradeAllowed { get; set; }
    public bool WeekendHoldingAllowed { get; set; }
    public decimal MaxRiskPerTradePctOfDailyLimit { get; set; }
}
