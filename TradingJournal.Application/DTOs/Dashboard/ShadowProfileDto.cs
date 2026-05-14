namespace TradingJournal.Application.DTOs.Dashboard;

public class ShadowRuleDto
{
    public string RuleText { get; set; } = string.Empty;
    public int Support { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgPL { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class PatternDto
{
    public string Description { get; set; } = string.Empty;
    public int TradeCount { get; set; }
    public decimal AvgPL { get; set; }
    public decimal WinRate { get; set; }
}

public class ShadowProfileDto
{
    public int TotalRoundtrips { get; set; }
    public int ProfitableRoundtrips { get; set; }
    public List<ShadowRuleDto> ExtractedRules { get; set; } = new();
    public string TradingDNA { get; set; } = string.Empty;
    public List<PatternDto> BestPatterns { get; set; } = new();
    public List<PatternDto> WorstPatterns { get; set; } = new();
}
