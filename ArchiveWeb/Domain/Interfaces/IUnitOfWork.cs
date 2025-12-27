using ArchiveWeb.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArchiveWeb.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IApplicantRepository Applicants { get; }
        IArchiveConfigurationRepository ArchiveConfig { get; }
        IBoxRepository Boxes { get; }
        ILetterRepository Letters { get; }
        IFileArchiveRepository FileArchives { get; }
        IArchiveHistoryRepository ArchiveHistories { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}