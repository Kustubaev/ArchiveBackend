using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Domain.Entities;

public class Box : EntityBase
{
    [Required]
    public int Number { get; set; }              // Номер коробки 
    public int? ExpectedCount { get; set; }      // Ожидаемое количество дел
    public int ActualCount { get; set; } = 0;    // Фактическое количество дел
    
    // Навигационные свойства
    public ICollection<FileArchive> FileArchives { get; init; } = new List<FileArchive>();
    public ICollection<ArchiveHistory> HistoryEntries { get; init; } = new List<ArchiveHistory>();
    
    // Вычисляемые свойства
    public bool HasAvailableSpace => 
        ExpectedCount.HasValue && ActualCount < ExpectedCount.Value;
    
    public int AvailableSpace => 
        ExpectedCount.HasValue ? ExpectedCount.Value - ActualCount : 0;
}

