namespace TradingJournal.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalProfitLoss { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public int WinCount { get; set; }
    public int LossCount { get; set; }
    public decimal AverageRiskRewardRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal CurrentDrawdown { get; set; }
    public decimal TodayPL { get; set; }
    public decimal WeekPL { get; set; }
    public decimal MonthPL { get; set; }
    public int TodayTradeCount { get; set; }
    public bool DailyLossLimitBreached { get; set; }
    public decimal DailyLossLimit { get; set; }
    public decimal AccountBalance { get; set; }
    public List<MonthlyPLDto> MonthlyPL { get; set; } = new();
    public List<EquityCurvePointDto> EquityCurve { get; set; } = new();
    public List<InstrumentPerformanceDto> InstrumentPerformance { get; set; } = new();
}

public class MonthlyPLDto
{
    public string Month { get; set; } = string.Empty;
    public decimal ProfitLoss { get; set; }
    public int TradeCount { get; set; }
}

public class EquityCurvePointDto
{
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }
    public decimal PL { get; set; }
}

public class InstrumentPerformanceDto
{
    public string InstrumentName { get; set; } = string.Empty;
    public decimal TotalPL { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
}

public class CalendarDayDto
{
    public DateTime Date { get; set; }
    public decimal TotalPL { get; set; }
    public int TradeCount { get; set; }
    public bool IsProfit => TotalPL > 0;
    public bool IsLoss => TotalPL < 0;
}

public class PerformanceMetricsDto
{
    public decimal SharpeRatio { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
    public int MaxConsecutiveWins { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal ExpectedValue { get; set; }
}

public class DrawdownDto
{
    public decimal MaxDrawdown { get; set; }
    public decimal CurrentDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public decimal CurrentDrawdownPercent { get; set; }
    public bool IsWarning { get; set; }
    public bool IsCritical { get; set; }
    public decimal AccountBalance { get; set; }
}
