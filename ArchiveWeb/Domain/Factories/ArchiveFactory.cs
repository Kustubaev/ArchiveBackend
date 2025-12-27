// Domain/Factories/ArchiveFactory.cs
using ArchiveWeb.Domain.Entities;

public static class ArchiveFactory
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

    public static List<Box> CreateBoxes(int count, int capacity)
    {
        List<Box> boxes = new List<Box>();
        for (int i = 1; i <= count; i++)
        {
            boxes.Add(new Box
            {
                Number = i,
                ExpectedCount = capacity,
                ActualCount = 0
            });
        }
        // Коробка на случай если и коробки для "+" переполнятся
        boxes.Add(new Box
        {
            Number = 99999,
            ExpectedCount = 99999,
            ActualCount = 0
        });

        return boxes;
    }
}