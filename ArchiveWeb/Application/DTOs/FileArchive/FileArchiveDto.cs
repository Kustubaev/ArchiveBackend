namespace ArchiveWeb.Application.DTOs.FileArchive;

public sealed record FileArchiveDto
{
    public Guid Id { get; init; }
    public Guid ApplicantId { get; init; }
    public string FileNumberForArchive { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public char FirstLetterSurname { get; init; }
    public char Letter { get; init; }
    public int FileNumberForLetter { get; init; }
    public int? BoxNumber { get; init; }
    public int PositionInBox { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

//new FileArchiveDto
//            {
//                Id = f.Id,
//                ApplicantId = f.ApplicantId,
//                FileNumberForArchive = f.FileNumberForArchive,
//                FullName = f.FullName,
//                Letter = f.Letter.Value,
//                FileNumberForLetter = f.FileNumberForLetter,
//                BoxNumber = f.Box.Number,
//                PositionInBox = f.PositionInBox,
//                IsDeleted = f.IsDeleted,
//                CreatedAt = f.CreatedAt,
//                UpdatedAt = f.UpdatedAt,
//            }