using ArchiveWeb.Application.DTOs;
using ArchiveWeb.Application.DTOs.Applicant;
using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Enums;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;

namespace ArchiveWeb.Application.Services;

/// <summary> Сервис для работы с абитуриентами </summary>
public sealed class ApplicantService : IApplicantService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ApplicantService> _logger;

    public ApplicantService(
        IUnitOfWork uow,
        ILogger<ApplicantService> logger)
    {
        _uow = uow;
        _logger = logger;
    }
    public async Task<PagedResponse<ApplicantDto>> GetApplicantsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var applicants = await _uow.Applicants.GetAllAsync(page, pageSize, cancellationToken);
        int totalCount = await _uow.Applicants.CountAsync(cancellationToken);

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
            FileArchiveId = a.FileArchive?.Id
        }).ToList();

        return new PagedResponse<ApplicantDto>
        {
            Items = applicantDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApplicantDto?> GetApplicantByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var applicant = await _uow.Applicants.GetByIdWithFileArchiveAsync(id, cancellationToken);
        if (applicant == null)
            return null;

        return new ApplicantDto
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
    }

    public async Task<ApplicantDto> CreateApplicantAsync(CreateApplicantDto dto, CancellationToken cancellationToken = default)
    {
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

        await _uow.Applicants.AddAsync(applicant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Создан новый абитуриент: {ApplicantId}, Email: {Email}",
            applicant.Id,
            applicant.Email);

        return new ApplicantDto
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
    }

    public async Task<ApplicantDto> UpdateApplicantAsync(Guid id, UpdateApplicantDto dto, CancellationToken cancellationToken = default)
    {
        var applicant = await _uow.Applicants.GetByIdWithFileArchiveAsync(id, cancellationToken);

        if (applicant == null)
            throw new InvalidOperationException($"Абитуриент с ID {id} не найден");

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

        await _uow.Applicants.UpdateAsync(applicant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Обновлен абитуриент: {ApplicantId}",
            applicant.Id);

        return new ApplicantDto
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
    }

    public async Task DeleteApplicantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var applicant = await _uow.Applicants.GetByIdWithFileArchiveAsync(id, cancellationToken);

        if (applicant == null)
            throw new InvalidOperationException($"Абитуриент с ID {id} не найден");

        if (applicant.FileArchive != null)
            throw new InvalidOperationException($"Невозможно удалить абитуриента: у него есть дело в архиве (ID: {applicant.FileArchive.Id})");

        await _uow.Applicants.DeleteAsync(applicant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Удален абитуриент: {ApplicantId}",
            applicant.Id);
    }

    public async Task<PagedResponse<ApplicantDto>> SearchApplicantsAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetApplicantsAsync(page, pageSize, cancellationToken);
        }

        var applicants = await _uow.Applicants.SearchAsync(query, page, pageSize, cancellationToken);
        int totalCount = await _uow.Applicants.CountSearchAsync(query, cancellationToken);

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
            FileArchiveId = a.FileArchive?.Id
        }).ToList();

        return new PagedResponse<ApplicantDto>
        {
            Items = applicantDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<ApplicantDto>> GenerateApplicantsAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 1)
            throw new ArgumentException("Количество должно быть больше 0", nameof(count));

        if (count > 1000)
            throw new ArgumentException("Максимальное количество для генерации: 1000", nameof(count));

        var generatedApplicants = new List<Applicant>();

        for (int i = 0; i < count; i++)
        {
            var applicant = GenerateApplicant();
            generatedApplicants.Add(applicant);
        }

        await _uow.Applicants.AddRangeAsync(generatedApplicants, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Сгенерировано абитуриентов: {Count}",
            count);

        return generatedApplicants.Select(a => new ApplicantDto
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
    }

    /// <summary> Генерирует случайного абитуриента </summary>
    private Applicant GenerateApplicant()
    {
        // Получаем распределение букв из DistributionHelper
        var distribution = DistributionHelper.CreateDefaultDistribution();

        // Выбираем букву на основе вероятностного распределения
        char selectedLetter = SelectLetterByDistribution(distribution);

        // Выбираем случайную фамилию для выбранной буквы
        var surnames = ApplicantHelper.SurnamesByLetter.GetValueOrDefault(selectedLetter, new List<string> { "Иванов" });
        string surname = surnames[Random.Shared.Next(surnames.Count)];

        // Выбираем случайное имя и отчество
        string firstName = ApplicantHelper.FirstNames[Random.Shared.Next(ApplicantHelper.FirstNames.Count)];
        string patronymic = ApplicantHelper.Patronymics[Random.Shared.Next(ApplicantHelper.Patronymics.Count)];

        // Генерируем 4 случайные цифры для телефона и email
        string randomDigits = Random.Shared.Next(1000, 10000).ToString();

        // Генерируем случайные значения для enum и bool
        var educationLevels = Enum.GetValues<EducationLevel>();
        var studyForms = Enum.GetValues<StudyForm>();

        return new Applicant
        {
            Surname = surname,
            FirstName = firstName,
            Patronymic = patronymic,
            EducationLevel = educationLevels[Random.Shared.Next(educationLevels.Length)],
            StudyForm = studyForms[Random.Shared.Next(studyForms.Length)],
            IsOriginalSubmitted = Random.Shared.Next(2) == 1,
            IsBudgetFinancing = Random.Shared.Next(2) == 1,
            PhoneNumber = $"895145{randomDigits}",
            Email = $"student{randomDigits}@yandex.ru"
        };
    }

    /// <summary> Выбирает букву на основе вероятностного распределения </summary>
    private static char SelectLetterByDistribution(Dictionary<char, double> distribution)
    {
        // Фильтруем буквы с нулевой вероятностью для более реалистичного распределения
        var nonZeroDistribution = distribution
            .Where(kvp => kvp.Value > 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (nonZeroDistribution.Count == 0)
            return 'А'; // Fallback на первую букву алфавита

        // Создаем взвешенный список букв
        var weightedLetters = new List<(char Letter, double Weight)>();
        double cumulativeWeight = 0;

        foreach (var kvp in nonZeroDistribution)
        {
            cumulativeWeight += kvp.Value;
            weightedLetters.Add((kvp.Key, cumulativeWeight));
        }

        // Генерируем случайное число от 0 до cumulativeWeight
        double randomValue = Random.Shared.NextDouble() * cumulativeWeight;

        // Находим букву, соответствующую случайному значению
        foreach (var (letter, weight) in weightedLetters)
        {
            if (randomValue <= weight)
                return letter;
        }

        // Fallback на последнюю букву из списка
        return weightedLetters.Last().Letter;
    }
}

