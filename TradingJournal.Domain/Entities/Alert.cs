namespace TradingJournal.Domain.Entities;

public class Alert : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal DailyLossLimit { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public int MaxTradesPerDay { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public bool EmailAlertEnabled { get; set; } = false;
    public string? Email { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
