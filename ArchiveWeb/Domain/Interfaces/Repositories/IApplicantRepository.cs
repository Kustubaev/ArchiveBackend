using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Repositories;

public interface IApplicantRepository
{
    Task<Applicant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Applicant?> GetByIdWithFileArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Applicant>> GetPendingApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountPendingApplicantsAsync(CancellationToken cancellationToken = default);
    Task<List<Applicant>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<List<Applicant>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountSearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Applicant> AddAsync(Applicant applicant, CancellationToken cancellationToken = default);
    Task AddRangeAsync(List<Applicant> applicants, CancellationToken cancellationToken = default);
    Task UpdateAsync(Applicant applicant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Applicant applicant, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}

