using FluentValidation.Results;
using Granit.IoT.Endpoints.Dtos;
using Granit.IoT.Endpoints.Validators;
using Shouldly;

namespace Granit.IoT.Endpoints.Tests.Validators;

public sealed class ValidatorsTests
{
    [Fact]
    public void DeviceProvisionRequest_ValidPayload_PassesValidation()
    {
        DeviceProvisionRequestValidator validator = new();
        DeviceProvisionRequest req = new()
        {
            SerialNumber = "SN-1",
            HardwareModel = "Model-1",
            FirmwareVersion = "1.0.0",
            Label = "ok",
        };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DeviceProvisionRequest_EmptyFields_FailsValidation()
    {
        DeviceProvisionRequestValidator validator = new();
        DeviceProvisionRequest req = new()
        {
            SerialNumber = string.Empty,
            HardwareModel = string.Empty,
            FirmwareVersion = string.Empty,
        };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void DeviceProvisionRequest_LabelTooLong_FailsValidation()
    {
        DeviceProvisionRequestValidator validator = new();
        DeviceProvisionRequest req = new()
        {
            SerialNumber = "SN-1",
            HardwareModel = "Model-1",
            FirmwareVersion = "1.0.0",
            Label = new string('x', 257),
        };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeviceProvisionRequest.Label));
    }

    [Fact]
    public void TelemetryQueryRequest_ValidRange_Passes()
    {
        TelemetryQueryRequestValidator validator = new();
        TelemetryQueryParameters req = new()
        {
            RangeStart = DateTimeOffset.UtcNow.AddHours(-1),
            RangeEnd = DateTimeOffset.UtcNow,
            MaxPoints = 1000,
        };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void TelemetryQueryRequest_EndBeforeStart_Fails()
    {
        TelemetryQueryRequestValidator validator = new();
        TelemetryQueryParameters req = new()
        {
            RangeStart = DateTimeOffset.UtcNow,
            RangeEnd = DateTimeOffset.UtcNow.AddHours(-1),
        };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void TelemetryQueryRequest_MaxPointsOutOfRange_Fails()
    {
        TelemetryQueryRequestValidator validator = new();
        TelemetryQueryParameters req = new() { MaxPoints = 0 };

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void TelemetryQueryRequest_AllNull_Passes()
    {
        TelemetryQueryRequestValidator validator = new();
        TelemetryQueryParameters req = new();

        ValidationResult result = validator.Validate(req);

        result.IsValid.ShouldBeTrue();
    }
}
