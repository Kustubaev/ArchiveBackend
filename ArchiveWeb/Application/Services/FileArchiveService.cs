using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Application.Models;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Exceptions;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Application.Services;

public sealed class FileArchiveService : IFileArchiveService
{
    private readonly ArchiveDbContext _context;
    private readonly ILogger<FileArchiveService> _logger;

    public FileArchiveService(
        ArchiveDbContext context,
        ILogger<FileArchiveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FileArchiveDto> CreateFileArchiveAsync(
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        DbUpdateConcurrencyException? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Получение абитуриента
                var applicant = await _context.Applicants
                    .FirstOrDefaultAsync(a => a.Id == applicantId, cancellationToken);
                if (applicant == null)
                    throw new InvalidOperationException($"Абитуриент с ID {applicantId} не найден");
                if (string.IsNullOrWhiteSpace(applicant.Surname))
                    throw new InvalidSurnameException("У абитуриента не заполнено поле фамилии");

                // Извлечение первой буквы фамилии (кириллица)
                var firstLetter = char.ToUpper(applicant.Surname.TrimStart()[0]);
                if (!IsCyrillicLetter(firstLetter))
                    throw new InvalidSurnameException($"Фамилия должна начинаться с кириллической буквы: {applicant.Surname}");

                // Поиск буквы
                var letter = await GetLetterByValueAsync(firstLetter, cancellationToken);

                // Проверка переполнения
                if (letter.IsOverflow())
                    letter = await GetOverflowLetterAsync(cancellationToken);

                // Получение последней конфигурации
                var config = await _context.ArchiveConfigurations
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (config == null)
                    throw new InvalidOperationException("Конфигурация архива не найдена. Необходимо инициализировать архив.");

                // Вычисляем позиции дела
                var position = CalculatePosition(firstLetter, letter, config.BoxCapacity, cancellationToken);

                // Получение коробки
                var box = await _context.Boxes
                    .FirstOrDefaultAsync(b => b.Number == position.BoxNumber, cancellationToken);
                if (box == null || !box.HasAvailableSpace || box.ActualCount >= config.BoxCapacity)
                    throw new BoxFullException(position.BoxNumber);

                string fileNumberForArchive = FileNumberHelper.CalculateFileNumberForArchive(
                    applicant.EducationLevel,
                    applicant.IsOriginalSubmitted,
                    letter.Value,
                    letter.UsedCount+1);

                // Создание FileArchive
                FileArchive fileArchive = new FileArchive
                {
                    FileNumberForArchive = fileNumberForArchive,
                    FullName = applicant.GetFullName,
                    FirstLetterSurname = char.ToUpper(applicant.Surname[0]),
                    FileNumberForLetter = letter.ActualCount+1,
                    PositionInBox = position.PositionInBox,
                    IsDeleted = false,
                    ApplicantId = applicantId,
                    BoxId = box.Id,
                    LetterId = letter.Id,
                };
                _context.FileArchives.Add(fileArchive);

                // Обновление счетчиков
                letter.UsedCount++;
                letter.ActualCount++;
                box.ActualCount++;

                // Запись в историю
                ArchiveHistory history = new ArchiveHistory
                {
                    Action = HistoryAction.Create,
                    FileArchiveId = fileArchive.Id,
                    Reason = "Создание нового дела",

                    OldBoxNumber = null,
                    OldPosition = null,
                    OldLetterId = null,
                    OldBoxId = null,

                    NewBoxNumber = box.Number,
                    NewPosition = position.PositionInBox,
                    NewLetterId = letter.Id,
                    NewBoxId = box.Id,
                };
                _context.ArchiveHistories.Add(history);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Создано дело в архиве: FileArchiveId={FileArchiveId}, ApplicantId={ApplicantId}, Box={BoxNumber}, Position={Position}",
                    fileArchive.Id,
                    applicantId,
                    position.BoxNumber,
                    position.PositionInBox);

                // Загружаем полные данные для DTO
                return new FileArchiveDto
                {
                    Id = fileArchive.Id,
                    ApplicantId = fileArchive.ApplicantId,
                    FileNumberForArchive = fileArchive.FileNumberForArchive,
                    FullName = fileArchive.FullName,
                    FirstLetterSurname = fileArchive.FirstLetterSurname,
                    Letter = letter.Value,
                    FileNumberForLetter = fileArchive.FileNumberForLetter,
                    BoxNumber = box.Number,
                    PositionInBox = fileArchive.PositionInBox,
                    IsDeleted = fileArchive.IsDeleted,
                    CreatedAt = fileArchive.CreatedAt,
                    UpdatedAt = fileArchive.UpdatedAt,
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                lastException = ex;

                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Не удалось создать дело после {MaxRetries} попыток", maxRetries + 1);
                    throw;
                }

                _logger.LogWarning(ex, "Конфликт параллельного доступа. Попытка {Attempt} из {MaxRetries}...", attempt + 1, maxRetries);
                await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        // Этот код недостижим, но нужен для компиляции
        throw new InvalidOperationException("Unexpected state", lastException);
    }

    //public async Task<FileArchiveDto> UpdateFileArchiveAsync(
    //    FileArchive fileArchive,
    //    Letter letter,
    //    CancellationToken cancellationToken = default)
    //{
    //    const int maxRetries = 3;
    //    DbUpdateConcurrencyException? lastException = null;

    //    for (int attempt = 0; attempt < maxRetries; attempt++)
    //    {
    //        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    //        try
    //        {
    //            // Загружаем связанные сущности, если они не загружены
    //            Applicant? applicant = await _context.Applicants
    //                .FirstOrDefaultAsync(a => a.Id == fileArchive.ApplicantId, cancellationToken);
    //            if (applicant == null) throw new InvalidOperationException($"Абитуриент с ID {fileArchive.ApplicantId} не найден");
    //            if (string.IsNullOrWhiteSpace(applicant.Surname))  throw new InvalidSurnameException("У абитуриента не заполнено поле фамилии");

    //            // Получаем новую первую букву абитуринта
    //            char newFirstLetter = char.ToUpper(applicant.Surname.TrimStart()[0]);
    //            if (!IsCyrillicLetter(newFirstLetter))
    //                throw new InvalidSurnameException($"Фамилия должна начинаться с кириллической буквы: {applicant.Surname}");

    //            // Получаем актуальную букву
    //            Letter? currentLetter = letter;
    //            if (fileArchive.FirstLetterSurname != newFirstLetter)
    //                currentLetter = await _context.Letters.FirstOrDefaultAsync(l => l.Value == newFirstLetter, cancellationToken) ;         
    //            if (currentLetter == null)
    //                throw new LetterNotFoundException(letter.Value);

    //            // Проверяем, что переданная Letter существует в БД
    //            var existingLetter = await _context.Letters
    //                .FirstOrDefaultAsync(l => l.Id == letter.Id, cancellationToken);
    //            if (existingLetter == null)
    //                throw new LetterNotFoundException(letter.Value);

                
    //            // Загружаем текущую Letter, связанную с FileArchive, для возможного уменьшения счетчиков
    //            var currentLetter = await _context.Letters
    //                .FirstOrDefaultAsync(l => l.Id == fileArchive.LetterId, cancellationToken);
    //            if (currentLetter == null)
    //                throw new InvalidOperationException($"Текущая буква с ID {fileArchive.LetterId} для дела {fileArchive.Id} не найдена");

    //            // --- Логика обновления ---
    //            // 1. Обновляем LetterId
    //            var oldLetterId = fileArchive.LetterId; // Для истории
    //            fileArchive.LetterId = letter.Id;

    //            // 2. Обновляем FirstLetterSurname (предполагаем, что она соответствует новой Letter)
    //            fileArchive.FirstLetterSurname = letter.Value;

    //            // 3. Обработка FileNumberForArchive
    //            bool generatedNewNumber = false;
    //            if (string.IsNullOrEmpty(fileArchive.FileNumberForArchive) || fileArchive.FirstLetterSurname != firstLetter)
    //            {
    //                // Генерируем новый номер
    //                fileArchive.FileNumberForArchive = FileNumberHelper.CalculateFileNumberForArchive(
    //                    applicant.EducationLevel,
    //                    applicant.IsOriginalSubmitted,
    //                    letter.Value,
    //                    letter.UsedCount + 1);
    //                generatedNewNumber = true;
    //            }

    //            // 4. Обновляем счетчики
    //            if (generatedNewNumber) 
    //            {
    //                letter.UsedCount++;
    //                _context.Letters.Update(letter);
    //            }

    //            fileArchive.UpdatedAt = DateTime.UtcNow; 

    //            // 6. Сохраняем изменения
    //            _context.FileArchives.Update(fileArchive);

    //            // 7. Запись в историю
    //            ArchiveHistory history = new ArchiveHistory
    //            {
    //                Action = HistoryAction.Update, // Предполагаем, что это подходящее действие
    //                OldBoxNumber = null, // Не изменяется в этом методе
    //                OldPosition = null,  // Не изменяется в этом методе
    //                NewBoxNumber = null, // Не изменяется в этом методе
    //                NewPosition = null,  // Не изменяется в этом методе
    //                Reason = "Обновление буквы и, при необходимости, шифра дела",
    //                FileArchiveId = fileArchive.Id,
    //                LetterId = fileArchive.LetterId, // Новая LetterId
    //                OldLetterId = oldLetterId, // Добавляем старую LetterId в историю
    //                OldBoxId = null, // Не изменяется в этом методе
    //                NewBoxId = null, // Не изменяется в этом методе
    //                OldUsedCount = generatedNewNumber ? letter.UsedCount - 1 : letter.UsedCount, // Показываем, что было до увеличения
    //                NewUsedCount = letter.UsedCount, // Показываем новое значение
    //            };
    //            _context.ArchiveHistories.Add(history);

    //            await _context.SaveChangesAsync(cancellationToken);
    //            await transaction.CommitAsync(cancellationToken);

    //            _logger.LogInformation(
    //                "Обновлено дело в архиве: FileArchiveId={FileArchiveId}, ApplicantId={ApplicantId}, NewLetterId={NewLetterId}, GeneratedNewNumber={GeneratedNewNumber}",
    //                fileArchive.Id,
    //                fileArchive.ApplicantId,
    //                fileArchive.LetterId,
    //                generatedNewNumber);

    //            // Возвращаем DTO, загрузив связанные данные для полноты
    //            // Повторно загружаем для получения актуальных связанных данных, если они не отслеживаются
    //            var updatedFileArchive = await _context.FileArchives
    //                .Include(fa => fa.Box) // Предполагаем, что BoxId не меняется в этом методе
    //                .Include(fa => fa.Letter)
    //                .Include(fa => fa.Applicant) // Уже загружен, но включим для DTO
    //                .FirstOrDefaultAsync(fa => fa.Id == fileArchive.Id, cancellationToken);

    //            if (updatedFileArchive == null)
    //                throw new InvalidOperationException($"Обновленное дело с ID {fileArchive.Id} не найдено после сохранения.");

    //            return new FileArchiveDto
    //            {
    //                Id = updatedFileArchive.Id,
    //                ApplicantId = updatedFileArchive.ApplicantId,
    //                FileNumberForArchive = updatedFileArchive.FileNumberForArchive,
    //                FullName = updatedFileArchive.FullName,
    //                FirstLetterSurname = updatedFileArchive.FirstLetterSurname,
    //                Letter = updatedFileArchive.Letter.Value,
    //                FileNumberForLetter = updatedFileArchive.FileNumberForLetter, // Остается неизменным, если не пересчитывается
    //                BoxNumber = updatedFileArchive.Box.Number, // Текущий Box, не изменяется этим методом
    //                PositionInBox = updatedFileArchive.PositionInBox, // Текущая позиция, не изменяется этим методом
    //                IsDeleted = updatedFileArchive.IsDeleted,
    //                CreatedAt = updatedFileArchive.CreatedAt,
    //                UpdatedAt = updatedFileArchive.UpdatedAt,
    //            };
    //        }
    //        catch (DbUpdateConcurrencyException ex)
    //        {
    //            await transaction.RollbackAsync(cancellationToken);
    //            lastException = ex;

    //            if (attempt == maxRetries - 1) // maxRetries попыток, индексация с 0
    //            {
    //                _logger.LogError(ex, "Не удалось обновить дело {FileArchiveId} после {MaxRetries} попыток", fileArchive.Id, maxRetries);
    //                throw; // Бросаем оригинальное исключение
    //            }

    //            _logger.LogWarning(ex, "Конфликт параллельного доступа при обновлении {FileArchiveId}. Попытка {Attempt} из {MaxRetries}...", fileArchive.Id, attempt + 1, maxRetries);
    //            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
    //        }
    //        catch (Exception ex) // Ловим другие исключения, откатываем транзакцию
    //        {
    //            await transaction.RollbackAsync(cancellationToken);
    //            _logger.LogError(ex, "Ошибка при обновлении дела {FileArchiveId}", fileArchive.Id);
    //            throw; // Бросаем дальше
    //        }
    //    }

    //    // Этот код недостижим, если maxRetries > 0, но компилятор может ругаться
    //    // Бросаем исключение, если цикл завершился без возврата (теоретически)
    //    throw new InvalidOperationException($"Не удалось обновить дело {fileArchive.Id} после {maxRetries} попыток.", lastException);
    //}

    public FilePosition CalculatePosition(
        char firstLetter,
        Letter letter,
        int boxCapacity,
        CancellationToken cancellationToken = default)
    {

        if (letter == null)
            throw new LetterNotFoundException(firstLetter);

        if (!letter.StartBox.HasValue || !letter.StartPosition.HasValue)
            throw new InvalidOperationException($"Буква '{letter.Value}' не инициализирована (отсутствуют StartBox или StartPosition)");

        if (letter.ExpectedCount <= 0)
            throw new InvalidOperationException($"Буква '{letter.Value}' не может содержать дел!");

        // Расчет позиции
        int totalPosition = letter.StartPosition.Value - 1 + letter.ActualCount;
        int boxNumber = letter.StartBox.Value + totalPosition / boxCapacity;
        int position = (totalPosition % boxCapacity) + 1;

        if (position == 0) position = boxCapacity;

        return new FilePosition
        {
            BoxNumber = boxNumber,
            PositionInBox = position
        };
    }

    public async Task<Letter> GetLetterByValueAsync(char letterValue, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letterValue);
        var letter = await _context.Letters
            .FirstOrDefaultAsync(l => l.Value == normalizeLetter, cancellationToken);

        if (letter == null)
            throw new LetterNotFoundException(normalizeLetter);

        return letter;
    }

    public async Task<Letter> GetOverflowLetterAsync(CancellationToken cancellationToken = default)
    {
        var overflowLetter = await _context.Letters
            .FirstOrDefaultAsync(l => l.Value == '+', cancellationToken);

        if (overflowLetter == null)
            throw new InvalidOperationException("Буква переполнения '+' не найдена. Необходимо инициализировать архив.");

        return overflowLetter;
    }

    private static bool IsCyrillicLetter(char c)
    {
        return (c >= 'А' && c <= 'Я') || c == 'Ё';
    }

}

