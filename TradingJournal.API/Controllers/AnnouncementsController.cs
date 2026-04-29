using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TradingJournal.API.Hubs;
using TradingJournal.Application.DTOs.Forum;
using TradingJournal.Application.Interfaces;
using System.Security.Claims;

namespace TradingJournal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _service;
    private readonly IHubContext<ChatHub> _hubContext;

    public AnnouncementsController(IAnnouncementService service, IHubContext<ChatHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (announcements, totalCount) = await _service.GetAnnouncementsAsync(page, pageSize);
        return Ok(new { Announcements = announcements, TotalCount = totalCount });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
            return Unauthorized();

        var announcement = await _service.CreateAnnouncementAsync(adminId, dto);
        
        // Broadcast via SignalR
        await _hubContext.Clients.All.SendAsync("ReceiveAnnouncement", announcement);

        return Ok(announcement);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAnnouncementAsync(id);
        return NoContent();
    }
}
