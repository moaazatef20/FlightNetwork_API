using FlightNetwork.Models.Entities;

namespace FlightNetwork.Services.Contracts;

public interface IRouteService
{
    /// <summary>Fewest-hop route between two airports. Null when none exists within maxHops.</summary>
    Task<FlightPath?> FindShortestPathAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        CancellationToken ct = default);

    /// <summary>Every route tying for the fewest hops.</summary>
    Task<IReadOnlyList<FlightPath>> FindAlternativePathsAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Routes ranked by great-circle distance flown, shortest first — geographic distance, not a
    /// fare. Slower than the hop-based methods; callers should expect to cache it.
    /// </summary>
    Task<IReadOnlyList<FlightPath>> FindShortestByDistanceAsync(
        string originCode,
        string destinationCode,
        int resultsCount = 5,
        int maxHops = 3,
        CancellationToken ct = default);

    /// <summary>Airports ordered by how many routes touch them, inbound and outbound combined.</summary>
    Task<IReadOnlyList<HubAirport>> GetHubAirportsAsync(int limit = 5, CancellationToken ct = default);
}
