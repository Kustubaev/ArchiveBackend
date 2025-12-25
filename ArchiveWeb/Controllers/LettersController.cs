using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.DTOs.Letter;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/letters")]
[Produces("application/json")]
[Tags("Буквы")]
public sealed class LettersController : ControllerBase
{
    private readonly ArchiveDbContext _context;
    private readonly IArchiveStatisticsService _statisticsService;
    private readonly ILogger<LettersController> _logger;

    public LettersController(
        ArchiveDbContext context,
        IArchiveStatisticsService statisticsService,
        ILogger<LettersController> logger)
    {
        _context = context;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary> Получить список всех букв </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LetterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LetterDto>>> GetLetters(CancellationToken cancellationToken = default)
    {
        var letters = await _context.Letters
            .OrderBy(l => l.Value == '+' || l.Value == '-' ? 1 : 0) // Спец-буквы '+' и '-' в конец
            .ThenBy(l => l.Value == '-' ? 1 : 0) // '-' после '+'
            .ThenBy(l => l.Value)
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
            .ToListAsync(cancellationToken);

        return Ok(letters);
    }

    /// <summary> Получить букву по значению </summary>
    [HttpGet("{letter}")]
    [ProducesResponseType(typeof(LetterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LetterDto>> GetLetter(
        char letter,
        CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        var letterEntity = await _context.Letters
            .Where(l => l.Value == normalizeLetter)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (letterEntity == null)
            return NotFound(new { message = $"Буква '{letter}' не найдена" });

        return Ok(letterEntity);
    }

    /// <summary> Получить список дел для буквы </summary>
    [HttpGet("{letter}/files")]
    [ProducesResponseType(typeof(List<FileArchiveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FileArchiveDto>>> GetLetterFiles(
        char letter,
        CancellationToken cancellationToken = default)
    {
        var letterEntity = await _context.Letters
            .FirstOrDefaultAsync(l => l.Value == letter, cancellationToken);

        if (letterEntity == null)
            return NotFound(new { message = $"Буква '{letter}' не найдена" });

        var files = await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.LetterId == letterEntity.Id)
            .OrderBy(f => f.Box.Number)
            .ThenBy(f => f.PositionInBox)
            .Select(f => new FileArchiveDto
            {
                Id = f.Id,
                ApplicantId = f.ApplicantId,
                FileNumberForArchive = f.FileNumberForArchive,
                FullName = f.FullName,
                FirstLetterSurname = f.FirstLetterSurname,
                Letter = f.Letter.Value,
                FileNumberForLetter = f.FileNumberForLetter,
                BoxNumber = f.Box.Number,
                PositionInBox = f.PositionInBox,
                IsDeleted = f.IsDeleted,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(files);
    }

    /// <summary> Получить статистику по букве </summary>
    [HttpGet("{letter}/statistics")]
    [ProducesResponseType(typeof(LetterStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LetterStatisticsDto>> GetLetterStatistics(
        char letter,
        CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        try
        {
            var statistics = await _statisticsService.GetLetterStatisticsAsync(normalizeLetter, cancellationToken);
            return Ok(statistics);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

