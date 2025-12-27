using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/history")]
[Produces("application/json")]
[Tags("История")]
public sealed class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        IHistoryService historyService,
        ILogger<HistoryController> logger)
    {
        _historyService = historyService;
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

        var response = await _historyService.GetArchiveHistoryAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary> Получить историю для дела </summary>
    [HttpGet("file/{fileId:guid}")]
    [ProducesResponseType(typeof(PagedResponse<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<ArchiveHistoryDto>>> GetFileHistory(
        Guid fileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var response = await _historyService.GetFileHistoryAsync(fileId, page, pageSize, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Получить историю для коробки </summary>
    [HttpGet("box/{boxNumber:int}")]
    [ProducesResponseType(typeof(List<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ArchiveHistoryDto>>> GetBoxHistory(
        int boxNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _historyService.GetBoxHistoryAsync(boxNumber, cancellationToken);
            return Ok(history);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary> Получить историю для буквы </summary>
    [HttpGet("letter/{letter}")]
    [ProducesResponseType(typeof(List<ArchiveHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ArchiveHistoryDto>>> GetLetterHistory(
        char letter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _historyService.GetLetterHistoryAsync(letter, cancellationToken);
            return Ok(history);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

