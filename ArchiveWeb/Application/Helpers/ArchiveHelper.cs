using ArchiveWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchiveWeb.Application.Helpers;

public static class ArchiveHelper
{
    /// <summary> Получает массив символов после чего рассчитывает для них позиции и количество дел. Сбрасывает ActualCount. </summary>
    public static void RecalculatedLetters(
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

}