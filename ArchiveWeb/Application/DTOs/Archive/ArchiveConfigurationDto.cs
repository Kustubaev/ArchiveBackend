using System.Text.Json;

namespace ArchiveWeb.Application.DTOs.ArchiveConfiguration;

public sealed record ArchiveConfigurationDto
{
    public Guid Id { get; init; }
    public int BoxCount { get; init; }
    public int BoxCapacity { get; init; }
    public int AdaptiveRedistributionThreshold { get; init; }
    public double AdaptiveWeightNew { get; init; }
    public double AdaptiveWeightOld { get; init; }
    public int PercentReservedFiles { get; init; }
    public int PercentDeletedFiles { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

