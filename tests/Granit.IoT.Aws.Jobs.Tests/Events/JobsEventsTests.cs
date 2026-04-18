using Granit.IoT.Aws.Jobs.Events;
using Shouldly;

namespace Granit.IoT.Aws.Jobs.Tests.Events;

public sealed class JobsEventsTests
{
    [Fact]
    public void DeviceCommandCompletedEvent_StoresAllFields()
    {
        var corr = Guid.NewGuid();
        var tenant = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

        DeviceCommandCompletedEvent evt = new(corr, "job-1", "thing-1", at, tenant);

        evt.CorrelationId.ShouldBe(corr);
        evt.JobId.ShouldBe("job-1");
        evt.ThingName.ShouldBe("thing-1");
        evt.CompletedAt.ShouldBe(at);
        evt.TenantId.ShouldBe(tenant);
    }

    [Fact]
    public void DeviceCommandFailedEvent_StoresAllFields()
    {
        var corr = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero);

        DeviceCommandFailedEvent evt = new(corr, "job-2", "thing-2", "FAILED", "boom", at, null);

        evt.CorrelationId.ShouldBe(corr);
        evt.JobId.ShouldBe("job-2");
        evt.ThingName.ShouldBe("thing-2");
        evt.Status.ShouldBe("FAILED");
        evt.Reason.ShouldBe("boom");
        evt.FailedAt.ShouldBe(at);
        evt.TenantId.ShouldBeNull();
    }
}
