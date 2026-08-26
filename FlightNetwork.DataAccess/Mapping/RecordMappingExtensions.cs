using FlightNetwork.Models.Entities;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Mapping;

/// <summary>
/// Maps flat Cypher projections to entities. Queries project the exact properties they need
/// instead of returning whole nodes, so nothing is fetched that the caller will not use.
/// </summary>
internal static class RecordMappingExtensions
{
    public static Airport ToAirport(this IRecord record) => new()
    {
        Code = record["code"].As<string>(),
        Name = record["name"].As<string>(),
        City = record["city"].As<string>(),
        Country = record["country"].As<string>(),
        Latitude = record["latitude"].As<double>(),
        Longitude = record["longitude"].As<double>()
    };

    public static Airline ToAirline(this IRecord record) => new()
    {
        Code = record["code"].As<string>(),
        Name = record["name"].As<string>()
    };

    public static HubAirport ToHubAirport(this IRecord record) => new()
    {
        Code = record["code"].As<string>(),
        Name = record["name"].As<string>(),
        TotalConnections = (int)record["totalConnections"].As<long>()
    };

    /// <summary>
    /// Rebuilds a path from the flat projection. Cypher returns the nodes and the relationships
    /// as parallel lists; because the pattern is directed, relationship i always connects
    /// stop i to stop i + 1, which is what lets the legs be zipped back together here.
    /// </summary>
    public static FlightPath ToFlightPath(this IRecord record)
    {
        var stops = record["stops"].As<List<string>>();
        var legAirlines = record["legAirlines"].As<List<object>>();
        var legDistances = record["legDistances"].As<List<double>>();

        var legs = new List<FlightLeg>(legAirlines.Count);
        for (var i = 0; i < legAirlines.Count; i++)
        {
            legs.Add(new FlightLeg
            {
                OriginCode = stops[i],
                DestinationCode = stops[i + 1],
                AirlineCodes = legAirlines[i].As<List<string>>(),
                DistanceKm = Math.Round(legDistances[i], 1)
            });
        }

        return new FlightPath
        {
            Stops = stops,
            Legs = legs,
            Hops = (int)record["hops"].As<long>(),
            // Rounded here rather than in Cypher: this server's round() takes no digits argument.
            TotalDistanceKm = Math.Round(record["totalDistanceKm"].As<double>(), 1)
        };
    }
}
