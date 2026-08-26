using FlightNetwork.DataAccess.Sessions;
using Microsoft.Extensions.Options;

namespace FlightNetwork.DataAccess.DataSeeding;

internal sealed class AirlineDataSeeder(
    INeo4jSessionFactory sessionFactory,
    SeedFileReader fileReader,
    IOptions<SeedingOptions> options)
    : Neo4jDataSeederBase(sessionFactory, fileReader, options), IDataSeeder
{
    private const string FileName = "airlines.json";

    private const string CountQuery = "MATCH (a:Airline) RETURN count(a) AS total";

    // The file repeats some codes (for example "-" appears as both "Unknown" and "Private flight"),
    // so MERGE is required here: CREATE would trip the uniqueness constraint. Last row wins.
    private const string UpsertQuery =
        """
        UNWIND $rows AS row
        MERGE (a:Airline {code: row.code})
        SET a.name = row.name
        """;

    public string Name => "Airlines";

    public async Task<int> SeedAsync(CancellationToken ct = default)
    {
        if (await CountAsync(CountQuery, ct) > 0)
        {
            return 0;
        }

        return await WriteBatchesAsync(
            UpsertQuery,
            FileReader.ReadAsync<AirlineSeedRecord>(FileName, ct),
            static record => new Dictionary<string, object>
            {
                ["code"] = record.Code,
                ["name"] = record.Name
            },
            ct);
    }
}
