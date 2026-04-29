using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class ForumMessage : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    
    public ChannelType ChannelType { get; set; }
    
    public Guid? ParentMessageId { get; set; }
    public ForumMessage? ParentMessage { get; set; }
    public ICollection<ForumMessage> Replies { get; set; } = new List<ForumMessage>();
    
    // For Direct Messages
    public Guid? SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public bool IsRead { get; set; }
    
    // Edit tracking
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
}
