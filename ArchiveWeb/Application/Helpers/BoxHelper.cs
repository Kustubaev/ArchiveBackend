using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Application.Helpers;
public static class BoxHelper
{
    public static List<Box> CreateBoxes(int boxCound, int boxCapacity)
    {
        List<Box> boxes = new List<Box>();
        for (int i = 1; i <= boxCound; i++)
        {
            boxes.Add(new Box
            {
                Number = i,
                ExpectedCount = boxCapacity,
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