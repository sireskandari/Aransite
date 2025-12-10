using FluentValidation;
using ImageProcessing.Application.Timelapses;

namespace ImageProcessing.Application.Timelapse;

public sealed class CreateTimelapseValidator : AbstractValidator<CreateTimelapseRequest>
{
    public CreateTimelapseValidator()
    {
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileFormat).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileSize).NotEmpty().MaximumLength(500);
    }
}
