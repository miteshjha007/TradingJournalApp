using TradingJournal.Application.DTOs.Forum;

namespace TradingJournal.Application.Interfaces;

public interface IForumMessageService
{
    Task<(List<ForumMessageDto> Messages, int TotalCount)> GetPublicMessagesAsync(int page, int pageSize);
    Task<ForumMessageDto> PostPublicMessageAsync(Guid authorId, CreateForumMessageDto dto);
    Task DeletePublicMessageAsync(Guid id, Guid userId, bool isAdmin);
    
    Task<List<DirectMessageDto>> GetDirectMessagesAsync(Guid userId, Guid otherUserId);
    Task<DirectMessageDto> SendDirectMessageAsync(Guid senderId, CreateForumMessageDto dto);
    Task MarkAsReadAsync(Guid senderId, Guid receiverId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
