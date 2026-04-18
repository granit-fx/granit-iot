using Granit.IoT.Ingestion.Abstractions;
using Granit.IoT.Ingestion.Aws.Diagnostics;
using Granit.IoT.Ingestion.Aws.Extensions;
using Granit.IoT.Ingestion.Aws.Internal;
using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests;

public sealed class IoTIngestionAwsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitIoTIngestionAws_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Should.Throw<ArgumentNullException>(() => services!.AddGranitIoTIngestionAws());
    }

    [Fact]
    public void AddGranitIoTIngestionAws_RegistersAllServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddGranitIoTIngestionAws();

        services.ShouldContain(d => d.ServiceType == typeof(IoTIngestionAwsMetrics));
        services.ShouldContain(d => d.ServiceType == typeof(ISnsSigningCertificateCache));
        services.ShouldContain(d => d.ServiceType == typeof(ISigV4RequestValidator));
        services.ShouldContain(d => d.ServiceType == typeof(IPayloadSignatureValidator));
        services.ShouldContain(d => d.ServiceType == typeof(IInboundMessageParser));
    }
}
