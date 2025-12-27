using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.DTOs.FileArchive;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Application.Models;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Exceptions;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace ArchiveWeb.Application.Services;

public sealed class FileArchiveService : IFileArchiveService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FileArchiveService> _logger;

    public FileArchiveService(
        IUnitOfWork uow,
        ILogger<FileArchiveService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<FileArchiveDto> CreateFileArchiveAsync(Guid applicantId, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        DbUpdateConcurrencyException? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using var transaction = await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                // Получение абитуриента
                var applicant = await _uow.Applicants.GetByIdAsync(applicantId, cancellationToken);
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
                var config = await _uow.ArchiveConfig.GetLastArchiveConfigurationAsync(cancellationToken);
                if (config == null)
                    throw new InvalidOperationException("Конфигурация архива не найдена. Необходимо инициализировать архив.");

                // Вычисляем позиции дела
                var position = await CalculatePositionAsync(firstLetter, letter, config.BoxCapacity, cancellationToken);

                // Получение коробки
                var box = await _uow.Boxes.GetByNumberAsync(position.BoxNumber, cancellationToken);
                if (box == null || !box.HasAvailableSpace)
                    throw new BoxFullException(position.BoxNumber);


                string fileNumberForArchive = FileNumberHelper.CalculateFileNumberForArchive(
                    applicant.EducationLevel,
                    applicant.IsOriginalSubmitted,
                    letter.Value,
                    letter.UsedCount + 1);

                // Создание FileArchive
                FileArchive fileArchive = new FileArchive
                {
                    FileNumberForArchive = fileNumberForArchive,
                    FullName = applicant.GetFullName,
                    FirstLetterSurname = char.ToUpper(applicant.Surname[0]),
                    FileNumberForLetter = letter.ActualCount + 1,
                    PositionInBox = position.PositionInBox,
                    IsDeleted = false,
                    ApplicantId = applicantId,
                    BoxId = box.Id,
                    LetterId = letter.Id,
                };
                await _uow.FileArchives.AddAsync(fileArchive, cancellationToken);

                // Обновление счетчиков
                letter.UsedCount++;
                letter.ActualCount++;
                box.ActualCount++;
                await _uow.Letters.UpdateAsync(letter, cancellationToken);

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
                await _uow.ArchiveHistories.AddAsync(history, cancellationToken);

                await _uow.SaveChangesAsync(cancellationToken);
                await _uow.CommitAsync(cancellationToken);

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
                await _uow.RollbackAsync(cancellationToken);
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
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }
        // Этот код недостижим, но нужен для компиляции
        throw new InvalidOperationException("Unexpected state", lastException);
    }

    public async Task<FileArchiveDto?> GetFileByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _uow.FileArchives.GetByIdWithIncludesAsync(id, cancellationToken);
        if (file == null)
            return null;

        return new FileArchiveDto
        {
            Id = file.Id,
            ApplicantId = file.ApplicantId,
            FileNumberForArchive = file.FileNumberForArchive,
            FullName = file.FullName,
            FirstLetterSurname = file.FirstLetterSurname,
            Letter = file.Letter.Value,
            FileNumberForLetter = file.FileNumberForLetter,
            BoxNumber = file.Box?.Number,
            PositionInBox = file.PositionInBox,
            IsDeleted = file.IsDeleted,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt,
        };
    }

    public async Task<PagedResponse<FileArchiveDto>> GetFilesAsync(char? letter, int? boxNumber, int page, int pageSize, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var files = await _uow.FileArchives.GetFilesWithFiltersAsync(letter, boxNumber, includeDeleted, page, pageSize, cancellationToken);
        var totalCount = await _uow.FileArchives.CountFilesWithFiltersAsync(letter, boxNumber, includeDeleted, cancellationToken);

        var fileDtos = files.Select(f => new FileArchiveDto
        {
            Id = f.Id,
            ApplicantId = f.ApplicantId,
            FileNumberForArchive = f.FileNumberForArchive,
            FullName = f.FullName,
            FirstLetterSurname = f.FirstLetterSurname,
            Letter = f.Letter.Value,
            FileNumberForLetter = f.FileNumberForLetter,
            BoxNumber = f.Box?.Number,
            PositionInBox = f.PositionInBox,
            IsDeleted = f.IsDeleted,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
        }).ToList();

        return new PagedResponse<FileArchiveDto>
        {
            Items = fileDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FileArchiveDto> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _uow.FileArchives.GetByIdWithIncludesAsync(id, cancellationToken);

        if (file == null)
            throw new InvalidOperationException($"Дело с ID {id} не найдено");

        if (file.IsDeleted)
            throw new InvalidOperationException($"Дело с ID {id} уже удалено.");

        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;

        // Запись в историю
        var history = new ArchiveHistory
        {
            Action = HistoryAction.Delete,
            FileArchiveId = file.Id,
            Reason = "Мягкое удаление дела",

            OldBoxNumber = file.Box?.Number,
            OldPosition = file.PositionInBox,
            OldLetterId = file.LetterId,
            OldBoxId = file.BoxId,

            NewBoxNumber = null,
            NewPosition = null,
            NewLetterId = null,
            NewBoxId = null,
        };

        await _uow.FileArchives.UpdateAsync(file, cancellationToken);
        await _uow.ArchiveHistories.AddAsync(history, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Дело помечено как удаленное: FileArchiveId={FileArchiveId}", file.Id);

        return new FileArchiveDto
        {
            Id = file.Id,
            ApplicantId = file.ApplicantId,
            FileNumberForArchive = file.FileNumberForArchive,
            FullName = file.FullName,
            FirstLetterSurname = file.FirstLetterSurname,
            Letter = file.Letter.Value,
            FileNumberForLetter = file.FileNumberForLetter,
            BoxNumber = file.Box.Number,
            PositionInBox = file.PositionInBox,
            IsDeleted = file.IsDeleted,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt,
        };
    }

    public async Task<List<FileArchiveDto>> DeleteRandomFilesAsync(int count, CancellationToken cancellation = default)
    {
        if (count < 1 || count > 1000) count = 10;

        List<FileArchive> randomFiles = await _uow.FileArchives.GetRandomActiveFilesAsync(count, cancellation);
        List<FileArchiveDto> removedFileDto = new List<FileArchiveDto>();

        foreach (var file in randomFiles)
        {
            removedFileDto.Add(await DeleteFileAsync(file.Id, cancellation));
        }

        return removedFileDto;
    }

    public async Task<PagedResponse<ApplicantDto>> GetPendingApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var applicants = await _uow.Applicants.GetPendingApplicantsAsync(page, pageSize, cancellationToken);
        var totalCount = await _uow.Applicants.CountPendingApplicantsAsync(cancellationToken);

        var applicantDtos = applicants.Select(a => new ApplicantDto
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
            FileArchiveId = null
        }).ToList();

        return new PagedResponse<ApplicantDto>
        {
            Items = applicantDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BulkAcceptFilesResultDto> AcceptAllFilesAsync(CancellationToken cancellationToken = default)
    {
        // Получаем всех абитуриентов без FileArchive, отсортированных по дате создания
        const int pageSize = 1000;
        var pendingApplicants = await _uow.Applicants.GetPendingApplicantsAsync(1, pageSize, cancellationToken);

        var acceptedFiles = new List<FileArchiveDto>();
        var rejectedFiles = new List<RejectedFileDto>();

        foreach (var applicant in pendingApplicants)
        {
            try
            {
                var fileArchive = await CreateFileArchiveAsync(
                    applicant.Id,
                    cancellationToken);

                acceptedFiles.Add(fileArchive);

                _logger.LogInformation(
                    "Дело успешно принято: ApplicantId={ApplicantId}, FileArchiveId={FileArchiveId}",
                    applicant.Id,
                    fileArchive.Id);
            }
            catch (ArchiveException ex)
            {
                // Получаем полное имя абитуриента
                var fullName = string.Join(' ',
                    new[] { applicant.Surname, applicant.FirstName, applicant.Patronymic ?? string.Empty }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                rejectedFiles.Add(new RejectedFileDto
                {
                    ApplicantId = applicant.Id,
                    FullName = fullName,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.ErrorCode
                });

                _logger.LogWarning(
                    "Не удалось принять дело: ApplicantId={ApplicantId}, Error={Error}",
                    applicant.Id,
                    ex.Message);
            }
            catch (Exception ex)
            {
                var fullName = string.Join(' ',
                    new[] { applicant.Surname, applicant.FirstName, applicant.Patronymic ?? string.Empty }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                rejectedFiles.Add(new RejectedFileDto
                {
                    ApplicantId = applicant.Id,
                    FullName = fullName,
                    ErrorMessage = ex.Message,
                    ErrorCode = null
                });

                _logger.LogError(
                    ex,
                    "Непредвиденная ошибка при принятии дела: ApplicantId={ApplicantId}",
                    applicant.Id);
            }
        }

        return new BulkAcceptFilesResultDto
        {
            AcceptedFiles = acceptedFiles,
            RejectedFiles = rejectedFiles,
            TotalProcessed = pendingApplicants.Count
        };
    }

    public async Task<PagedResponse<FileArchiveDto>> SearchBySurnameAsync(string surname, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var files = await _uow.FileArchives.SearchBySurnameAsync(surname, page, pageSize, cancellationToken);
        var totalCount = await _uow.FileArchives.CountSearchBySurnameAsync(surname, cancellationToken);

        var fileDtos = files.Select(f => new FileArchiveDto
        {
            Id = f.Id,
            ApplicantId = f.ApplicantId,
            FileNumberForArchive = f.FileNumberForArchive,
            FullName = f.FullName,
            FirstLetterSurname = f.FirstLetterSurname,
            Letter = f.Letter.Value,
            FileNumberForLetter = f.FileNumberForLetter,
            BoxNumber = f.Box?.Number,
            PositionInBox = f.PositionInBox,
            IsDeleted = f.IsDeleted,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
        }).ToList();

        return new PagedResponse<FileArchiveDto>
        {
            Items = fileDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FileArchiveDto?> SearchByApplicantIdAsync(Guid applicantId, CancellationToken cancellationToken = default)
    {
        var file = await _uow.FileArchives.GetByApplicantIdAsync(applicantId, cancellationToken);
        if (file == null)
            return null;

        return new FileArchiveDto
        {
            Id = file.Id,
            ApplicantId = file.ApplicantId,
            FileNumberForArchive = file.FileNumberForArchive,
            FullName = file.FullName,
            FirstLetterSurname = file.FirstLetterSurname,
            Letter = file.Letter.Value,
            FileNumberForLetter = file.FileNumberForLetter,
            BoxNumber = file.Box?.Number,
            PositionInBox = file.PositionInBox,
            IsDeleted = file.IsDeleted,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt,
        };
    }



    private async Task<Letter> GetOverflowLetterAsync(CancellationToken cancellationToken = default)
    {
        var overflowLetter = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == '+', cancellationToken);

        if (overflowLetter == null)
            throw new InvalidOperationException("Буква переполнения '+' не найдена. Необходимо инициализировать архив.");

        return overflowLetter;
    }

    private static bool IsCyrillicLetter(char c)
    {
        return (c >= 'А' && c <= 'Я') || c == 'Ё';
    }

    private async Task<FilePosition> CalculatePositionAsync(
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

        // Вычисление позиции и коробки
        int totalPosition = letter.StartPosition.Value - 1 + letter.ActualCount;
        int boxNumber = letter.StartBox.Value + totalPosition / boxCapacity;
        int position = (totalPosition % boxCapacity) + 1;

        if (position == 0) position = boxCapacity;

        if (letter.IsOverflow())
        {
            Box? actualBox = await _uow.Boxes.GetByNumberAsync(99999, cancellationToken);
            if (actualBox == null)
                throw new BoxFullException(99999);
            boxNumber = actualBox.Number;
            position = actualBox.ActualCount + 1;
        }

        return new FilePosition
        {
            BoxNumber = boxNumber,
            PositionInBox = position
        };
    }

    private async Task<Letter> GetLetterByValueAsync(char letterValue, CancellationToken cancellationToken = default)
    {
        char normalizeLetter = char.ToUpper(letterValue);
        var letter = await _uow.Letters.GetFirstByConditionAsync(l => l.Value == normalizeLetter, cancellationToken);

        if (letter == null)
            throw new LetterNotFoundException(normalizeLetter);

        return letter;
    }
}

