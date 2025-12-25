namespace ArchiveWeb.Application.DTOs.Box;

public sealed record BoxDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public int? ExpectedCount { get; init; }
    public int ActualCount { get; init; }
    public int AvailableSpace { get; init; }
    public bool HasAvailableSpace { get; init; }
    public DateTime CreatedAt { get; init; }
}

