using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;

namespace ArchiveWeb.Application.Services;

public sealed class BoxService : IBoxService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BoxService> _logger;

    public BoxService(
        IUnitOfWork uow,
        ILogger<BoxService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<BoxDto>> GetBoxesAsync(CancellationToken cancellationToken = default)
    {
        var boxes = await _uow.Boxes.GetAllAsync(cancellationToken);
        return boxes
            .OrderBy(b => b.Number)
            .Select(b => new BoxDto
            {
                Id = b.Id,
                Number = b.Number,
                ExpectedCount = b.ExpectedCount,
                ActualCount = b.ActualCount,
                AvailableSpace = b.AvailableSpace,
                HasAvailableSpace = b.HasAvailableSpace,
                CreatedAt = b.CreatedAt
            })
            .ToList();
    }

    public async Task<BoxDto?> GetBoxByNumberAsync(int number, CancellationToken cancellationToken = default)
    {
        var box = await _uow.Boxes.GetByNumberAsync(number, cancellationToken);
        if (box == null)
            return null;

        return new BoxDto
        {
            Id = box.Id,
            Number = box.Number,
            ExpectedCount = box.ExpectedCount,
            ActualCount = box.ActualCount,
            AvailableSpace = box.AvailableSpace,
            HasAvailableSpace = box.HasAvailableSpace,
            CreatedAt = box.CreatedAt
        };
    }

    public async Task<List<FileArchiveDto>> GetBoxFilesAsync(int number, CancellationToken cancellationToken = default)
    {
        var box = await _uow.Boxes.GetByNumberAsync(number, cancellationToken);

        if (box == null)
            throw new InvalidOperationException($"Коробка с номером {number} не найдена");

        var files = await _uow.FileArchives.GetFilesByBoxIdAsync(box.Id, cancellationToken);
        var sortedFiles = files.OrderBy(f => f.PositionInBox).ToList();

        return sortedFiles.Select(f => new FileArchiveDto
        {
            Id = f.Id,
            ApplicantId = f.ApplicantId,
            FileNumberForArchive = f.FileNumberForArchive,
            FullName = f.FullName,
            FirstLetterSurname = f.FirstLetterSurname,
            Letter = f.Letter.Value,
            FileNumberForLetter = f.FileNumberForLetter,
            BoxNumber = f.Box?.Number,
            PositionInBox = f.PositionInBox,
            IsDeleted = f.IsDeleted,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
        }).ToList();
    }
}