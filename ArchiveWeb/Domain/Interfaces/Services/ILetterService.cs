using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.DTOs.Letter;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface ILetterService
    {
        Task<LetterDto?> GetLetterByValueAsync(char letter, CancellationToken cancellationToken = default);
        Task<List<FileArchiveDto>> GetLetterFilesAsync(char letter, CancellationToken cancellationToken = default);
        Task<List<FileArchiveDto>> GetFirstLetterFilesAsync(char firstLetter, CancellationToken cancellationToken = default);
        Task<List<LetterDto>> GetLettersAsync(CancellationToken cancellationToken = default);
        Task<LetterStatisticsDto> GetLetterStatisticsAsync(char letter, CancellationToken cancellationToken = default);
    }
}