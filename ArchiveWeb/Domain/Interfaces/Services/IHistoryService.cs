using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.ArchiveHistory;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface IHistoryService
    {
        Task<PagedResponse<ArchiveHistoryDto>> GetArchiveHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistoryDto>> GetBoxHistoryAsync(int boxNumber, CancellationToken cancellationToken = default);
        Task<PagedResponse<ArchiveHistoryDto>> GetFileHistoryAsync(Guid fileId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<List<ArchiveHistoryDto>> GetLetterHistoryAsync(char letter, CancellationToken cancellationToken = default);
    }
}