using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Import;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize]
public class TradeImportController : ControllerBase
{
    private readonly ITradeImportService _tradeImportService;
    private readonly ILogger<TradeImportController> _logger;

    public TradeImportController(ITradeImportService tradeImportService, ILogger<TradeImportController> logger)
    {
        _tradeImportService = tradeImportService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    [HttpPost("csv/preview")]
    public async Task<ActionResult<CsvImportPreviewDto>> PreviewCsvImport([FromBody] CsvImportRequestDto dto)
    {
        try
        {
            _logger.LogInformation("CSV preview for user: {UserId}", UserId);
            var result = await _tradeImportService.ParseCsvAsync(dto, UserId);
            _logger.LogInformation("CSV preview completed: {TotalRows} rows parsed", result.TotalRows);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing CSV for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while previewing the CSV file." });
        }
    }

    [HttpPost("csv/confirm")]
    public async Task<ActionResult<ImportResultDto>> ConfirmCsvImport([FromBody] CsvImportConfirmDto dto)
    {
        try
        {
            _logger.LogInformation("CSV import confirmed for user: {UserId}", UserId);
            var result = await _tradeImportService.ConfirmCsvImportAsync(dto, UserId);
            _logger.LogInformation("CSV import completed: {Summary}", result.Summary);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming CSV import for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while importing trades." });
        }
    }

    [HttpGet("mt5/config")]
    public async Task<ActionResult<Mt5WebhookConfigDto>> GetMt5Config()
    {
        try
        {
            _logger.LogInformation("Getting MT5 config for user: {UserId}", UserId);
            var result = await _tradeImportService.GetOrCreateConfigAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MT5 config for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching MT5 configuration." });
        }
    }

    [HttpPut("mt5/config")]
    public async Task<ActionResult<Mt5WebhookConfigDto>> UpdateMt5Config([FromBody] UpdateMt5ConfigDto dto)
    {
        try
        {
            _logger.LogInformation("Updating MT5 config for user: {UserId}", UserId);
            var result = await _tradeImportService.UpdateConfigAsync(UserId, dto);
            _logger.LogInformation("MT5 config updated for user: {UserId}", UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MT5 config for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while updating MT5 configuration." });
        }
    }

    [HttpPost("mt5/regenerate-token")]
    public async Task<ActionResult<Mt5WebhookConfigDto>> RegenerateToken()
    {
        try
        {
            var existingConfig = await _tradeImportService.GetOrCreateConfigAsync(UserId);
            var updateDto = new UpdateMt5ConfigDto
            {
                IsActive = existingConfig.IsActive,
                Description = existingConfig.Description,
                DefaultTradingAccountId = existingConfig.DefaultTradingAccountId,
                InstrumentMappings = existingConfig.InstrumentMappings
            };

            var config = await _tradeImportService.GetOrCreateConfigAsync(UserId);
            var newMappings = new Dictionary<string, string>();
            foreach (var kvp in config.InstrumentMappings)
            {
                newMappings[kvp.Key] = kvp.Value;
            }

            var regenerateDto = new UpdateMt5ConfigDto
            {
                IsActive = config.IsActive,
                Description = config.Description,
                DefaultTradingAccountId = config.DefaultTradingAccountId,
                InstrumentMappings = newMappings
            };

            var result = await _tradeImportService.UpdateConfigAsync(UserId, regenerateDto);
            _logger.LogInformation("MT5 token regenerated for user: {UserId}", UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating MT5 token for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while regenerating the token." });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<ImportLogDto>>> GetImportHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting import history for user: {UserId}, page: {Page}", UserId, page);
            var result = await _tradeImportService.GetImportHistoryAsync(UserId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import history for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching import history." });
        }
    }

    [HttpPost("mt5-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Mt5Webhook([FromBody] Mt5TradePayloadDto dto)
    {
        try
        {
            var token = Request.Headers["X-Webhook-Token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("MT5 webhook rejected: missing token");
                return Ok(new { success = false, message = "Missing token" });
            }

            var result = await _tradeImportService.ProcessMt5WebhookAsync(dto, token);
            _logger.LogInformation("MT5 webhook processed: {Summary}", result.Summary);
            return Ok(new { success = true, message = result.Summary });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("MT5 webhook rejected: invalid token");
            return Ok(new { success = false, message = "Invalid token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MT5 webhook error");
            return Ok(new { success = false, message = "Server error" });
        }
    }

    [HttpGet("mt5-ea-download")]
    [AllowAnonymous]
    public IActionResult DownloadMt5Ea()
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", "TradingJournalEA.mq5");
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("MT5 EA file not found: {Path}", filePath);
                return NotFound(new { error = "EA file not found" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/octet-stream", "TradingJournalEA.mq5");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading MT5 EA");
            return StatusCode(500, new { error = "Failed to download EA" });
        }
    }
}