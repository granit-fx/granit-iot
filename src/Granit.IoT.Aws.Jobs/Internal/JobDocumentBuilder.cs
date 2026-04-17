using System.Text.Json;
using Granit.IoT.Aws.Jobs.Abstractions;

namespace Granit.IoT.Aws.Jobs.Internal;

/// <summary>
/// Serialises a Granit <see cref="IDeviceCommand"/> into the AWS IoT Jobs
/// document JSON the device runtime parses. Format:
/// <code>{"operation":"…","correlationId":"…","parameters":{…}}</code>
/// </summary>
internal static class JobDocumentBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Build(IDeviceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var doc = new
        {
            operation = command.Operation,
            correlationId = command.CorrelationId.ToString(),
            parameters = command.Parameters,
        };
        return JsonSerializer.Serialize(doc, JsonOptions);
    }
}
