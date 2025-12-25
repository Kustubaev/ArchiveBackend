using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories
{
    public interface IFileArchiveRepository
    {
        void AddRange(List<FileArchive> fileArchives);
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetDeletedSortedAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetActiveFileAsync(CancellationToken cancellationToken = default);
        Task<List<(char Letter, int Count)>> GetFileDistributionAsync(CancellationToken cancellationToken);
        Task<Dictionary<char, List<FileArchive>>> GetFileArchivesGroupedByFirstLetterSurnameSortedAsync(CancellationToken cancellationToken = default);
    }
}