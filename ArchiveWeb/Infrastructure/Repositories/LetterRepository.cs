using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Repositories;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ArchiveWeb.Infrastructure.Repositories;

public sealed class LetterRepository : ILetterRepository
{
    private readonly ArchiveDbContext _context;

    public LetterRepository(ArchiveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Letters.AnyAsync(cancellationToken);
    }

    public async Task<List<Letter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Letters.ToListAsync(cancellationToken);
    }

    public async Task<List<Letter>> GetByConditionAsync(Expression<Func<Letter, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Letters
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Letter?> GetFirstByConditionAsync(Expression<Func<Letter, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Letters
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Letters.CountAsync(cancellationToken);
    }

    public async Task<Letter> AddAsync(Letter letter, CancellationToken cancellationToken = default)
    {
        await _context.Letters.AddAsync(letter, cancellationToken);
        return letter;
    }

    public async Task UpdateAsync(Letter letter, CancellationToken cancellationToken = default)
    {
        _context.Letters.Update(letter);
    }

    public async Task DeleteAsync(Letter letter, CancellationToken cancellationToken = default)
    {
        _ = _context.Letters.Remove(letter);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(List<Letter> letters, CancellationToken cancellationToken = default)
    {
        await _context.Letters.AddRangeAsync(letters, cancellationToken);
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _context.Letters.RemoveRange(_context.Letters);
        return Task.CompletedTask;
    }
}