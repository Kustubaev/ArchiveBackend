using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories;
public interface IBoxRepository
{
    Task<bool> ExistsAsync(CancellationToken cancellationToken);

    Task<List<Box>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Box?> GetByNumberAsync(int number, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Box, bool>> predicate, CancellationToken cancellationToken = default);

    Task RemoveRangeAsync(List<Box> boxes, CancellationToken cancellationToken = default);

    Task ClearAllAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync(List<Box> boxes, CancellationToken cancellationToken = default);
}