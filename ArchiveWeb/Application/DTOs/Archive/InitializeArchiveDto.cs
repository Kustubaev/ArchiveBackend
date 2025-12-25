using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Application.DTOs.Archive;

public sealed record InitializeArchiveDto
{
    [Range(1, 1000, ErrorMessage = "Общее количество коробок должно быть от 1 до 1000")]
    public int BoxCount { get; init; } = 50;
    
    [Range(10, 200, ErrorMessage = "Вместимость коробки должна быть от 10 до 200")]
    public int BoxCapacity { get; init; } = 20;

    [Range(50, 100, ErrorMessage = "Порог перераспределения должен быть от 50 до 100")]
    public int AdaptiveRedistributionThreshold { get; init; } = 80;

    [Range(0.0, 1.0, ErrorMessage = "Вес нового распределения должен быть от 0.0 до 1.0")]
    public double AdaptiveWeightNew { get; init; } = 0.7;

    [Range(0.0, 1.0, ErrorMessage = "Вес старого распределения должен быть от 0.0 до 1.0")]
    public double AdaptiveWeightOld { get; init; } = 0.3;

    [Range(1, 100, ErrorMessage = "Процент мест для резервированных дел должен быть от 1 до 100")]
    public int PercentReservedFiles { get; init; } = 10;

    [Range(1, 100, ErrorMessage = "Процент мест для удалённых  дел должен быть от 1 до 100")]
    public int PercentDeletedFiles { get; init; } = 5;
}

