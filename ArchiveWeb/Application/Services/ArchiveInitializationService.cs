using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.Letter;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;

namespace ArchiveWeb.Application.Services;

public sealed class ArchiveInitializationService : IArchiveInitializationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArchiveInitializationService> _logger;

    public ArchiveInitializationService(
        IUnitOfWork unitOfWork,
        ILogger<ArchiveInitializationService> logger)
    {
        _uow = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> IsArchiveInitializedAsync(CancellationToken cancellationToken = default)
    {
        var hasConfig = await _uow.ArchiveConfig.ExistsAsync(cancellationToken);
        var hasLetters = await _uow.Letters.ExistsAsync(cancellationToken);
        var hasBoxes = await _uow.Boxes.ExistsAsync(cancellationToken);

        return hasConfig && hasLetters && hasBoxes;
    }

    public async Task InitializeArchiveAsync(InitializeArchiveDto dto, CancellationToken cancellationToken = default)
    {
        // Проверка, не инициализирован ли уже архив
        if (await IsArchiveInitializedAsync(cancellationToken))
            throw new InvalidOperationException("Архив уже инициализирован. Для переинициализации необходимо очистить существующие данные.");


        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            ArchiveConfiguration config = new ArchiveConfiguration
            {
                BoxCount = dto.BoxCount,
                BoxCapacity = dto.BoxCapacity,
                AdaptiveRedistributionThreshold = dto.AdaptiveRedistributionThreshold,
                AdaptiveWeightNew = dto.AdaptiveWeightNew,
                AdaptiveWeightOld = dto.AdaptiveWeightOld,
                PercentReservedFiles = dto.PercentReservedFiles,
                PercentDeletedFiles = dto.PercentDeletedFiles,
            };

            await _uow.ArchiveConfig.AddAsync(config, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            
            List<Letter> letters = LetterHelper.CreateAllLetters(); // Создание все букв (пустые без позиций)
            await _uow.Letters.AddRangeAsync(letters);
            await _uow.SaveChangesAsync(cancellationToken);


            Dictionary<char, double> distribution = DistributionHelper.CreateDefaultDistribution(); // Создание базового (идеального распределения)
            ArchiveHelper.RecalculatedLetters(config, letters, distribution); // Буквам присваиют их границы распределения 
            await _uow.SaveChangesAsync(cancellationToken);


            List<Box> boxes = BoxHelper.CreateBoxes(dto.BoxCount, dto.BoxCapacity); // Создание коробок от 1 до заданного вкл
            await _uow.Boxes.AddRangeAsync(boxes);
            await _uow.SaveChangesAsync(cancellationToken);


            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("Архив успешно инициализирован: Конфигурация создана, Букв: {LettersCount}, Коробок: {BoxesCount}", letters.Count, boxes.Count);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RedistributionResultDto> RedistributeArchiveAsync(CancellationToken cancellationToken = default, ArchiveConfiguration? config = null)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            DateTime startedAt = DateTime.UtcNow;
            config = config ?? await _uow.ArchiveConfig.GetLastArchiveConfigurationAsync(cancellationToken); // Получение последнее конфигурации архива
            if (config == null) throw new InvalidOperationException("Конфигурация архива не найдена");


            List<Letter> letters = await _uow.Letters.GetAllAsync(cancellationToken); // Получение всех букв


            List<(char, int)> fileDistribution = await _uow.FileArchives.GetFileDistributionAsync(cancellationToken);
            Dictionary<char, double> idealDistribution = DistributionHelper.GetIdealDistribution(config, fileDistribution); // Вычисление акту распределения на основе дел


            ArchiveHelper.RecalculatedLetters(config, letters, idealDistribution); // Буквы пересчитываются (новые границы, сбрасывается счётчик дел) 
            await _uow.SaveChangesAsync(cancellationToken);


            // Загружаем все дела ПЕРЕД удалением коробок, чтобы сохранить старые BoxId и Box.Number
            List<FileArchive> sortedDeletedFiles = await _uow.FileArchives.GetDeletedSortedAsync(cancellationToken);
            Dictionary<char, List<FileArchive>> activeFileAsync = await _uow.FileArchives.GetFileArchivesGroupedByFirstLetterSurnameSortedAsync(cancellationToken);
            
            // Сохраняем старые BoxId и Box.Number для истории (до удаления коробок)
            var oldBoxData = new Dictionary<Guid, (Guid? BoxId, int? BoxNumber)>();
            foreach (var file in sortedDeletedFiles.Concat(activeFileAsync.Values.SelectMany(f => f)))
            {
                if (!oldBoxData.ContainsKey(file.Id))
                {
                    oldBoxData[file.Id] = (file.BoxId, file.Box?.Number);
                }
            }


            // Заменяем старые коробки на новые
            await _uow.Boxes.ClearAllAsync(cancellationToken);
            List<Box> newBoxes = BoxHelper.CreateBoxes(config.BoxCount, config.BoxCapacity);
            await _uow.Boxes.AddRangeAsync(newBoxes);
            await _uow.SaveChangesAsync(cancellationToken); // ВАЖНО: Сохраняем новые коробки, чтобы получить их Id


            List<ArchiveHistory> newItemsArchiveHistories = new List<ArchiveHistory>(); // Создаёт массив для истории измений дел


            // Перераспределение удалённых дел
            foreach (FileArchive file in sortedDeletedFiles)
            {
                FileArchiveHelper.ChangePositionFileArchive(file, letters, newBoxes, config.BoxCapacity, newItemsArchiveHistories, oldBoxData);
            }

            // Перераспределение остальных дел
            foreach (var (letterChar, files) in activeFileAsync)
            {
                foreach (FileArchive file in files)
                {
                    FileArchiveHelper.ChangePositionFileArchive(file, letters, newBoxes, config.BoxCapacity, newItemsArchiveHistories, oldBoxData);
                }
            }

            await _uow.ArchiveHistories.AddRangeAsync(newItemsArchiveHistories, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken); // Сохраняем все изменения в FileArchive и историю

            await _uow.CommitAsync(cancellationToken);
            _logger.LogInformation("Архив успешно инициализирован: Конфигурация создана, Букв: {LettersCount}, Коробок: {BoxesCount}", letters.Count, newBoxes.Count);

            List<ArchiveHistoryDto> archiveHistoriesDto = newItemsArchiveHistories.Select(h => new ArchiveHistoryDto
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

            string order = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ+-";
            List<LetterDto> lettersDto = letters
            .OrderBy(l => order.IndexOf(l.Value))
            .Select(l => new LetterDto
            {
                Id = l.Id,
                Value = l.Value,
                ExpectedCount = l.ExpectedCount,
                StartBox = l.StartBox,
                EndBox = l.EndBox,
                StartPosition = l.StartPosition,
                EndPosition = l.EndPosition,
                ActualCount = l.ActualCount,
                UsedCount = l.UsedCount,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToList();

            List<BoxDto> boxesDto = newBoxes.OrderBy(b => b.Number)
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
            .ToList();


            return new RedistributionResultDto
            {
                ActualDistribution = idealDistribution,
                ArchiveHistories = archiveHistoriesDto,
                Letters = lettersDto,
                Boxs = boxesDto,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
            };
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ArchiveConfigurationDto> UpdateArchiveAsync(UpdateArchiveConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        ArchiveConfiguration? config = await _uow.ArchiveConfig.GetLastArchiveConfigurationAsync(cancellationToken);
        if (config == null) 
            throw new Exception("Конфигурация архива не найдена" );


        // Получаем существующее количество дел в архиве
        int countFiles = await _uow.FileArchives.CountAsync(cancellationToken); 

        //  Определяем сколько мест в новом архиве
        int boxCount = dto.BoxCount ?? config.BoxCount;
        int boxCapacity = dto.BoxCapacity ?? config.BoxCapacity;
        int newCountFiles = boxCount * boxCapacity; 

        if (countFiles > newCountFiles)
            throw new InvalidOperationException($"Невозможно изменить конфигурацию архива: текущее количество дел ({countFiles}) превышает доступное место в новой конфигурации ({newCountFiles}). Пожалуйста, увеличьте количество коробок или их вместимость.");


        // Валидация и синхронизация весов адаптивного распределения
        if (dto.AdaptiveWeightNew.HasValue || dto.AdaptiveWeightOld.HasValue)
        {
            var newWeight = dto.AdaptiveWeightNew ?? (1 - (dto.AdaptiveWeightOld ?? config.AdaptiveWeightOld));
            var oldWeight = dto.AdaptiveWeightOld ?? (1 - newWeight);

            var sum = newWeight + oldWeight;
            if (Math.Abs(sum - 1.0) > 0.01)
                throw new Exception("Сумма весов адаптивного распределения должна быть равна 1.0" );

            if (newWeight < 0 || newWeight > 1 || oldWeight < 0 || oldWeight > 1)
                throw new Exception("Каждый вес должен быть в диапазоне от 0.0 до 1.0");

            config.AdaptiveWeightNew = newWeight;
            config.AdaptiveWeightOld = oldWeight;
        }
        config.BoxCount = dto.BoxCount ?? config.BoxCount;
        config.BoxCapacity = dto.BoxCapacity ?? config.BoxCapacity;
        config.AdaptiveRedistributionThreshold = dto.AdaptiveRedistributionThreshold ?? config.AdaptiveRedistributionThreshold;
        config.PercentReservedFiles = dto.PercentReservedFiles ?? config.PercentReservedFiles;
        config.PercentDeletedFiles = dto.PercentDeletedFiles ?? config.PercentDeletedFiles;
        config.UpdatedAt = DateTime.UtcNow;


        // Перераспределение дел в архиве
        await RedistributeArchiveAsync(cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Конфигурация архива обновлена: ConfigId={ConfigId}", config.Id);


        ArchiveConfigurationDto updatedDto = new ArchiveConfigurationDto
        {
            Id = config.Id,
            BoxCount = config.BoxCount,
            BoxCapacity = config.BoxCapacity,
            AdaptiveRedistributionThreshold = config.AdaptiveRedistributionThreshold,
            AdaptiveWeightNew = config.AdaptiveWeightNew,
            AdaptiveWeightOld = config.AdaptiveWeightOld,
            PercentReservedFiles = config.PercentReservedFiles,
            PercentDeletedFiles = config.PercentDeletedFiles,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
        return updatedDto;  
    }
}