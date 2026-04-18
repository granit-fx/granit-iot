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

/// <summary>
/// Query-string parameters accepted by the telemetry range endpoint.
/// Validated by <see cref="TelemetryQueryRequestValidator"/>.
/// </summary>
public sealed record TelemetryQueryParameters
{
    /// <summary>Inclusive lower bound of the recorded-at range. Must be strictly before <see cref="RangeEnd"/> when both are set.</summary>
    public DateTimeOffset? RangeStart { get; init; }
    /// <summary>Exclusive upper bound of the recorded-at range.</summary>
    public DateTimeOffset? RangeEnd { get; init; }
    /// <summary>Maximum number of points returned; clamped to <c>[1, 10000]</c>.</summary>
    public int? MaxPoints { get; init; }
}
