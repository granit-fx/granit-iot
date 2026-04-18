using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Value object wrapping a firmware version string. Accepts semver-ish shapes
/// (<c>1.2</c>, <c>1.2.3</c>, <c>1.2.3-beta</c>, <c>1.2.3+build.42</c>). Strict validation
/// is kept loose to accommodate vendor-specific schemes.
/// </summary>
public sealed partial class FirmwareVersion : SingleValueObject<string>
{
    /// <summary>Maximum length (in characters) enforced by <see cref="Create(string)"/>.</summary>
    public const int MaxLength = 64;

    /// <inheritdoc/>
    public override required string Value { get; init; }

    /// <summary>
    /// Factory method — the only supported construction path. Validates length and
    /// shape (<c>^[0-9]+\.[0-9]+(\.[0-9]+)?([+\-].+)?$</c>).
    /// </summary>
    /// <param name="value">Firmware version string.</param>
    /// <exception cref="ArgumentException">Thrown when the input is null, empty, whitespace, over <see cref="MaxLength"/>, or does not match the expected shape.</exception>
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

    /// <summary>Implicit string conversion for backward compatibility with string-typed callers.</summary>
    public static implicit operator string(FirmwareVersion version) => version.Value;

    /// <summary>Implicit string-to-<see cref="FirmwareVersion"/> conversion that delegates to <see cref="Create(string)"/>.</summary>
    public static implicit operator FirmwareVersion(string value) => Create(value);

    [GeneratedRegex(@"^[0-9]+\.[0-9]+(\.[0-9]+)?([+\-].+)?$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();
}
