using Granit.Domain;

namespace Granit.IoT.Domain;

/// <summary>
/// Single-value object wrapping the hardware model identifier (e.g. <c>"TempProbe-v2"</c>).
/// Bounded at <see cref="MaxLength"/> characters; EF Core converter applied automatically
/// via <c>ApplyGranitConventions</c>.
/// </summary>
public sealed class HardwareModel : SingleValueObject<string>
{
    /// <summary>Maximum length (in characters) enforced by <see cref="Create(string)"/>.</summary>
    public const int MaxLength = 256;

    /// <inheritdoc/>
    public override required string Value { get; init; }

    /// <summary>
    /// Factory method — the only supported construction path. Rejects null, empty,
    /// whitespace, and values longer than <see cref="MaxLength"/>.
    /// </summary>
    /// <param name="value">Model identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the input is null, empty, whitespace, or exceeds <see cref="MaxLength"/>.</exception>
    public static HardwareModel Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Hardware model must not exceed {MaxLength} characters.", nameof(value));
        }
        return new HardwareModel { Value = value };
    }

    /// <summary>Implicit string conversion for backward compatibility with string-typed callers.</summary>
    public static implicit operator string(HardwareModel model) => model.Value;

    /// <summary>Implicit string-to-<see cref="HardwareModel"/> conversion that delegates to <see cref="Create(string)"/>.</summary>
    public static implicit operator HardwareModel(string value) => Create(value);
}
