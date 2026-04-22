using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingJournal.Application.DTOs.Notes;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INoteService noteService, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<PagedNotesDto>> GetAll([FromQuery] NoteFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Fetching notes for user: {UserId}", UserId);
            var result = await _noteService.GetAllAsync(UserId, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notes for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while fetching notes." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NoteDto>> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching note {Id} for user: {UserId}", id, UserId);
            var result = await _noteService.GetByIdAsync(id, UserId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching note {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while fetching the note." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create([FromBody] CreateNoteDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new note for user: {UserId}", UserId);
            var result = await _noteService.CreateAsync(dto, UserId);
            _logger.LogInformation("Successfully created note {Id} for user: {UserId}", result.Id, UserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note for user: {UserId}", UserId);
            return StatusCode(500, new { error = "An error occurred while creating the note." });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NoteDto>> Update(Guid id, [FromBody] UpdateNoteDto dto)
    {
        try
        {
            _logger.LogInformation("Updating note {Id} for user: {UserId}", id, UserId);
            var result = await _noteService.UpdateAsync(id, dto, UserId);
            _logger.LogInformation("Successfully updated note {Id} for user: {UserId}", id, UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while updating the note." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting note {Id} for user: {UserId}", id, UserId);
            await _noteService.DeleteAsync(id, UserId);
            _logger.LogInformation("Successfully deleted note {Id} for user: {UserId}", id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note {Id} for user: {UserId}", id, UserId);
            return StatusCode(500, new { error = "An error occurred while deleting the note." });
        }
    }
}
