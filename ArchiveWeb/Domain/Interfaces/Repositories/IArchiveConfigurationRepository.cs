using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories;
public interface IArchiveConfigurationRepository
{
    Task<bool> ExistsAsync(CancellationToken cancellationToken);
    Task<ArchiveConfiguration?> GetLastArchiveConfigurationAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task AddAsync(ArchiveConfiguration config, CancellationToken cancellationToken = default);
}