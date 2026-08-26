using FlightNetwork.Api.DTOs;
using FlightNetwork.Api.Mapping;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FlightNetwork.Api.Controllers;

[ApiController]
[Route("api/routes")]
public sealed class RoutesController(IRouteService routeService) : ControllerBase
{
    [HttpGet("shortest-path")]
    public async Task<ActionResult<FlightPathDto>> GetShortestPath(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] int maxHops = 4,
        CancellationToken ct = default)
    {
        var path = await routeService.FindShortestPathAsync(origin, destination, maxHops, ct);
        return path is null ? NotFound() : Ok(path.ToDto());
    }

    [HttpGet("alternative-paths")]
    public async Task<ActionResult<IReadOnlyList<FlightPathDto>>> GetAlternativePaths(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] int maxHops = 4,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var paths = await routeService.FindAlternativePathsAsync(origin, destination, maxHops, limit, ct);
        return Ok(paths.Select(p => p.ToDto()).ToList());
    }

    /// <summary>
    /// Ranked by great-circle distance flown, shortest first. Geographic distance, not a fare —
    /// the dataset has no pricing data.
    /// </summary>
    [HttpGet("shortest-by-distance")]
    public async Task<ActionResult<IReadOnlyList<FlightPathDto>>> GetShortestByDistance(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] int resultsCount = 5,
        [FromQuery] int maxHops = 3,
        CancellationToken ct = default)
    {
        var paths = await routeService.FindShortestByDistanceAsync(origin, destination, resultsCount, maxHops, ct);
        return Ok(paths.Select(p => p.ToDto()).ToList());
    }

    [HttpGet("hubs")]
    public async Task<ActionResult<IReadOnlyList<HubAirportDto>>> GetHubAirports(
        [FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var hubs = await routeService.GetHubAirportsAsync(limit, ct);
        return Ok(hubs.Select(h => h.ToDto()).ToList());
    }
}
