using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Application.Helpers;

/// <summary> Вспомогательный класс для вычисления номеров дел в архиве </summary>
public static class FileNumberHelper
{
    /// <summary>
    /// Вычисляет номер дела в архиве.
    /// Формат: [L][LL][NNN], где
    /// L   - код уровня образования с учетом оригинала (1–8)
    /// LL  - код первой буквы фамилии (01–33)
    /// NNN - порядковый номер дела для этой буквы (001–999)
    /// </summary>
    public static string CalculateFileNumberForArchive(
        EducationLevel educationLevel,
        bool isOriginalSubmitted,
        char letter,
        int usedCount)
    {
        int levelCode = GetEducationLevelCode(educationLevel, isOriginalSubmitted);
        string letterPart = GetLetterOrder(letter).ToString("D2");
        string numberPart = usedCount.ToString("D3");

        return $"{levelCode}{letterPart}{numberPart}";
    }

    private static int GetEducationLevelCode(EducationLevel educationLevel, bool isOriginalSubmitted) =>
        (educationLevel, isOriginalSubmitted) switch
        {
            (EducationLevel.SecondaryProfessional, true) => 1,
            (EducationLevel.SecondaryProfessional, false) => 2,
            (EducationLevel.BachelorOrSpecialist, true) => 3,
            (EducationLevel.BachelorOrSpecialist, false) => 4,
            (EducationLevel.Master, true) => 5,
            (EducationLevel.Master, false) => 6,
            (EducationLevel.Postgraduate, true) => 7,
            (EducationLevel.Postgraduate, false) => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(educationLevel), educationLevel, "Неизвестный уровень образования")
        };

    /// <summary>
    /// Возвращает порядковый номер буквы русского алфавита с Ё:
    /// А=1, Б=2, ..., Е=6, Ё=7, Ж=8, ..., Я=33.
    /// Для спец-букв '+' (переполнение - 88) и '-' (удалённые - 99).
    /// Иначе 0.
    /// </summary>
    private static int GetLetterOrder(char letter)
    {
        const string Alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        char normolizeLetter = char.ToUpper(letter);
        int index = Alphabet.IndexOf(normolizeLetter);

        if (index >= 0) return index + 1;
        if (normolizeLetter == '+') return 88;
        if (normolizeLetter == '-') return 99;
        return 0;
    }
}

