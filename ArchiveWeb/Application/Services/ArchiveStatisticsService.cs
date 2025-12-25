using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Application.Services;

public sealed class ArchiveStatisticsService : IArchiveStatisticsService
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<ArchiveStatisticsService> _logger;

    public ArchiveStatisticsService(
        ArchiveDbContext context,
        ILogger<ArchiveStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ArchiveStatisticsDto> GetArchiveStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        int totalFiles = await _context.FileArchives.CountAsync(cancellationToken);
        int deletedFiles = await _context.FileArchives.CountAsync(f => f.IsDeleted, cancellationToken);
        
        int totalBoxes = await _context.Boxes.CountAsync(cancellationToken);
        int usedBoxes = await _context.Boxes.CountAsync(b => b.ActualCount > 0, cancellationToken);
        int emptyBoxes = totalBoxes - usedBoxes;

        int totalLetters = await _context.Letters.CountAsync(cancellationToken);

        // Статистика по буквам
        var letterStats = await _context.Letters.Select(l => new LetterStatisticsDto
            {
                Letter = l.Value,
                ExpectedCount = l.ExpectedCount ?? 0,
                ActualCount = l.ActualCount,
                UsedCount = l.UsedCount,
                FillPercentage = l.ExpectedCount.HasValue && l.ExpectedCount.Value > 0
                    ? (l.ActualCount / (double)l.ExpectedCount.Value) * 100.0
                    : 0.0,
                IsOverflow = l.ExpectedCount.HasValue && l.ActualCount >= l.ExpectedCount.Value,
                StartBox = l.StartBox,
                EndBox = l.EndBox
            })
            .ToListAsync(cancellationToken);

        // Статистика по коробкам
        var boxStats = await _context.Boxes
            .OrderBy(b => b.Number)
            .Select(b => new BoxStatisticsDto
            {
                BoxNumber = b.Number,
                ExpectedCount = b.ExpectedCount ?? 0,
                CompletedCount = b.ActualCount,
                AvailableSpace = b.AvailableSpace,
                FillPercentage = b.ExpectedCount.HasValue && b.ExpectedCount.Value > 0
                    ? (b.ActualCount / (double)b.ExpectedCount.Value) * 100.0
                    : 0.0,
                IsFull = !b.HasAvailableSpace
            })
            .ToListAsync(cancellationToken);

        // Общий процент заполнения архива
        int totalExpectedCapacity = boxStats.Sum(b => b.ExpectedCount);
        double archiveFillPercentage = totalExpectedCapacity > 0
            ? (totalFiles / (double)totalExpectedCapacity) * 100.0
            : 0.0;

        return new ArchiveStatisticsDto
        {
            TotalFiles = totalFiles,
            DeletedFiles = deletedFiles,
            BoxCount = totalBoxes,
            TotalLetters = totalLetters,
            UsedBoxes = usedBoxes,
            EmptyBoxes = emptyBoxes,
            ArchiveFillPercentage = archiveFillPercentage,
            LetterStatistics = letterStats.ToDictionary(s => s.Letter),
            BoxStatistics = boxStats
        };
    }

    public async Task<LetterStatisticsDto> GetLetterStatisticsAsync(
        char letter,
        CancellationToken cancellationToken = default)
    {
        var letterEntity = await _context.Letters
            .FirstOrDefaultAsync(l => l.Value == letter, cancellationToken);

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

