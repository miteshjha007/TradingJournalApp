using TradingJournal.Application.DTOs.Analytics;
using TradingJournal.Application.DTOs.Dashboard;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAlertRepository _alertRepository;

    public DashboardService(ITradeRepository tradeRepository,
        IInstrumentRepository instrumentRepository,
        IUserRepository userRepository,
        IAlertRepository alertRepository)
    {
        _tradeRepository = tradeRepository;
        _instrumentRepository = instrumentRepository;
        _userRepository = userRepository;
        _alertRepository = alertRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId)
    {
        var allTrades = await _tradeRepository.GetByUserIdAsync(userId);
        var user = await _userRepository.GetByIdAsync(userId);
        var alert = await _alertRepository.GetByUserIdAsync(userId);
        var instruments = await _instrumentRepository.GetByUserIdAsync(userId);

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todayTrades = allTrades.Where(t => t.TradeDate.Date == today).ToList();
        var weekTrades = allTrades.Where(t => t.TradeDate.Date >= weekStart).ToList();
        var monthTrades = allTrades.Where(t => t.TradeDate.Date >= monthStart).ToList();

        var totalPL = allTrades.Sum(t => t.ProfitLoss);
        var todayPL = todayTrades.Sum(t => t.ProfitLoss);
        var wins = allTrades.Count(t => t.Result == TradeResult.Win);
        var total = allTrades.Count;

        // Monthly PL
        var monthlyPL = allTrades.GroupBy(t => new { t.TradeDate.Year, t.TradeDate.Month })
            .Select(g => new MonthlyPLDto
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                ProfitLoss = g.Sum(t => t.ProfitLoss),
                TradeCount = g.Count()
            }).OrderBy(m => m.Month).TakeLast(12).ToList();

        // Equity curve
        var sortedTrades = allTrades.OrderBy(t => t.TradeDate).ToList();
        var balance = user?.AccountBalance ?? 10000;
        var equityCurve = new List<EquityCurvePointDto>();
        var runningBalance = balance;
        foreach (var t in sortedTrades)
        {
            runningBalance += t.ProfitLoss;
            equityCurve.Add(new EquityCurvePointDto
            {
                Date = t.TradeDate,
                Balance = runningBalance,
                PL = t.ProfitLoss
            });
        }

        // Instrument performance
        var instrPerf = instruments.Select(i =>
        {
            var iTrades = allTrades.Where(t => t.InstrumentId == i.Id).ToList();
            var iWins = iTrades.Count(t => t.Result == TradeResult.Win);
            return new InstrumentPerformanceDto
            {
                InstrumentName = i.Name,
                TotalPL = iTrades.Sum(t => t.ProfitLoss),
                TotalTrades = iTrades.Count,
                WinRate = iTrades.Count > 0 ? Math.Round((decimal)iWins / iTrades.Count * 100, 2) : 0
            };
        }).ToList();

        // Drawdown
        var maxDD = CalculateMaxDrawdown(sortedTrades, balance);
        var currentDD = CalculateCurrentDrawdown(sortedTrades, balance);

        return new DashboardSummaryDto
        {
            TotalProfitLoss = totalPL,
            WinRate = total > 0 ? Math.Round((decimal)wins / total * 100, 2) : 0,
            TotalTrades = total,
            WinCount = wins,
            LossCount = allTrades.Count(t => t.Result == TradeResult.Loss),
            AverageRiskRewardRatio = total > 0 ? Math.Round(allTrades.Average(t => t.RiskRewardRatio), 2) : 0,
            MaxDrawdown = maxDD,
            CurrentDrawdown = currentDD,
            TodayPL = todayPL,
            WeekPL = weekTrades.Sum(t => t.ProfitLoss),
            MonthPL = monthTrades.Sum(t => t.ProfitLoss),
            TodayTradeCount = todayTrades.Count,
            DailyLossLimitBreached = alert != null && todayPL < -alert.DailyLossLimit,
            DailyLossLimit = alert?.DailyLossLimit ?? 0,
            AccountBalance = user?.AccountBalance ?? 10000,
            MonthlyPL = monthlyPL,
            EquityCurve = equityCurve,
            InstrumentPerformance = instrPerf
        };
    }

    public async Task<List<CalendarDayDto>> GetCalendarDataAsync(Guid userId, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var trades = await _tradeRepository.GetByDateRangeAsync(userId, from, to);

        return trades.GroupBy(t => t.TradeDate.Date)
            .Select(g => new CalendarDayDto
            {
                Date = g.Key,
                TotalPL = g.Sum(t => t.ProfitLoss),
                TradeCount = g.Count()
            }).ToList();
    }

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(Guid userId)
    {
        var trades = await _tradeRepository.GetByUserIdAsync(userId);
        var wins = trades.Where(t => t.Result == TradeResult.Win).ToList();
        var losses = trades.Where(t => t.Result == TradeResult.Loss).ToList();

        var avgWin = wins.Count > 0 ? wins.Average(t => t.ProfitLoss) : 0;
        var avgLoss = losses.Count > 0 ? Math.Abs(losses.Average(t => t.ProfitLoss)) : 0;
        var winRate = trades.Count > 0 ? (decimal)wins.Count / trades.Count : 0;

        // Consecutive wins/losses
        var (maxConsWins, maxConsLosses) = CalculateConsecutive(trades.OrderBy(t => t.TradeDate).ToList());

        // Sharpe ratio (simplified: mean / std dev of daily returns)
        var dailyReturns = trades.GroupBy(t => t.TradeDate.Date).Select(g => g.Sum(t => t.ProfitLoss)).ToList();
        var meanReturn = dailyReturns.Count > 0 ? dailyReturns.Average() : 0;
        var stdDev = dailyReturns.Count > 1
            ? (decimal)Math.Sqrt((double)dailyReturns.Select(r => (r - meanReturn) * (r - meanReturn)).Average())
            : 1;
        var sharpe = stdDev != 0 ? Math.Round(meanReturn / stdDev, 2) : 0;

        var totalWins = wins.Sum(t => t.ProfitLoss);
        var totalLosses = Math.Abs(losses.Sum(t => t.ProfitLoss));

        return new PerformanceMetricsDto
        {
            SharpeRatio = sharpe,
            AverageWin = Math.Round(avgWin, 2),
            AverageLoss = Math.Round(avgLoss, 2),
            LargestWin = wins.Count > 0 ? wins.Max(t => t.ProfitLoss) : 0,
            LargestLoss = losses.Count > 0 ? Math.Abs(losses.Min(t => t.ProfitLoss)) : 0,
            MaxConsecutiveWins = maxConsWins,
            MaxConsecutiveLosses = maxConsLosses,
            ProfitFactor = totalLosses > 0 ? Math.Round(totalWins / totalLosses, 2) : 0,
            ExpectedValue = Math.Round(winRate * avgWin - (1 - winRate) * avgLoss, 2)
        };
    }

    public async Task<DrawdownDto> GetDrawdownAsync(Guid userId)
    {
        var trades = await _tradeRepository.GetByUserIdAsync(userId);
        var user = await _userRepository.GetByIdAsync(userId);
        var alert = await _alertRepository.GetByUserIdAsync(userId);
        var balance = user?.AccountBalance ?? 10000;

        var sortedTrades = trades.OrderBy(t => t.TradeDate).ToList();
        var maxDD = CalculateMaxDrawdown(sortedTrades, balance);
        var currentDD = CalculateCurrentDrawdown(sortedTrades, balance);
        var maxDDPct = balance > 0 ? Math.Round(maxDD / balance * 100, 2) : 0;
        var currentDDPct = balance > 0 ? Math.Round(currentDD / balance * 100, 2) : 0;
        var limitPct = alert?.MaxDrawdownPercent ?? 20;

        return new DrawdownDto
        {
            MaxDrawdown = maxDD,
            CurrentDrawdown = currentDD,
            MaxDrawdownPercent = maxDDPct,
            CurrentDrawdownPercent = currentDDPct,
            IsWarning = currentDDPct >= limitPct * 0.7m,
            IsCritical = currentDDPct >= limitPct,
            AccountBalance = balance
        };
    }

    public async Task<AiAnalysisDto> GetAiInsightsAsync(Guid userId)
    {
        var trades = await _tradeRepository.GetByUserIdAsync(userId);
        var instruments = await _instrumentRepository.GetByUserIdAsync(userId);
        var insights = new List<AiInsightDto>();

        if (!trades.Any())
        {
            insights.Add(new AiInsightDto
            {
                Category = "Info",
                Severity = "Info",
                Title = "No Trade Data",
                Message = "Start journaling your trades to receive AI insights.",
                Recommendation = "Add your first trade to begin analysis.",
                Icon = "💡"
            });
            return new AiAnalysisDto { Insights = insights, OverallScore = "N/A" };
        }

        // Overtrading detection
        var avgDailyTrades = trades.GroupBy(t => t.TradeDate.Date).Average(g => g.Count());
        if (avgDailyTrades > 5)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Overtrading",
                Severity = "Warning",
                Title = "Overtrading Detected",
                Message = $"You average {avgDailyTrades:F1} trades per day which is high.",
                Recommendation = "Limit to 3-5 quality trades per day. Quality over quantity.",
                Icon = "⚠️"
            });
        }

        // Lot size inconsistency
        var lotSizes = trades.Select(t => t.LotSize).ToList();
        var lotStdDev = CalculateStdDev(lotSizes);
        if (lotStdDev > lotSizes.Average() * 0.5m)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Risk",
                Severity = "Warning",
                Title = "Inconsistent Lot Sizing",
                Message = "Your lot sizes vary significantly across trades.",
                Recommendation = "Use a fixed risk percentage (1-2%) to determine lot size consistently.",
                Icon = "📊"
            });
        }

        // Win rate analysis
        var winRate = (decimal)trades.Count(t => t.Result == TradeResult.Win) / trades.Count * 100;
        if (winRate < 40)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Performance",
                Severity = "Critical",
                Title = "Low Win Rate",
                Message = $"Your win rate is {winRate:F1}%, which is below the recommended 40-50%.",
                Recommendation = "Review your entry strategy. Consider waiting for stronger confirmation signals.",
                Icon = "📉"
            });
        }

        // High RRR
        var avgRRR = trades.Average(t => t.RiskRewardRatio);
        if (avgRRR < 1)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Risk",
                Severity = "Warning",
                Title = "Poor Risk-to-Reward Ratio",
                Message = $"Average RRR is {avgRRR:F2}. You risk more than you gain.",
                Recommendation = "Aim for minimum 1:1.5 risk-to-reward on each trade.",
                Icon = "⚖️"
            });
        }

        // Best time of day
        var bestHour = trades.Where(t => t.Result == TradeResult.Win)
            .GroupBy(t => t.TradeDate.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var bestTimeOfDay = bestHour != null
            ? $"{bestHour.Key:D2}:00 - {(bestHour.Key + 1) % 24:D2}:00 UTC"
            : "N/A";

        // Best instrument
        var bestInstrument = instruments
            .Select(i => new
            {
                i.Name,
                PL = trades.Where(t => t.InstrumentId == i.Id).Sum(t => t.ProfitLoss)
            })
            .OrderByDescending(i => i.PL)
            .FirstOrDefault();

        if (bestInstrument != null && bestInstrument.PL > 0)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Performance",
                Severity = "Info",
                Title = "Best Performing Instrument",
                Message = $"{bestInstrument.Name} is your most profitable instrument.",
                Recommendation = $"Focus more trades on {bestInstrument.Name} when conditions are favorable.",
                Icon = "🏆"
            });
        }

        // Positive insight
        if (winRate >= 55)
        {
            insights.Add(new AiInsightDto
            {
                Category = "Performance",
                Severity = "Info",
                Title = "Strong Win Rate",
                Message = $"Excellent! Your win rate of {winRate:F1}% is above average.",
                Recommendation = "Maintain your current strategy. Consider slightly increasing position size.",
                Icon = "✅"
            });
        }

        var score = CalculateScore(winRate, avgRRR, avgDailyTrades, (decimal)lotStdDev);

        return new AiAnalysisDto
        {
            OverallScore = score,
            BestInstrument = bestInstrument?.Name ?? "N/A",
            BestTimeOfDay = bestTimeOfDay,
            MostCommonMistake = insights.Where(i => i.Severity != "Info").FirstOrDefault()?.Title ?? "None detected",
            Insights = insights
        };
    }

    public async Task<RiskResultDto> CalculateRiskAsync(RiskCalculationDto dto, Guid userId)
    {
        var riskAmount = dto.AccountBalance * dto.RiskPercent / 100;
        decimal suggestedLot = 0.01m;
        decimal maxAllowed = 1.0m;
        string warning = string.Empty;

        if (dto.InstrumentId.HasValue)
        {
            var instrument = await _instrumentRepository.GetByIdAsync(dto.InstrumentId.Value, userId);
            if (instrument != null)
            {
                suggestedLot = Math.Min(instrument.SafeLotSize, riskAmount / 100);
                maxAllowed = instrument.MaxLot;

                if (suggestedLot > instrument.SafeLotSize)
                    warning = $"Suggested lot exceeds safe lot ({instrument.SafeLotSize}) for this instrument.";

                if (instrument.VolatilityLevel == VolatilityLevel.High)
                    warning += " High volatility instrument - trade with caution.";
            }
        }
        else
        {
            suggestedLot = Math.Round(riskAmount / 1000, 2);
        }

        var riskLevel = dto.RiskPercent <= 1 ? "Conservative" : dto.RiskPercent <= 2 ? "Moderate" : "Aggressive";
        var maxTradesPerDay = dto.RiskPercent <= 1 ? 10 : dto.RiskPercent <= 2 ? 5 : 3;

        return new RiskResultDto
        {
            RiskAmount = Math.Round(riskAmount, 2),
            SuggestedLotSize = Math.Max(0.01m, Math.Round(suggestedLot, 2)),
            MaxAllowedLotSize = maxAllowed,
            MaxTradesPerDay = maxTradesPerDay,
            RiskLevel = riskLevel,
            Warning = warning
        };
    }

    public async Task<AlertDto?> GetAlertAsync(Guid userId)
    {
        var alert = await _alertRepository.GetByUserIdAsync(userId);
        return alert == null ? null : MapAlertToDto(alert);
    }

    public async Task<AlertDto> UpsertAlertAsync(CreateAlertDto dto, Guid userId)
    {
        var existing = await _alertRepository.GetByUserIdAsync(userId);
        var alert = existing ?? new Alert { UserId = userId };
        alert.DailyLossLimit = dto.DailyLossLimit;
        alert.MaxDrawdownPercent = dto.MaxDrawdownPercent;
        alert.MaxTradesPerDay = dto.MaxTradesPerDay;
        alert.IsActive = dto.IsActive;
        alert.EmailAlertEnabled = dto.EmailAlertEnabled;
        alert.Email = dto.Email;
        alert.UpdatedAt = DateTime.UtcNow;

        var saved = await _alertRepository.UpsertAsync(alert);
        return MapAlertToDto(saved);
    }

    // Helpers
    private decimal CalculateMaxDrawdown(List<Trade> trades, decimal startBalance)
    {
        if (!trades.Any()) return 0;
        var peak = startBalance;
        var maxDD = 0m;
        var balance = startBalance;
        foreach (var t in trades)
        {
            balance += t.ProfitLoss;
            if (balance > peak) peak = balance;
            var dd = peak - balance;
            if (dd > maxDD) maxDD = dd;
        }
        return Math.Round(maxDD, 2);
    }

    private decimal CalculateCurrentDrawdown(List<Trade> trades, decimal startBalance)
    {
        if (!trades.Any()) return 0;
        var balance = startBalance + trades.Sum(t => t.ProfitLoss);
        var peak = startBalance;
        var runningBalance = startBalance;
        foreach (var t in trades)
        {
            runningBalance += t.ProfitLoss;
            if (runningBalance > peak) peak = runningBalance;
        }
        return Math.Round(Math.Max(0, peak - balance), 2);
    }

    private (int maxWins, int maxLosses) CalculateConsecutive(List<Trade> trades)
    {
        int maxW = 0, maxL = 0, curW = 0, curL = 0;
        foreach (var t in trades)
        {
            if (t.Result == TradeResult.Win) { curW++; curL = 0; }
            else if (t.Result == TradeResult.Loss) { curL++; curW = 0; }
            if (curW > maxW) maxW = curW;
            if (curL > maxL) maxL = curL;
        }
        return (maxW, maxL);
    }

    private decimal CalculateStdDev(List<decimal> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        var variance = values.Select(v => (v - avg) * (v - avg)).Average();
        return (decimal)Math.Sqrt((double)variance);
    }

    private string CalculateScore(decimal winRate, decimal avgRRR, double avgDailyTrades, decimal lotStdDev)
    {
        var score = 0;
        if (winRate >= 50) score += 25;
        else if (winRate >= 40) score += 15;
        if (avgRRR >= 1.5m) score += 25;
        else if (avgRRR >= 1m) score += 15;
        if (avgDailyTrades <= 5) score += 25;
        else if (avgDailyTrades <= 8) score += 10;
        if (lotStdDev <= 0.1m) score += 25;
        else if (lotStdDev <= 0.3m) score += 10;

        return score >= 75 ? "A (Excellent)" : score >= 50 ? "B (Good)" : score >= 25 ? "C (Average)" : "D (Needs Work)";
    }

    private AlertDto MapAlertToDto(Alert alert) => new AlertDto
    {
        Id = alert.Id,
        DailyLossLimit = alert.DailyLossLimit,
        MaxDrawdownPercent = alert.MaxDrawdownPercent,
        MaxTradesPerDay = alert.MaxTradesPerDay,
        IsActive = alert.IsActive,
        EmailAlertEnabled = alert.EmailAlertEnabled,
        Email = alert.Email
    };
}
