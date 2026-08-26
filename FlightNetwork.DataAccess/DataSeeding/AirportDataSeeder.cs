using FlightNetwork.DataAccess.Sessions;
using Microsoft.Extensions.Options;

namespace FlightNetwork.DataAccess.DataSeeding;

internal sealed class AirportDataSeeder(
    INeo4jSessionFactory sessionFactory,
    SeedFileReader fileReader,
    IOptions<SeedingOptions> options)
    : Neo4jDataSeederBase(sessionFactory, fileReader, options), IDataSeeder
{
    private const string FileName = "airports.json";

    private const string CountQuery = "MATCH (a:Airport) RETURN count(a) AS total";

    // MERGE on the constrained code makes the seed re-runnable: a rerun updates in place
    // instead of failing on the uniqueness constraint or duplicating nodes.
    private const string UpsertQuery =
        """
        UNWIND $rows AS row
        MERGE (a:Airport {code: row.code})
        SET a.name = row.name,
            a.city = row.city,
            a.country = row.country,
            a.latitude = row.latitude,
            a.longitude = row.longitude
        """;

    public string Name => "Airports";

    public async Task<int> SeedAsync(CancellationToken ct = default)
    {
        if (await CountAsync(CountQuery, ct) > 0)
        {
            return 0;
        }

        return await WriteBatchesAsync(
            UpsertQuery,
            FileReader.ReadAsync<AirportSeedRecord>(FileName, ct),
            static record => new Dictionary<string, object>
            {
                ["code"] = record.Code,
                ["name"] = record.Name,
                ["city"] = record.City,
                ["country"] = record.Country,
                ["latitude"] = record.Latitude,
                ["longitude"] = record.Longitude
            },
            ct);
    }
}
