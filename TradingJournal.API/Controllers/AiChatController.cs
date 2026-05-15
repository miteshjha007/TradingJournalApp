using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using TradingJournal.Application.DTOs.Ai;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _service;
    private readonly ILogger<AiChatController> _logger;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public AiChatController(IAiChatService service, ILogger<AiChatController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            _logger.LogInformation("GetAiSettings called for user {UserId}", UserId);
            var settings = await _service.GetSettingsAsync(UserId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI settings for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] SaveAiSettingsDto dto)
    {
        try
        {
            _logger.LogInformation("SaveAiSettings called for user {UserId}", UserId);
            await _service.SaveSettingsAsync(UserId, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving AI settings for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("chat")]
    public async Task SendMessage([FromBody] SendAiMessageDto dto, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            _logger.LogInformation("AI chat message sent by user {UserId}", UserId);

            await foreach (var token in _service.SendMessageAsync(UserId, dto).WithCancellation(ct))
            {
                var data = $"data: {token}\n\n";
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(data), ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        catch (InvalidOperationException ex)
        {
            // SSE lines must not contain newlines — collapse the full error (incl. multi-line JSON bodies) to one line
            var singleLineMsg = ex.Message.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            _logger.LogError("AI provider error for user {UserId}: {Message}", UserId, ex.Message);
            var errMsg = $"data: [ERROR] {singleLineMsg}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(errMsg), ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI chat for user {UserId}", UserId);
            var errMsg = "data: [ERROR] An error occurred. Please try again.\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(errMsg), ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            _logger.LogInformation("GetAiSessions called for user {UserId}", UserId);
            var sessions = await _service.GetSessionsAsync(UserId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI sessions for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        try
        {
            _logger.LogInformation("GetAiSession {SessionId} called for user {UserId}", id, UserId);
            var session = await _service.GetSessionAsync(id, UserId);
            return session == null ? NotFound() : Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            _logger.LogInformation("DeleteAiSession {SessionId} called for user {UserId}", id, UserId);
            await _service.DeleteSessionAsync(id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting AI session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
