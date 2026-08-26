using FlightNetwork.Models.Entities;

namespace FlightNetwork.DataAccess.Contracts;

public interface IRouteRepository
{
    /// <summary>
    /// Fewest-hop route between two airports, following flight direction.
    /// Returns null when no route exists within <paramref name="maxHops"/>.
    /// </summary>
    Task<FlightPath?> FindShortestPathAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        CancellationToken ct = default);

    /// <summary>
    /// Every route that ties for the fewest hops, so the caller can choose between equally
    /// short itineraries. This is not "all routes up to N hops" — see the repository for why.
    /// </summary>
    Task<IReadOnlyList<FlightPath>> FindAlternativePathsAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Routes ranked by total great-circle distance flown, shortest first. This is geographic
    /// distance — the dataset has no fares, so this is deliberately not called "cheapest".
    /// A route using more hops wins if it flies fewer kilometres, which is the whole point:
    /// on this data the fewest-hop route is the longer one for about a third of airport pairs.
    /// Searches up to 3 hops — one less than the hop-based methods — and takes seconds rather
    /// than milliseconds, so callers should expect to cache it.
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
