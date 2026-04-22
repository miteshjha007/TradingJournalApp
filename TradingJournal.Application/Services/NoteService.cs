using TradingJournal.Application.DTOs.Notes;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;

    public NoteService(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<PagedNotesDto> GetAllAsync(Guid userId, NoteFilterDto filter)
    {
        var (notes, total) = await _noteRepository.GetPagedAsync(userId, filter.Page, filter.PageSize, filter.SearchTerm);
        return new PagedNotesDto
        {
            Notes = notes.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / filter.PageSize)
        };
    }

    public async Task<NoteDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var note = await _noteRepository.GetByIdAsync(id, userId);
        return note != null ? MapToDto(note) : null;
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto dto, Guid userId)
    {
        var note = new Note
        {
            UserId = userId,
            Title = dto.Title,
            Content = dto.Content,
            Tags = dto.Tags,
            IsPinned = dto.IsPinned
        };
        await _noteRepository.CreateAsync(note);
        return MapToDto(note);
    }

    public async Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto dto, Guid userId)
    {
        var note = await _noteRepository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException("Note not found.");
        note.Title = dto.Title;
        note.Content = dto.Content;
        note.Tags = dto.Tags;
        note.IsPinned = dto.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;
        await _noteRepository.UpdateAsync(note);
        return MapToDto(note);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        await _noteRepository.DeleteAsync(id, userId);
    }

    private NoteDto MapToDto(Note note) => new NoteDto
    {
        Id = note.Id,
        Title = note.Title,
        Content = note.Content,
        Tags = note.Tags,
        IsPinned = note.IsPinned,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt
    };
}
