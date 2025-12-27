using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.DTOs.Letter;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;

namespace ArchiveWeb.Application.Services;

public sealed class LetterService : ILetterService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LetterService> _logger;

    public LetterService(
        IUnitOfWork uow,
        ILogger<LetterService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<LetterDto>> GetLettersAsync(CancellationToken cancellationToken = default)
    {
        string order = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ+-";
        var letters = await _uow.Letters.GetAllAsync(cancellationToken);
        return letters
            .OrderBy(l => order.IndexOf(l.Value))
            .Select(l => new LetterDto
            {
                Id = l.Id,
                Value = l.Value,
                ExpectedCount = l.ExpectedCount,
                StartBox = l.StartBox,
                EndBox = l.EndBox,
                StartPosition = l.StartPosition,
                EndPosition = l.EndPosition,
                ActualCount = l.ActualCount,
                UsedCount = l.UsedCount,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToList();
    }

    public async Task<LetterDto?> GetLetterByValueAsync(char letter, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        var letterEntity = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == normalizeLetter, cancellationToken);
        if (letterEntity == null)
            return null;

        return new LetterDto
        {
            Id = letterEntity.Id,
            Value = letterEntity.Value,
            ExpectedCount = letterEntity.ExpectedCount,
            StartBox = letterEntity.StartBox,
            EndBox = letterEntity.EndBox,
            StartPosition = letterEntity.StartPosition,
            EndPosition = letterEntity.EndPosition,
            ActualCount = letterEntity.ActualCount,
            UsedCount = letterEntity.UsedCount,
            CreatedAt = letterEntity.CreatedAt,
            UpdatedAt = letterEntity.UpdatedAt
        };
    }

    public async Task<List<FileArchiveDto>> GetLetterFilesAsync(char letter, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        var letterEntity = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == normalizeLetter, cancellationToken);

        if (letterEntity == null)
            throw new InvalidOperationException($"Буква '{letter}' не найдена");

        var files = await _uow.FileArchives.GetFilesByLetterIdAsync(letterEntity.Id, cancellationToken);

        return files.Select(f => new FileArchiveDto
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

    public async Task<List<FileArchiveDto>> GetFirstLetterFilesAsync(char letter, CancellationToken cancellationToken = default)
    {
        const string ValidLetters = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ+-";
        char normalizedLetter = char.ToUpper(letter);

        if (!ValidLetters.Contains(normalizedLetter))
        {
            throw new ArgumentException($"Буква '{letter}' не найдена");
        }

        var files = await _uow.FileArchives.GetFirstFilesByLetterIdAsync(normalizedLetter, cancellationToken);

        return files.Select(f => new FileArchiveDto
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

    public async Task<LetterStatisticsDto> GetLetterStatisticsAsync(char letter, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        var letterEntity = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == normalizeLetter, cancellationToken);

        if (letterEntity == null)
            throw new InvalidOperationException($"Буква '{letter}' не найдена");

        return new LetterStatisticsDto
        {
            Letter = letterEntity.Value,
            ExpectedCount = letterEntity.ExpectedCount ?? 0,
            ActualCount = letterEntity.ActualCount,
            UsedCount = letterEntity.UsedCount,
            FillPercentage = letterEntity.ExpectedCount.HasValue && letterEntity.ExpectedCount.Value > 0
                ? (letterEntity.ActualCount / (double)letterEntity.ExpectedCount.Value) * 100.0
                : 0.0,
            IsOverflow = letterEntity.ExpectedCount.HasValue &&
                        letterEntity.ActualCount >= letterEntity.ExpectedCount.Value,
            StartBox = letterEntity.StartBox,
            EndBox = letterEntity.EndBox
        };
    }
}