namespace ArchiveWeb.Application.Helpers;

public static class Constants
{
    public static readonly IReadOnlyDictionary<char, double> IDEAL_DISTRIBUTION = new Dictionary<char, double>
    {
        { 'А', 3.1 }, { 'Б', 5.6 }, { 'В', 5.2 }, { 'Г', 4.2 }, { 'Д', 4.7 },
        { 'Е', 2.8 }, { 'Ё', 0.2 }, { 'Ж', 1.5 }, { 'З', 2.8 }, { 'И', 4.8 },
        { 'Й', 0.1 }, { 'К', 10.8 }, { 'Л', 4.0 }, { 'М', 6.0 }, { 'Н', 4.9 },
        { 'О', 2.3 }, { 'П', 7.4 }, { 'Р', 3.1 }, { 'С', 9.1 }, { 'Т', 4.8 },
        { 'У', 2.1 }, { 'Ф', 2.0 }, { 'Х', 1.3 }, { 'Ц', 0.8 }, { 'Ч', 1.2 },
        { 'Ш', 1.1 }, { 'Щ', 0.7 }, { 'Ъ', 0.0 }, { 'Ы', 0.0 }, { 'Ь', 0.0 },
        { 'Э', 0.5 }, { 'Ю', 0.4 }, { 'Я', 2.5 }
    }.AsReadOnly();
}