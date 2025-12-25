using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Domain.Entities;

public class FileArchive : EntityBase
{
    [Required]
    public string FileNumberForArchive { get; set; } = string.Empty; // Шифр дела (например, "301005")
    
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty; // ФИО (Фамилия Имя Отчество)

    [Required]
    [MaxLength(1)]
    public char FirstLetterSurname { get; set; } // Первая буква фамилии
    
    [Required]
    public int FileNumberForLetter { get; set; } // Порядковый номер для буквы
    
    [Required]
    public int PositionInBox { get; set; }       // Позиция в коробке (1-based)
    
    public bool IsDeleted { get; set; } = false; // Мягкое удаление
    public DateTime? DeletedAt { get; set; } = null;

    // Внешние ключи
    [Required]
    public Guid ApplicantId { get; set; } // ID из системы абитуриентов
    
    public Guid? BoxId { get; set; }
    
    [Required]
    public Guid LetterId { get; set; }
    
    // Навигационные свойства
    public Box? Box { get; set; } = null!;
    public Letter Letter { get; set; } = null!;
    public Applicant Applicant { get; set; } = null!;
    public ICollection<ArchiveHistory> History { get; set; } = new List<ArchiveHistory>();
}

