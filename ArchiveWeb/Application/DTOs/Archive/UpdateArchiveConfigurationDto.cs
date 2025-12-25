using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ArchiveWeb.Application.DTOs.Archive;

public sealed record UpdateArchiveConfigurationDto
{
    [Range(1, 1000, ErrorMessage = "Общее количество коробок должно быть от 1 до 1000")]
    public int? BoxCount { get; init; }

    [Range(10, 200, ErrorMessage = "Вместимость коробки должна быть от 10 до 200")]
    public int? BoxCapacity { get; init; }
    
    [Range(50, 100, ErrorMessage = "Порог перераспределения должен быть от 50 до 100")]
    public int? AdaptiveRedistributionThreshold { get; init; }
    
    [Range(0.0, 1.0, ErrorMessage = "Вес нового распределения должен быть от 0.0 до 1.0")]
    public double? AdaptiveWeightNew { get; init; }
    
    [Range(0.0, 1.0, ErrorMessage = "Вес старого распределения должен быть от 0.0 до 1.0")]
    public double? AdaptiveWeightOld { get; init; }

    [Range(1, 100, ErrorMessage = "Процент мест для резервированных дел должен быть от 1 до 100")]
    public int? PercentReservedFiles { get; init; }

    [Range(1, 100, ErrorMessage = "Процент мест для удалённых  дел должен быть от 1 до 100")]
    public int? PercentDeletedFiles { get; init; }
}

