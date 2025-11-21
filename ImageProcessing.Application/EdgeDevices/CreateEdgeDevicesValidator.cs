using FluentValidation;

namespace ImageProcessing.Application.EdgeDevices;

public sealed class CreateEdgeDeviceValidator : AbstractValidator<CreateEdgeDeviceRequest>
{
    public CreateEdgeDeviceValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.LocalIp).NotEmpty();
    }
}
