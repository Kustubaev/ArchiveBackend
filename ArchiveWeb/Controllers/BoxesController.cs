using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveWeb.Controllers;

[ApiController]
[Route("api/boxes")]
[Produces("application/json")]
[Tags("Коробки")]
public sealed class BoxesController : ControllerBase
{
    private readonly IBoxService _boxService;
    private readonly ILogger<BoxesController> _logger;

    public BoxesController(
        IBoxService boxService,
        ILogger<BoxesController> logger)
    {
        _boxService = boxService;
        _logger = logger;
    }

    /// <summary> Получить список всех коробок </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BoxDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BoxDto>>> GetBoxes(
        CancellationToken cancellationToken = default)
    {
        var boxes = await _boxService.GetBoxesAsync(cancellationToken);
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
        var box = await _boxService.GetBoxByNumberAsync(number, cancellationToken);

        if (box == null)
            return NotFound(new { message = $"Коробка с номером {number} не найдена" });

        return Ok(box);
    }

    /// <summary> Получить список дел в коробке </summary>
    [HttpGet("{number:int}/files")]
    [ProducesResponseType(typeof(List<FileArchiveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FileArchiveDto>>> GetBoxFiles(
        int number,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _boxService.GetBoxFilesAsync(number, cancellationToken);
            return Ok(files);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

