using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Абитуриенты")]
public sealed class ApplicantsController : ControllerBase
{
    private readonly IApplicantService _applicantService;
    private readonly ILogger<ApplicantsController> _logger;

    public ApplicantsController(
        IApplicantService applicantService,
        ILogger<ApplicantsController> logger)
    {
        _applicantService = applicantService;
        _logger = logger;
    }

    /// <summary> Получить список абитуриентов с пагинацией </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ApplicantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ApplicantDto>>> GetApplicants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _applicantService.GetApplicantsAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary> Создать нового абитуриента </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplicantDto>> CreateApplicant(
        [FromBody] CreateApplicantDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _applicantService.CreateApplicantAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetApplicant), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Получить абитуриента по ID </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicantDto>> GetApplicant(Guid id, CancellationToken cancellationToken = default)
    {
        var applicant = await _applicantService.GetApplicantByIdAsync(id, cancellationToken);

        if (applicant == null)
            return NotFound(new { message = $"Абитуриент с ID {id} не найден" });

        return Ok(applicant);
    }
        
    /// <summary> Обновить данные абитуриента </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicantDto>> UpdateApplicant(
        Guid id,
        [FromBody] UpdateApplicantDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _applicantService.UpdateApplicantAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary> Удалить абитуриента </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteApplicant(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _applicantService.DeleteApplicantAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("не найден"))
                return NotFound(new { message = ex.Message });
            
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary> Поиск абитуриентов по имени, email или телефону </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResponse<ApplicantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ApplicantDto>>> SearchApplicants(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _applicantService.SearchApplicantsAsync(query, page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary> Автоматическая генерация абитуриентов </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(List<ApplicantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ApplicantDto>>> GenerateApplicants(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _applicantService.GenerateApplicantsAsync(count, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

