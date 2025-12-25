using System.ComponentModel.DataAnnotations;

namespace ArchiveWeb.Application.DTOs.FileArchive;

public sealed record CreateFileRequestDto
{
    [Required(ErrorMessage = "ID абитуриента обязателен")]
    public Guid ApplicantId { get; init; }
}

