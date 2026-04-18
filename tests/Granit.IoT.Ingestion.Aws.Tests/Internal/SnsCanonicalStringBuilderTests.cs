using Granit.IoT.Ingestion.Aws.Internal;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal;

public sealed class SnsCanonicalStringBuilderTests
{
    [Fact]
    public void Build_NullEnvelope_Throws()
    {
        Should.Throw<ArgumentNullException>(() => SnsCanonicalStringBuilder.Build(null!));
    }

    [Fact]
    public void Build_UnknownType_ReturnsNull()
    {
        SnsEnvelope env = new() { Type = "Other", MessageId = "id", Message = "m", Timestamp = "t", TopicArn = "arn" };

        SnsCanonicalStringBuilder.Build(env).ShouldBeNull();
    }

    [Fact]
    public void Build_Notification_WithoutSubject_OmitsSubjectField()
    {
        SnsEnvelope env = new()
        {
            Type = "Notification",
            MessageId = "id-1",
            Message = "hello",
            Timestamp = "2026-01-01T00:00:00Z",
            TopicArn = "arn:aws:sns:r:a:t",
        };

        string? canonical = SnsCanonicalStringBuilder.Build(env);

        canonical.ShouldNotBeNull();
        canonical.ShouldContain("Message\nhello\n");
        canonical.ShouldContain("MessageId\nid-1\n");
        canonical.ShouldNotContain("Subject\n");
        canonical.ShouldContain("TopicArn\narn:aws:sns:r:a:t\n");
    }

    [Fact]
    public void Build_Notification_WithSubject_IncludesSubject()
    {
        SnsEnvelope env = new()
        {
            Type = "Notification",
            MessageId = "id-1",
            Subject = "topic-1",
            Message = "hello",
            Timestamp = "2026-01-01T00:00:00Z",
            TopicArn = "arn:aws:sns:r:a:t",
        };

        string? canonical = SnsCanonicalStringBuilder.Build(env);

        canonical.ShouldNotBeNull();
        canonical.ShouldContain("Subject\ntopic-1\n");
    }

    [Fact]
    public void Build_Notification_MissingMessage_ReturnsNull()
    {
        SnsEnvelope env = new()
        {
            Type = "Notification",
            MessageId = "id",
            Message = null,
            Timestamp = "t",
            TopicArn = "arn",
        };

        SnsCanonicalStringBuilder.Build(env).ShouldBeNull();
    }

    [Fact]
    public void Build_SubscriptionConfirmation_AllFields_IncludesSubscribeUrlAndToken()
    {
        SnsEnvelope env = new()
        {
            Type = "SubscriptionConfirmation",
            MessageId = "id-2",
            Message = "Please confirm",
            Timestamp = "2026-01-01T00:00:00Z",
            TopicArn = "arn:aws:sns:r:a:t",
            SubscribeUrl = "https://sns/example",
            Token = "tok123",
        };

        string? canonical = SnsCanonicalStringBuilder.Build(env);

        canonical.ShouldNotBeNull();
        canonical.ShouldContain("SubscribeURL\nhttps://sns/example\n");
        canonical.ShouldContain("Token\ntok123\n");
    }

    [Fact]
    public void Build_SubscriptionConfirmation_MissingToken_ReturnsNull()
    {
        SnsEnvelope env = new()
        {
            Type = "SubscriptionConfirmation",
            MessageId = "id",
            Message = "msg",
            Timestamp = "t",
            TopicArn = "arn",
            SubscribeUrl = "https://x",
            Token = null,
        };

        SnsCanonicalStringBuilder.Build(env).ShouldBeNull();
    }

    [Fact]
    public void Build_UnsubscribeConfirmation_UsesSubscriptionFormat()
    {
        SnsEnvelope env = new()
        {
            Type = "UnsubscribeConfirmation",
            MessageId = "id-3",
            Message = "Unsubscribed",
            Timestamp = "2026-01-01T00:00:00Z",
            TopicArn = "arn:aws:sns:r:a:t",
            SubscribeUrl = "https://sns/example",
            Token = "tok-x",
        };

        string? canonical = SnsCanonicalStringBuilder.Build(env);

        canonical.ShouldNotBeNull();
        canonical.ShouldContain("Token\ntok-x\n");
    }
}
