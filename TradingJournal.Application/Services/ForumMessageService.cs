using TradingJournal.Application.DTOs.Forum;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class ForumMessageService : IForumMessageService
{
    private readonly IForumMessageRepository _repository;
    private readonly IUserRepository _userRepository;

    public ForumMessageService(IForumMessageRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<(List<ForumMessageDto> Messages, int TotalCount)> GetPublicMessagesAsync(int page, int pageSize)
    {
        var messages = await _repository.GetPublicMessagesAsync(page, pageSize);
        var count = await _repository.GetPublicMessagesCountAsync();

        var dtos = messages.Select(m => new ForumMessageDto
        {
            Id = m.Id,
            Content = m.Content,
            AuthorId = m.AuthorId,
            AuthorName = $"{m.Author.FirstName} {m.Author.LastName}".Trim(),
            AuthorInitials = GetInitials(m.Author.FirstName, m.Author.LastName),
            ChannelType = m.ChannelType,
            ParentMessageId = m.ParentMessageId,
            ReplyCount = m.Replies?.Count ?? 0,
            IsEdited = m.IsEdited,
            EditedAt = m.EditedAt,
            CreatedAt = m.CreatedAt
        }).ToList();

        return (dtos, count);
    }

    public async Task<ForumMessageDto> PostPublicMessageAsync(Guid authorId, CreateForumMessageDto dto)
    {
        var message = new ForumMessage
        {
            Content = dto.Content,
            AuthorId = authorId,
            ChannelType = ChannelType.PublicForum,
            ParentMessageId = dto.ParentMessageId
        };

        var created = await _repository.CreateAsync(message);

        return new ForumMessageDto
        {
            Id = created.Id,
            Content = created.Content,
            AuthorId = created.AuthorId,
            AuthorName = $"{created.Author.FirstName} {created.Author.LastName}".Trim(),
            AuthorInitials = GetInitials(created.Author.FirstName, created.Author.LastName),
            ChannelType = created.ChannelType,
            ParentMessageId = created.ParentMessageId,
            ReplyCount = 0,
            IsEdited = created.IsEdited,
            EditedAt = created.EditedAt,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task DeletePublicMessageAsync(Guid id, Guid userId, bool isAdmin)
    {
        var message = await _repository.GetByIdAsync(id);
        if (message == null) return;

        if (message.AuthorId == userId || isAdmin)
        {
            await _repository.DeleteAsync(message);
        }
        else
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this message.");
        }
    }

    public async Task<List<DirectMessageDto>> GetDirectMessagesAsync(Guid userId, Guid otherUserId)
    {
        var messages = await _repository.GetDirectMessagesAsync(userId, otherUserId);
        
        return messages.Select(m => new DirectMessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId ?? Guid.Empty,
            SenderName = $"{m.Author.FirstName} {m.Author.LastName}".Trim(),
            SenderInitials = GetInitials(m.Author.FirstName, m.Author.LastName),
            ReceiverId = m.ReceiverId ?? Guid.Empty,
            ReceiverName = "", // In UI, we usually know who we're talking to
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<DirectMessageDto> SendDirectMessageAsync(Guid senderId, CreateForumMessageDto dto)
    {
        if (!dto.ReceiverId.HasValue) throw new ArgumentException("ReceiverId is required for Direct Messages");

        var message = new ForumMessage
        {
            Content = dto.Content,
            AuthorId = senderId,
            ChannelType = ChannelType.DirectMessage,
            SenderId = senderId,
            ReceiverId = dto.ReceiverId
        };

        var created = await _repository.CreateAsync(message);

        return new DirectMessageDto
        {
            Id = created.Id,
            SenderId = created.SenderId ?? Guid.Empty,
            SenderName = $"{created.Author.FirstName} {created.Author.LastName}".Trim(),
            SenderInitials = GetInitials(created.Author.FirstName, created.Author.LastName),
            ReceiverId = created.ReceiverId ?? Guid.Empty,
            Content = created.Content,
            IsRead = created.IsRead,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task MarkAsReadAsync(Guid senderId, Guid receiverId)
    {
        await _repository.MarkAsReadAsync(senderId, receiverId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _repository.GetUnreadCountAsync(userId);
    }

    private static string GetInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrEmpty(firstName) ? "" : firstName[0].ToString();
        var last = string.IsNullOrEmpty(lastName) ? "" : lastName[0].ToString();
        return (first + last).ToUpper();
    }
}
