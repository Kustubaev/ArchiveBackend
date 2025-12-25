using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories
{
    public interface IArchiveHistoryRepository
    {
        Task AddRangeAsync(List<ArchiveHistory> archiveHistories, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}