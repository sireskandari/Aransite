using FluentValidation;

namespace ImageProcessing.Application.DetectTargets;

public sealed class CreateDetectTargetValidator : AbstractValidator<CreateDetectTargetRequest>
{
    public CreateDetectTargetValidator()
    {
        RuleFor(x => x.CameraKey).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Targets).NotEmpty().MaximumLength(500);
    }
}
