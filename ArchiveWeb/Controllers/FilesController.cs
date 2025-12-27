using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Domain.Exceptions;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/files")]
[Produces("application/json")]
[Tags("Дела архива")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileArchiveService _fileArchiveService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileArchiveService fileArchiveService,
        ILogger<FilesController> logger)
    {
        _fileArchiveService = fileArchiveService;
        _logger = logger;
    }

    /// <summary> Создать новое дело в архиве </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FileArchiveDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileArchiveDto>> CreateFile(
        [FromBody] CreateFileRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var fileArchive = await _fileArchiveService.CreateFileArchiveAsync(dto.ApplicantId, cancellationToken);

            return CreatedAtAction(nameof(GetFile), new { id = fileArchive.Id }, fileArchive);
        }
        catch (ArchiveException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, errorCode = ex.ErrorCode });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("не найден"))
                return NotFound(new { message = ex.Message });
            
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Получить дело по ID </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FileArchiveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileArchiveDto>> GetFile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileArchiveService.GetFileByIdAsync(id, cancellationToken);

        if (file == null)
            return NotFound(new { message = $"Дело с ID {id} не найдено" });

        return Ok(file);
    }

    /// <summary> Получить список дел с пагинацией </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<FileArchiveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<FileArchiveDto>>> GetFiles(
        [FromQuery] char? letter = null,
        [FromQuery] int? boxNumber = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _fileArchiveService.GetFilesAsync(letter, boxNumber, page, pageSize, includeDeleted, cancellationToken);
        return Ok(response);
    }

    /// <summary> Мягкое удаление дела (установка флага IsDeleted) </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            FileArchiveDto fileArchiveDto = await _fileArchiveService.DeleteFileAsync(id, cancellationToken);
            return Ok(fileArchiveDto);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Не найдено"))
                return NotFound(new { message = ex.Message });
            
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Мягкое удаление дела (установка флага IsDeleted) </summary>
    [HttpDelete("delete/{count:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRandomFiles(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            List<FileArchiveDto> filesArchiveDto = await _fileArchiveService.DeleteRandomFilesAsync(count, cancellationToken);
            return Ok(filesArchiveDto);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Не найдено"))
                return NotFound(new { message = ex.Message });

            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Получить список абитуриентов, которые нужно принять в архив (без FileArchive) </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResponse<ApplicantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ApplicantDto>>> GetPendingApplicants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _fileArchiveService.GetPendingApplicantsAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary> Принять все дела в архив (массовое принятие) </summary>
    [HttpPost("accept-all")]
    [ProducesResponseType(typeof(BulkAcceptFilesResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkAcceptFilesResultDto>> AcceptAllFiles(
        CancellationToken cancellationToken = default)
    {
        var result = await _fileArchiveService.AcceptAllFilesAsync(cancellationToken);

        _logger.LogInformation(
            "Массовое принятие дел завершено: Всего={Total}, Принято={Accepted}, Отклонено={Rejected}",
            result.TotalProcessed,
            result.AcceptedCount,
            result.RejectedCount);

        return Ok(result);
    }
}

