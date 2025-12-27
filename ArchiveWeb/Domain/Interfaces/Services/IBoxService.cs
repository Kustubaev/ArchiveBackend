using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.FileArchive;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface IBoxService
    {
        Task<BoxDto?> GetBoxByNumberAsync(int number, CancellationToken cancellationToken = default);
        Task<List<BoxDto>> GetBoxesAsync(CancellationToken cancellationToken = default);
        Task<List<FileArchiveDto>> GetBoxFilesAsync(int number, CancellationToken cancellationToken = default);
    }
}