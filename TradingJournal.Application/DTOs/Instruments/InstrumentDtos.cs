using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Instruments;

public class CreateInstrumentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal SafeLotSize { get; set; }
    public decimal MaxLot { get; set; }
    public VolatilityLevel VolatilityLevel { get; set; }
    public string? Notes { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }
}

public class UpdateInstrumentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal SafeLotSize { get; set; }
    public decimal MaxLot { get; set; }
    public VolatilityLevel VolatilityLevel { get; set; }
    public string? Notes { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }
}

public class InstrumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SafeLotSize { get; set; }
    public decimal MaxLot { get; set; }
    public string VolatilityLevel { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalTrades { get; set; }
    public decimal TotalPL { get; set; }
    public decimal WinRate { get; set; }
}
