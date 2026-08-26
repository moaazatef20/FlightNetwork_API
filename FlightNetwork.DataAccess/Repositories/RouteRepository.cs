using System.Text;
using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.DataAccess.Mapping;
using FlightNetwork.DataAccess.Sessions;
using FlightNetwork.Models.Entities;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Repositories;

internal sealed class RouteRepository(INeo4jSessionFactory sessionFactory)
    : Neo4jRepositoryBase(sessionFactory), IRouteRepository
{
    /// <summary>
    /// The instance kills the connection on deeper expansions, so the hop bound is capped here
    /// rather than trusted from the caller. Four hops reaches every airport in the current data.
    /// </summary>
    private const int MaxSupportedHops = 4;

    /// <summary>
    /// The distance search cannot go as deep as the hop search. It relaxes a layer per hop
    /// instead of using the native shortestPath expander, and measured against this instance a
    /// fourth layer exceeds the server's query deadline (`context deadline exceeded`) while
    /// three layers complete in about ten seconds.
    /// </summary>
    private const int MaxSupportedDistanceHops = 3;

    private const string PathProjection =
        """
        RETURN [n IN nodes(path) | n.code] AS stops,
               [r IN relationships(path) | r.airlineCodes] AS legAirlines,
               [r IN relationships(path) | r.distanceKm] AS legDistances,
               length(path) AS hops,
               reduce(total = 0.0, r IN relationships(path) | total + r.distanceKm) AS totalDistanceKm
        """;

    private const string HubQuery =
        """
        MATCH (a:Airport)-[r:ROUTE]-()
        RETURN a.code AS code, a.name AS name, count(r) AS totalConnections
        ORDER BY totalConnections DESC
        LIMIT $limit
        """;

    public async Task<FlightPath?> FindShortestPathAsync(
        string originCode,
        string destinationCode,
        int maxHops = MaxSupportedHops,
        CancellationToken ct = default)
    {
        ValidateEndpoints(originCode, destinationCode, maxHops);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(
                ShortestPathQuery(maxHops),
                new { originCode, destinationCode });

            return await cursor.FetchAsync() ? cursor.Current.ToFlightPath() : null;
        });
    }

    public async Task<IReadOnlyList<FlightPath>> FindAlternativePathsAsync(
        string originCode,
        string destinationCode,
        int maxHops = MaxSupportedHops,
        int limit = 10,
        CancellationToken ct = default)
    {
        ValidateEndpoints(originCode, destinationCode, maxHops);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(
                AllShortestPathsQuery(maxHops),
                new { originCode, destinationCode, limit });

            return await cursor.ToListAsync(record => record.ToFlightPath(), limit, ct);
        });
    }

    public async Task<IReadOnlyList<FlightPath>> FindShortestByDistanceAsync(
        string originCode,
        string destinationCode,
        int resultsCount = 5,
        int maxHops = MaxSupportedDistanceHops,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxHops, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxHops, MaxSupportedDistanceHops);
        ArgumentOutOfRangeException.ThrowIfLessThan(resultsCount, 1);

        // One hop-limited query per hop count, because a single query cannot both force an exact
        // hop count and report shorter paths found on the way. Results are merged and re-ranked
        // here, so a 4-hop route that flies less distance beats a 2-hop one.
        var candidates = new List<FlightPath>();

        for (var hops = 1; hops <= maxHops; hops++)
        {
            candidates.AddRange(
                await FindByDistanceWithExactHopsAsync(hops, originCode, destinationCode, resultsCount, ct));
        }

        return candidates
            .GroupBy(path => string.Join(">", path.Stops))
            .Select(group => group.First())
            .OrderBy(path => path.TotalDistanceKm)
            .Take(resultsCount)
            .ToList();
    }

    private async Task<IReadOnlyList<FlightPath>> FindByDistanceWithExactHopsAsync(
        int hops,
        string originCode,
        string destinationCode,
        int limit,
        CancellationToken ct) =>
        await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(
                ShortestByDistanceQuery(hops),
                new { originCode, destinationCode, limit });

            return await cursor.ToListAsync(record => record.ToFlightPath(), limit, ct);
        });

    public async Task<IReadOnlyList<HubAirport>> GetHubAirportsAsync(
        int limit = 5,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(HubQuery, new { limit });
            return await cursor.ToListAsync(record => record.ToHubAirport(), limit, ct);
        });
    }

    // Cypher does not accept a parameter inside a variable-length bound: [:ROUTE*1..$maxHops]
    // is a syntax error, so the bound has to be part of the query text. maxHops is validated to
    // a small integer range before it gets here, so this is not a string-injection surface.
    private static string ShortestPathQuery(int maxHops) =>
        $$"""
          MATCH path = shortestPath(
              (origin:Airport {code: $originCode})-[:ROUTE*1..{{maxHops}}]->(destination:Airport {code: $destinationCode})
          )
          {{PathProjection}}
          """;

    // allShortestPaths, not a plain variable-length MATCH. Enumerating every path up to N hops
    // between two airports overwhelms the server (it dropped the connection at three hops on
    // this data), while allShortestPaths returns every route that ties for the fewest hops and
    // completes in about a second.
    private static string AllShortestPathsQuery(int maxHops) =>
        $$"""
          MATCH path = allShortestPaths(
              (origin:Airport {code: $originCode})-[:ROUTE*1..{{maxHops}}]->(destination:Airport {code: $destinationCode})
          )
          {{PathProjection}}
          LIMIT $limit
          """;

    /// <summary>
    /// Paths carried forward per airport at each layer. One is both correct and necessary:
    /// hop-limited shortest path is exact when the best path per (airport, hop count) is kept,
    /// and raising this multiplies rows layer over layer — at three, the four-hop query times
    /// out with `context deadline exceeded`. Alternatives still come from the final layer,
    /// which fans in from every airport with a route into the destination.
    /// </summary>
    private const int CandidatesPerAirportPerLayer = 1;

    /// <summary>
    /// Hop-limited shortest path by distance, expressed as layered relaxation: expand one hop,
    /// keep the best few paths per airport, repeat. Each layer is bounded by the number of
    /// airports, which is what makes this safe where a variable-length MATCH is not — that
    /// enumerates every route and takes the instance down. `allShortestPaths` is not usable here
    /// either: it minimises hops, and on this data a longer-in-hops route flies less distance for
    /// 31% of airport pairs.
    /// </summary>
    private static string ShortestByDistanceQuery(int hops)
    {
        var query = new StringBuilder();

        query.AppendLine(
            """
            MATCH (origin:Airport {code: $originCode})
            WITH origin.code AS code, 0.0 AS km, [origin.code] AS stops, [] AS legs
            """);

        // Intermediate hops: expand anywhere, then prune back to the best few per airport.
        for (var layer = 1; layer < hops; layer++)
        {
            query.AppendLine(
                $$$"""
                   MATCH (current:Airport {code: code})-[r:ROUTE]->(next:Airport)
                   WHERE NOT next.code IN stops
                   WITH next.code AS code,
                        km + r.distanceKm AS km,
                        stops + [next.code] AS stops,
                        legs + [{airlines: r.airlineCodes, km: r.distanceKm}] AS legs
                   ORDER BY km
                   WITH code, collect({km: km, stops: stops, legs: legs})[0..{{{CandidatesPerAirportPerLayer}}}] AS best
                   UNWIND best AS candidate
                   WITH code, candidate.km AS km, candidate.stops AS stops, candidate.legs AS legs
                   """);
        }

        // Final hop lands on the destination, so the last expansion is a single index seek.
        query.AppendLine(
            """
            MATCH (current:Airport {code: code})-[r:ROUTE]->(destination:Airport {code: $destinationCode})
            WHERE NOT destination.code IN stops
            WITH km + r.distanceKm AS km,
                 stops + [destination.code] AS stops,
                 legs + [{airlines: r.airlineCodes, km: r.distanceKm}] AS legs
            ORDER BY km
            RETURN stops,
                   [leg IN legs | leg.airlines] AS legAirlines,
                   [leg IN legs | leg.km] AS legDistances,
                   size(legs) AS hops,
                   km AS totalDistanceKm
            LIMIT $limit
            """);

        return query.ToString();
    }

    private static void ValidateEndpoints(string originCode, string destinationCode, int maxHops)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxHops, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxHops, MaxSupportedHops);
    }
}
