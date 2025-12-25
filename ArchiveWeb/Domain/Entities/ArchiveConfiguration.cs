using System.Text.Json;

namespace ArchiveWeb.Domain.Entities;

public class ArchiveConfiguration : EntityBase
{
    public int BoxCount { get; set; } = 50; // Количество коробок
    public int BoxCapacity { get; set; } = 20; // Вместимость коробок
    public int AdaptiveRedistributionThreshold { get; set; } = 80; // Процент порога одаптивного распределения
    public double AdaptiveWeightNew { get; set; } = 0.7; // Процент адаптивного распределения для существующих дел в архиве
    public double AdaptiveWeightOld { get; set; } = 0.3; // Процент адаптивного распределения для идеального распределения const
    public int PercentReservedFiles { get; set; } = 10; // процент от общего количества для дел на букву "+"
    public int PercentDeletedFiles { get; set; } = 5; // процент от общего количества для дел на букву "-"


    // Вычисляемые свойства
    public int TotalFiles => BoxCount * BoxCapacity;
}

