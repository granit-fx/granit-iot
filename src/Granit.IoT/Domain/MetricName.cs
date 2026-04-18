using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Value object wrapping a telemetry metric name. Enforces lowercase dot-notation
/// with at most 10 segments (e.g. <c>temperature</c>, <c>sensor.battery.level</c>) —
/// the shape is stricter than <see cref="TelemetryPoint"/>'s floor so callers using
/// <see cref="MetricName"/> get an additional guarantee on emitted keys.
/// </summary>
public sealed partial class MetricName : SingleValueObject<string>
{
    /// <summary>Maximum length (in characters) enforced by <see cref="Create(string)"/>.</summary>
    public const int MaxLength = 64;

    /// <inheritdoc/>
    public override required string Value { get; init; }

    /// <summary>
    /// Factory method — the only supported construction path. Validates length and
    /// shape (<c>^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){0,9}$</c>).
    /// </summary>
    /// <param name="value">Metric name in lowercase dot-notation.</param>
    /// <exception cref="ArgumentException">Thrown when the input is null, empty, whitespace, over <see cref="MaxLength"/>, or does not match the expected shape.</exception>
    public static MetricName Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Metric name must not exceed {MaxLength} characters.", nameof(value));
        }

        if (!MetricNamePattern().IsMatch(value))
        {
            throw new ArgumentException($"Metric name '{value}' must be lowercase dot-notation (e.g. 'temperature', 'sensor.battery.level').", nameof(value));
        }
        return new MetricName { Value = value };
    }

    /// <summary>Implicit string conversion for backward compatibility with string-typed callers.</summary>
    public static implicit operator string(MetricName name) => name.Value;

    /// <summary>Implicit string-to-<see cref="MetricName"/> conversion that delegates to <see cref="Create(string)"/>.</summary>
    public static implicit operator MetricName(string value) => Create(value);

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){0,9}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MetricNamePattern();
}
