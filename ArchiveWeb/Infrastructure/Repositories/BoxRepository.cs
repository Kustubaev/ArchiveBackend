using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Repositories;
public class BoxRepository : IBoxRepository
{
    private readonly ArchiveDbContext _context;
    public BoxRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes.AnyAsync(cancellationToken);
    }

    public async Task<List<Box>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes.ToListAsync(cancellationToken);
    }

    public async Task RemoveRangeAsync(List<Box> boxes, CancellationToken cancellationToken = default)
    {
        _context.Boxes.RemoveRange(boxes);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _context.Boxes.RemoveRange(_context.Boxes);
    }

    public async Task AddRangeAsync(List<Box> boxes, CancellationToken cancellationToken = default)
    {
        _context.Boxes.AddRangeAsync(boxes, cancellationToken);
    }

}
