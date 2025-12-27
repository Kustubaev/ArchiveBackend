using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ArchiveWeb.Infrastructure.Repositories;
public sealed class FileArchiveRepository : IFileArchiveRepository
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

    public async Task<FileArchive> AddAsync(FileArchive fileArchive, CancellationToken cancellationToken = default)
    {
        await _context.FileArchives.AddAsync(fileArchive, cancellationToken);
        return fileArchive;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives.CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<FileArchive, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives.CountAsync(predicate, cancellationToken);
    }

    public async Task<List<FileArchive>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .ToListAsync(cancellationToken);
    }

    public async Task<FileArchive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FileArchive?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FileArchive?> GetByApplicantIdAsync(Guid applicantId, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.ApplicantId == applicantId && !f.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task<List<FileArchive>> GetFilesWithFiltersAsync(char? letter, int? boxNumber, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .AsQueryable();

        if (!includeDeleted) query = query.Where(f => !f.IsDeleted);
        if (letter.HasValue) query = query.Where(f => f.Letter.Value == char.ToUpper(letter.Value));
        if (boxNumber.HasValue) query = query.Where(f => f.Box.Number == boxNumber.Value);

        return await query
            .OrderBy(f => f.Box.Number)
            .ThenBy(f => f.PositionInBox)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountFilesWithFiltersAsync(char? letter, int? boxNumber, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = _context.FileArchives.AsQueryable();

        if (!includeDeleted) query = query.Where(f => !f.IsDeleted);
        if (letter.HasValue) query = query.Where(f => f.Letter.Value == char.ToUpper(letter.Value));
        if (boxNumber.HasValue) query = query.Where(f => f.Box.Number == boxNumber.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> SearchBySurnameAsync(string surname, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        string searchTerm = surname.Trim().ToLower();
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => !f.IsDeleted && f.FullName.ToLower().Contains(searchTerm))
            .OrderBy(f => f.FullName)
            .ThenBy(f => f.Box.Number)
            .ThenBy(f => f.PositionInBox)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSearchBySurnameAsync(string surname, CancellationToken cancellationToken = default)
    {
        string searchTerm = surname.Trim().ToLower();
        return await _context.FileArchives
            .Where(f => !f.IsDeleted && f.FullName.ToLower().Contains(searchTerm))
            .CountAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetFilesByBoxIdAsync(Guid boxId, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.BoxId == boxId)
            .OrderBy(f => f.PositionInBox)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetFilesByLetterIdAsync(Guid letterId, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.LetterId == letterId)
            .OrderBy(f => f.Box!.Number)
            .ThenBy(f => f.PositionInBox)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetFirstFilesByLetterIdAsync(char firstLetter, CancellationToken cancellationToken = default)
    {
        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.FirstLetterSurname == firstLetter)
            .OrderBy(f => f.Box!.Number)
            .ThenBy(f => f.PositionInBox)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileArchive>> GetRandomActiveFilesAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return new List<FileArchive>();

        return await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => !f.IsDeleted)
            .OrderBy(_ => Guid.NewGuid()) // <-- ключевой момент: случайная сортировка
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(FileArchive fileArchive, CancellationToken cancellationToken = default)
    {
        _context.FileArchives.Update(fileArchive);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.FileArchives.RemoveRange(_context.FileArchives);
        return Task.CompletedTask;
    }
}
