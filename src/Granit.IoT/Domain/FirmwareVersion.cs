using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed partial class FirmwareVersion : SingleValueObject<string>
{
    public const int MaxLength = 64;

    public override required string Value { get; init; }

    public static FirmwareVersion Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Firmware version must not exceed {MaxLength} characters.", nameof(value));
        }

        if (!VersionPattern().IsMatch(value))
        {
            throw new ArgumentException($"Firmware version '{value}' is not a valid version format.", nameof(value));
        }
        return new FirmwareVersion { Value = value };
    }

    public static implicit operator string(FirmwareVersion version) => version.Value;
    public static implicit operator FirmwareVersion(string value) => Create(value);

    [GeneratedRegex(@"^[0-9]+\.[0-9]+(\.[0-9]+)?([+\-].+)?$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();
}
