using ArchiveWeb.Application.DTOs.Archive;
using FluentValidation;

namespace ArchiveWeb.Application.Validators;

public sealed class UpdateArchiveConfigurationDtoValidator : AbstractValidator<UpdateArchiveConfigurationDto>
{
    public UpdateArchiveConfigurationDtoValidator()
    {
        RuleFor(x => x.BoxCount)
            .InclusiveBetween(1, 1000)
            .WithMessage("Общее количество коробок должно быть от 1 до 1000");

        RuleFor(x => x.BoxCapacity)
            .InclusiveBetween(10, 200)
            .WithMessage("Вместимость коробки должна быть от 10 до 200");

        RuleFor(x => x.AdaptiveRedistributionThreshold)
            .InclusiveBetween(50, 100)
            .When(x => x.AdaptiveRedistributionThreshold.HasValue)
            .WithMessage("Порог перераспределения должен быть от 50 до 100");

        RuleFor(x => x.AdaptiveWeightNew)
            .InclusiveBetween(0.0, 1.0)
            .When(x => x.AdaptiveWeightNew.HasValue)
            .WithMessage("Вес нового распределения должен быть от 0.0 до 1.0");

        RuleFor(x => x.AdaptiveWeightOld)
            .InclusiveBetween(0.0, 1.0)
            .When(x => x.AdaptiveWeightOld.HasValue)
            .WithMessage("Вес старого распределения должен быть от 0.0 до 1.0");
    }
}

