using FluentValidation;
using Granit.IoT.Domain;
using Granit.IoT.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.IoT.Endpoints.Validators;

internal sealed class DeviceProvisionRequestValidator : GranitValidator<DeviceProvisionRequest>
{
    public DeviceProvisionRequestValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(DeviceSerialNumber.MaxLength);

        RuleFor(x => x.HardwareModel)
            .NotEmpty()
            .MaximumLength(HardwareModel.MaxLength);

        RuleFor(x => x.FirmwareVersion)
            .NotEmpty()
            .MaximumLength(FirmwareVersion.MaxLength);

        RuleFor(x => x.Label)
            .MaximumLength(256);
    }
}
