using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class ForumMessageRepository : IForumMessageRepository
{
    private readonly ApplicationDbContext _context;

    public ForumMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ForumMessage>> GetPublicMessagesAsync(int page, int pageSize)
    {
        return await _context.ForumMessages
            .Include(m => m.Author)
            .Include(m => m.Replies)
            .Where(m => m.ChannelType == ChannelType.PublicForum && m.ParentMessageId == null)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetPublicMessagesCountAsync()
    {
        return await _context.ForumMessages
            .Where(m => m.ChannelType == ChannelType.PublicForum && m.ParentMessageId == null)
            .CountAsync();
    }

    public async Task<List<ForumMessage>> GetDirectMessagesAsync(Guid userId, Guid otherUserId)
    {
        return await _context.ForumMessages
            .Include(m => m.Author)
            .Where(m => m.ChannelType == ChannelType.DirectMessage &&
                        ((m.SenderId == userId && m.ReceiverId == otherUserId) ||
                         (m.SenderId == otherUserId && m.ReceiverId == userId)))
            .OrderBy(m => m.CreatedAt) // ascending order for chat history
            .ToListAsync();
    }

    public async Task<ForumMessage?> GetByIdAsync(Guid id)
    {
        return await _context.ForumMessages.FindAsync(id);
    }

    public async Task<ForumMessage> CreateAsync(ForumMessage message)
    {
        _context.ForumMessages.Add(message);
        await _context.SaveChangesAsync();
        
        // Load author info for DTO mapping
        if (message.Author == null)
        {
            await _context.Entry(message).Reference(m => m.Author).LoadAsync();
        }
        
        return message;
    }

    public async Task UpdateAsync(ForumMessage message)
    {
        _context.ForumMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ForumMessage message)
    {
        message.IsDeleted = true;
        _context.ForumMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.ForumMessages
            .Where(m => m.ChannelType == ChannelType.DirectMessage &&
                        m.ReceiverId == userId &&
                        !m.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid senderId, Guid receiverId)
    {
        var unreadMessages = await _context.ForumMessages
            .Where(m => m.ChannelType == ChannelType.DirectMessage &&
                        m.SenderId == senderId &&
                        m.ReceiverId == receiverId &&
                        !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }

        if (unreadMessages.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
