using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Application.Helpers;

public static class LetterHelper
{
    public static List<Letter> CreateAllLetters()
    {
        string allLetters = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ+-";
        List<Letter> letters = new List<Letter>();

        foreach (var letterValue in allLetters)
        {
            Letter letter = new Letter
            {
                Value = letterValue,
                ActualCount = 0,
                UsedCount = 0,
            };
            letters.Add(letter);
        }

        return letters;
    }
}