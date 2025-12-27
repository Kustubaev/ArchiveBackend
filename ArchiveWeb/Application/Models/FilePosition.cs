namespace ArchiveWeb.Application.Models;

public sealed record FilePosition
{
    public int BoxNumber { get; init; }
    public int PositionInBox { get; init; }
}