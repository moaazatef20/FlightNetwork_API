using FlightNetwork.DataAccess.Sessions;

namespace FlightNetwork.DataAccess.Schema;

internal sealed class Neo4jSchemaInitializer(INeo4jSessionFactory sessionFactory) : INeo4jSchemaInitializer
{
    private static readonly string[] Statements =
    [
        "CREATE CONSTRAINT airport_code_unique IF NOT EXISTS FOR (a:Airport) REQUIRE a.code IS UNIQUE",
        "CREATE CONSTRAINT airline_code_unique IF NOT EXISTS FOR (a:Airline) REQUIRE a.code IS UNIQUE"
    ];

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var session = sessionFactory.CreateWriteSession();

        foreach (var statement in Statements)
        {
            ct.ThrowIfCancellationRequested();

            await session.ExecuteWriteAsync(async runner =>
            {
                var cursor = await runner.RunAsync(statement);
                await cursor.ConsumeAsync();
            });
        }
    }
}
