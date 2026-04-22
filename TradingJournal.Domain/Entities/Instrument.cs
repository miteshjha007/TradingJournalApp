using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class Instrument : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SafeLotSize { get; set; }
    public decimal MaxLot { get; set; }
    public VolatilityLevel VolatilityLevel { get; set; } = VolatilityLevel.Medium;
    public string? Notes { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
