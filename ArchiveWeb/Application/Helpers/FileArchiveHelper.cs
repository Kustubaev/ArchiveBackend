using ArchiveWeb.Application.Models;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Exceptions;
using System.Threading;

namespace ArchiveWeb.Application.Helpers;

public static class FileArchiveHelper
{
    public static void ChangePositionFileArchive(
        FileArchive fileArchive, 
        List<Letter> letters, 
        List<Box> boxes, 
        int boxCapacity, 
        List<ArchiveHistory> archiveHistories,
        Dictionary<Guid, (Guid? BoxId, int? BoxNumber)>? oldBoxData = null)
    {
        // 1. Получение буквы по первой букве фамиллии
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


        // Нахождение нужной коробки
        Box? actualBox = boxes.FirstOrDefault(b => b.Number == boxNumber);
        if (actualBox == null)
            throw new BoxFullException(boxNumber);

        if (!actualBox.HasAvailableSpace || actualBox.ActualCount >= boxCapacity) // Подумать !!! над HasAvailableSpace
            actualBox = boxes.FirstOrDefault(b => b.Number == 99999);

        if (actualBox == null)
            throw new BoxFullException(boxNumber);

        int actualPosition = actualBox.Number != 99999 ? position : actualBox.ActualCount + 1;

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
            NewPosition = actualPosition,
            NewLetterId = letter.Id,
            NewBoxId = actualBox.Id,
        });

        // Обновление дела в архиве
        fileArchive.PositionInBox = actualPosition;
        fileArchive.BoxId = actualBox.Id; // actualBox.Id уже сохранен в БД после SaveChanges
        fileArchive.LetterId = letter.Id;
        fileArchive.FileNumberForLetter = letter.ActualCount + 1;

        letter.ActualCount++;
        actualBox.ActualCount++;
    }

}