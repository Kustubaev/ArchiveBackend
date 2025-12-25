using ArchiveWeb.Application.Helpers;
using ArchiveWeb.Domain.Entities;
using ArchiveWeb.Domain.Enums;

namespace ArchiveWeb.Application.Helpers;

/// <summary> Вспомогательный класс для генерации тестовых абитуриентов </summary>
public static class ApplicantGeneratorHelper
{
    private static readonly Dictionary<char, List<string>> SurnamesByLetter = new()
    {
        { 'А', new List<string> { "Алексеев", "Андреев", "Антонов", "Артемьев", "Афанасьев" } },
        { 'Б', new List<string> { "Борисов", "Белов", "Богданов", "Баранов", "Беляев" } },
        { 'В', new List<string> { "Васильев", "Волков", "Воробьев", "Виноградов", "Власов" } },
        { 'Г', new List<string> { "Григорьев", "Горбачев", "Гусев", "Герасимов", "Голубев" } },
        { 'Д', new List<string> { "Дмитриев", "Данилов", "Денисов", "Дорофеев", "Дементьев" } },
        { 'Е', new List<string> { "Егоров", "Емельянов", "Ефимов", "Ермаков", "Евдокимов" } },
        { 'Ё', new List<string> { "Ёлкин", "Ёжиков", "Ёршов", "Ёлкин", "Ёжиков" } },
        { 'Ж', new List<string> { "Жуков", "Жданов", "Жигалов", "Журавлев", "Жилин" } },
        { 'З', new List<string> { "Захаров", "Зайцев", "Зуев", "Зимин", "Золотов" } },
        { 'И', new List<string> { "Иванов", "Ильин", "Исаев", "Игнатьев", "Иванов" } },
        { 'Й', new List<string> { "Йорданов", "Йошкин", "Йорданов", "Йошкин", "Йорданов" } },
        { 'К', new List<string> { "Кузнецов", "Козлов", "Комаров", "Королев", "Киселев" } },
        { 'Л', new List<string> { "Лебедев", "Лазарев", "Логинов", "Лукин", "Львов" } },
        { 'М', new List<string> { "Морозов", "Михайлов", "Максимов", "Медведев", "Марков" } },
        { 'Н', new List<string> { "Новиков", "Николаев", "Назаров", "Носов", "Нестеров" } },
        { 'О', new List<string> { "Орлов", "Осипов", "Овчинников", "Одинцов", "Орехов" } },
        { 'П', new List<string> { "Петров", "Павлов", "Попов", "Петухов", "Пономарев" } },
        { 'Р', new List<string> { "Романов", "Рыбаков", "Родионов", "Русаков", "Румянцев" } },
        { 'С', new List<string> { "Смирнов", "Соколов", "Степанов", "Семенов", "Сергеев" } },
        { 'Т', new List<string> { "Тихонов", "Тарасов", "Трофимов", "Терентьев", "Титов" } },
        { 'У', new List<string> { "Устинов", "Уваров", "Ушаков", "Уткин", "Ульянов" } },
        { 'Ф', new List<string> { "Федоров", "Филиппов", "Фомин", "Фролов", "Федотов" } },
        { 'Х', new List<string> { "Харитонов", "Хомяков", "Худяков", "Харитонов", "Хомяков" } },
        { 'Ц', new List<string> { "Цветков", "Цыганов", "Цветков", "Цыганов", "Цветков" } },
        { 'Ч', new List<string> { "Чернов", "Чернышев", "Черкасов", "Чернов", "Чернышев" } },
        { 'Ш', new List<string> { "Широков", "Шубин", "Шестаков", "Широков", "Шубин" } },
        { 'Щ', new List<string> { "Щербаков", "Щукин", "Щербаков", "Щукин", "Щербаков" } },
        { 'Ъ', new List<string> { "Редкий", "Петров", "Сидоров", "Козлов", "Смирнов" } }, // Редкие буквы - используем общие фамилии
        { 'Ы', new List<string> { "Иванов", "Петров", "Сидоров", "Козлов", "Смирнов" } },
        { 'Ь', new List<string> { "Иванов", "Петров", "Сидоров", "Козлов", "Смирнов" } },
        { 'Э', new List<string> { "Элькин", "Эрдман", "Элькин", "Эрдман", "Элькин" } },
        { 'Ю', new List<string> { "Юдин", "Юрьев", "Юдин", "Юрьев", "Юдин" } },
        { 'Я', new List<string> { "Яковлев", "Яшин", "Якушев", "Яковлев", "Яшин" } }
    };

    private static readonly List<string> FirstNames = new()
    {
        "Александр", "Алексей", "Андрей", "Антон", "Артем",
        "Борис", "Василий", "Виктор", "Владимир", "Дмитрий",
        "Евгений", "Иван", "Игорь", "Константин", "Максим",
        "Михаил", "Николай", "Олег", "Павел", "Сергей"
    };

    private static readonly List<string> Patronymics = new()
    {
        "Александрович", "Алексеевич", "Андреевич", "Антонович", "Артемович",
        "Борисович", "Васильевич", "Викторович", "Владимирович", "Дмитриевич",
        "Евгеньевич", "Иванович", "Игоревич", "Константинович", "Максимович",
        "Михайлович", "Николаевич", "Олегович", "Павлович", "Сергеевич"
    };

    /// <summary> Генерирует случайного абитуриента </summary>
    public static Applicant GenerateApplicant()
    {
        // Получаем распределение букв из DistributionHelper
        var distribution = DistributionHelper.CreateDefaultDistribution();
        
        // Выбираем букву на основе вероятностного распределения
        char selectedLetter = SelectLetterByDistribution(distribution);
        
        // Выбираем случайную фамилию для выбранной буквы
        var surnames = SurnamesByLetter.GetValueOrDefault(selectedLetter, new List<string> { "Иванов" });
        string surname = surnames[Random.Shared.Next(surnames.Count)];
        
        // Выбираем случайное имя и отчество
        string firstName = FirstNames[Random.Shared.Next(FirstNames.Count)];
        string patronymic = Patronymics[Random.Shared.Next(Patronymics.Count)];
        
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

