using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingJournal.Application.DTOs.Playbook;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlaybookController : ControllerBase
{
    private readonly IPlaybookService _service;
    private readonly ILogger<PlaybookController> _logger;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public PlaybookController(IPlaybookService service, ILogger<PlaybookController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        try
        {
            _logger.LogInformation("GetRules called for user {UserId}", UserId);
            var rules = await _service.GetRulesAsync(UserId);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playbook rules for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreatePlaybookRuleDto dto)
    {
        try
        {
            _logger.LogInformation("CreateRule called for user {UserId}", UserId);
            var rule = await _service.CreateRuleAsync(UserId, dto);
            return CreatedAtAction(nameof(GetRules), new { }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating playbook rule for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] UpdatePlaybookRuleDto dto)
    {
        try
        {
            _logger.LogInformation("UpdateRule {RuleId} called for user {UserId}", id, UserId);
            var rule = await _service.UpdateRuleAsync(id, UserId, dto);
            return Ok(rule);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating playbook rule {RuleId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        try
        {
            _logger.LogInformation("DeleteRule {RuleId} called for user {UserId}", id, UserId);
            await _service.DeleteRuleAsync(id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting playbook rule {RuleId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderRules([FromBody] List<Guid> orderedIds)
    {
        try
        {
            _logger.LogInformation("ReorderRules called for user {UserId}", UserId);
            await _service.ReorderRulesAsync(UserId, orderedIds);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering playbook rules for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("checklist/{tradeId:guid}")]
    public async Task<IActionResult> GetChecklist(Guid tradeId)
    {
        try
        {
            _logger.LogInformation("GetChecklist for trade {TradeId} by user {UserId}", tradeId, UserId);
            var checklist = await _service.GetChecklistForTradeAsync(tradeId, UserId);
            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checklist for trade {TradeId}", tradeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("checklist")]
    public async Task<IActionResult> SaveChecklist([FromBody] SaveChecklistDto dto)
    {
        try
        {
            _logger.LogInformation("SaveChecklist for trade {TradeId} by user {UserId}", dto.TradeId, UserId);
            var compliance = await _service.SaveChecklistAsync(dto, UserId);
            return Ok(new { compliancePercent = compliance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving checklist for trade {TradeId}", dto.TradeId);
            return StatusCode(500, "Internal server error");
        }
    }
}
