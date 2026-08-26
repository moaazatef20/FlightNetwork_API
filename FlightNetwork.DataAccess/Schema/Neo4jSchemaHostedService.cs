using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlightNetwork.DataAccess.Schema;

/// <summary>
/// Applies the graph schema once, before the host starts serving requests. Failing here is
/// deliberate: an app running against a graph without its uniqueness constraints would let
/// duplicate codes in and turn every code lookup into a label scan.
/// </summary>
internal sealed class Neo4jSchemaHostedService(
    INeo4jSchemaInitializer schemaInitializer,
    ILogger<Neo4jSchemaHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying Neo4j schema...");
        await schemaInitializer.EnsureSchemaAsync(cancellationToken);
        logger.LogInformation("Neo4j schema is up to date.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
