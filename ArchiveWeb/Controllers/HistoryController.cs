using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/history")]
[Produces("application/json")]
[Tags("История")]
public sealed class HistoryController : ControllerBase
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        ArchiveDbContext context,
        ILogger<HistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary> Получить список всех изменений </summary>
    [HttpGet("histories")]
    [ProducesResponseType(typeof(PagedResponse<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ArchiveHistoryDto>>> GetArchiveHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;


        var archiveHistories= _context.ArchiveHistories.Include(h => h.FileArchive);

        var totalCount = await archiveHistories.CountAsync(cancellationToken);

        var history = await archiveHistories
            .OrderBy(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new ArchiveHistoryDto
            {
                Id = h.Id,

                FileArchiveId = h.FileArchiveId,
                FullName = h.FileArchive.FullName,
                FileNumberForArchive = h.FileArchive.FileNumberForArchive,

                Action = h.Action,
                Reason = h.Reason,

                OldBoxNumber = h.OldBoxNumber,
                OldPosition = h.OldPosition,
                NewBoxNumber = h.NewBoxNumber,
                NewPosition = h.NewPosition,

                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<ArchiveHistoryDto>
        {
            Items = history,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary> Получить историю для дела </summary>
    [HttpGet("file/{fileId:guid}")]
    [ProducesResponseType(typeof(PagedResponse<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ArchiveHistoryDto>>> GetFileHistory(
        Guid fileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var fileExists = await _context.FileArchives
            .AnyAsync(f => f.Id == fileId, cancellationToken);
        if (!fileExists)
            return NotFound(new { message = $"Дело с ID {fileId} не найдено" });

        var query = _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.FileArchiveId == fileId);

        var totalCount = await query.CountAsync(cancellationToken);

        var history = await query
            .OrderBy(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new ArchiveHistoryDto
            {
                Id = h.Id,

                FileArchiveId = h.FileArchiveId,
                FullName = h.FileArchive.FullName,
                FileNumberForArchive = h.FileArchive.FileNumberForArchive,

                Action = h.Action,
                Reason = h.Reason,

                OldBoxNumber = h.OldBoxNumber,
                OldPosition = h.OldPosition,
                NewBoxNumber = h.NewBoxNumber,
                NewPosition = h.NewPosition,

                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<ArchiveHistoryDto>
        {
            Items = history,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary> Получить историю для коробки </summary>
    [HttpGet("box/{boxNumber:int}")]
    [ProducesResponseType(typeof(List<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArchiveHistoryDto>>> GetBoxHistory(
        int boxNumber,
        CancellationToken cancellationToken = default)
    {
        var box = await _context.Boxes
            .FirstOrDefaultAsync(b => b.Number == boxNumber, cancellationToken);
        if (box == null)
            return NotFound(new { message = $"Коробка с номером {boxNumber} не найдена" });

        var history = await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.OldBoxId == box.Id || h.NewBoxId == box.Id || h.OldBoxNumber == boxNumber || h.NewBoxNumber == boxNumber)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new ArchiveHistoryDto
            {
                Id = h.Id,

                FileArchiveId = h.FileArchiveId,
                FullName = h.FileArchive.FullName,
                FileNumberForArchive = h.FileArchive.FileNumberForArchive,

                Action = h.Action,
                Reason = h.Reason,

                OldBoxNumber = h.OldBoxNumber,
                OldPosition = h.OldPosition,
                NewBoxNumber = h.NewBoxNumber,
                NewPosition = h.NewPosition,

                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    /// <summary> Получить историю для буквы </summary>
    [HttpGet("letter/{letter}")]
    [ProducesResponseType(typeof(List<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArchiveHistoryDto>>> GetLetterHistory(
        char letter,
        CancellationToken cancellationToken = default)
    {

        var letterEntity = await _context.Letters
            .FirstOrDefaultAsync(l => l.Value == char.ToUpper(letter), cancellationToken);
        if (letterEntity == null)
            return NotFound(new { message = $"Буква '{letter}' не найдена" });

        var history = await _context.ArchiveHistories
            .Include(h => h.FileArchive)
            .Where(h => h.OldLetterId == letterEntity.Id || h.NewLetterId == letterEntity.Id)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new ArchiveHistoryDto
            {
                Id = h.Id,

                FileArchiveId = h.FileArchiveId,
                FullName = h.FileArchive.FullName,
                FileNumberForArchive = h.FileArchive.FileNumberForArchive,

                Action = h.Action,
                Reason = h.Reason,

                OldBoxNumber = h.OldBoxNumber,
                OldPosition = h.OldPosition,
                NewBoxNumber = h.NewBoxNumber,
                NewPosition = h.NewPosition,

                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
    }
}

