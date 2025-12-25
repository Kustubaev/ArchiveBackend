namespace ArchiveWeb.Application.DTOs.Letter;

public sealed record LetterDto
{
    public Guid Id { get; init; }
    public char Value { get; init; }
    public int? ExpectedCount { get; init; }
    public int? StartBox { get; init; }
    public int? EndBox { get; init; }
    public int? StartPosition { get; init; }
    public int? EndPosition { get; init; }
    public int ActualCount { get; init; }
    public int UsedCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

