using System.Diagnostics;

namespace Granit.IoT.Diagnostics;

internal static class IoTActivitySource
{
    internal const string Name = "Granit.IoT";

    internal static readonly ActivitySource Source = new(Name);
}
