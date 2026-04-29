using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IForumMessageRepository
{
    Task<List<ForumMessage>> GetPublicMessagesAsync(int page, int pageSize);
    Task<int> GetPublicMessagesCountAsync();
    
    Task<List<ForumMessage>> GetDirectMessagesAsync(Guid userId, Guid otherUserId);
    
    Task<ForumMessage?> GetByIdAsync(Guid id);
    Task<ForumMessage> CreateAsync(ForumMessage message);
    Task UpdateAsync(ForumMessage message);
    Task DeleteAsync(ForumMessage message);
    
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid senderId, Guid receiverId);
}
