using TradingJournal.Application.DTOs.Dashboard;
using TradingJournal.Application.DTOs.Analytics;

namespace TradingJournal.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid userId);
    Task<List<CalendarDayDto>> GetCalendarDataAsync(Guid userId, int year, int month);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(Guid userId);
    Task<DrawdownDto> GetDrawdownAsync(Guid userId);
    Task<AiAnalysisDto> GetAiInsightsAsync(Guid userId);
    Task<RiskResultDto> CalculateRiskAsync(RiskCalculationDto dto, Guid userId);
    Task<PropRiskResultDto> CalculatePropRiskAsync(PropRiskCalculationDto dto, Guid userId);
    Task<AlertDto?> GetAlertAsync(Guid userId);
    Task<AlertDto> UpsertAlertAsync(CreateAlertDto dto, Guid userId);

    Task<PropFirmStatusDto?> GetPropFirmStatusAsync(Guid userId);
    Task<HeatmapDto> GetHeatmapAsync(Guid userId);
    Task<ShadowProfileDto> GetShadowProfileAsync(Guid userId);
}
