using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.DTOs.Letter;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/letters")]
[Produces("application/json")]
[Tags("Буквы")]
public sealed class LettersController : ControllerBase
{
    private readonly ILetterService _letterService;
    private readonly ILogger<LettersController> _logger;

    public LettersController(
        ILetterService letterService,
        ILogger<LettersController> logger)
    {
        _letterService = letterService;
        _logger = logger;
    }

    /// <summary> Получить список всех букв </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LetterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LetterDto>>> GetLetters(CancellationToken cancellationToken = default)
    {
        var letters = await _letterService.GetLettersAsync(cancellationToken);
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
        var letterEntity = await _letterService.GetLetterByValueAsync(letter, cancellationToken);

        if (letterEntity == null)
            return NotFound(new { message = $"Буква '{letter}' не найдена" });

        return Ok(letterEntity);
    }

    /// <summary> Получить список дел для буквы </summary>
    [HttpGet("{letter}/files")]
    [ProducesResponseType(typeof(List<FileArchiveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FileArchiveDto>>> GetLetterFiles(char letter, CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _letterService.GetLetterFilesAsync(letter, cancellationToken);
            return Ok(files);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Получить список дел по первой букве фамилии </summary>
    [HttpGet("{firstLetter}/first-letter-files")]
    [ProducesResponseType(typeof(List<FileArchiveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<FileArchiveDto>>> GetFirstLetterFiles(char firstLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _letterService.GetFirstLetterFilesAsync(firstLetter, cancellationToken);
            return Ok(files);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Получить статистику по букве </summary>
    [HttpGet("{letter}/statistics")]
    [ProducesResponseType(typeof(LetterStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LetterStatisticsDto>> GetLetterStatistics(
        char letter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            char normalizeLetter = char.ToUpper(letter);
            var statistics = await _letterService.GetLetterStatisticsAsync(normalizeLetter, cancellationToken);
            return Ok(statistics);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

