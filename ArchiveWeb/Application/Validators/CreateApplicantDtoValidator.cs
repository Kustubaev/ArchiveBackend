using ArchiveWeb.Application.DTOs.Applicant;
using FluentValidation;

namespace ArchiveWeb.Application.Validators;

public sealed class CreateApplicantDtoValidator : AbstractValidator<CreateApplicantDto>
{
    public CreateApplicantDtoValidator()
    {
        RuleFor(x => x.Surname)
            .NotEmpty()
            .WithMessage("Фамилия обязательно для заполнения")
            .MaximumLength(100)
            .WithMessage("ФИО не должно превышать 100 символов")
            .Matches(@"^[А-ЯЁ][а-яё]+(\s+[А-ЯЁ][а-яё]+)*$")
            .WithMessage("Фамилия должна содержать только русские буквы, каждое слово должно начинаться с заглавной буквы");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Имя обязательно для заполнения")
            .MaximumLength(100)
            .WithMessage("Имя не должно превышать 100 символов")
            .Matches(@"^[А-ЯЁ][а-яё]+(\s+[А-ЯЁ][а-яё]+)*$")
            .WithMessage("Имя должно содержать только русские буквы, каждое слово должно начинаться с заглавной буквы");

        RuleFor(x => x.Patronymic)
            .MaximumLength(100)
            .WithMessage("Отчество не должно превышать 100 символов")
            .Matches(@"^[А-ЯЁ][а-яё]+(\s+[А-ЯЁ][а-яё]+)*$")
            .WithMessage("Отчество должно содержать только русские буквы, каждое слово должно начинаться с заглавной буквы");

        RuleFor(x => x.EducationLevel)
            .IsInEnum()
            .WithMessage("Некорректный уровень образования");

        RuleFor(x => x.StudyForm)
            .IsInEnum()
            .WithMessage("Некорректная форма обучения");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Номер телефона обязателен")
            .MaximumLength(50)
            .WithMessage("Номер телефона не должен превышать 50 символов")
            .Matches(@"^[\+]?[0-9\s\-\(\)]+$")
            .WithMessage("Некорректный формат номера телефона");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email обязателен")
            .EmailAddress()
            .WithMessage("Некорректный формат email")
            .MaximumLength(255)
            .WithMessage("Email не должен превышать 255 символов");
    }
}

