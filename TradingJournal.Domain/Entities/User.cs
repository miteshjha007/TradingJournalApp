using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public decimal AccountBalance { get; set; } = 0;
    public string? ProfileImageUrl { get; set; }

    // Navigation
    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<TradingAccount> TradingAccounts { get; set; } = new List<TradingAccount>();
    
    // Permissions
    public List<string> AllowedSections { get; set; } = new();
}
