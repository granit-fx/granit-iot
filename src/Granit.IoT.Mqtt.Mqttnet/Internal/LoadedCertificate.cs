using System.Security.Cryptography.X509Certificates;

namespace Granit.IoT.Mqtt.Mqttnet.Internal;

/// <summary>
/// A loaded MQTT client certificate along with its (provider-reported or X509-derived)
/// expiry, used to schedule the proactive reload Timer before mid-connection expiry.
/// </summary>
internal sealed record LoadedCertificate(X509Certificate2 Certificate, DateTimeOffset? ExpiresOn);
