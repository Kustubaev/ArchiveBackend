using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Repositories;
public class ArchiveHistoryRepository : IArchiveHistoryRepository
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

    public async Task AddRangeAsync(List<ArchiveHistory> archiveHistories, CancellationToken cancellationToken = default)
    {
        _context.ArchiveHistories.AddRangeAsync(archiveHistories, cancellationToken);
    }

}
