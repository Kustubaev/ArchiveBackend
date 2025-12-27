using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface IApplicantService
    {
        Task<ApplicantDto> CreateApplicantAsync(CreateApplicantDto dto, CancellationToken cancellationToken = default);
        Task DeleteApplicantAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<ApplicantDto>> GenerateApplicantsAsync(int count, CancellationToken cancellationToken = default);
        Task<ApplicantDto?> GetApplicantByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResponse<ApplicantDto>> GetApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResponse<ApplicantDto>> SearchApplicantsAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<ApplicantDto> UpdateApplicantAsync(Guid id, UpdateApplicantDto dto, CancellationToken cancellationToken = default);
    }
}