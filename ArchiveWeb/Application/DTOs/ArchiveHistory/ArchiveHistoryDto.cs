using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Application.DTOs.ArchiveHistory;

public sealed record ArchiveHistoryDto
{
    public Guid Id { get; init; }

    public Guid FileArchiveId { get; init; }
    public string? FullName { get; init; }
    public string? FileNumberForArchive { get; set; } // Шифр дела (например, "301005")

    public HistoryAction Action { get; init; }
    public string? Reason { get; init; }

    public int? OldBoxNumber { get; init; }
    public int? OldPosition { get; init; }
    public int? NewBoxNumber { get; init; }
    public int? NewPosition { get; init; }
    
    public DateTime CreatedAt { get; init; }
}

