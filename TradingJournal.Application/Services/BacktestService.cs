using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingJournal.Application.DTOs.Backtest;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class BacktestService : IBacktestService
{
    private readonly IBacktestRepository _backtestRepo;
    private readonly ITradeRepository _tradeRepo;
    private readonly IInstrumentRepository _instrumentRepo;
    private readonly ITradingAccountRepository _accountRepo;
    private readonly ILogger<BacktestService> _logger;

    public BacktestService(
        IBacktestRepository backtestRepo,
        ITradeRepository tradeRepo,
        IInstrumentRepository instrumentRepo,
        ITradingAccountRepository accountRepo,
        ILogger<BacktestService> logger)
    {
        _backtestRepo = backtestRepo;
        _tradeRepo = tradeRepo;
        _instrumentRepo = instrumentRepo;
        _accountRepo = accountRepo;
        _logger = logger;
    }

    public async Task<BacktestResultDto> RunBacktestAsync(Guid userId, BacktestRequestDto dto)
    {
        var allTrades = await _tradeRepo.GetByDateRangeAsync(userId, dto.FromDate, dto.ToDate);

        if (dto.InstrumentId.HasValue)
            allTrades = allTrades.Where(t => t.InstrumentId == dto.InstrumentId.Value).ToList();

        allTrades = ApplyFilters(allTrades, dto.RuleFilters);
        allTrades = allTrades.OrderBy(t => t.TradeDate).ToList();

        if (allTrades.Count == 0)
        {
            _logger.LogInformation("Backtest {Name} for user {UserId} returned 0 trades after filters", dto.Name, userId);
            return CreateEmptyResult(dto);
        }

        var equityCurve = BuildEquityCurve(allTrades);
        var dailyReturns = ComputeDailyReturns(allTrades);

        var wins = allTrades.Where(t => t.Result == TradeResult.Win).ToList();
        var losses = allTrades.Where(t => t.Result == TradeResult.Loss).ToList();

        var totalWins = wins.Sum(t => t.ProfitLoss);
        var totalLosses = Math.Abs(losses.Sum(t => t.ProfitLoss));
        var profitFactor = totalLosses > 0 ? Math.Round(totalWins / totalLosses, 2) : (totalWins > 0 ? 99 : 0);
        var sharpe = ComputeSharpeRatio(dailyReturns);
        var sortino = ComputeSortinoRatio(dailyReturns);
        var (maxDD, maxDDPct) = ComputeMaxDrawdown(equityCurve);

        var byDay = allTrades.GroupBy(t => t.TradeDate.Date)
            .Select(g => new { Day = g.Key, PL = g.Sum(t => t.ProfitLoss) })
            .OrderByDescending(x => x.PL).ToList();

        var bestDay = byDay.FirstOrDefault()?.Day;
        var worstDay = byDay.LastOrDefault()?.Day;

        var tradeResults = equityCurve.Select(ec => new BacktestTradeResult
        {
            Date = ec.trade.TradeDate,
            PL = ec.trade.ProfitLoss,
            RunningBalance = ec.balance,
            IsWin = ec.trade.Result == TradeResult.Win,
            InstrumentName = ec.trade.Instrument?.Name ?? ""
        }).ToList();

        // Monte Carlo
        var account = await _accountRepo.GetDefaultByUserIdAsync(userId);
        var ruinThreshold = account?.IsPropFirm == true
            ? -(account.Balance * account.MaxOverallLossPct / 100)
            : -(tradeResults.Sum(t => t.PL) * 0.2m);

        var monteCarlo = RunMonteCarlo(allTrades.Select(t => t.ProfitLoss).ToList(), ruinThreshold);

        string? instrumentName = null;
        if (dto.InstrumentId.HasValue)
        {
            var instr = await _instrumentRepo.GetByIdAsync(dto.InstrumentId.Value, userId);
            instrumentName = instr?.Name;
        }

        var entity = new BacktestResult
        {
            UserId = userId,
            Name = dto.Name,
            StrategyDescription = dto.StrategyDescription,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            InstrumentId = dto.InstrumentId,
            TotalTrades = allTrades.Count,
            WinningTrades = wins.Count,
            LosingTrades = losses.Count,
            TotalPL = allTrades.Sum(t => t.ProfitLoss),
            WinRate = allTrades.Count > 0 ? Math.Round((decimal)wins.Count / allTrades.Count * 100, 2) : 0,
            ProfitFactor = profitFactor,
            SharpeRatio = sharpe,
            SortinoRatio = sortino,
            MaxDrawdown = maxDD,
            MaxDrawdownPercent = maxDDPct,
            AverageRRR = allTrades.Count > 0 ? Math.Round(allTrades.Average(t => t.RiskRewardRatio), 2) : 0,
            BestDay = bestDay,
            WorstDay = worstDay,
            ResultsJson = JsonSerializer.Serialize(tradeResults)
        };

        var saved = await _backtestRepo.CreateAsync(entity);
        _logger.LogInformation("Backtest {Name} completed for user {UserId}, {Count} trades", dto.Name, userId, allTrades.Count);

        return new BacktestResultDto
        {
            Id = saved.Id,
            Name = saved.Name,
            StrategyDescription = saved.StrategyDescription,
            FromDate = saved.FromDate,
            ToDate = saved.ToDate,
            InstrumentId = saved.InstrumentId,
            InstrumentName = instrumentName,
            TotalTrades = saved.TotalTrades,
            WinningTrades = saved.WinningTrades,
            LosingTrades = saved.LosingTrades,
            TotalPL = saved.TotalPL,
            WinRate = saved.WinRate,
            ProfitFactor = saved.ProfitFactor,
            SharpeRatio = saved.SharpeRatio,
            SortinoRatio = saved.SortinoRatio,
            MaxDrawdown = saved.MaxDrawdown,
            MaxDrawdownPercent = saved.MaxDrawdownPercent,
            AverageRRR = saved.AverageRRR,
            BestDay = saved.BestDay,
            WorstDay = saved.WorstDay,
            Trades = tradeResults,
            MonteCarlo = monteCarlo,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<List<BacktestResultDto>> GetBacktestHistoryAsync(Guid userId)
    {
        var results = await _backtestRepo.GetByUserIdAsync(userId);
        return results.Select(r => MapToDto(r, null)).ToList();
    }

    public async Task<BacktestResultDto?> GetBacktestByIdAsync(Guid id, Guid userId)
    {
        var r = await _backtestRepo.GetByIdAsync(id, userId);
        if (r == null) return null;
        var trades = JsonSerializer.Deserialize<List<BacktestTradeResult>>(r.ResultsJson) ?? new();
        return MapToDto(r, trades);
    }

    public async Task DeleteBacktestAsync(Guid id, Guid userId)
    {
        await _backtestRepo.DeleteAsync(id, userId);
        _logger.LogInformation("Backtest {Id} deleted for user {UserId}", id, userId);
    }

    private static List<Trade> ApplyFilters(List<Trade> trades, List<BacktestRuleFilter> filters)
    {
        foreach (var f in filters)
        {
            switch (f.RuleType)
            {
                case BacktestRuleType.MinRRR when f.Value.HasValue:
                    trades = trades.Where(t => t.RiskRewardRatio >= f.Value.Value).ToList();
                    break;
                case BacktestRuleType.ChecklistCompliance when f.Value.HasValue:
                    trades = trades.Where(t => (t.ChecklistCompliancePercent ?? 0) >= f.Value.Value).ToList();
                    break;
                case BacktestRuleType.MinRiskPercent when f.Value.HasValue:
                    trades = trades.Where(t => t.RiskPercentage >= f.Value.Value).ToList();
                    break;
                case BacktestRuleType.MaxRiskPercent when f.Value.HasValue:
                    trades = trades.Where(t => t.RiskPercentage <= f.Value.Value).ToList();
                    break;
                case BacktestRuleType.TimeOfDayFrom when f.Value.HasValue:
                    trades = trades.Where(t => t.TradeDate.Hour >= (int)f.Value.Value).ToList();
                    break;
                case BacktestRuleType.TimeOfDayTo when f.Value.HasValue:
                    trades = trades.Where(t => t.TradeDate.Hour <= (int)f.Value.Value).ToList();
                    break;
                case BacktestRuleType.MaxDailyTrades when f.Value.HasValue:
                    var dayGroups = trades.GroupBy(t => t.TradeDate.Date)
                        .Where(g => g.Count() <= (int)f.Value.Value)
                        .SelectMany(g => g).ToHashSet();
                    trades = trades.Where(t => dayGroups.Contains(t)).ToList();
                    break;
                case BacktestRuleType.TradeType when f.Value.HasValue:
                    if ((int)f.Value.Value == 1)
                        trades = trades.Where(t => t.TradeType == TradeType.Buy).ToList();
                    else if ((int)f.Value.Value == 2)
                        trades = trades.Where(t => t.TradeType == TradeType.Sell).ToList();
                    break;
            }
        }
        return trades;
    }

    private static List<(Trade trade, decimal balance)> BuildEquityCurve(List<Trade> trades)
    {
        var curve = new List<(Trade, decimal)>();
        decimal running = 0;
        foreach (var t in trades)
        {
            running += t.ProfitLoss;
            curve.Add((t, running));
        }
        return curve;
    }

    private static List<decimal> ComputeDailyReturns(List<Trade> trades)
    {
        return trades
            .GroupBy(t => t.TradeDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => g.Sum(t => t.ProfitLoss))
            .ToList();
    }

    private static decimal ComputeSharpeRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2) return 0;
        var mean = dailyReturns.Average();
        var variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / (dailyReturns.Count - 1);
        var stdDev = (decimal)Math.Sqrt((double)variance);
        return stdDev == 0 ? 0 : Math.Round(mean / stdDev * (decimal)Math.Sqrt(252), 2);
    }

    private static decimal ComputeSortinoRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2) return 0;
        var mean = dailyReturns.Average();
        var negReturns = dailyReturns.Where(r => r < 0).ToList();
        if (negReturns.Count == 0) return mean > 0 ? 99 : 0;
        var downVar = negReturns.Sum(r => r * r) / negReturns.Count;
        var downDev = (decimal)Math.Sqrt((double)downVar);
        return downDev == 0 ? 0 : Math.Round(mean / downDev * (decimal)Math.Sqrt(252), 2);
    }

    private static (decimal maxDD, decimal maxDDPct) ComputeMaxDrawdown(List<(Trade, decimal balance)> curve)
    {
        if (curve.Count == 0) return (0, 0);
        decimal peak = curve[0].balance;
        decimal maxDD = 0;
        decimal maxDDPct = 0;
        foreach (var (_, bal) in curve)
        {
            if (bal > peak) peak = bal;
            var dd = peak - bal;
            if (dd > maxDD) { maxDD = dd; maxDDPct = peak != 0 ? dd / peak * 100 : 0; }
        }
        return (Math.Round(maxDD, 2), Math.Round(maxDDPct, 2));
    }

    private static MonteCarloDto RunMonteCarlo(List<decimal> plValues, decimal ruinThreshold)
    {
        const int simCount = 1000;
        const int maxSeriesInChart = 50;
        var rng = new Random(42);
        var finalPLs = new List<decimal>(simCount);
        var chartSeries = new List<List<decimal>>();
        int ruinCount = 0;

        for (int i = 0; i < simCount; i++)
        {
            var shuffled = plValues.OrderBy(_ => rng.Next()).ToList();
            decimal running = 0;
            var series = new List<decimal>();
            bool ruined = false;

            foreach (var pl in shuffled)
            {
                running += pl;
                series.Add(running);
                if (running <= ruinThreshold) { ruined = true; break; }
            }

            finalPLs.Add(running);
            if (ruined) ruinCount++;
            if (i < maxSeriesInChart) chartSeries.Add(series);
        }

        finalPLs.Sort();
        var p5 = finalPLs[(int)(simCount * 0.05)];
        var median = finalPLs[simCount / 2];
        var p95 = finalPLs[(int)(simCount * 0.95)];

        return new MonteCarloDto
        {
            Simulations = simCount,
            P5FinalPL = Math.Round(p5, 2),
            MedianFinalPL = Math.Round(median, 2),
            P95FinalPL = Math.Round(p95, 2),
            RuinProbability = Math.Round((decimal)ruinCount / simCount * 100, 2),
            ChartData = chartSeries
        };
    }

    private static BacktestResultDto CreateEmptyResult(BacktestRequestDto dto) => new()
    {
        Name = dto.Name,
        StrategyDescription = dto.StrategyDescription,
        FromDate = dto.FromDate,
        ToDate = dto.ToDate
    };

    private static BacktestResultDto MapToDto(BacktestResult r, List<BacktestTradeResult>? trades) => new()
    {
        Id = r.Id,
        Name = r.Name,
        StrategyDescription = r.StrategyDescription,
        FromDate = r.FromDate,
        ToDate = r.ToDate,
        InstrumentId = r.InstrumentId,
        TotalTrades = r.TotalTrades,
        WinningTrades = r.WinningTrades,
        LosingTrades = r.LosingTrades,
        TotalPL = r.TotalPL,
        WinRate = r.WinRate,
        ProfitFactor = r.ProfitFactor,
        SharpeRatio = r.SharpeRatio,
        SortinoRatio = r.SortinoRatio,
        MaxDrawdown = r.MaxDrawdown,
        MaxDrawdownPercent = r.MaxDrawdownPercent,
        AverageRRR = r.AverageRRR,
        BestDay = r.BestDay,
        WorstDay = r.WorstDay,
        Trades = trades ?? new(),
        CreatedAt = r.CreatedAt
    };
}
