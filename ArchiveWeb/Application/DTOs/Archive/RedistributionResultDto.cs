using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.Letter;

namespace ArchiveWeb.Application.DTOs;

public sealed record RedistributionResultDto
{
    public Dictionary<char, double> ActualDistribution { get; init; } = new();
    public List<ArchiveHistoryDto> ArchiveHistories { get; init; } = new();
    public List<LetterDto> Letters { get; init; } = new();
    public List<BoxDto> Boxs { get; init; } = new();
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
}

