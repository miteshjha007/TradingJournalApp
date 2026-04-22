using TradingJournal.Application.DTOs.Instruments;

namespace TradingJournal.Application.Interfaces;

public interface IInstrumentService
{
    Task<List<InstrumentDto>> GetAllAsync(Guid userId);
    Task<InstrumentDto?> GetByIdAsync(Guid id, Guid userId);
    Task<InstrumentDto> CreateAsync(CreateInstrumentDto dto, Guid userId);
    Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
