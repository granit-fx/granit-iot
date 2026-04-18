using System.Text.RegularExpressions;
using Granit.Domain;

namespace Granit.IoT.Aws.Domain;

/// <summary>
/// AWS IoT Thing name. Imposes the format <c>t{tenantId:N}-{serialNumber}</c>
/// (32 hex characters from a Guid + a dash + the device serial number) so that
/// IAM policies can isolate tenants natively at the AWS broker level via
/// <c>${iot:Connection.Thing.ThingName}</c> with a <c>t{tenantId}-*</c> pattern.
/// AWS allows up to 128 characters and the regex <c>[a-zA-Z0-9:_-]+</c>; using
/// the full 32-char hex tenant id eliminates any collision risk.
/// </summary>
public sealed partial class ThingName : SingleValueObject<string>
{
    /// <summary>AWS IoT maximum Thing name length (128 characters).</summary>
    public const int MaxLength = 128;
    private const int TenantPrefixLength = 33; // 't' + 32 hex chars
    private const string TenantPrefixChar = "t";

    /// <inheritdoc/>
    public override required string Value { get; init; }

    /// <summary>
    /// Factory method — validates the full <c>t{tenantId:N}-{serialNumber}</c> shape.
    /// Prefer <see cref="From(Guid, string)"/> when composing from a known tenant id
    /// and serial number.
    /// </summary>
    /// <param name="value">Complete Thing name string.</param>
    /// <exception cref="ArgumentException">Thrown when the input is null, empty, whitespace, over <see cref="MaxLength"/>, or does not match the expected shape.</exception>
    public static ThingName Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Thing name must not exceed {MaxLength} characters.", nameof(value));
        }

        if (!ThingNamePattern().IsMatch(value))
        {
            throw new ArgumentException(
                $"Thing name '{value}' does not match the required format 't{{tenantId:N}}-{{serialNumber}}' " +
                "(32-hex tenant id then '-' then a device serial number using [A-Za-z0-9_-]).",
                nameof(value));
        }

        return new ThingName { Value = value };
    }

    /// <summary>
    /// Composes a <see cref="ThingName"/> from a tenant id and a serial number.
    /// Use this whenever the bridge needs to derive a Thing name from a
    /// <c>Device</c> — never concatenate the format yourself in calling code.
    /// </summary>
    public static ThingName From(Guid tenantId, string serialNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        return Create($"{TenantPrefixChar}{tenantId:N}-{serialNumber}");
    }

    /// <summary>
    /// Returns the tenant id encoded in the prefix. Useful for AWS-side audit
    /// reconciliation when only the Thing name is available.
    /// </summary>
    public Guid GetTenantId() => Guid.ParseExact(Value.AsSpan(1, 32), "N");

    /// <summary>Returns the serial number portion (everything after <c>t{guid:N}-</c>).</summary>
    public string GetSerialNumber() => Value[(TenantPrefixLength + 1)..];

    /// <summary>Implicit string conversion for backward compatibility with string-typed callers (AWS SDK, ARNs).</summary>
    public static implicit operator string(ThingName name) => name.Value;

    [GeneratedRegex(@"^t[0-9a-f]{32}-[A-Za-z0-9][A-Za-z0-9_-]{0,94}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ThingNamePattern();
}
