using ArchiveWeb.Application.DTOs.Archive;

namespace ArchiveWeb.Domain.Interfaces.Services;

public interface IArchiveStatisticsService
{
    Task<ArchiveStatisticsDto> GetArchiveStatisticsAsync(CancellationToken cancellationToken = default);
    Task<LetterStatisticsDto> GetLetterStatisticsAsync(char letter, CancellationToken cancellationToken = default);
}

