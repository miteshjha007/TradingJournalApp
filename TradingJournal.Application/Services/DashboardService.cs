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
    private readonly IAlertRepository _alertRepository;
    private readonly ITradingAccountService _accountService;

    public DashboardService(
        ITradeRepository tradeRepository,
        IInstrumentRepository instrumentRepository,
        IAlertRepository alertRepository,
        ITradingAccountService accountService)
    {
        _tradeRepository = tradeRepository;
        _instrumentRepository = instrumentRepository;
        _alertRepository = alertRepository;
        _accountService = accountService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId)
    {
        var allTrades = await _tradeRepository.GetByUserIdAsync(userId);
        var account = await _accountService.GetDefaultAccountAsync(userId);
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
        var balance = account?.Balance ?? 10000;
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
            AccountBalance = balance,
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
        var account = await _accountService.GetDefaultAccountAsync(userId);
        var alert = await _alertRepository.GetByUserIdAsync(userId);
        var balance = account?.Balance ?? 10000;

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

    public async Task<PropRiskResultDto> CalculatePropRiskAsync(PropRiskCalculationDto dto, Guid userId)
    {
        var account = await _accountService.GetDefaultAccountAsync(userId);
        var balance = account?.Balance ?? dto.AccountBalance;
        
        // Use account configurations or fallback to DTO defaults
        var ddLimitPct = account?.DailyDrawdownLimitPct > 0 ? account.DailyDrawdownLimitPct : dto.DailyDrawdownLimit;
        var maxOverallLossPct = account?.MaxOverallLossPct > 0 ? account.MaxOverallLossPct : 6.0m;
        var useDynamicEquity = account?.UseDynamicEquity ?? true;
        var has5xRule = account?.Has5xLotRule ?? true;
        var maxAllowedLotSize = account?.MaxAllowedLotSize > 0 ? account.MaxAllowedLotSize : 5.0m;
        var maxRiskPerTradePct = account?.MaxRiskPerTradePctOfDailyLimit > 0 ? account.MaxRiskPerTradePctOfDailyLimit : 40.0m;
        
        var symbol = dto.InstrumentSymbol?.ToUpper().Trim() ?? string.Empty;
        var category = "Forex";

        // Resolve pip value and category from instrument or symbol
        decimal pipValuePer001Lot = 0.10m; // default: Forex USD-quote pair

        if (dto.InstrumentId.HasValue)
        {
            var instrument = await _instrumentRepository.GetByIdAsync(dto.InstrumentId.Value, userId);
            if (instrument != null)
            {
                symbol = instrument.Symbol?.ToUpper() ?? symbol;
            }
        }

        (pipValuePer001Lot, category) = GetPipValueAndCategory(symbol);

        // Daily Drawdown Check with Dynamic Equity support
        var baseDdLimitAmount = balance * ddLimitPct / 100m;
        
        // If Dynamic Equity is enabled, profitable days increase your limit
        var dynamicLimitAmount = baseDdLimitAmount;
        if (useDynamicEquity && dto.TodayLoss > 0) // if TodayLoss is positive, it means profit
        {
            dynamicLimitAmount += dto.TodayLoss;
        }

        // TodayLoss in DTO is negative if loss, positive if profit
        var actualLossAmount = dto.TodayLoss < 0 ? Math.Abs(dto.TodayLoss) : 0;
        
        var ddRemaining = Math.Max(0, dynamicLimitAmount - actualLossAmount);
        var ddBreached = actualLossAmount >= dynamicLimitAmount;

        // 40% Rule check (Risk per trade shouldn't exceed X% of daily limit)
        var maxRiskDollar = dynamicLimitAmount * (maxRiskPerTradePct / 100m);
        var requestedRiskAmount = balance * dto.RiskPercent / 100m;
        var riskAmount = Math.Min(requestedRiskAmount, maxRiskDollar);

        decimal suggestedLot = 0.01m;
        if (dto.StopLossPips > 0)
        {
            var denominator = dto.StopLossPips * pipValuePer001Lot * 100m;
            suggestedLot = denominator > 0 ? riskAmount / denominator : 0.01m;
        }

        suggestedLot = Math.Round(Math.Max(0.001m, suggestedLot), 3);
        
        // Enforce Hard Max Allowed Lot Size
        if (suggestedLot > maxAllowedLotSize)
            suggestedLot = maxAllowedLotSize;

        var maxLossIfSLHit = suggestedLot * 100m * dto.StopLossPips * pipValuePer001Lot;

        // 5x Rule: subsequent lots must be <= 5x the first trade lot
        decimal fiveXMax = 0m;
        bool violatesFiveX = false;
        
        if (has5xRule && dto.FirstTradeLotSize.HasValue && dto.FirstTradeLotSize.Value > 0)
        {
            fiveXMax = dto.FirstTradeLotSize.Value * 5m;
            violatesFiveX = suggestedLot > fiveXMax;
            if (violatesFiveX) suggestedLot = Math.Round(fiveXMax, 3);
        }

        // Cap suggested lot so loss won't exceed remaining daily drawdown
        if (ddRemaining > 0 && dto.StopLossPips > 0)
        {
            var maxLotForDD = ddRemaining / (dto.StopLossPips * pipValuePer001Lot * 100m);
            if (suggestedLot > maxLotForDD)
                suggestedLot = Math.Round(maxLotForDD, 3);
        }

        // Determine Warnings
        var warnings = new List<string>();
        if (has5xRule && violatesFiveX) warnings.Add($"⚠️ 5x Rule: capped lot from original to {fiveXMax:F3} (5× first trade).");
        if (ddBreached) warnings.Add("🔴 Daily drawdown limit reached. No more trades today.");
        else if (ddRemaining < maxLossIfSLHit) warnings.Add($"⚠️ Trade risk (${maxLossIfSLHit:F2}) exceeds daily drawdown remaining (${ddRemaining:F2}).");
        
        if (requestedRiskAmount > maxRiskDollar) 
            warnings.Add($"⚠️ {maxRiskPerTradePct}% Rule: Risk capped at ${maxRiskDollar:F2} ({maxRiskPerTradePct}% of daily limit).");

        var isSafe = !ddBreached && (!has5xRule || !violatesFiveX) && maxLossIfSLHit <= riskAmount * 1.1m;

        return new PropRiskResultDto
        {
            SuggestedLotSize = suggestedLot,
            PipValuePer001Lot = pipValuePer001Lot,
            RiskAmountDollar = Math.Round(maxLossIfSLHit, 2), // The actual risk with the suggested lot
            MaxLossIfSLHit = Math.Round(maxLossIfSLHit, 2),
            FiveXRuleMaxLot = Math.Round(fiveXMax, 3),
            ViolatesFiveXRule = violatesFiveX,
            DailyDrawdownLimitAmount = Math.Round(dynamicLimitAmount, 2),
            DailyDrawdownRemaining = Math.Round(ddRemaining, 2),
            DailyDrawdownBreached = ddBreached,
            RiskLevel = requestedRiskAmount > maxRiskDollar ? "Aggressive" : "Moderate",
            Warning = string.Join(" ", warnings),
            IsSafe = isSafe,
            InstrumentCategory = category
        };
    }

    private static (decimal pipValue, string category) GetPipValueAndCategory(string symbol)
    {
        // Metals
        if (symbol.Contains("XAUUSD") || symbol.Contains("GOLD")) return (1.00m, "Metals");
        if (symbol.Contains("XAGUSD") || symbol.Contains("SILVER")) return (0.50m, "Metals");

        // Crypto
        if (symbol.Contains("BTC")) return (0.001m, "Crypto");
        if (symbol.Contains("ETH")) return (0.01m, "Crypto");
        if (symbol.Contains("XRP") || symbol.Contains("LTC") || symbol.Contains("BCH")) return (0.01m, "Crypto");

        // JPY crosses (pip value lower due to yen denomination)
        if (symbol.Contains("JPY")) return (0.09m, "Forex-JPY");

        // Exotic pairs (wider spreads)
        if (symbol.Contains("MXN") || symbol.Contains("TRY") || symbol.Contains("ZAR")) return (0.01m, "Forex-Exotic");

        // Default: Forex USD-quote (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCAD, USDCHF...)
        return (0.10m, "Forex");
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

    // ─────────────────────────────────────────────────────────────
    // PROP FIRM RULE ENGINE — Real-time status calculation
    // ─────────────────────────────────────────────────────────────
    public async Task<PropFirmStatusDto?> GetPropFirmStatusAsync(Guid userId)
    {
        var account = await _accountService.GetDefaultAccountAsync(userId);

        // Only proceed for prop firm accounts
        if (account == null || !account.IsPropFirm)
            return null;

        var allTrades = await _tradeRepository.GetByUserIdAsync(userId);
        var today = DateTime.UtcNow.Date;

        // ── Daily Loss Calculation ──────────────────────────────
        var todayTrades = allTrades.Where(t => t.TradeDate.Date == today).ToList();
        var todayPL = todayTrades.Sum(t => t.ProfitLoss);
        var dailyLossUsed = todayPL < 0 ? Math.Abs(todayPL) : 0m;

        // Dynamic equity: if profitable today, daily budget increases
        var baseDailyLimit = account.Balance * account.DailyDrawdownLimitPct / 100m;
        var dynamicDailyLimit = account.UseDynamicEquity && todayPL > 0
            ? baseDailyLimit + todayPL
            : baseDailyLimit;

        var dailyLossUsedPct = dynamicDailyLimit > 0
            ? Math.Round(dailyLossUsed / dynamicDailyLimit * 100, 1)
            : 0;
        var remainingDailyBudget = Math.Max(0, dynamicDailyLimit - dailyLossUsed);

        // ── Overall Drawdown Calculation ───────────────────────
        var sortedTrades = allTrades.OrderBy(t => t.TradeDate).ToList();
        var totalDrawdown = CalculateCurrentDrawdown(sortedTrades, account.Balance);
        var maxOverallLimitAmt = account.Balance * account.MaxOverallLossPct / 100m;
        var totalDrawdownPct = maxOverallLimitAmt > 0
            ? Math.Round(totalDrawdown / maxOverallLimitAmt * 100, 1)
            : 0;
        var remainingOverallBudget = Math.Max(0, maxOverallLimitAmt - totalDrawdown);

        // ── Profit Target ──────────────────────────────────────
        var totalPL = allTrades.Sum(t => t.ProfitLoss);
        var profitTargetAmt = account.Balance * account.ProfitTargetPct / 100m;
        var profitEarnedPct = profitTargetAmt > 0
            ? Math.Round(totalPL / profitTargetAmt * 100, 1)
            : 0;
        var estimatedPayout = totalPL > 0
            ? Math.Round(totalPL * account.ProfitSplitPct / 100m, 2)
            : 0;

        // ── Trading Days ───────────────────────────────────────
        var tradingDaysCompleted = allTrades
            .Select(t => t.TradeDate.Date)
            .Distinct()
            .Count();

        // ── Rule Breach Detection ──────────────────────────────
        var dailyLimitBreached = dailyLossUsed >= dynamicDailyLimit;
        var overallLimitBreached = totalDrawdown >= maxOverallLimitAmt;
        var profitTargetReached = totalPL >= profitTargetAmt;

        // ── Status Determination (the 🟢🟡🔴 logic) ───────────
        string status;
        string statusColor;
        var warnings = new List<string>();

        if (overallLimitBreached)
        {
            status = "BREACHED_OVERALL";
            statusColor = "#dc2626"; // red-600
            warnings.Add($"🔴 ACCOUNT BLOWN: Overall drawdown limit of ${maxOverallLimitAmt:F0} breached. Account at risk!");
        }
        else if (dailyLimitBreached)
        {
            status = "BREACHED_DAILY";
            statusColor = "#ef4444"; // red-500
            warnings.Add($"🔴 STOP TRADING: Daily loss of ${dailyLossUsed:F2} has hit the ${dynamicDailyLimit:F2} daily limit.");
        }
        else if (profitTargetReached)
        {
            status = "PASSED";
            statusColor = "#6366f1"; // indigo — challenge passed!
            warnings.Add($"🎉 CHALLENGE PASSED! You have hit {account.ProfitTargetPct}% profit target. Request your payout!");
        }
        else if (dailyLossUsedPct >= 90 || totalDrawdownPct >= 90)
        {
            status = "CRITICAL";
            statusColor = "#f97316"; // orange-500
            if (dailyLossUsedPct >= 90)
                warnings.Add($"🟠 CRITICAL: {dailyLossUsedPct}% of daily limit used. Only ${remainingDailyBudget:F2} left today!");
            if (totalDrawdownPct >= 90)
                warnings.Add($"🟠 CRITICAL: {totalDrawdownPct}% of overall drawdown limit reached!");
        }
        else if (dailyLossUsedPct >= 70 || totalDrawdownPct >= 70)
        {
            status = "WARNING";
            statusColor = "#f59e0b"; // amber-500
            if (dailyLossUsedPct >= 70)
                warnings.Add($"⚠️ WARNING: {dailyLossUsedPct}% of daily drawdown used. Remaining: ${remainingDailyBudget:F2}");
            if (totalDrawdownPct >= 70)
                warnings.Add($"⚠️ WARNING: {totalDrawdownPct}% of overall loss limit reached.");
        }
        else
        {
            status = "SAFE";
            statusColor = "#10b981"; // emerald-500
        }

        return new PropFirmStatusDto
        {
            FirmName = account.PropFirmName ?? "Prop Firm",
            PlanName = account.PropFirmPlan ?? account.Name,
            AccountBalance = account.Balance,

            DailyLossUsed = Math.Round(dailyLossUsed, 2),
            DailyLossLimit = Math.Round(dynamicDailyLimit, 2),
            DailyLossUsedPct = dailyLossUsedPct,
            RemainingDailyBudget = Math.Round(remainingDailyBudget, 2),

            TotalDrawdown = Math.Round(totalDrawdown, 2),
            MaxDrawdownLimit = Math.Round(maxOverallLimitAmt, 2),
            TotalDrawdownPct = totalDrawdownPct,
            RemainingOverallBudget = Math.Round(remainingOverallBudget, 2),

            ProfitEarned = Math.Round(totalPL, 2),
            ProfitTarget = Math.Round(profitTargetAmt, 2),
            ProfitEarnedPct = profitEarnedPct,
            EstimatedPayout = estimatedPayout,

            TradingDaysCompleted = tradingDaysCompleted,
            MinTradingDaysRequired = account.MinTradingDays,

            NewsTradeAllowed = account.NewsTradeAllowed,
            WeekendHoldingAllowed = account.WeekendHoldingAllowed,
            Has5xLotRule = account.Has5xLotRule,
            UseDynamicEquity = account.UseDynamicEquity,

            AccountStatus = status,
            StatusColor = statusColor,
            DailyLimitBreached = dailyLimitBreached,
            OverallLimitBreached = overallLimitBreached,
            ProfitTargetReached = profitTargetReached,
            ActiveWarnings = warnings
        };
    }
}

