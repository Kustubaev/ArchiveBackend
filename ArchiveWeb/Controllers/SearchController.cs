using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/search")]
[Produces("application/json")]
[Tags("Поиск")]
public sealed class SearchController : ControllerBase
{
    private readonly IFileArchiveService _fileArchiveService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IFileArchiveService fileArchiveService,
        ILogger<SearchController> logger)
    {
        _fileArchiveService = fileArchiveService;
        _logger = logger;
    }

    /// <summary> Поиск дел по фамилии </summary>
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

        var response = await _fileArchiveService.SearchBySurnameAsync(surname, page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary> Поиск дела по ID абитуриента </summary>
    [HttpGet("by-applicant/{applicantId:guid}")]
    [ProducesResponseType(typeof(FileArchiveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileArchiveDto>> SearchByApplicant(
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileArchiveService.SearchByApplicantIdAsync(applicantId, cancellationToken);

        if (file == null)
        {
            return NotFound(new { message = $"Дело для абитуриента с ID {applicantId} не найдено" });
        }

        return Ok(file);
    }
}

