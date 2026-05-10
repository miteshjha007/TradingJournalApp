using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.DTOs.Dashboard;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.API.Controllers;

/// <summary>
/// Provides prop firm presets from the database so the frontend
/// can auto-fill account settings when user selects a firm + plan.
/// Presets are seeded in DbSeeder.cs — add new firms there.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropFirmController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PropFirmController> _logger;

    public PropFirmController(ApplicationDbContext db, ILogger<PropFirmController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all active prop firm presets (used for firm selector dropdown with auto-fill).
    /// </summary>
    [HttpGet("presets")]
    public async Task<ActionResult<List<PropFirmPresetDto>>> GetPresets()
    {
        _logger.LogInformation("Fetching all prop firm presets");
        var presets = await _db.PropFirmPresets
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.FirmName).ThenBy(p => p.AccountSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        _logger.LogInformation("Returning {Count} prop firm presets", presets.Count);
        return Ok(presets);
    }

    /// <summary>
    /// Get presets for a specific firm (e.g. "Funding Pips")
    /// </summary>
    [HttpGet("presets/{firmName}")]
    public async Task<ActionResult<List<PropFirmPresetDto>>> GetPresetsByFirm(string firmName)
    {
        _logger.LogInformation("Fetching presets for firm: {FirmName}", firmName);
        var presets = await _db.PropFirmPresets
            .Where(p => p.IsActive && !p.IsDeleted &&
                        p.FirmName.ToLower() == firmName.ToLower())
            .OrderBy(p => p.AccountSize)
            .Select(p => MapToDto(p))
            .ToListAsync();
        return Ok(presets);
    }

    /// <summary>Get unique list of firm names for the top-level dropdown</summary>
    [HttpGet("firm-names")]
    public async Task<ActionResult<List<string>>> GetFirmNames()
    {
        var names = await _db.PropFirmPresets
            .Where(p => p.IsActive && !p.IsDeleted)
            .Select(p => p.FirmName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
        return Ok(names);
    }

    private static PropFirmPresetDto MapToDto(TradingJournal.Domain.Entities.PropFirmPreset p) => new()
    {
        FirmName = p.FirmName,
        PlanName = p.PlanName,
        AccountSize = p.AccountSize,
        DailyDrawdownLimitPct = p.DailyDrawdownLimitPct,
        MaxOverallLossPct = p.MaxOverallLossPct,
        ProfitTargetPct = p.ProfitTargetPct,
        ProfitSplitPct = p.ProfitSplitPct,
        MinTradingDays = p.MinTradingDays,
        MaxAllowedLotSize = p.MaxAllowedLotSize,
        Has5xLotRule = p.Has5xLotRule,
        UseDynamicEquity = p.UseDynamicEquity,
        NewsTradeAllowed = p.NewsTradeAllowed,
        WeekendHoldingAllowed = p.WeekendHoldingAllowed,
        MaxRiskPerTradePctOfDailyLimit = p.MaxRiskPerTradePctOfDailyLimit
    };
}
