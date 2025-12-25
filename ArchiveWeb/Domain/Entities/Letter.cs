using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Domain.Entities;

public class Letter : EntityBase
{
    [Required]
    [MaxLength(1)]
    public char Value { get; set; }           // Сама буква
    
    public int? ExpectedCount { get; set; }      // Расчетное количество дел
    public int? StartBox { get; set; } = 0;          // Первая коробка для буквы
    public int? EndBox { get; set; } = 0;         // Последняя коробка
    public int? StartPosition { get; set; } = 0;     // Начальная позиция
    public int? EndPosition { get; set; } = 0;       // Конечная позиция
    public int ActualCount { get; set; } = 0;   // Фактическое количество дел (обнуляется при перестановках в архиве)
    public int UsedCount { get; set; } = 0;   // Фактическое количество дел (не обнуляется, используется для FileNumberForLetter)

    // Навигационные свойства
    public ICollection<FileArchive> FileArchives { get; set; } = new List<FileArchive>();
    public ICollection<ArchiveHistory> HistoryEntries { get; set; } = new List<ArchiveHistory>();
    
    // Методы
    public bool IsOverflow() => ExpectedCount.HasValue && ActualCount >= ExpectedCount.Value;
}

