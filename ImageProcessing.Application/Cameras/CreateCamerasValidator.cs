using FluentValidation;

namespace ImageProcessing.Application.Cameras;

public sealed class CreateCameraValidator : AbstractValidator<CreateCameraRequest>
{
    public CreateCameraValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(256);
        RuleFor(x => x.RTSP).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IsActive).NotEmpty();
    }
}
