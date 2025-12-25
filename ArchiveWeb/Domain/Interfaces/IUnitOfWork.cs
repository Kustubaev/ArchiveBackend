using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArchiveWeb.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IArchiveConfigurationRepository ArchiveConfig { get; }
        IBoxRepository Boxes { get; }
        ILetterRepository Letters { get; }
        IFileArchiveRepository FileArchives { get; }
        IArchiveHistoryRepository ArchiveHistories { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        IQueryable<T> Query<T>() where T : class;

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}