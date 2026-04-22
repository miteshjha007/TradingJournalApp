using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Trades;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradesController : ControllerBase
{
    private readonly ITradeService _tradeService;
    private readonly ILogger<TradesController> _logger;

    public TradesController(ITradeService tradeService, ILogger<TradesController> logger)
    {
        _tradeService = tradeService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<PagedTradesDto>> GetAll([FromQuery] TradeFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Fetching trades for user: {UserId}", UserId);
            var result = await _tradeService.GetAllAsync(UserId, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trades for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching trades." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TradeDto>> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching trade {Id} for user: {UserId}", id, UserId);
            var result = await _tradeService.GetByIdAsync(id, UserId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trade {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while fetching the trade." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TradeDto>> Create([FromBody] CreateTradeDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new trade for user: {UserId}", UserId);
            var result = await _tradeService.CreateAsync(dto, UserId);
            _logger.LogInformation("Successfully created trade {Id} for user: {UserId}", result.Id, UserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trade for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while creating the trade." });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TradeDto>> Update(Guid id, [FromBody] UpdateTradeDto dto)
    {
        try
        {
            _logger.LogInformation("Updating trade {Id} for user: {UserId}", id, UserId);
            var result = await _tradeService.UpdateAsync(id, dto, UserId);
            _logger.LogInformation("Successfully updated trade {Id} for user: {UserId}", id, UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trade {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while updating the trade." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting trade {Id} for user: {UserId}", id, UserId);
            await _tradeService.DeleteAsync(id, UserId);
            _logger.LogInformation("Successfully deleted trade {Id} for user: {UserId}", id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trade {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while deleting the trade." });
        }
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<List<TradeDto>>> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            _logger.LogInformation("Fetching calendar data for user: {UserId}, Year: {Year}, Month: {Month}", UserId, year, month);
            var result = await _tradeService.GetForCalendarAsync(UserId, year, month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calendar data for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching calendar data." });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] TradeFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Exporting trades for user: {UserId}", UserId);
            var csv = await _tradeService.ExportToCsvAsync(UserId, filter);
            _logger.LogInformation("Successfully exported trades for user: {UserId}", UserId);
            return File(csv, "text/csv", $"trades_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting trades for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while exporting trades." });
        }
    }
}
