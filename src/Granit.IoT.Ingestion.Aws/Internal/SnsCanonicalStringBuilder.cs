using System.Text;

namespace Granit.IoT.Ingestion.Aws.Internal;

/// <summary>
/// Builds the <c>StringToSign</c> per the AWS SNS HTTP-subscription
/// documentation: fields in alphabetical order, each on its own two lines
/// (<c>name\nvalue\n</c>), with <c>Subject</c> included only when present for
/// <c>Notification</c>, and the <c>SubscribeURL</c>/<c>Token</c> pair used in
/// place of <c>Subject</c>/<c>TopicArn</c>-companion fields for the
/// confirmation variants.
/// </summary>
internal static class SnsCanonicalStringBuilder
{
    /// <summary>
    /// Returns the canonical <c>StringToSign</c> or <c>null</c> if required
    /// fields are missing (in which case the caller should treat the message
    /// as invalid).
    /// </summary>
    internal static string? Build(SnsEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return envelope.Type switch
        {
            SnsEnvelope.MessageTypes.Notification => BuildNotification(envelope),
            SnsEnvelope.MessageTypes.SubscriptionConfirmation => BuildSubscription(envelope),
            SnsEnvelope.MessageTypes.UnsubscribeConfirmation => BuildSubscription(envelope),
            _ => null,
        };
    }

    private static string? BuildNotification(SnsEnvelope e)
    {
        if (e.Message is null
            || e.MessageId is null
            || e.Timestamp is null
            || e.TopicArn is null
            || e.Type is null)
        {
            return null;
        }

        var sb = new StringBuilder(capacity: 512);
        Append(sb, "Message", e.Message);
        Append(sb, "MessageId", e.MessageId);
        if (!string.IsNullOrEmpty(e.Subject))
        {
            Append(sb, "Subject", e.Subject);
        }
        Append(sb, "Timestamp", e.Timestamp);
        Append(sb, "TopicArn", e.TopicArn);
        Append(sb, "Type", e.Type);
        return sb.ToString();
    }

    private static string? BuildSubscription(SnsEnvelope e)
    {
        if (e.Message is null
            || e.MessageId is null
            || e.SubscribeUrl is null
            || e.Timestamp is null
            || e.Token is null
            || e.TopicArn is null
            || e.Type is null)
        {
            return null;
        }

        var sb = new StringBuilder(capacity: 512);
        Append(sb, "Message", e.Message);
        Append(sb, "MessageId", e.MessageId);
        Append(sb, "SubscribeURL", e.SubscribeUrl);
        Append(sb, "Timestamp", e.Timestamp);
        Append(sb, "Token", e.Token);
        Append(sb, "TopicArn", e.TopicArn);
        Append(sb, "Type", e.Type);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string name, string value)
    {
        sb.Append(name).Append('\n').Append(value).Append('\n');
    }
}
