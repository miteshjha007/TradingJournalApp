using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TradingJournal.Application.DTOs.Forum;

namespace TradingJournal.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendAnnouncement(AnnouncementDto dto)
    {
        await Clients.All.SendAsync("ReceiveAnnouncement", dto);
    }

    public async Task SendForumMessage(ForumMessageDto dto)
    {
        await Clients.All.SendAsync("ReceiveForumMessage", dto);
    }

    public async Task SendDirectMessage(string receiverUserId, DirectMessageDto dto)
    {
        await Clients.User(receiverUserId).SendAsync("ReceiveDirectMessage", dto);
        // Also send back to sender so their UI updates if multiple clients open
        await Clients.Caller.SendAsync("ReceiveDirectMessage", dto);
    }

    public override async Task OnConnectedAsync()
    {
        // Add to online users tracking if needed
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove from online users
        await base.OnDisconnectedAsync(exception);
    }
}
