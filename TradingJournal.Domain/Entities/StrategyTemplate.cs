using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Domain.Entities;

public class StrategyTemplate : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(50)]
    public string Methodology { get; set; } = string.Empty; // SMC, Price Action, etc.

    [MaxLength(50)]
    public string Instrument { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public List<string> Rules { get; set; } = new();

    public string DefaultFilters { get; set; } = "{}"; // JSON string of ExtractedStrategyFilters

    [MaxLength(100)]
    public string? SessionBadge { get; set; }

    [MaxLength(100)]
    public string? TimeframeBadge { get; set; }

    public decimal MinRRR { get; set; }

    public bool IsSystemTemplate { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public bool IsActive { get; set; } = true;
}
