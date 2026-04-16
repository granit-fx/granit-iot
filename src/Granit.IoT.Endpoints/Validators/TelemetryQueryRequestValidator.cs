using FluentValidation;
using Granit.Validation;

namespace Granit.IoT.Endpoints.Validators;

internal sealed class TelemetryQueryRequestValidator : GranitValidator<TelemetryQueryParameters>
{
    public TelemetryQueryRequestValidator()
    {
        RuleFor(x => x.RangeStart)
            .LessThan(x => x.RangeEnd)
            .When(x => x.RangeStart.HasValue && x.RangeEnd.HasValue)
            .WithMessage("'RangeStart' must be before 'RangeEnd'.");

        RuleFor(x => x.MaxPoints)
            .InclusiveBetween(1, 10000)
            .When(x => x.MaxPoints.HasValue);
    }
}

public sealed record TelemetryQueryParameters
{
    public DateTimeOffset? RangeStart { get; init; }
    public DateTimeOffset? RangeEnd { get; init; }
    public int? MaxPoints { get; init; }
}
