using FlightNetwork.DataAccess.Configuration;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Sessions;

internal sealed class Neo4jSessionFactory(IDriver driver, IOptions<Neo4jSettings> settings) : INeo4jSessionFactory
{
    private readonly string _database = settings.Value.Database;

    public IAsyncSession CreateReadSession() =>
        driver.AsyncSession(builder => builder
            .WithDatabase(_database)
            .WithDefaultAccessMode(AccessMode.Read));

    public IAsyncSession CreateWriteSession() =>
        driver.AsyncSession(builder => builder
            .WithDatabase(_database)
            .WithDefaultAccessMode(AccessMode.Write));
}
