using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Domain.Entities;

public class ArchiveHistory : EntityBase
{
    [Required]
    public HistoryAction Action { get; set; } // CREATE, MOVE, DELETE, REDISTRIBUTE
    
    public int? OldBoxNumber { get; set; }
    public int? OldPosition { get; set; }
    public int? NewBoxNumber { get; set; }
    public int? NewPosition { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; } // Причина перемещения
    
    // Внешние ключи
    [Required]
    public Guid FileArchiveId { get; set; }
    public Guid? NewLetterId { get; set; }
    public Guid? OldLetterId { get; set; }
    public Guid? NewBoxId { get; set; }
    public Guid? OldBoxId { get; set; }

    // Навигационные свойства
    public FileArchive FileArchive { get; set; } = null!;
    public Letter? ActualLetter { get; set; } // хранить NewBoxId 
    public Box? ActualBox { get; set; } // Хранит NewBoxId
}

public enum HistoryAction
{
    Create = 1,      // Создание дела
    Update = 2,      // Создание дела
    Move = 3,        // Перемещение
    Delete = 4,      // Мягкое удаление
    Redistribute = 5 // Автоматическое перераспределение
}

