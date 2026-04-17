using System.Text.Json.Serialization;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Shape of the AWS SNS HTTP-subscription delivery envelope. Only the fields
/// used for signature validation and replay protection are bound here —
/// <c>UnsubscribeURL</c>, <c>Subject</c>, and friends are pass-through.
/// </summary>
internal sealed class SnsEnvelope
{
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("TopicArn")]
    public string? TopicArn { get; set; }

    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("Timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("SignatureVersion")]
    public string? SignatureVersion { get; set; }

    [JsonPropertyName("Signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("SigningCertURL")]
    public string? SigningCertUrl { get; set; }

    [JsonPropertyName("Token")]
    public string? Token { get; set; }

    [JsonPropertyName("SubscribeURL")]
    public string? SubscribeUrl { get; set; }

    /// <summary>Well-known SNS message types.</summary>
    internal static class MessageTypes
    {
        internal const string Notification = "Notification";
        internal const string SubscriptionConfirmation = "SubscriptionConfirmation";
        internal const string UnsubscribeConfirmation = "UnsubscribeConfirmation";
    }
}
