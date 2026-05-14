namespace TradingJournal.Application.DTOs.Backtest;

public enum BacktestRuleType
{
    MinRRR = 1,
    MaxDailyTrades = 2,
    ChecklistCompliance = 3,
    TradeType = 4,
    TimeOfDayFrom = 5,
    TimeOfDayTo = 6,
    MinRiskPercent = 7,
    MaxRiskPercent = 8
}

public class BacktestRuleFilter
{
    public BacktestRuleType RuleType { get; set; }
    public decimal? Value { get; set; }
}

public class BacktestRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string StrategyDescription { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? InstrumentId { get; set; }
    public List<BacktestRuleFilter> RuleFilters { get; set; } = new();
}

public class BacktestTradeResult
{
    public DateTime Date { get; set; }
    public decimal PL { get; set; }
    public decimal RunningBalance { get; set; }
    public bool IsWin { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
}

public class MonteCarloDto
{
    public int Simulations { get; set; }
    public decimal MedianFinalPL { get; set; }
    public decimal P5FinalPL { get; set; }
    public decimal P95FinalPL { get; set; }
    public decimal RuinProbability { get; set; }
    public List<List<decimal>> ChartData { get; set; } = new();
}

public class BacktestResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyDescription { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? InstrumentName { get; set; }
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
    public List<BacktestTradeResult> Trades { get; set; } = new();
    public MonteCarloDto? MonteCarlo { get; set; }
    public DateTime CreatedAt { get; set; }
}
