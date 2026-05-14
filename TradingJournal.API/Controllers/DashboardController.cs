using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Analytics;
using TradingJournal.Application.DTOs.Dashboard;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStreakService _streakService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, IStreakService streakService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _streakService = streakService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Get full dashboard summary with equity curve, monthly P/L</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        try
        {
            _logger.LogInformation("Fetching dashboard summary for user: {UserId}", UserId);
            var result = await _dashboardService.GetSummaryAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard summary for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching dashboard summary." });
        }
    }

    /// <summary>Get trading calendar data for a month</summary>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<CalendarDayDto>>> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            year = year == 0 ? DateTime.UtcNow.Year : year;
            month = month == 0 ? DateTime.UtcNow.Month : month;
            _logger.LogInformation("Fetching calendar data for user: {UserId}, Year: {Year}, Month: {Month}", UserId, year, month);
            var result = await _dashboardService.GetCalendarDataAsync(UserId, year, month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calendar data for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching calendar data." });
        }
    }

    /// <summary>Get performance metrics (Sharpe, Win/Loss, etc.)</summary>
    [HttpGet("performance")]
    public async Task<ActionResult<PerformanceMetricsDto>> GetPerformance()
    {
        try
        {
            _logger.LogInformation("Fetching performance metrics for user: {UserId}", UserId);
            var result = await _dashboardService.GetPerformanceMetricsAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance metrics for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching performance metrics." });
        }
    }

    /// <summary>Get drawdown tracking info</summary>
    [HttpGet("drawdown")]
    public async Task<ActionResult<DrawdownDto>> GetDrawdown()
    {
        try
        {
            _logger.LogInformation("Fetching drawdown for user: {UserId}", UserId);
            var result = await _dashboardService.GetDrawdownAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching drawdown for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching drawdown tracking info." });
        }
    }

    /// <summary>Get AI trade analysis and insights</summary>
    [HttpGet("ai-insights")]
    public async Task<ActionResult<AiAnalysisDto>> GetAiInsights()
    {
        try
        {
            _logger.LogInformation("Fetching AI insights for user: {UserId}", UserId);
            var result = await _dashboardService.GetAiInsightsAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching AI insights for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching AI insights." });
        }
    }

    /// <summary>Calculate risk and suggested lot size</summary>
    [HttpPost("risk-calculate")]
    public async Task<ActionResult<RiskResultDto>> CalculateRisk([FromBody] RiskCalculationDto dto)
    {
        try
        {
            _logger.LogInformation("Calculating risk for user: {UserId}", UserId);
            var result = await _dashboardService.CalculateRiskAsync(dto, UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating risk for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while calculating risk." });
        }
    }

    /// <summary>Calculate prop-firm specific lot size (5x rule, drawdown, pip value)</summary>
    [HttpPost("prop-risk-calculate")]
    public async Task<ActionResult<PropRiskResultDto>> CalculatePropRisk([FromBody] PropRiskCalculationDto dto)
    {
        try
        {
            _logger.LogInformation("Calculating prop risk for user: {UserId}", UserId);
            var result = await _dashboardService.CalculatePropRiskAsync(dto, UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating prop risk for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while calculating prop firm risk." });
        }
    }

    /// <summary>Get alert settings</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<AlertDto?>> GetAlert()
    {
        try
        {
            _logger.LogInformation("Fetching alert settings for user: {UserId}", UserId);
            var result = await _dashboardService.GetAlertAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert settings for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching alert settings." });
        }
    }

    /// <summary>Create or update alert settings</summary>
    [HttpPost("alerts")]
    public async Task<ActionResult<AlertDto>> UpsertAlert([FromBody] CreateAlertDto dto)
    {
        try
        {
            _logger.LogInformation("Upserting alert settings for user: {UserId}", UserId);
            var result = await _dashboardService.UpsertAlertAsync(dto, UserId);
            _logger.LogInformation("Successfully upserted alert settings for user: {UserId}", UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting alert settings for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while updating alert settings." });
        }
    }

    [HttpGet("streak")]
    public async Task<IActionResult> GetStreak()
    {
        try
        {
            _logger.LogInformation("Fetching streak for user: {UserId}", UserId);
            var result = await _streakService.GetStreakAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching streak for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching streak." });
        }
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap()
    {
        try
        {
            _logger.LogInformation("Fetching heatmap for user: {UserId}", UserId);
            var result = await _dashboardService.GetHeatmapAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching heatmap for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching heatmap." });
        }
    }

    [HttpGet("shadow-profile")]
    public async Task<IActionResult> GetShadowProfile()
    {
        try
        {
            _logger.LogInformation("Fetching shadow profile for user: {UserId}", UserId);
            var result = await _dashboardService.GetShadowProfileAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shadow profile for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching shadow profile." });
        }
    }

    /// <summary>
    /// Get real-time prop firm rule engine status — daily loss, overall drawdown,
    /// profit target progress, trading days. Returns null if account is not a prop firm.
    /// </summary>
    [HttpGet("prop-firm-status")]
    public async Task<ActionResult<PropFirmStatusDto?>> GetPropFirmStatus()
    {
        try
        {
            _logger.LogInformation("Fetching prop firm status for user: {UserId}", UserId);
            var result = await _dashboardService.GetPropFirmStatusAsync(UserId);

            if (result == null)
            {
                _logger.LogInformation("No prop firm account found for user: {UserId} — returning null", UserId);
                return Ok(null);
            }

            _logger.LogInformation(
                "Prop firm status for user {UserId}: Status={Status}, DailyUsed={DailyPct}%, OverallDD={OverallPct}%",
                UserId, result.AccountStatus, result.DailyLossUsedPct, result.TotalDrawdownPct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prop firm status for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching prop firm status." });
        }
    }
}

