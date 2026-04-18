namespace Granit.IoT.Aws.Provisioning;

/// <summary>
/// Thrown when the saga encounters an unrecoverable AWS-side error during
/// Thing provisioning. Carries the <see cref="ThingName"/> for log
/// correlation and operator triage.
/// </summary>
public sealed class AwsThingProvisioningException : Exception
{
    /// <summary>Initializes a new instance with the offending Thing name and a message.</summary>
    public AwsThingProvisioningException(string thingName, string message)
        : base(message)
    {
        ThingName = thingName;
    }

    /// <summary>Initializes a new instance with the offending Thing name, a message and an inner exception.</summary>
    public AwsThingProvisioningException(string thingName, string message, Exception innerException)
        : base(message, innerException)
    {
        ThingName = thingName;
    }

    /// <summary>AWS IoT Thing name the saga was operating on when the failure occurred.</summary>
    public string ThingName { get; }
}
