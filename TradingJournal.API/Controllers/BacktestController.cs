using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingJournal.Application.DTOs.Backtest;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly IBacktestService _service;
    private readonly ILogger<BacktestController> _logger;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public BacktestController(IBacktestService service, ILogger<BacktestController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestRequestDto dto)
    {
        try
        {
            _logger.LogInformation("RunBacktest '{Name}' called for user {UserId}", dto.Name, UserId);
            var result = await _service.RunBacktestAsync(UserId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running backtest for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        try
        {
            _logger.LogInformation("GetBacktestHistory called for user {UserId}", UserId);
            var history = await _service.GetBacktestHistoryAsync(UserId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backtest history for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("GetBacktest {Id} called for user {UserId}", id, UserId);
            var result = await _service.GetBacktestByIdAsync(id, UserId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backtest {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation("DeleteBacktest {Id} called for user {UserId}", id, UserId);
            await _service.DeleteBacktestAsync(id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backtest {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
