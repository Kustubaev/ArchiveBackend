using ArchiveWeb.Domain.Entities;

namespace ArchiveWeb.Application.Helpers;

/// <summary> Вспомогательный класс для создания идеального распределения букв </summary>
public static class DistributionHelper
{
    /// <summary> Создает примерное распределение букв на основе статистики русских фамилий </summary>
    public static Dictionary<char, double> CreateDefaultDistribution()
    {
        return new Dictionary<char, double>
        {
            { 'А', 3.1 }, { 'Б', 5.6 }, { 'В', 5.2 }, { 'Г', 4.2 }, { 'Д', 4.7 },
            { 'Е', 2.8 }, { 'Ё', 0.2 }, { 'Ж', 1.5 }, { 'З', 2.8 }, { 'И', 4.8 },
            { 'Й', 0.1 }, { 'К', 10.8 }, { 'Л', 4.0 }, { 'М', 6.0 }, { 'Н', 4.9 },
            { 'О', 2.3 }, { 'П', 7.4 }, { 'Р', 3.1 }, { 'С', 9.1 }, { 'Т', 4.8 },
            { 'У', 2.1 }, { 'Ф', 2.0 }, { 'Х', 1.3 }, { 'Ц', 0.8 }, { 'Ч', 1.2 },
            { 'Ш', 1.1 }, { 'Щ', 0.7 }, { 'Ъ', 0.0 }, { 'Ы', 0.0 }, { 'Ь', 0.0 },
            { 'Э', 0.5 }, { 'Ю', 0.4 }, { 'Я', 2.5 }
        };
    }

    /// <summary> Получение сбалансированного распределения на основе двух и двух распредделений</summary>
    public static Dictionary<char, double> GetIdealDistribution(
      ArchiveConfiguration config,
      List<(char Letter, int Value)> fileDistribution)
    {
        Dictionary<char, double> oldDistribution = CreateDefaultDistribution(); // Создание базового (идеального) распределения
        Dictionary<char, double> newDistribution = CalculateActualDistribution(fileDistribution); // Вычисление реального распределения
        Dictionary<char, double> idealDistribution = CalculateIdealDistribution(config, oldDistribution, newDistribution); // Новое сбалансированная распределение

        return idealDistribution;
    }

    /// <summary> Вычисление распределения на основе данных </summary>
    private static Dictionary<char, double> CalculateActualDistribution(List<(char Letter, int Count)> fileDistribution)
    {
        int totalFiles = fileDistribution.Sum(f => f.Count);

        if (fileDistribution.Count == 0 || totalFiles == 0)
            return new Dictionary<char, double>();

        return fileDistribution
            .ToDictionary(
                item => item.Letter,
                item => (item.Count / (double)totalFiles) * 100.0
            );
    }

    /// <summary> На основе двух распределений при помощи весов создаёт сбалансированное распределение</summary>
    public static Dictionary<char, double> CalculateIdealDistribution(
      ArchiveConfiguration config,
      Dictionary<char, double> oldDistribution,
      Dictionary<char, double> newDistribution)
    {
        var idealDistribution = new Dictionary<char, double>();
        string allLetters = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

        foreach (var letter in allLetters)
        {
            double oldPercentage = oldDistribution.GetValueOrDefault(letter, 0.0);
            double newPercentage = newDistribution.GetValueOrDefault(letter, 0.0);

            idealDistribution[letter] = (oldPercentage * config.AdaptiveWeightOld) + (newPercentage * config.AdaptiveWeightNew);
        }

        return NormalizeDistribution(idealDistribution);
    }

    /// <summary> Нормализует распределение, чтобы сумма была равна 100% </summary>
    public static Dictionary<char, double> NormalizeDistribution(Dictionary<char, double> distribution)
    {
        var total = distribution.Values.Sum();
        if (total <= 0)
            throw new ArgumentException("Сумма распределения не может быть меньше либо равна нулю", nameof(distribution));

        var normalized = new Dictionary<char, double>();
        foreach (var kvp in distribution)
            normalized[kvp.Key] = (kvp.Value / total) * 100.0;

        return normalized;
    }

    
}

