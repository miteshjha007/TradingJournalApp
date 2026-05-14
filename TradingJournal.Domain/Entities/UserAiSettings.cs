using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class UserAiSettings : BaseEntity
{
    public Guid UserId { get; set; }
    public AiProvider Provider { get; set; } = AiProvider.OpenAI;
    public string? ApiKeyEncrypted { get; set; }
    public string? ModelName { get; set; }
    public string? CustomBaseUrl { get; set; }
    public bool IsConfigured { get; set; } = false;

    public User User { get; set; } = null!;
}
