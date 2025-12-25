using ArchiveWeb.Application.DTOs.Archive;
using FluentValidation;

namespace ArchiveWeb.Application.Validators;

public sealed class InitializeArchiveDtoValidator : AbstractValidator<InitializeArchiveDto>
{
    public InitializeArchiveDtoValidator()
    {
        RuleFor(x => x.BoxCount)
            .InclusiveBetween(1, 1000)
            .WithMessage("Общее количество коробок должно быть от 1 до 1000");

        RuleFor(x => x.BoxCapacity)
            .InclusiveBetween(10, 200)
            .WithMessage("Вместимость коробки должна быть от 10 до 200");
    }
}

