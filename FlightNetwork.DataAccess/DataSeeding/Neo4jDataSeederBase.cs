using FlightNetwork.DataAccess.Sessions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.DataSeeding;

internal abstract class Neo4jDataSeederBase(
    INeo4jSessionFactory sessionFactory,
    SeedFileReader fileReader,
    IOptions<SeedingOptions> options)
{
    protected SeedFileReader FileReader { get; } = fileReader;

    private int BatchSize => options.Value.BatchSize;

    protected async Task<long> CountAsync(string countQuery, CancellationToken ct)
    {
        await using var session = sessionFactory.CreateReadSession();

        return await session.ExecuteReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(countQuery);
            var record = await cursor.SingleAsync(ct);
            return record["total"].As<long>();
        });
    }

    protected async Task<int> WriteBatchesAsync<T>(
        string cypher,
        IAsyncEnumerable<T> rows,
        Func<T, Dictionary<string, object>> toParameters,
        CancellationToken ct)
    {
        var written = 0;

        await using var session = sessionFactory.CreateWriteSession();

        await foreach (var batch in rows.ChunkAsync(BatchSize, ct))
        {
            ct.ThrowIfCancellationRequested();

            var parameters = new List<Dictionary<string, object>>(batch.Count);
            foreach (var row in batch)
            {
                parameters.Add(toParameters(row));
            }

            await session.ExecuteWriteAsync(async runner =>
            {
                var cursor = await runner.RunAsync(cypher, new { rows = parameters });
                await cursor.ConsumeAsync();
            });

            written += batch.Count;
        }

        return written;
    }
}
