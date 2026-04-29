namespace TradingJournal.Application.DTOs.Forum;

public class DirectMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderInitials { get; set; } = string.Empty;
    
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
