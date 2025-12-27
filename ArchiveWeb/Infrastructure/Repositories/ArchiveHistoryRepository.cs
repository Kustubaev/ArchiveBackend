using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Repositories;
public sealed class ArchiveHistoryRepository : IArchiveHistoryRepository
{
    private readonly ArchiveDbContext _context;
    public ArchiveHistoryRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories.AnyAsync(cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ArchiveHistory archiveHistory, CancellationToken cancellationToken = default)
    {
        await _context.ArchiveHistories.AddAsync(archiveHistory, cancellationToken);
    }

    public async Task AddRangeAsync(List<ArchiveHistory> archiveHistories, CancellationToken cancellationToken = default)
    {
        await _context.ArchiveHistories.AddRangeAsync(archiveHistories, cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetByFileArchiveIdAsync(Guid fileArchiveId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.FileArchiveId == fileArchiveId)
            .OrderBy(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByFileArchiveIdAsync(Guid fileArchiveId, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Where(h => h.FileArchiveId == fileArchiveId)
            .CountAsync(cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetByBoxIdAsync(Guid boxId, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.OldBoxId == boxId || h.NewBoxId == boxId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetByBoxNumberAsync(int boxNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.OldBoxNumber == boxNumber || h.NewBoxNumber == boxNumber)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetByLetterIdAsync(Guid letterId, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.OldLetterId == letterId || h.NewLetterId == letterId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ArchiveHistory>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .OrderBy(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ArchiveHistories.CountAsync(cancellationToken);
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.ArchiveHistories.RemoveRange(_context.ArchiveHistories);
        return Task.CompletedTask;
    }
}
