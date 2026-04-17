namespace Granit.IoT.Aws.Domain;

/// <summary>
/// Saga checkpoints for the AWS IoT provisioning workflow. The bridge handler
/// progresses through these states one step at a time and persists the new
/// status + ARN in the same transaction as the Wolverine inbox ack, so a
/// replay resumes exactly where it stopped without re-issuing AWS calls.
/// </summary>
public enum AwsThingProvisioningStatus
{
    /// <summary>Row reserved in the database — no AWS API call attempted yet.</summary>
    Pending = 0,

    /// <summary><c>CreateThing</c> succeeded; <c>ThingArn</c> persisted.</summary>
    ThingCreated = 1,

    /// <summary><c>CreateKeysAndCertificate</c> succeeded; <c>CertificateArn</c> persisted.</summary>
    CertIssued = 2,

    /// <summary>Private key stored in Secrets Manager; <c>CertificateSecretArn</c> persisted.</summary>
    SecretStored = 3,

    /// <summary>IoT policy attached and certificate bound to the Thing — ready to connect.</summary>
    Active = 4,

    /// <summary>Thing, certificate and secret have been deleted from AWS.</summary>
    Decommissioned = 5,

    /// <summary>Non-recoverable error — manual reconciliation required.</summary>
    Failed = 6,
}
