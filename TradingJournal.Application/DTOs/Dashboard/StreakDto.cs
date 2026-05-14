namespace TradingJournal.Application.DTOs.Dashboard;

public class StreakDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int DisciplineScore { get; set; }
    public string DisciplineGrade { get; set; } = string.Empty;
    public bool TradedToday { get; set; }
    public decimal ChecklistAvgComplianceToday { get; set; }
    public string? StreakBrokenReason { get; set; }
}
