using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storage;
    private readonly ILogger<UploadController> _logger;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public UploadController(IStorageService storage, ILogger<UploadController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpPost("chart")]
    public async Task<IActionResult> UploadChart([FromQuery] Guid tradeId, IFormFile file)
    {
        try
        {
            _logger.LogInformation("Chart upload started for trade {TradeId} by user {UserId}", tradeId, UserId);

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPG, PNG, and WebP images are allowed.");

            const long maxSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxSize)
                return BadRequest("File size exceeds 5MB limit.");

            using var stream = file.OpenReadStream();
            var url = await _storage.SaveChartAsync(stream, file.ContentType, UserId, tradeId);

            _logger.LogInformation("Chart uploaded for trade {TradeId} by user {UserId}: {Url}", tradeId, UserId, url);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chart for trade {TradeId} by user {UserId}", tradeId, UserId);
            return StatusCode(500, "Chart upload failed.");
        }
    }

    [HttpGet("chart/{tradeId:guid}")]
    public IActionResult GetChartUrl(Guid tradeId)
    {
        try
        {
            if (_storage.ChartExists(UserId, tradeId))
                return Ok(new { url = _storage.GetChartUrl(UserId, tradeId) });

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart URL for trade {TradeId}", tradeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("chart/{tradeId:guid}")]
    public async Task<IActionResult> DeleteChart(Guid tradeId)
    {
        try
        {
            await _storage.DeleteChartAsync(UserId, tradeId);
            _logger.LogInformation("Chart deleted for trade {TradeId} by user {UserId}", tradeId, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chart for trade {TradeId}", tradeId);
            return StatusCode(500, "Internal server error");
        }
    }
}
