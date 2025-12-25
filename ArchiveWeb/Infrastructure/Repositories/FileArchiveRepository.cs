using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Infrastructure.Repositories;
public class FileArchiveRepository : IFileArchiveRepository
{
    private readonly ArchiveDbContext _context;
    public FileArchiveRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
    {
        return await _context.FileArchives.AnyAsync(cancellationToken);
    }

    public void AddRange(List<FileArchive> fileArchives)
    {
        _context.FileArchives.AddRange(fileArchives);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives.CountAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetDeletedSortedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.IsDeleted)
            .OrderBy(f => f.DeletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetActiveFileAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => !f.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(char Letter, int Count)>> GetFileDistributionAsync(CancellationToken cancellationToken)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .GroupBy(f => f.FirstLetterSurname)
            .Select(g => new { Letter = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(x => (x.Letter, x.Count)).ToList(), cancellationToken);
    }

    public async Task<Dictionary<char, List<FileArchive>>> GetFileArchivesGroupedByFirstLetterSurnameSortedAsync(CancellationToken cancellationToken = default)
    {
        var fileArchives = await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(fa => !fa.IsDeleted)
            .ToListAsync(cancellationToken);

        var groupedAndSorted = fileArchives
            .GroupBy(fa => fa.FirstLetterSurname)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(fa => int.Parse(fa.FileNumberForArchive.Substring(3)))
                .ToList()
            );

        return groupedAndSorted;
    }
}
