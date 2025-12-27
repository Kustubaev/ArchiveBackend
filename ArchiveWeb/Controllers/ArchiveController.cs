using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/archive")]
[Produces("application/json")]
[Tags("Архив")]
public sealed class ArchiveController : ControllerBase
{
    private readonly IArchiveService _archiveService;
    private readonly ILogger<ArchiveController> _logger;

    public ArchiveController(
        IArchiveService archiveService,
        ILogger<ArchiveController> logger)
    {
        _archiveService = archiveService;
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
            await _archiveService.InitializeArchiveAsync(dto, cancellationToken);

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
        var statistics = await _archiveService.GetArchiveStatisticsAsync(cancellationToken);
        if (statistics == null) return NotFound();
        return Ok(statistics);
    }

    /// <summary> Проверить статус инициализации архива </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetArchiveStatus(CancellationToken cancellationToken = default)
    {
        var status = await _archiveService.GetArchiveStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary> Получить конфигурацию архива </summary>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(ArchiveConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchiveConfigurationDto>> GetConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _archiveService.GetConfigurationAsync(cancellationToken);
            return Ok(config);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Выполнить адаптивное перераспределение архива </summary>
    [HttpPost("redistribute")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RedistributeArchive(CancellationToken cancellationToken = default)
    {
        if (!(await _archiveService.IsArchiveInitializedAsync(cancellationToken)))
            return BadRequest(new { message = "Архив не инициализирован!" });

        try
        {
            RedistributionResultDto result = await _archiveService.RedistributeArchiveAsync(cancellationToken);

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

        if (!(await _archiveService.IsArchiveInitializedAsync(cancellationToken)))
            return BadRequest(new { message = "Архив не инициализирован." });

        try
        {
            ArchiveConfigurationDto archive = await _archiveService.UpdateArchiveAsync(dto, cancellationToken);

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

    /// <summary> Полная очистка архива (удаление всех сущностей кроме абитуриентов) </summary>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearArchive([FromQuery] bool clearApplicants = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _archiveService.ClearArchiveAsync(clearApplicants, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке архива");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Произошла ошибка при очистке архива", error = ex.Message });
        }
    }
}

