using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/archive")]
[Produces("application/json")]
[Tags("Архив")]
public sealed class ArchiveController : ControllerBase
{
    private readonly IArchiveInitializationService _initializationService;
    private readonly IArchiveStatisticsService _statisticsService;
    private readonly ArchiveDbContext _context;
    private readonly ILogger<ArchiveController> _logger;

    public ArchiveController(
        IArchiveInitializationService initializationService,
        IArchiveStatisticsService statisticsService,
        ArchiveDbContext context,
        ILogger<ArchiveController> logger)
    {
        _initializationService = initializationService;
        _statisticsService = statisticsService;
        _context = context;
        _logger = logger;
    }

    /// <summary> Инициализация архива </summary>
    [HttpPost("initialize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InitializeArchive([FromBody] InitializeArchiveDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);


        try
        {
            await _initializationService.InitializeArchiveAsync(dto, cancellationToken);

            return Ok(new { message = "Архив успешно инициализирован" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary> Получить статистику архива </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ArchiveStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ArchiveStatisticsDto>> GetStatistics(CancellationToken cancellationToken = default)
    {
        var statistics = await _statisticsService.GetArchiveStatisticsAsync(cancellationToken);
        if (statistics == null) return NotFound();
        return Ok(statistics);
    }

    /// <summary> Проверить статус инициализации архива </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetArchiveStatus(CancellationToken cancellationToken = default)
    {
        var isInitialized = await _initializationService.IsArchiveInitializedAsync(cancellationToken);

        var config = await _context.ArchiveConfigurations
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lettersCount = await _context.Letters.CountAsync(cancellationToken);
        var boxesCount = await _context.Boxes.CountAsync(cancellationToken);
        var allfilesCount = await _context.FileArchives.CountAsync(cancellationToken);
        var deletedfilesCount = await _context.FileArchives.CountAsync(f => f.IsDeleted, cancellationToken);

        return Ok(new
        {
            IsInitialized = isInitialized,
            ConfigurationExists = config != null,
            LettersCount = lettersCount,
            BoxesCount = boxesCount,
            AllFilesCount = allfilesCount,
            DeletedfilesCount = deletedfilesCount,
        });
    }

    /// <summary> Получить конфигурацию архива </summary>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(ArchiveConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchiveConfigurationDto>> GetConfiguration(CancellationToken cancellationToken = default)
    {
        var config = await _context.ArchiveConfigurations
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
            return NotFound(new { message = "Конфигурация архива не найдена" });

        var dto = new ArchiveConfigurationDto
        {
            Id = config.Id,
            BoxCount = config.BoxCount,
            BoxCapacity = config.BoxCapacity,
            AdaptiveRedistributionThreshold = config.AdaptiveRedistributionThreshold,
            AdaptiveWeightNew = config.AdaptiveWeightNew,
            AdaptiveWeightOld = config.AdaptiveWeightOld,
            PercentReservedFiles = config.PercentReservedFiles,
            PercentDeletedFiles = config.PercentDeletedFiles,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
        };

        return Ok(dto);
    }

    /// <summary> Выполнить адаптивное перераспределение архива </summary>
    [HttpPost("redistribute")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RedistributeArchive(CancellationToken cancellationToken = default)
    {
        if (!(await _initializationService.IsArchiveInitializedAsync(cancellationToken)))
            return BadRequest(new { message = "Архив не инициализирован!" });

        try
        {
            RedistributionResultDto result = await _initializationService.RedistributeArchiveAsync(cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Обновить конфигурацию архива </summary>
    [HttpPatch("configuration")]
    [ProducesResponseType(typeof(ArchiveConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchiveConfigurationDto>> UpdateConfiguration(
        [FromBody] UpdateArchiveConfigurationDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        if (!(await _initializationService.IsArchiveInitializedAsync(cancellationToken)))
            return BadRequest(new { message = "Архив не инициализирован." });

        try
        {
            ArchiveConfigurationDto archive = await _initializationService.UpdateArchiveAsync(dto, cancellationToken);

            return Ok(archive);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

