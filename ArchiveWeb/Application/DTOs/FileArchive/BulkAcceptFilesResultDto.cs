namespace ArchiveWeb.Application.DTOs.FileArchive;

public sealed record BulkAcceptFilesResultDto
{
    public List<FileArchiveDto> AcceptedFiles { get; init; } = new();
    public List<RejectedFileDto> RejectedFiles { get; init; } = new();
    public int TotalProcessed { get; init; }
    public int AcceptedCount => AcceptedFiles.Count;
    public int RejectedCount => RejectedFiles.Count;
}

public sealed record RejectedFileDto
{
    public Guid ApplicantId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
}

