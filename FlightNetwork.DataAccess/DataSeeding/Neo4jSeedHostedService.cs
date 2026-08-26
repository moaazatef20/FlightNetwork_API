using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightNetwork.DataAccess.DataSeeding;


internal sealed class Neo4jSeedHostedService(
    IEnumerable<IDataSeeder> seeders,
    IOptions<SeedingOptions> options,
    ILogger<Neo4jSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Seeding is disabled; skipping.");
            return;
        }

        foreach (var seeder in seeders)
        {
            var stopwatch = Stopwatch.StartNew();
            var written = await seeder.SeedAsync(cancellationToken);
            stopwatch.Stop();

            if (written == 0)
            {
                logger.LogInformation("{Seeder}: already seeded, nothing to do.", seeder.Name);
            }
            else
            {
                logger.LogInformation(
                    "{Seeder}: seeded {Count} rows in {Elapsed}.",
                    seeder.Name,
                    written,
                    stopwatch.Elapsed);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
