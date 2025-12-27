using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace ArchiveWeb.Infrastructure.Repositories;
public sealed class ArchiveConfigurationRepository : IArchiveConfigurationRepository
{
    private readonly ArchiveDbContext _context;
    public ArchiveConfigurationRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
    {
        return await _context.ArchiveConfigurations.AnyAsync(cancellationToken);
    }

    public async Task<ArchiveConfiguration?> GetLastArchiveConfigurationAsync(CancellationToken cancellationToken)
    {
        return await _context.ArchiveConfigurations
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return await _context.ArchiveConfigurations
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(ArchiveConfiguration config, CancellationToken cancellationToken = default)
    {
        await _context.ArchiveConfigurations.AddAsync(config, cancellationToken);
    }

    public Task UpdateAsync(ArchiveConfiguration config, CancellationToken cancellationToken = default)
    {
        _context.ArchiveConfigurations.Update(config);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.ArchiveConfigurations.RemoveRange(_context.ArchiveConfigurations);
        return Task.CompletedTask;
    }
}