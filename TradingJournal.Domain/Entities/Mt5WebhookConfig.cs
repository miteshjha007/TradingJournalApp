namespace TradingJournal.Domain.Entities;

public class Mt5WebhookConfig : BaseEntity
{
    public Guid UserId { get; set; }
    public string WebhookToken { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid? DefaultTradingAccountId { get; set; }
    public string? DefaultInstrumentMappings { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int TotalTradesImported { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;
    public TradingAccount? DefaultTradingAccount { get; set; }
}