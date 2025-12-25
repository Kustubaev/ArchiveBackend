using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Exceptions;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/files")]
[Produces("application/json")]
[Tags("Дела архива")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileArchiveService _fileArchiveService;
    private readonly ArchiveDbContext _context;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileArchiveService fileArchiveService,
        ArchiveDbContext context,
        ILogger<FilesController> logger)
    {
        _fileArchiveService = fileArchiveService;
        _context = context;
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
        
        // Проверка существования абитуриента
        var applicantExists = await _context.Applicants
            .AnyAsync(a => a.Id == dto.ApplicantId, cancellationToken);

        if (!applicantExists)
            return NotFound(new { message = $"Абитуриент с ID {dto.ApplicantId} не найден" });

        // Проверка, что у абитуриента еще нет дела
        var existingFile = await _context.FileArchives
            .FirstOrDefaultAsync(f => f.ApplicantId == dto.ApplicantId, cancellationToken);

        if (existingFile != null)
            return Conflict(new { message = $"У абитуриента уже есть дело в архиве (ID: {existingFile.Id})" });

        try
        {
            var fileArchive = await _fileArchiveService.CreateFileArchiveAsync(dto.ApplicantId, cancellationToken);

            return CreatedAtAction(
                nameof(GetFile),
                new { id = fileArchive.Id },
                fileArchive);
        }
        catch (ArchiveException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, errorCode = ex.ErrorCode });
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
        var file = await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.Id == id)
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
                UpdatedAt = f.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

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

        var query = _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .AsQueryable();

        if (!includeDeleted) query = query.Where(f => !f.IsDeleted);
        if (letter.HasValue) query = query.Where(f => f.Letter.Value == char.ToUpper(letter.Value));
        if (boxNumber.HasValue) query = query.Where(f => f.Box.Number == boxNumber.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var files = await query
            .OrderBy(f => f.Box.Number)
            .ThenBy(f => f.PositionInBox)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                UpdatedAt = f.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<FileArchiveDto>
        {
            Items = files,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary> Мягкое удаление дела (установка флага IsDeleted) </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.FileArchives
            .Include(f => f.Letter)
            .Include(f => f.Box)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (file == null)
            return NotFound(new { message = $"Дело с ID {id} не найдено" });

        if (file.IsDeleted)
            return BadRequest(new { message = $"Дело с ID {id} уже удалено." });

        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;

        // Запись в историю
        var history = new ArchiveHistory
        {
            Action = HistoryAction.Delete,
            FileArchiveId = file.Id,
            Reason = "Мягкое удаление дела",

            OldBoxNumber = file.Box?.Number,
            OldPosition = file.PositionInBox,
            OldLetterId = file.LetterId,
            OldBoxId = file.BoxId,

            NewBoxNumber = null,
            NewPosition = null,
            NewLetterId = null,
            NewBoxId = null,
        };

        _context.ArchiveHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Дело помечено как удаленное: FileArchiveId={FileArchiveId}", file.Id);

        return Ok(new { message = $"Дело с ID {id} успешно удалено." });
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

        var query = _context.Applicants
            .Where(a => a.FileArchive == null)
            .OrderBy(a => a.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var applicants = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApplicantDto
            {
                Id = a.Id,
                Surname = a.Surname,
                FirstName = a.FirstName,
                Patronymic = a.Patronymic,
                EducationLevel = a.EducationLevel,
                StudyForm = a.StudyForm,
                IsOriginalSubmitted = a.IsOriginalSubmitted,
                IsBudgetFinancing = a.IsBudgetFinancing,
                PhoneNumber = a.PhoneNumber,
                Email = a.Email,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                FileArchiveId = null
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<ApplicantDto>
        {
            Items = applicants,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary> Принять все дела в архив (массовое принятие) </summary>
    [HttpPost("accept-all")]
    [ProducesResponseType(typeof(BulkAcceptFilesResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkAcceptFilesResultDto>> AcceptAllFiles(
        CancellationToken cancellationToken = default)
    {
        // Получаем всех абитуриентов без FileArchive, отсортированных по дате создания
        var pendingApplicants = await _context.Applicants
            .Where(a => a.FileArchive == null)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var acceptedFiles = new List<FileArchiveDto>();
        var rejectedFiles = new List<RejectedFileDto>();

        foreach (var applicant in pendingApplicants)
        {
            try
            {
                var fileArchive = await _fileArchiveService.CreateFileArchiveAsync(
                    applicant.Id, 
                    cancellationToken);
                
                acceptedFiles.Add(fileArchive);
                
                _logger.LogInformation(
                    "Дело успешно принято: ApplicantId={ApplicantId}, FileArchiveId={FileArchiveId}",
                    applicant.Id,
                    fileArchive.Id);
            }
            catch (ArchiveException ex)
            {
                // Получаем полное имя абитуриента
                var fullName = string.Join(' ',
                    new[] { applicant.Surname, applicant.FirstName, applicant.Patronymic ?? string.Empty }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                rejectedFiles.Add(new RejectedFileDto
                {
                    ApplicantId = applicant.Id,
                    FullName = fullName,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.ErrorCode
                });

                _logger.LogWarning(
                    "Не удалось принять дело: ApplicantId={ApplicantId}, Error={Error}",
                    applicant.Id,
                    ex.Message);
            }
            catch (Exception ex)
            {
                // Получаем полное имя абитуриента
                var fullName = string.Join(' ',
                    new[] { applicant.Surname, applicant.FirstName, applicant.Patronymic ?? string.Empty }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                rejectedFiles.Add(new RejectedFileDto
                {
                    ApplicantId = applicant.Id,
                    FullName = fullName,
                    ErrorMessage = ex.Message,
                    ErrorCode = null
                });

                _logger.LogError(
                    ex,
                    "Неожиданная ошибка при принятии дела: ApplicantId={ApplicantId}",
                    applicant.Id);
            }
        }

        var result = new BulkAcceptFilesResultDto
        {
            AcceptedFiles = acceptedFiles,
            RejectedFiles = rejectedFiles,
            TotalProcessed = pendingApplicants.Count
        };

        _logger.LogInformation(
            "Массовое принятие дел завершено: Всего={Total}, Принято={Accepted}, Отклонено={Rejected}",
            result.TotalProcessed,
            result.AcceptedCount,
            result.RejectedCount);

        return Ok(result);
    }
}

