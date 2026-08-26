using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;
using FlightNetwork.Services.Contracts;

namespace FlightNetwork.Services;

internal sealed class RouteService(IRouteRepository repository) : IRouteService
{
    public Task<FlightPath?> FindShortestPathAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        CancellationToken ct = default) =>
        repository.FindShortestPathAsync(Code.Normalize(originCode), Code.Normalize(destinationCode), maxHops, ct);

    public Task<IReadOnlyList<FlightPath>> FindAlternativePathsAsync(
        string originCode,
        string destinationCode,
        int maxHops = 4,
        int limit = 10,
        CancellationToken ct = default) =>
        repository.FindAlternativePathsAsync(
            Code.Normalize(originCode), Code.Normalize(destinationCode), maxHops, limit, ct);

    public Task<IReadOnlyList<FlightPath>> FindShortestByDistanceAsync(
        string originCode,
        string destinationCode,
        int resultsCount = 5,
        int maxHops = 3,
        CancellationToken ct = default) =>
        repository.FindShortestByDistanceAsync(
            Code.Normalize(originCode), Code.Normalize(destinationCode), resultsCount, maxHops, ct);

    public Task<IReadOnlyList<HubAirport>> GetHubAirportsAsync(int limit = 5, CancellationToken ct = default) =>
        repository.GetHubAirportsAsync(limit, ct);
}
