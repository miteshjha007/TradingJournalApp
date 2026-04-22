namespace TradingJournal.Domain.Entities;

public class TradingAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Broker { get; set; }
    public bool IsDefault { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
