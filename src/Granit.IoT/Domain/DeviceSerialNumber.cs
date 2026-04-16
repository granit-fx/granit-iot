using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed partial class DeviceSerialNumber : SingleValueObject<string>
{
    public const int MaxLength = 128;

    public override required string Value { get; init; }

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

    public static implicit operator string(DeviceSerialNumber sn) => sn.Value;
    public static implicit operator DeviceSerialNumber(string value) => Create(value);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9\-_]{0,127}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SerialNumberPattern();
}
