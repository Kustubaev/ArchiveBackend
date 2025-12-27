using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Services
{
    public interface IArchiveService
    {
        Task<object> ClearArchiveAsync(bool clearApplicants, CancellationToken cancellationToken = default);
        Task<ArchiveStatisticsDto> GetArchiveStatisticsAsync(CancellationToken cancellationToken = default);
        Task<object> GetArchiveStatusAsync(CancellationToken cancellationToken = default);
        Task<ArchiveConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
        Task InitializeArchiveAsync(InitializeArchiveDto dto, CancellationToken cancellationToken = default);
        Task<bool> IsArchiveInitializedAsync(CancellationToken cancellationToken = default);
        Task<RedistributionResultDto> RedistributeArchiveAsync(CancellationToken cancellationToken = default, ArchiveConfiguration? config = null);
        Task<ArchiveConfigurationDto> UpdateArchiveAsync(UpdateArchiveConfigurationDto dto, CancellationToken cancellationToken = default);
    }
}