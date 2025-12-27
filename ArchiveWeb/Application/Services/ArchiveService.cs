using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Archive;
using ArchiveWeb.Application.DTOs.ArchiveConfiguration;
using ArchiveWeb.Application.DTOs.ArchiveHistory;
using ArchiveWeb.Application.DTOs.Box;
using ArchiveWeb.Application.DTOs.Letter;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Exceptions;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;


namespace ArchiveWeb.Application.Services;

public sealed class ArchiveService : IArchiveService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArchiveService> _logger;

    public ArchiveService(
        IUnitOfWork unitOfWork,
        ILogger<ArchiveService> logger)
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


            List<Letter> letters = ArchiveFactory.CreateAllLetters(); // Создание все букв (пустые без позиций)
            await _uow.Letters.AddRangeAsync(letters);
            await _uow.SaveChangesAsync(cancellationToken);


            Dictionary<char, double> distribution = DistributionHelper.CreateDefaultDistribution(); // Создание базового (идеального распределения)
            RecalculateLetters(config, letters, distribution); // Буквам присваиют их границы распределения 
            await _uow.SaveChangesAsync(cancellationToken);


            List<Box> boxes = ArchiveFactory.CreateBoxes(dto.BoxCount, dto.BoxCapacity); // Создание коробок от 1 до заданного вкл
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

    public async Task<ArchiveStatisticsDto> GetArchiveStatisticsAsync(CancellationToken cancellationToken = default)
    {
        int totalFiles = await _uow.FileArchives.CountAsync(cancellationToken);
        int deletedFiles = await _uow.FileArchives.CountAsync(f => f.IsDeleted, cancellationToken);

        int totalBoxes = await _uow.Boxes.CountAsync(cancellationToken);
        int usedBoxes = await _uow.Boxes.CountAsync(b => b.ActualCount > 0, cancellationToken);
        int emptyBoxes = totalBoxes - usedBoxes;

        int totalLetters = await _uow.Letters.CountAsync(cancellationToken);

        // Статистика по буквам
        var letters = await _uow.Letters.GetAllAsync(cancellationToken);
        var letterStats = letters.Select(l => new LetterStatisticsDto
        {
            Letter = l.Value,
            ExpectedCount = l.ExpectedCount ?? 0,
            ActualCount = l.ActualCount,
            UsedCount = l.UsedCount,
            FillPercentage = l.ExpectedCount.HasValue && l.ExpectedCount.Value > 0
                    ? (l.ActualCount / (double)l.ExpectedCount.Value) * 100.0
                    : 0.0,
            IsOverflow = l.ExpectedCount.HasValue && l.ActualCount >= l.ExpectedCount.Value,
            StartBox = l.StartBox,
            EndBox = l.EndBox
        }).ToList();

        // Статистика по коробкам
        var boxes = await _uow.Boxes.GetAllAsync(cancellationToken);
        var boxStats = boxes
            .OrderBy(b => b.Number)
            .Select(b => new BoxStatisticsDto
            {
                BoxNumber = b.Number,
                ExpectedCount = b.ExpectedCount ?? 0,
                CompletedCount = b.ActualCount,
                AvailableSpace = b.AvailableSpace,
                FillPercentage = b.ExpectedCount.HasValue && b.ExpectedCount.Value > 0
                    ? (b.ActualCount / (double)b.ExpectedCount.Value) * 100.0
                    : 0.0,
                IsFull = !b.HasAvailableSpace
            })
            .ToList();

        // Общий процент заполнения архива
        int totalExpectedCapacity = boxStats.Sum(b => b.ExpectedCount);
        double archiveFillPercentage = totalExpectedCapacity > 0
            ? (totalFiles / (double)totalExpectedCapacity) * 100.0
            : 0.0;

        return new ArchiveStatisticsDto
        {
            TotalFiles = totalFiles,
            DeletedFiles = deletedFiles,
            BoxCount = totalBoxes,
            TotalLetters = totalLetters,
            UsedBoxes = usedBoxes,
            EmptyBoxes = emptyBoxes,
            ArchiveFillPercentage = archiveFillPercentage,
            LetterStatistics = letterStats.ToDictionary(s => s.Letter),
            BoxStatistics = boxStats
        };
    }

    public async Task<object> GetArchiveStatusAsync(CancellationToken cancellationToken = default)
    {
        var isInitialized = await IsArchiveInitializedAsync(cancellationToken);

        var config = await _uow.ArchiveConfig.GetLastArchiveConfigurationAsync(cancellationToken);
        var lettersCount = await _uow.Letters.CountAsync(cancellationToken);
        var boxesCount = await _uow.Boxes.CountAsync(cancellationToken);
        var allfilesCount = await _uow.FileArchives.CountAsync(cancellationToken);
        var deletedfilesCount = await _uow.FileArchives.CountAsync(f => f.IsDeleted, cancellationToken);

        return new
        {
            IsInitialized = isInitialized,
            ConfigurationExists = config != null,
            LettersCount = lettersCount,
            BoxesCount = boxesCount,
            AllFilesCount = allfilesCount,
            DeletedfilesCount = deletedfilesCount,
        };
    }

    public async Task<ArchiveConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await _uow.ArchiveConfig.GetLastArchiveConfigurationAsync(cancellationToken);

        if (config == null)
            throw new InvalidOperationException("Конфигурация архива не найдена");

        return new ArchiveConfigurationDto
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
            UpdatedAt = config.UpdatedAt,
        };
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
            Dictionary<char, double> idealDistribution = DistributionHelper.GetIdealDistribution(config, fileDistribution); // Вычисление актуального распределения на основе дел


            RecalculateLetters(config, letters, idealDistribution); // Буквы пересчитываются (новые границы, сбрасывается счётчик дел) 
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
            List<Box> newBoxes = ArchiveFactory.CreateBoxes(config.BoxCount, config.BoxCapacity);
            await _uow.Boxes.AddRangeAsync(newBoxes);
            await _uow.SaveChangesAsync(cancellationToken); // ВАЖНО: Сохраняем новые коробки, чтобы получить их Id


            List<ArchiveHistory> newItemsArchiveHistories = new List<ArchiveHistory>(); // Создаёт массив для истории измений дел


            // Перераспределение удалённых дел
            foreach (FileArchive file in sortedDeletedFiles)
            {
                ChangePositionFileArchive(file, letters, newBoxes, config.BoxCapacity, newItemsArchiveHistories, oldBoxData);
            }

            // Перераспределение остальных дел
            foreach (var (letterChar, files) in activeFileAsync)
            {
                foreach (FileArchive file in files)
                {
                    ChangePositionFileArchive(file, letters, newBoxes, config.BoxCapacity, newItemsArchiveHistories, oldBoxData);
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
            throw new Exception("Конфигурация архива не найдена");


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
                throw new Exception("Сумма весов адаптивного распределения должна быть равна 1.0");

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
            await _uow.ArchiveConfig.UpdateAsync(config, cancellationToken);

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

    public async Task<object> ClearArchiveAsync(bool clearApplicants, CancellationToken cancellationToken = default)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // Подсчитываем количество удаляемых сущностей для логирования
            var historyCount = await _uow.ArchiveHistories.CountAsync(cancellationToken);
            var fileArchiveCount = await _uow.FileArchives.CountAsync(cancellationToken);
            var boxCount = await _uow.Boxes.CountAsync(cancellationToken);
            var letterCount = await _uow.Letters.CountAsync(cancellationToken);
            var configCount = await _uow.ArchiveConfig.CountAsync(cancellationToken);
            var applicantCount = clearApplicants ? await _uow.Applicants.CountAsync(cancellationToken) : 0;

            // Удаляем сущности в правильном порядке (с учетом внешних ключей)
            // 1. ArchiveHistory (зависит от FileArchive)
            await _uow.ArchiveHistories.DeleteAllAsync(cancellationToken);

            // 2. FileArchive (зависит от Box, Letter, Applicant - Applicant не удаляем)
            await _uow.FileArchives.DeleteAllAsync(cancellationToken);

            // 3. Box
            await _uow.Boxes.ClearAllAsync(cancellationToken);

            // 4. Letter
            await _uow.Letters.DeleteAllAsync(cancellationToken);

            // 5. ArchiveConfiguration
            await _uow.ArchiveConfig.DeleteAllAsync(cancellationToken);

            // 6. Applicant
            if (clearApplicants)
            {
                await _uow.Applicants.DeleteAllAsync(cancellationToken);
            }

            // Сохраняем изменения
            await _uow.SaveChangesAsync(cancellationToken);

            // Коммитим транзакцию
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Архив полностью очищен. Удалено: History={HistoryCount}, FileArchive={FileArchiveCount}, Box={BoxCount}, Letter={LetterCount}, Config={ConfigCount}, Applicant={ApplicantCount}",
                historyCount,
                fileArchiveCount,
                boxCount,
                letterCount,
                configCount,
                applicantCount);

            return new
            {
                message = "Архив успешно очищен",
                deletedEntities = new
                {
                    HistoryCount = historyCount,
                    FileArchiveCount = fileArchiveCount,
                    BoxCount = boxCount,
                    LetterCount = letterCount,
                    ArchiveConfigurationCount = configCount,
                    ApplicantCount = applicantCount
                }
            };
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Ошибка при очистке архива");
            throw;
        }
    }



    /// <summary> Получает массив символов после чего рассчитывает для них позиции и количество дел. Сбрасывает ActualCount. </summary>
    private static void RecalculateLetters(
        ArchiveConfiguration config,
        List<Letter> letters,
        Dictionary<char, double> distribution)
    {
        // Рассчитывает количество файлов для каждой буквы алфавита на основе распределения.
        var countFileLetter = CalculateFileCountsPerLetter(config, distribution);

        int totalAllocated = countFileLetter.Sum(f => f.Value);
        if (totalAllocated != config.TotalFiles)
            throw new InvalidOperationException($"Несоответствие в распределении файлов: ожидалось {config.TotalFiles}, распределено {totalAllocated}.");

        string order = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ+-";
        var sortedLetters = letters
            .OrderBy(l => order.IndexOf(l.Value))
            .ToList();

        // Рассчитывает и присваивает позиции (StartBox, EndBox и т.д.) для каждой буквы на основе количества файлов.
        CalculateAndAssignPositions(sortedLetters, countFileLetter, config.BoxCapacity);
    }

    /// <summary> Рассчитывает количество файлов для каждой буквы алфавита и '+' и '-' на основе распределения.</summary>
    private static Dictionary<char, int> CalculateFileCountsPerLetter(ArchiveConfiguration config, Dictionary<char, double> distribution)
    {
        int boxCapacity = config.BoxCapacity;
        int totalreservedFiles = boxCapacity * (int)Math.Round(config.BoxCount / 100.0 * (double)config.PercentReservedFiles);
        int toteldeletedFiles = boxCapacity * (int)Math.Round(config.BoxCount / 100.0 * (double)config.PercentDeletedFiles);
        int totalAlphabetFiles = config.TotalFiles - totalreservedFiles - toteldeletedFiles;

        char maxLetterKey = distribution.MaxBy(kvp => kvp.Value).Key;
        var filteredDistribution = distribution.Where(kvp => kvp.Key != maxLetterKey);

        int actualFiles = 0;
        Dictionary<char, int> countFileLetter = new Dictionary<char, int>();

        foreach (var (letter, value) in filteredDistribution)
        {
            int count = (int)Math.Round(totalAlphabetFiles / 100.0 * value);
            actualFiles += count;
            countFileLetter.Add(letter, count);
        }

        int remainingForMax = totalAlphabetFiles - actualFiles;
        countFileLetter.Add(maxLetterKey, remainingForMax > 0 ? remainingForMax : 0);
        countFileLetter.Add('+', totalreservedFiles);
        countFileLetter.Add('-', toteldeletedFiles);

        return countFileLetter;
    }

    /// <summary> Рассчитывает и присваивает позиции (StartBox, EndBox и т.д.) для каждой буквы на основе количества файлов. </summary>
    private static void CalculateAndAssignPositions(List<Letter> sortedLetters, Dictionary<char, int> countFileLetter, int boxCapacity)
    {
        int currentPosition = 0;

        foreach (var letter in sortedLetters)
        {
            int letterFileCount = countFileLetter[letter.Value];

            if (letterFileCount > 0)
            {
                int startGlobalPosition = currentPosition;
                int startBox = (startGlobalPosition / boxCapacity) + 1;
                int startPosition = (startGlobalPosition % boxCapacity) + 1;

                currentPosition += letterFileCount;
                int endGlobalPosition = currentPosition - 1;
                int endBox = (endGlobalPosition / boxCapacity) + 1;
                int endPosition = (endGlobalPosition % boxCapacity) + 1;

                letter.ExpectedCount = letterFileCount;
                letter.ActualCount = 0;
                letter.StartBox = startBox;
                letter.EndBox = endBox;
                letter.StartPosition = startPosition;
                letter.EndPosition = endPosition;
            }
            else
            {
                letter.ExpectedCount = 0;
                letter.ActualCount = 0;
                letter.StartBox = null;
                letter.EndBox = null;
                letter.StartPosition = null;
                letter.EndPosition = null;
            }
        }
    }

    /// <summary> Изменяет позицию дела в архиве при перераспределении </summary>
    private void ChangePositionFileArchive(
        FileArchive fileArchive,
        List<Letter> letters,
        List<Box> boxes,
        int boxCapacity,
        List<ArchiveHistory> archiveHistories,
        Dictionary<Guid, (Guid? BoxId, int? BoxNumber)>? oldBoxData = null)
    {
        // 1. Получение буквы по первой букве фамилии
        Letter? letter = null;
        if (fileArchive.IsDeleted)
            letter = letters.FirstOrDefault(l => l.Value == '-');
        else
            letter = letters.FirstOrDefault(l => l.Value == fileArchive.FirstLetterSurname);

        if (letter == null)
            throw new Exception("Не получилось найти нужную букву!");

        if (letter.IsOverflow() && letter.Value != '-')
            letter = letters.FirstOrDefault(l => l.Value == '+');

        if (letter == null)
            throw new Exception("Первая буква фамилии переполнена, а символ '+' не удалось найти!");

        if (!letter.StartBox.HasValue || !letter.StartPosition.HasValue)
            throw new InvalidOperationException($"Буква '{letter.Value}' не инициализирована (отсутствуют StartBox или StartPosition)");

        if (letter.ExpectedCount <= 0)
            throw new InvalidOperationException($"Буква '{letter.Value}' не может содержать дел!");

        // Вычисление позиции и коробки
        int totalPosition = letter.StartPosition.Value - 1 + letter.ActualCount;
        int boxNumber = letter.StartBox.Value + totalPosition / boxCapacity;
        int position = (totalPosition % boxCapacity) + 1;
        
        if (position == 0) position = boxCapacity;


        Box? actualBox = null;
        // Если переполнены буквы '+' и '-'
        if (letter.IsOverflow())
        {
            actualBox = boxes.FirstOrDefault(b => b.Number == 99999);
            if (actualBox == null)
                throw new BoxFullException(99999);
            boxNumber = actualBox.Number;
            position = actualBox.ActualCount + 1;
        }
        else
        {
            actualBox = boxes.FirstOrDefault(b => b.Number == boxNumber);
            if (actualBox == null)
                throw new BoxFullException(boxNumber);
        }

        // Получаем старые данные для истории
        Guid? oldBoxId = fileArchive.BoxId;
        int? oldBoxNumber = fileArchive.Box?.Number;

        if (oldBoxData != null && oldBoxData.TryGetValue(fileArchive.Id, out var oldData))
        {
            oldBoxId = oldData.BoxId;
            oldBoxNumber = oldData.BoxNumber;
        }

        // Создание записи об изменении
        archiveHistories.Add(new ArchiveHistory
        {
            Action = HistoryAction.Redistribute,
            FileArchiveId = fileArchive.Id,
            Reason = "Перемещение дела по перераспределению архива",

            OldBoxNumber = oldBoxNumber,
            OldPosition = fileArchive.PositionInBox,
            OldLetterId = fileArchive.LetterId,
            OldBoxId = oldBoxId,

            NewBoxNumber = actualBox.Number,
            NewPosition = position,
            NewLetterId = letter.Id,
            NewBoxId = actualBox.Id,
        });

        // Обновление дела в архиве
        fileArchive.PositionInBox = position;
        fileArchive.BoxId = actualBox.Id;
        fileArchive.LetterId = letter.Id;
        fileArchive.FileNumberForLetter = letter.ActualCount + 1;

        letter.ActualCount++;
        actualBox.ActualCount++;
    }

}