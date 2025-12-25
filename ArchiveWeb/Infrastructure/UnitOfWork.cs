using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using ArchiveWeb.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArchiveWeb.Infrastructure;
public class UnitOfWork : IUnitOfWork
{
    private readonly ArchiveDbContext _context;

    private IArchiveConfigurationRepository? _archiveConfigurationRepository;
    private ILetterRepository? _letterRepository;
    private IBoxRepository? _boxRepository;
    private IFileArchiveRepository? _fileArchiveRepository;
    private IArchiveHistoryRepository? _archiveHistoryRepository;

    public UnitOfWork(ArchiveDbContext context)
    {
        _context = context;
    }

    public IArchiveConfigurationRepository ArchiveConfig
        => _archiveConfigurationRepository ??= new ArchiveConfigurationRepository(_context);

    public ILetterRepository Letters
        => _letterRepository ??= new LetterRepository(_context);

    public IBoxRepository Boxes
        => _boxRepository ??= new BoxRepository(_context);

    public IFileArchiveRepository FileArchives
        => _fileArchiveRepository ??= new FileArchiveRepository(_context);

    public IArchiveHistoryRepository ArchiveHistories
        => _archiveHistoryRepository ??= new ArchiveHistoryRepository(_context);

    // ... другие свойства репозиториев

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<T> Query<T>() where T : class
    {
        return _context.Set<T>();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}