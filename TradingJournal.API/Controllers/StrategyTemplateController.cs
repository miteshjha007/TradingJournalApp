using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingJournal.Application.DTOs.StrategyTemplates;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/strategy-templates")]
public class StrategyTemplateController : ControllerBase
{
    private readonly IStrategyTemplateService _service;
    private readonly ILogger<StrategyTemplateController> _logger;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public StrategyTemplateController(IStrategyTemplateService service, ILogger<StrategyTemplateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] string? instrument)
    {
        try
        {
            _logger.LogInformation("GetTemplates called for user {UserId} with instrument {Instrument}", UserId, instrument);
            var templates = await _service.GetTemplatesAsync(UserId, instrument);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateStrategyTemplateDto dto)
    {
        try
        {
            _logger.LogInformation("CreateTemplate called for user {UserId}", UserId);
            var result = await _service.CreateCustomTemplateAsync(UserId, dto);
            return CreatedAtAction(nameof(GetTemplates), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] CreateStrategyTemplateDto dto)
    {
        try
        {
            _logger.LogInformation("UpdateTemplate called for user {UserId}, id {Id}", UserId, id);
            await _service.UpdateCustomTemplateAsync(UserId, id, dto);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            _logger.LogInformation("DeleteTemplate called for user {UserId}, id {Id}", UserId, id);
            await _service.DeleteCustomTemplateAsync(UserId, id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }
}
