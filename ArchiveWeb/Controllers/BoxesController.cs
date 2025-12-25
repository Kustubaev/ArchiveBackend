using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/boxes")]
[Produces("application/json")]
[Tags("Коробки")]
public sealed class BoxesController : ControllerBase
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<BoxesController> _logger;

    public BoxesController(
        ArchiveDbContext context,
        ILogger<BoxesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary> Получить список всех коробок </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BoxDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BoxDto>>> GetBoxes(
        CancellationToken cancellationToken = default)
    {
        var boxes = await _context.Boxes
            .OrderBy(b => b.Number)
            .Select(b => new BoxDto
            {
                Id = b.Id,
                Number = b.Number,
                ExpectedCount = b.ExpectedCount,
                ActualCount = b.ActualCount,
                AvailableSpace = b.AvailableSpace,
                HasAvailableSpace = b.HasAvailableSpace,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(boxes);
    }

    /// <summary> Получить коробку по номеру </summary>
    [HttpGet("{number:int}")]
    [ProducesResponseType(typeof(BoxDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoxDto>> GetBox(
        int number,
        CancellationToken cancellationToken = default)
    {
        var box = await _context.Boxes
            .Where(b => b.Number == number)
            .Select(b => new BoxDto
            {
                Id = b.Id,
                Number = b.Number,
                ExpectedCount = b.ExpectedCount,
                ActualCount = b.ActualCount,
                AvailableSpace = b.AvailableSpace,
                HasAvailableSpace = b.HasAvailableSpace,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (box == null)
            return NotFound(new { message = $"Коробка с номером {number} не найдена" });

        return Ok(box);
    }

    /// <summary> Получить список дел в коробке </summary>
    [HttpGet("{number:int}/files")]
    [ProducesResponseType(typeof(List<FileArchiveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FileArchiveDto>>> GetBoxFiles(
        int number,
        CancellationToken cancellationToken = default)
    {
        var box = await _context.Boxes
            .FirstOrDefaultAsync(b => b.Number == number, cancellationToken);

        if (box == null)
            return NotFound(new { message = $"Коробка с номером {number} не найдена" });

        var files = await _context.FileArchives
            .Include(f => f.Box)
            .Include(f => f.Letter)
            .Where(f => f.BoxId == box.Id)
            .OrderBy(f => f.PositionInBox)
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
}

