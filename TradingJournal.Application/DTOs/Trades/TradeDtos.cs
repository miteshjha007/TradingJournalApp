using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Trades;

public class CreateTradeDto
{
    public Guid InstrumentId { get; set; }
    public decimal LotSize { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal RiskPercentage { get; set; }
    public DateTime TradeDate { get; set; }
    public int TradeDurationMinutes { get; set; }
    public TradeType TradeType { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public Guid? TradingAccountId { get; set; }
    public List<Guid>? CheckedRuleIds { get; set; }
    public string? ChartImageUrl { get; set; }
}

public class UpdateTradeDto
{
    public decimal LotSize { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal RiskPercentage { get; set; }
    public DateTime TradeDate { get; set; }
    public int TradeDurationMinutes { get; set; }
    public TradeType TradeType { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public List<Guid>? CheckedRuleIds { get; set; }
    public string? ChartImageUrl { get; set; }
}

public class TradeDto
{
    public Guid Id { get; set; }
    public Guid InstrumentId { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
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
    public string TradeType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Prop Firm Rule Violations
    public List<string> RuleViolations { get; set; } = new();
    public decimal? ChecklistCompliancePercent { get; set; }
    public string? ChartImageUrl { get; set; }
    public Guid? TradingAccountId { get; set; }
}

public class TradeFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? InstrumentId { get; set; }
    public string? Result { get; set; }
    public string? TradeType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedTradesDto
{
    public List<TradeDto> Trades { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
