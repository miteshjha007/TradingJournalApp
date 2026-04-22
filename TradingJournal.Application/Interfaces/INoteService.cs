using TradingJournal.Application.DTOs.Notes;

namespace TradingJournal.Application.Interfaces;

public interface INoteService
{
    Task<PagedNotesDto> GetAllAsync(Guid userId, NoteFilterDto filter);
    Task<NoteDto?> GetByIdAsync(Guid id, Guid userId);
    Task<NoteDto> CreateAsync(CreateNoteDto dto, Guid userId);
    Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
