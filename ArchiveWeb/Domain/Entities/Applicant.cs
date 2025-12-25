using System.ComponentModel.DataAnnotations;
using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Domain.Entities;

public class Applicant : EntityBase
{
    [Required]
    [MaxLength(255)]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Patronymic { get; set; }

    [Required]
    public EducationLevel EducationLevel { get; set; }

    [Required]
    public StudyForm StudyForm { get; set; }

    [Required]
    public bool IsOriginalSubmitted { get; set; } = false;

    [Required]
    public bool IsBudgetFinancing { get; set; }

    [Required]
    [Phone]
    [MaxLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    // Навигационное свойство
    public FileArchive? FileArchive { get; set; }

    // Вычисляемые свойства
    public string GetFullName =>
        string.Join(' ',  new[] { Surname, FirstName, Patronymic ?? string.Empty }
              .Where(x => !string.IsNullOrWhiteSpace(x)));
}

