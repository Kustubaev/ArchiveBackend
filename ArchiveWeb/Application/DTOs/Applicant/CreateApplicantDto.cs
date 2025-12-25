using System.ComponentModel.DataAnnotations;
using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Application.DTOs.Applicant;

public sealed record CreateApplicantDto
{
    [Required(ErrorMessage = "Фамилия обязательно для заполнения")]
    [MaxLength(100, ErrorMessage = "Фамилия не должно превышать 100 символов")]
    public string Surname { get; init; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно для заполнения")]
    [MaxLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
    public string FirstName { get; init; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Отчество не должно превышать 100 символов")]
    public string? Patronymic { get; init; }

    [Required(ErrorMessage = "Уровень образования обязателен")]
    public EducationLevel EducationLevel { get; init; }

    [Required(ErrorMessage = "Форма обучения обязательна")]
    public StudyForm StudyForm { get; init; }

    public bool IsOriginalSubmitted { get; init; } = false;

    [Required(ErrorMessage = "Необходимо указать тип финансирования")]
    public bool IsBudgetFinancing { get; init; }

    [Required(ErrorMessage = "Номер телефона обязателен")]
    [Phone(ErrorMessage = "Некорректный формат номера телефона")]
    [MaxLength(50, ErrorMessage = "Номер телефона не должен превышать 50 символов")]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(255, ErrorMessage = "Email не должен превышать 255 символов")]
    public string Email { get; init; } = string.Empty;
}

