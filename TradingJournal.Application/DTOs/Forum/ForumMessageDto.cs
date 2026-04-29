using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Forum;

public class ForumMessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    
    public ChannelType ChannelType { get; set; }
    
    public Guid? ParentMessageId { get; set; }
    public int ReplyCount { get; set; }
    
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
