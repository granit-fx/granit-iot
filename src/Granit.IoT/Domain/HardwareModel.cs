using Granit.Domain;

namespace Granit.IoT.Domain;

public sealed class HardwareModel : SingleValueObject<string>
{
    public const int MaxLength = 256;

    public override required string Value { get; init; }

    public static HardwareModel Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Hardware model must not exceed {MaxLength} characters.", nameof(value));
        }
        return new HardwareModel { Value = value };
    }

    public static implicit operator string(HardwareModel model) => model.Value;
    public static implicit operator HardwareModel(string value) => Create(value);
}
