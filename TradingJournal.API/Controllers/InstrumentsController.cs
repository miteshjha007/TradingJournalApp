using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Instruments;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstrumentsController : ControllerBase
{
    private readonly IInstrumentService _instrumentService;
    private readonly ILogger<InstrumentsController> _logger;

    public InstrumentsController(IInstrumentService instrumentService, ILogger<InstrumentsController> logger)
    {
        _instrumentService = instrumentService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<List<InstrumentDto>>> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all instruments for user: {UserId}", UserId);
            var result = await _instrumentService.GetAllAsync(UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching instruments for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching instruments." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstrumentDto>> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching instrument {Id} for user: {UserId}", id, UserId);
            var result = await _instrumentService.GetByIdAsync(id, UserId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching instrument {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while fetching the instrument." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] CreateInstrumentDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new instrument {Name} for user: {UserId}", dto.Name, UserId);
            var result = await _instrumentService.CreateAsync(dto, UserId);
            _logger.LogInformation("Successfully created instrument {Id} for user: {UserId}", result.Id, UserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instrument {Name} for user: {UserId}", dto.Name, UserId);
            return StatusCode(500, new { error = "An error occurred while creating the instrument." });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InstrumentDto>> Update(Guid id, [FromBody] UpdateInstrumentDto dto)
    {
        try
        {
            _logger.LogInformation("Updating instrument {Id} for user: {UserId}", id, UserId);
            var result = await _instrumentService.UpdateAsync(id, dto, UserId);
            _logger.LogInformation("Successfully updated instrument {Id} for user: {UserId}", id, UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating instrument {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while updating the instrument." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting instrument {Id} for user: {UserId}", id, UserId);
            await _instrumentService.DeleteAsync(id, UserId);
            _logger.LogInformation("Successfully deleted instrument {Id} for user: {UserId}", id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting instrument {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while deleting the instrument." });
        }
    }
}
