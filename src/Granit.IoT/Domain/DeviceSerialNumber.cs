using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Value object wrapping a device serial number. Enforces the character class
/// <c>[A-Za-z0-9_-]</c> with a leading alphanumeric — safe for use in URL segments,
/// log output, and OpenTelemetry tags without additional escaping.
/// </summary>
public sealed partial class DeviceSerialNumber : SingleValueObject<string>
{
    /// <summary>Maximum length (in characters) enforced by <see cref="Create(string)"/>.</summary>
    public const int MaxLength = 128;

    /// <inheritdoc/>
    public override required string Value { get; init; }

    /// <summary>
    /// Factory method — the only supported construction path. Validates length and
    /// shape (<c>^[A-Za-z0-9][A-Za-z0-9\-_]{0,127}$</c>).
    /// </summary>
    /// <param name="value">Serial number.</param>
    /// <exception cref="ArgumentException">Thrown when the input is null, empty, whitespace, over <see cref="MaxLength"/>, or contains invalid characters.</exception>
    public static DeviceSerialNumber Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Serial number must not exceed {MaxLength} characters.", nameof(value));
        }

        if (!SerialNumberPattern().IsMatch(value))
        {
            throw new ArgumentException($"Serial number '{value}' contains invalid characters. Only alphanumeric, dashes, and underscores are allowed.", nameof(value));
        }
        return new DeviceSerialNumber { Value = value };
    }

    /// <summary>Implicit string conversion for backward compatibility with string-typed callers.</summary>
    public static implicit operator string(DeviceSerialNumber sn) => sn.Value;

    /// <summary>Implicit string-to-<see cref="DeviceSerialNumber"/> conversion that delegates to <see cref="Create(string)"/>.</summary>
    public static implicit operator DeviceSerialNumber(string value) => Create(value);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9\-_]{0,127}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SerialNumberPattern();
}
