namespace TradingJournal.Application.DTOs.Dashboard;

public class HeatmapCellDto
{
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public decimal TotalPL { get; set; }
    public int TradeCount { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgPL { get; set; }
    public decimal Intensity { get; set; }
}

public class SessionStatsDto
{
    public string Name { get; set; } = string.Empty;
    public decimal TotalPL { get; set; }
    public int TradeCount { get; set; }
    public decimal WinRate { get; set; }
}

public class HeatmapDto
{
    public List<HeatmapCellDto> Cells { get; set; } = new();
    public string BestSlot { get; set; } = string.Empty;
    public string WorstSlot { get; set; } = string.Empty;
    public decimal LondonSessionPL { get; set; }
    public decimal NYSessionPL { get; set; }
    public decimal AsiaSessionPL { get; set; }
    public List<SessionStatsDto> SessionBreakdown { get; set; } = new();
}
