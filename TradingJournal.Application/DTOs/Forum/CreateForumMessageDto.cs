using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Forum;

public class CreateForumMessageDto
{
    public string Content { get; set; } = string.Empty;
    public ChannelType ChannelType { get; set; } = ChannelType.PublicForum;
    public Guid? ParentMessageId { get; set; }
    public Guid? ReceiverId { get; set; }
}
