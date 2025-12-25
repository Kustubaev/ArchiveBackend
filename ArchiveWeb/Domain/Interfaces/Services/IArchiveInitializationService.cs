using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Domain.Interfaces.Services;

public interface IArchiveInitializationService
{
    Task<bool> IsArchiveInitializedAsync(CancellationToken cancellationToken = default);
    Task InitializeArchiveAsync(InitializeArchiveDto initializeArchiveDto, CancellationToken cancellationToken = default);
    Task<RedistributionResultDto> RedistributeArchiveAsync(CancellationToken cancellationToken = default, ArchiveConfiguration? config = null);
    Task<ArchiveConfigurationDto> UpdateArchiveAsync(UpdateArchiveConfigurationDto dto, CancellationToken cancellationToken = default);
}

