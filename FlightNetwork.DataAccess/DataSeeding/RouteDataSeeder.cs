using FlightNetwork.DataAccess.Sessions;
using Microsoft.Extensions.Options;

namespace FlightNetwork.DataAccess.DataSeeding;

/// <summary>
/// Routes are edges, not nodes. All the airlines flying the same airport pair collapse into a
/// single (:Airport)-[:ROUTE]->(:Airport) relationship carrying an airlineCodes array, so a
/// traversal walks each pair once instead of once per operating airline. Each edge also carries
/// distanceKm, the great-circle distance between the two airports. Must run after the airport
/// seeder, since the MATCHes below only connect airports that already exist.
/// </summary>
internal sealed class RouteDataSeeder(
    INeo4jSessionFactory sessionFactory,
    SeedFileReader fileReader,
    IOptions<SeedingOptions> options)
    : Neo4jDataSeederBase(sessionFactory, fileReader, options), IDataSeeder
{
    private const string RoutesFileName = "routes.json";

    private const string AirportsFileName = "airports.json";

    private const string CountQuery = "MATCH ()-[r:ROUTE]->() RETURN count(r) AS total";

    // Both MATCHes are index seeks on the uniqueness constraint. MATCH rather than MERGE on the
    // airports: a route naming an unknown code is skipped, never turned into a blank airport node.
    private const string UpsertQuery =
        """
        UNWIND $rows AS row
        MATCH (origin:Airport {code: row.originCode})
        MATCH (destination:Airport {code: row.destinationCode})
        MERGE (origin)-[r:ROUTE]->(destination)
        SET r.airlineCodes = row.airlineCodes,
            r.distanceKm = row.distanceKm
        """;

    public string Name => "Routes";

    public async Task<int> SeedAsync(CancellationToken ct = default)
    {
        if (await CountAsync(CountQuery, ct) > 0)
        {
            return 0;
        }

        var coordinates = await ReadCoordinatesAsync(ct);
        var grouped = await GroupByAirportPairAsync(coordinates, ct);

        return await WriteBatchesAsync(
            UpsertQuery,
            grouped.ToAsyncEnumerable(),
            static route => new Dictionary<string, object>
            {
                ["originCode"] = route.OriginCode,
                ["destinationCode"] = route.DestinationCode,
                ["airlineCodes"] = route.AirlineCodes,
                ["distanceKm"] = route.DistanceKm
            },
            ct);
    }

    /// <summary>
    /// Coordinates come from the airports file rather than the graph: the seeder would otherwise
    /// have to query the two endpoints for every one of the ~21k pairs.
    /// </summary>
    private async Task<Dictionary<string, (double Latitude, double Longitude)>> ReadCoordinatesAsync(
        CancellationToken ct)
    {
        var coordinates = new Dictionary<string, (double, double)>();

        await foreach (var airport in FileReader.ReadAsync<AirportSeedRecord>(AirportsFileName, ct))
        {
            coordinates[airport.Code] = (airport.Latitude, airport.Longitude);
        }

        return coordinates;
    }

    /// <summary>
    /// Collapses the file's one-row-per-airline shape into one row per airport pair, and computes
    /// each pair's great-circle distance once. The whole file has to be seen before any pair is
    /// complete, but the grouped result is small (~21k pairs).
    /// </summary>
    private async Task<List<GroupedRoute>> GroupByAirportPairAsync(
        Dictionary<string, (double Latitude, double Longitude)> coordinates,
        CancellationToken ct)
    {
        var pairs = new Dictionary<(string Origin, string Destination), SortedSet<string>>();

        await foreach (var record in FileReader.ReadAsync<RouteSeedRecord>(RoutesFileName, ct))
        {
            var key = (record.OriginCode, record.DestinationCode);

            if (!pairs.TryGetValue(key, out var airlineCodes))
            {
                airlineCodes = [];
                pairs[key] = airlineCodes;
            }

            airlineCodes.Add(record.AirlineCode);
        }

        var grouped = new List<GroupedRoute>(pairs.Count);

        foreach (var (key, airlineCodes) in pairs)
        {
            // A route whose endpoints are missing from the airports file cannot be measured, and
            // the Cypher MATCH would drop it anyway.
            if (!coordinates.TryGetValue(key.Origin, out var origin) ||
                !coordinates.TryGetValue(key.Destination, out var destination))
            {
                continue;
            }

            var distanceKm = GeoDistance.HaversineKm(
                origin.Latitude,
                origin.Longitude,
                destination.Latitude,
                destination.Longitude);

            grouped.Add(new GroupedRoute(key.Origin, key.Destination, [.. airlineCodes], distanceKm));
        }

        return grouped;
    }

    private sealed record GroupedRoute(
        string OriginCode,
        string DestinationCode,
        List<string> AirlineCodes,
        double DistanceKm);
}
