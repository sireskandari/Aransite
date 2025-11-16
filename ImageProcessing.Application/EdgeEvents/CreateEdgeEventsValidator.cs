using FluentValidation;

namespace ImageProcessing.Application.EdgeEvents;

public sealed class CreateEdgeEventsValidator : AbstractValidator<CreateEdgeEventsRequest>
{
    public CreateEdgeEventsValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty().MaximumLength(200);
    }
}
