using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories
{
    public interface IFileArchiveRepository
    {
        void AddRange(List<FileArchive> fileArchives);
        Task<FileArchive> AddAsync(FileArchive fileArchive, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(System.Linq.Expressions.Expression<Func<FileArchive, bool>> predicate, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<FileArchive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<FileArchive?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default);
        Task<FileArchive?> GetByApplicantIdAsync(Guid applicantId, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetDeletedSortedAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetActiveFileAsync(CancellationToken cancellationToken = default);
        Task<List<(char Letter, int Count)>> GetFileDistributionAsync(CancellationToken cancellationToken);
        Task<Dictionary<char, List<FileArchive>>> GetFileArchivesGroupedByFirstLetterSurnameSortedAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetFilesWithFiltersAsync(char? letter, int? boxNumber, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountFilesWithFiltersAsync(char? letter, int? boxNumber, bool includeDeleted, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> SearchBySurnameAsync(string surname, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountSearchBySurnameAsync(string surname, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetFilesByBoxIdAsync(Guid boxId, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetFilesByLetterIdAsync(Guid letterId, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetFirstFilesByLetterIdAsync(char firstLetter, CancellationToken cancellationToken = default);
        Task<List<FileArchive>> GetRandomActiveFilesAsync(int count, CancellationToken cancellationToken = default);
        Task UpdateAsync(FileArchive fileArchive, CancellationToken cancellationToken = default);
        Task DeleteAllAsync(CancellationToken cancellationToken = default);
    }
}