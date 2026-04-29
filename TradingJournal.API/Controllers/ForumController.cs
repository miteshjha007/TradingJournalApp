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
public class ForumController : ControllerBase
{
    private readonly IForumMessageService _service;
    private readonly IHubContext<ChatHub> _hubContext;

    public ForumController(IForumMessageService service, IHubContext<ChatHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (messages, totalCount) = await _service.GetPublicMessagesAsync(page, pageSize);
        return Ok(new { Messages = messages, TotalCount = totalCount });
    }

    [HttpPost("public")]
    public async Task<IActionResult> PostPublicMessage([FromBody] CreateForumMessageDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var message = await _service.PostPublicMessageAsync(userId, dto);
        
        await _hubContext.Clients.All.SendAsync("ReceiveForumMessage", message);

        return Ok(message);
    }

    [HttpDelete("public/{id}")]
    public async Task<IActionResult> DeletePublicMessage(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        await _service.DeletePublicMessageAsync(id, userId, isAdmin);
        
        // Notify clients to remove message
        await _hubContext.Clients.All.SendAsync("MessageDeleted", id);

        return NoContent();
    }

    [HttpGet("dm/{otherUserId}")]
    public async Task<IActionResult> GetDirectMessages(Guid otherUserId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var messages = await _service.GetDirectMessagesAsync(userId, otherUserId);
        return Ok(messages);
    }

    [HttpPost("dm")]
    public async Task<IActionResult> SendDirectMessage([FromBody] CreateForumMessageDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var message = await _service.SendDirectMessageAsync(userId, dto);
        
        if (dto.ReceiverId.HasValue)
        {
            await _hubContext.Clients.User(dto.ReceiverId.Value.ToString())
                .SendAsync("ReceiveDirectMessage", message);
            
            // Also send to caller's other connections
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveDirectMessage", message);
        }

        return Ok(message);
    }

    [HttpPut("dm/read/{senderId}")]
    public async Task<IActionResult> MarkAsRead(Guid senderId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _service.MarkAsReadAsync(senderId, userId);
        return NoContent();
    }

    [HttpGet("dm/unread")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var count = await _service.GetUnreadCountAsync(userId);
        return Ok(new UnreadCountDto { UserId = userId, UnreadCount = count });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsersForChat([FromServices] TradingJournal.Infrastructure.Data.ApplicationDbContext context)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var currentUserId))
            return Unauthorized();

        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            System.Linq.Queryable.Select(
                System.Linq.Queryable.Where(context.Users, u => u.Id != currentUserId && !u.IsDeleted),
                u => new { Id = u.Id, Name = u.FirstName + " " + u.LastName, Email = u.Email }
            )
        );
        return Ok(users);
    }
}
