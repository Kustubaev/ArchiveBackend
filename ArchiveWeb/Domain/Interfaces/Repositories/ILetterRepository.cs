using ArchiveWeb.Domain.Entities;
using System.Linq.Expressions;

namespace ArchiveWeb.Domain.Interfaces.Repositories
{
    public interface ILetterRepository
    {
        Task<Letter> AddAsync(Letter letter, CancellationToken cancellationToken = default);
        Task AddRangeAsync(List<Letter> letters, CancellationToken cancellationToken = default);
        Task DeleteAsync(Letter letter, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
        Task<List<Letter>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Letter>> GetByConditionAsync(Expression<Func<Letter, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Letter?> GetFirstByConditionAsync(Expression<Func<Letter, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(Letter letter, CancellationToken cancellationToken = default);
        Task DeleteAllAsync(CancellationToken cancellationToken = default);
    }
}