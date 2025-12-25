using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Абитуриенты")]
public sealed class ApplicantsController : ControllerBase
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<ApplicantsController> _logger;

    public ApplicantsController(
        ArchiveDbContext context,
        ILogger<ApplicantsController> logger)
    {
        _context = context;
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

        int totalCount = await _context.Applicants.CountAsync(cancellationToken);

        List<ApplicantDto> applicants = await _context.Applicants
            .OrderBy(a => a.Surname)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.Patronymic)
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
                FileArchiveId = a.FileArchive != null ? a.FileArchive.Id : null
            })
            .ToListAsync(cancellationToken);

        PagedResponse<ApplicantDto> response = new PagedResponse<ApplicantDto>
        {
            Items = applicants,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary> Получить абитуриента по ID </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicantDto>> GetApplicant(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var applicant = await _context.Applicants
            .Where(a => a.Id == id)
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
                FileArchiveId = a.FileArchive != null ? a.FileArchive.Id : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (applicant == null)
            return NotFound(new { message = $"Абитуриент с ID {id} не найден" });

        return Ok(applicant);
    }

    /// <summary> Создать нового абитуриента </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicantDto>> CreateApplicant(
        [FromBody] CreateApplicantDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        //if (await _context.Applicants.AnyAsync(a => a.Email == dto.Email, cancellationToken))
        //    return Conflict(new { message = "Пользователь с таким email существует." });

        var applicant = new Applicant
        {
            Surname = dto.Surname,
            FirstName = dto.FirstName,
            Patronymic = dto.Patronymic,
            EducationLevel = dto.EducationLevel,
            StudyForm = dto.StudyForm,
            IsOriginalSubmitted = dto.IsOriginalSubmitted,
            IsBudgetFinancing = dto.IsBudgetFinancing,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email
        };

        _context.Applicants.Add(applicant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Создан новый абитуриент: {ApplicantId}, Email: {Email}",
            applicant.Id,
            applicant.Email);

        ApplicantDto result = new ApplicantDto
        {
            Id = applicant.Id,
            Surname = applicant.Surname,
            FirstName = applicant.FirstName,
            Patronymic = applicant.Patronymic,
            EducationLevel = applicant.EducationLevel,
            StudyForm = applicant.StudyForm,
            IsOriginalSubmitted = applicant.IsOriginalSubmitted,
            IsBudgetFinancing = applicant.IsBudgetFinancing,
            PhoneNumber = applicant.PhoneNumber,
            Email = applicant.Email,
            CreatedAt = applicant.CreatedAt,
            UpdatedAt = applicant.UpdatedAt,
            FileArchiveId = null
        };

        return CreatedAtAction(
            nameof(GetApplicant),
            new { id = applicant.Id },
            result);
    }

    /// <summary> Обновить данные абитуриента </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApplicantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicantDto>> UpdateApplicant(
        Guid id,
        [FromBody] UpdateApplicantDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var applicant = await _context.Applicants
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (applicant == null)
            return NotFound(new { message = $"Абитуриент с ID {id} не найден" });

        // Проверка уникальности email (если изменился)
        //if (applicant.Email != dto.Email)
        //{
        //    var emailExists = await _context.Applicants.AnyAsync(a => a.Email == dto.Email && a.Id != id, cancellationToken);
        //    if (emailExists) return Conflict(new { message = $"Абитуриент с email {dto.Email} уже существует" });
        //}

        applicant.Surname = dto.Surname ?? applicant.Surname;
        applicant.FirstName = dto.FirstName ?? applicant.FirstName;
        applicant.Patronymic = dto.Patronymic ?? applicant.Patronymic;
        applicant.EducationLevel = dto.EducationLevel ?? applicant.EducationLevel;
        applicant.StudyForm = dto.StudyForm ?? applicant.StudyForm;
        applicant.IsOriginalSubmitted = dto.IsOriginalSubmitted ?? applicant.IsOriginalSubmitted;
        applicant.IsBudgetFinancing = dto.IsBudgetFinancing ?? applicant.IsBudgetFinancing;
        applicant.PhoneNumber = dto.PhoneNumber ?? applicant.PhoneNumber;
        applicant.Email = dto.Email ?? applicant.Email;
        applicant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Обновлен абитуриент: {ApplicantId}",
            applicant.Id);

        ApplicantDto result = new ApplicantDto
        {
            Id = applicant.Id,
            Surname = applicant.Surname,
            FirstName = applicant.FirstName,
            Patronymic = applicant.Patronymic,
            EducationLevel = applicant.EducationLevel,
            StudyForm = applicant.StudyForm,
            IsOriginalSubmitted = applicant.IsOriginalSubmitted,
            IsBudgetFinancing = applicant.IsBudgetFinancing,
            PhoneNumber = applicant.PhoneNumber,
            Email = applicant.Email,
            CreatedAt = applicant.CreatedAt,
            UpdatedAt = applicant.UpdatedAt,
            FileArchiveId = applicant.FileArchive?.Id
        };

        return Ok(result);
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
        var applicant = await _context.Applicants
            .Include(a => a.FileArchive)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (applicant == null) return NotFound(new { message = $"Абитуриент с ID {id} не найден" });

        // Проверка наличия связанного дела
        if (applicant.FileArchive != null)
            return Conflict(new
            {
                message = $"Невозможно удалить абитуриента: у него есть дело в архиве (ID: {applicant.FileArchive.Id})"
            });

        _context.Applicants.Remove(applicant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Удален абитуриент: {ApplicantId}",
            applicant.Id);

        return NoContent();
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

        var applicantsQuery = _context.Applicants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            string searchTerm = query.Trim().ToLower();
            applicantsQuery = applicantsQuery.Where(a =>
                (a.Surname + " " + a.FirstName + " " + (a.Patronymic ?? string.Empty)).ToLower().Contains(searchTerm) ||
                a.Email.ToLower().Contains(searchTerm) ||
                a.PhoneNumber.Contains(searchTerm));
        }

        int totalCount = await applicantsQuery.CountAsync(cancellationToken);

        List<ApplicantDto> applicants = await applicantsQuery
            .OrderBy(a => a.Surname)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.Patronymic)
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
                FileArchiveId = a.FileArchive != null ? a.FileArchive.Id : null
            })
            .ToListAsync(cancellationToken);

        PagedResponse<ApplicantDto> response = new PagedResponse<ApplicantDto>
        {
            Items = applicants,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }
}

