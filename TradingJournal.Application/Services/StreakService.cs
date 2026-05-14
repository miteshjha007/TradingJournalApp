using Microsoft.Extensions.Logging;
using TradingJournal.Application.DTOs.Dashboard;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class StreakService : IStreakService
{
    private readonly IUserRepository _userRepo;
    private readonly ITradeRepository _tradeRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly ILogger<StreakService> _logger;

    public StreakService(
        IUserRepository userRepo,
        ITradeRepository tradeRepo,
        IAlertRepository alertRepo,
        ILogger<StreakService> logger)
    {
        _userRepo = userRepo;
        _tradeRepo = tradeRepo;
        _alertRepo = alertRepo;
        _logger = logger;
    }

    public async Task<StreakDto> GetStreakAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var allTrades = await _tradeRepo.GetByUserIdAsync(userId);
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-7);

        var todayTrades = allTrades.Where(t => t.TradeDate.Date == today).ToList();
        var weekTrades = allTrades.Where(t => t.TradeDate.Date >= weekStart).ToList();

        var tradedToday = todayTrades.Count > 0;
        var checklistAvgToday = todayTrades.Count > 0
            ? todayTrades.Average(t => t.ChecklistCompliancePercent ?? 0)
            : 0;

        // Compute discipline score components
        int score = 0;

        // +25 if today avg compliance >= 80%
        if (checklistAvgToday >= 80) score += 25;

        // +25 if no daily limit breach this week
        var alert = await _alertRepo.GetByUserIdAsync(userId);
        var noBreachThisWeek = !HasDailyLimitBreach(weekTrades, alert?.DailyLossLimit);
        if (noBreachThisWeek) score += 25;

        // +25 if avg RRR >= 1.5 this week
        var weekAvgRRR = weekTrades.Count > 0 ? weekTrades.Average(t => t.RiskRewardRatio) : 0;
        if (weekAvgRRR >= 1.5m) score += 25;

        // +25 if avg daily trades <= 5 this week
        var avgDailyTrades = weekTrades.Count > 0
            ? weekTrades.GroupBy(t => t.TradeDate.Date).Average(g => (double)g.Count())
            : 0;
        if (avgDailyTrades <= 5) score += 25;

        var grade = score >= 90 ? "A (Excellent)"
            : score >= 75 ? "B (Good)"
            : score >= 50 ? "C (Average)"
            : "D (Needs Work)";

        return new StreakDto
        {
            CurrentStreak = user.CurrentStreak,
            LongestStreak = user.LongestStreak,
            DisciplineScore = score,
            DisciplineGrade = grade,
            TradedToday = tradedToday,
            ChecklistAvgComplianceToday = (decimal)checklistAvgToday
        };
    }

    public async Task UpdateStreakOnLoginAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return;

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        if (user.LastLoginDate.HasValue)
        {
            var lastDay = user.LastLoginDate.Value.Date;

            var allTrades = await _tradeRepo.GetByUserIdAsync(userId);
            var tradedYesterday = allTrades.Any(t => t.TradeDate.Date == yesterday);

            if (lastDay == yesterday && tradedYesterday)
                user.CurrentStreak += 1;
            else if (lastDay < yesterday)
                user.CurrentStreak = 0;
        }
        else
        {
            user.CurrentStreak = 0;
        }

        if (user.CurrentStreak > user.LongestStreak)
            user.LongestStreak = user.CurrentStreak;

        user.LastLoginDate = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Streak updated for user {UserId}: streak={Streak}", userId, user.CurrentStreak);
    }

    private static bool HasDailyLimitBreach(List<Domain.Entities.Trade> trades, decimal? limit)
    {
        if (!limit.HasValue || limit.Value == 0) return false;
        return trades
            .GroupBy(t => t.TradeDate.Date)
            .Any(g => g.Where(t => t.Result == TradeResult.Loss).Sum(t => Math.Abs(t.ProfitLoss)) >= limit.Value);
    }
}
