using Granit.IoT.Ingestion;
using Granit.Modularity;

namespace Granit.IoT.Ingestion.Endpoints;

/// <summary>
/// HTTP ingestion endpoint: <c>POST /iot/ingest/{source}</c>. Returns <c>202 Accepted</c>
/// after enqueueing to the Wolverine outbox — no synchronous DB write in the request path
/// (see CLAUDE.md §Key design decisions).
/// </summary>
[DependsOn(typeof(GranitIoTIngestionModule))]
public sealed class GranitIoTIngestionEndpointsModule : GranitModule;
