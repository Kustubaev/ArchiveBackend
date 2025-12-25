using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/search")]
[Produces("application/json")]
[Tags("Поиск")]
public sealed class SearchController : ControllerBase
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ArchiveDbContext context,
        ILogger<SearchController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Поиск дел по фамилии
    /// </summary>
    /// <param name="surname">Фамилия или часть фамилии</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список найденных дел</returns>
    /// <response code="200">Поиск выполнен успешно</response>
    /// <response code="400">Некорректный запрос</response>
    [HttpGet("by-surname")]
    [ProducesResponseType(typeof(PagedResponse<FileArchiveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<FileArchiveDto>>> SearchBySurname(
        [FromQuery] string surname,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(surname))
        {
            return BadRequest(new { message = "Параметр surname обязателен" });
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var searchTerm = surname.Trim().ToLower();

        var query = _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => !f.IsDeleted && f.FullName.ToLower().Contains(searchTerm));

        var totalCount = await query.CountAsync(cancellationToken);

        var files = await query
            .OrderBy(f => f.FullName)
            .ThenBy(f => f.Box.Number)
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

    /// <summary>
    /// Поиск дела по ID абитуриента
    /// </summary>
    /// <param name="applicantId">Идентификатор абитуриента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Дело абитуриента</returns>
    /// <response code="200">Дело найдено</response>
    /// <response code="404">Дело не найдено</response>
    [HttpGet("by-applicant/{applicantId:guid}")]
    [ProducesResponseType(typeof(FileArchiveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileArchiveDto>> SearchByApplicant(
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.ApplicantId == applicantId && !f.IsDeleted)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (file == null)
        {
            return NotFound(new { message = $"Дело для абитуриента с ID {applicantId} не найдено" });
        }

        return Ok(file);
    }
}

