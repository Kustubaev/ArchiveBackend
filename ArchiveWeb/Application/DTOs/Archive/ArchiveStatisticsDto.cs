namespace ArchiveWeb.Application.DTOs.Archive;

public sealed record ArchiveStatisticsDto
{
    public int TotalFiles { get; init; }
    public int DeletedFiles { get; init; }
    public int BoxCount { get; init; }
    public int TotalLetters { get; init; }
    public int UsedBoxes { get; init; }
    public int EmptyBoxes { get; init; }
    public double ArchiveFillPercentage { get; init; }
    public Dictionary<char, LetterStatisticsDto> LetterStatistics { get; init; } = new();
    public List<BoxStatisticsDto> BoxStatistics { get; init; } = new();
}

public sealed record LetterStatisticsDto
{
    public char Letter { get; init; }
    public int ExpectedCount { get; init; }
    public int ActualCount { get; init; }
    public int UsedCount { get; init; }
    public double FillPercentage { get; init; }
    public bool IsOverflow { get; init; }
    public int? StartBox { get; init; }
    public int? EndBox { get; init; }
}

public sealed record BoxStatisticsDto
{
    public int BoxNumber { get; init; }
    public int ExpectedCount { get; init; }
    public int CompletedCount { get; init; }
    public int AvailableSpace { get; init; }
    public double FillPercentage { get; init; }
    public bool IsFull { get; init; }
}

