using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories
{
    public interface IArchiveHistoryRepository
    {
        Task AddAsync(ArchiveHistory archiveHistory, CancellationToken cancellationToken = default);
        Task AddRangeAsync(List<ArchiveHistory> archiveHistories, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetByFileArchiveIdAsync(Guid fileArchiveId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountByFileArchiveIdAsync(Guid fileArchiveId, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetByBoxIdAsync(Guid boxId, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetByBoxNumberAsync(int boxNumber, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetByLetterIdAsync(Guid letterId, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistory>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task DeleteAllAsync(CancellationToken cancellationToken = default);
    }
}