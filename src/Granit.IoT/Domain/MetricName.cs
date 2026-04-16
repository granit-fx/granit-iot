using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed partial class MetricName : SingleValueObject<string>
{
    public const int MaxLength = 64;

    public override required string Value { get; init; }

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

    public static implicit operator string(MetricName name) => name.Value;
    public static implicit operator MetricName(string value) => Create(value);

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){0,9}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MetricNamePattern();
}
