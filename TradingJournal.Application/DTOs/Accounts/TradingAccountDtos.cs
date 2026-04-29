namespace TradingJournal.Application.DTOs.Accounts;

public class TradingAccountDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Broker { get; set; }
    public bool IsDefault { get; set; }
    
    // Prop Firm Configurations
    public bool IsPropFirm { get; set; }
    public decimal DailyDrawdownLimitPct { get; set; }
    public decimal MaxOverallLossPct { get; set; }
    public decimal ProfitTargetPct { get; set; }
    public decimal ProfitSplitPct { get; set; }
    public decimal MaxRiskPerTradePctOfDailyLimit { get; set; }
    public decimal MaxAllowedLotSize { get; set; }
    public bool UseDynamicEquity { get; set; }
    public bool Has5xLotRule { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public class CreateTradingAccountDto
{
    public Guid? UserId { get; set; } // Admin can assign to specific user
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Broker { get; set; }
    public bool IsDefault { get; set; }
    
    public bool IsPropFirm { get; set; }
    public decimal DailyDrawdownLimitPct { get; set; } = 3.0m;
    public decimal MaxOverallLossPct { get; set; } = 6.0m;
    public decimal ProfitTargetPct { get; set; } = 10.0m;
    public decimal ProfitSplitPct { get; set; } = 80.0m;
    public decimal MaxRiskPerTradePctOfDailyLimit { get; set; } = 40.0m;
    public decimal MaxAllowedLotSize { get; set; } = 5.0m;
    public bool UseDynamicEquity { get; set; } = true;
    public bool Has5xLotRule { get; set; } = true;
}

public class UpdateTradingAccountDto : CreateTradingAccountDto
{
    public Guid Id { get; set; }
}
