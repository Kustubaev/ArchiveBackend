using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;

namespace ArchiveWeb.Application.Services;

public sealed class HistoryService : IHistoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(
        IUnitOfWork uow,
        ILogger<HistoryService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResponse<ArchiveHistoryDto>> GetArchiveHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var history = await _uow.ArchiveHistories.GetPagedAsync(page, pageSize, cancellationToken);
        var totalCount = await _uow.ArchiveHistories.CountAsync(cancellationToken);

        var historyDtos = history.Select(h => new ArchiveHistoryDto
        {
            Id = h.Id,
            FileArchiveId = h.FileArchiveId,
            FullName = h.FileArchive?.FullName,
            FileNumberForArchive = h.FileArchive?.FileNumberForArchive,
            Action = h.Action,
            Reason = h.Reason,
            OldBoxNumber = h.OldBoxNumber,
            OldPosition = h.OldPosition,
            NewBoxNumber = h.NewBoxNumber,
            NewPosition = h.NewPosition,
            CreatedAt = h.CreatedAt
        }).ToList();

        return new PagedResponse<ArchiveHistoryDto>
        {
            Items = historyDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<ArchiveHistoryDto>> GetFileHistoryAsync(Guid fileId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var file = await _uow.FileArchives.GetByIdAsync(fileId, cancellationToken);
        if (file == null)
            throw new InvalidOperationException($"Дело с ID {fileId} не найдено");

        var history = await _uow.ArchiveHistories.GetByFileArchiveIdAsync(fileId, page, pageSize, cancellationToken);
        var totalCount = await _uow.ArchiveHistories.CountByFileArchiveIdAsync(fileId, cancellationToken);

        var historyDtos = history.Select(h => new ArchiveHistoryDto
        {
            Id = h.Id,
            FileArchiveId = h.FileArchiveId,
            FullName = h.FileArchive?.FullName,
            FileNumberForArchive = h.FileArchive?.FileNumberForArchive,
            Action = h.Action,
            Reason = h.Reason,
            OldBoxNumber = h.OldBoxNumber,
            OldPosition = h.OldPosition,
            NewBoxNumber = h.NewBoxNumber,
            NewPosition = h.NewPosition,
            CreatedAt = h.CreatedAt
        }).ToList();

        return new PagedResponse<ArchiveHistoryDto>
        {
            Items = historyDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<ArchiveHistoryDto>> GetBoxHistoryAsync(int boxNumber, CancellationToken cancellationToken = default)
    {
        var box = await _uow.Boxes.GetByNumberAsync(boxNumber, cancellationToken);
        if (box == null)
            throw new InvalidOperationException($"Коробка с номером {boxNumber} не найдена");

        var history = await _uow.ArchiveHistories.GetByBoxNumberAsync(boxNumber, cancellationToken);

        return history.Select(h => new ArchiveHistoryDto
        {
            Id = h.Id,
            FileArchiveId = h.FileArchiveId,
            FullName = h.FileArchive?.FullName,
            FileNumberForArchive = h.FileArchive?.FileNumberForArchive,
            Action = h.Action,
            Reason = h.Reason,
            OldBoxNumber = h.OldBoxNumber,
            OldPosition = h.OldPosition,
            NewBoxNumber = h.NewBoxNumber,
            NewPosition = h.NewPosition,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    public async Task<List<ArchiveHistoryDto>> GetLetterHistoryAsync(char letter, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letter);
        var letterEntity = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == normalizeLetter, cancellationToken);
        if (letterEntity == null)
            throw new InvalidOperationException($"Буква '{letter}' не найдена");

        var history = await _uow.ArchiveHistories.GetByLetterIdAsync(letterEntity.Id, cancellationToken);

        return history.Select(h => new ArchiveHistoryDto
        {
            Id = h.Id,
            FileArchiveId = h.FileArchiveId,
            FullName = h.FileArchive?.FullName,
            FileNumberForArchive = h.FileArchive?.FileNumberForArchive,
            Action = h.Action,
            Reason = h.Reason,
            OldBoxNumber = h.OldBoxNumber,
            OldPosition = h.OldPosition,
            NewBoxNumber = h.NewBoxNumber,
            NewPosition = h.NewPosition,
            CreatedAt = h.CreatedAt
        }).ToList();
    }
}