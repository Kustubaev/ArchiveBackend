using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.DTOs.FileArchive;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface IFileArchiveService
    {
        Task<BulkAcceptFilesResultDto> AcceptAllFilesAsync(CancellationToken cancellationToken = default);
        Task<FileArchiveDto> CreateFileArchiveAsync(Guid applicantId, CancellationToken cancellationToken = default);
        Task<FileArchiveDto> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<FileArchiveDto>> DeleteRandomFilesAsync(int count, CancellationToken cancellation = default);
        Task<FileArchiveDto?> GetFileByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResponse<FileArchiveDto>> GetFilesAsync(char? letter, int? boxNumber, int page, int pageSize, bool includeDeleted, CancellationToken cancellationToken = default);
        Task<PagedResponse<ApplicantDto>> GetPendingApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<FileArchiveDto?> SearchByApplicantIdAsync(Guid applicantId, CancellationToken cancellationToken = default);
        Task<PagedResponse<FileArchiveDto>> SearchBySurnameAsync(string surname, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}