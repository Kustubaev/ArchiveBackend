using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Application.DTOs.Applicant;

public sealed record ApplicantDto
{
    public Guid Id { get; init; }
    public string Surname { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? Patronymic { get; init; }
    public EducationLevel EducationLevel { get; init; }
    public StudyForm StudyForm { get; init; }
    public bool IsOriginalSubmitted { get; init; }
    public bool IsBudgetFinancing { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid? FileArchiveId { get; init; }
}

