namespace TradingJournal.Domain.Entities;

public class AiChatSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = "New Chat";
    public string MessagesJson { get; set; } = "[]";

    public User User { get; set; } = null!;
}
