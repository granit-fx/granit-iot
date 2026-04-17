namespace Granit.IoT.Aws.Provisioning;

/// <summary>
/// Thrown when the saga encounters an unrecoverable AWS-side error during
/// Thing provisioning. Carries the <see cref="ThingName"/> for log
/// correlation and operator triage.
/// </summary>
public sealed class AwsThingProvisioningException : Exception
{
    public AwsThingProvisioningException(string thingName, string message)
        : base(message)
    {
        ThingName = thingName;
    }

    public AwsThingProvisioningException(string thingName, string message, Exception innerException)
        : base(message, innerException)
    {
        ThingName = thingName;
    }

    public string ThingName { get; }
}
