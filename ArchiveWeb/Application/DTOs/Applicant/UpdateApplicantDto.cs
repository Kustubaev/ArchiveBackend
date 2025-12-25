using System.ComponentModel.DataAnnotations;
using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Application.DTOs.Applicant;

public sealed record UpdateApplicantDto
{
    [MaxLength(100, ErrorMessage = "Фамилия не должно превышать 100 символов")]
    public string? Surname { get; init; }

    [MaxLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
    public string? FirstName { get; init; }

    [MaxLength(100, ErrorMessage = "Отчество не должно превышать 100 символов")]
    public string? Patronymic { get; init; } 

    public EducationLevel? EducationLevel { get; init; }

    public StudyForm? StudyForm { get; init; }

    public bool? IsOriginalSubmitted { get; init; }

    public bool? IsBudgetFinancing { get; init; }

    [Phone(ErrorMessage = "Некорректный формат номера телефона")]
    [MaxLength(50, ErrorMessage = "Номер телефона не должен превышать 50 символов")]
    public string? PhoneNumber { get; init; }

    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(255, ErrorMessage = "Email не должен превышать 255 символов")]
    public string? Email { get; init; }
}

