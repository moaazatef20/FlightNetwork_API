using FlightNetwork.Api.Controllers;
using FlightNetwork.Api.DTOs;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlightNetwork.Api.Tests.Controllers;

public class RoutesControllerTests
{
    private readonly Mock<IRouteService> _service = new();
    private readonly RoutesController _sut;

    public RoutesControllerTests()
    {
        _sut = new RoutesController(_service.Object);
    }

    [Fact]
    public async Task GetShortestPath_ExistingRoute_ReturnsOkWithDto()
    {
        _service.Setup(s => s.FindShortestPathAsync("CAI", "DLZ", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPath { Stops = ["CAI", "PEK", "ULN", "DLZ"], Hops = 3, TotalDistanceKm = 9204.7 });

        var result = await _sut.GetShortestPath("CAI", "DLZ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FlightPathDto>(ok.Value);
        Assert.Equal(3, dto.Hops);
    }

    [Fact]
    public async Task GetShortestPath_NoRouteWithinHops_ReturnsNotFound()
    {
        _service.Setup(s => s.FindShortestPathAsync("CAI", "GKA", 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightPath?)null);

        var result = await _sut.GetShortestPath("CAI", "GKA");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAlternativePaths_ReturnsOkWithAllTiedPaths()
    {
        _service.Setup(s => s.FindAlternativePathsAsync("CAI", "DLZ", 4, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new FlightPath { Stops = ["CAI", "SVO", "ULN", "DLZ"], Hops = 3, TotalDistanceKm = 8054.6 },
                new FlightPath { Stops = ["CAI", "PEK", "ULN", "DLZ"], Hops = 3, TotalDistanceKm = 9204.7 }
            ]);

        var result = await _sut.GetAlternativePaths("CAI", "DLZ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paths = Assert.IsAssignableFrom<IReadOnlyList<FlightPathDto>>(ok.Value);
        Assert.Equal(2, paths.Count);
    }

    [Fact]
    public async Task GetShortestByDistance_ReturnsPathsRankedByDistance()
    {
        _service.Setup(s => s.FindShortestByDistanceAsync("HKT", "CXR", 5, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new FlightPath { Stops = ["HKT", "BKK", "SGN", "CXR"], Hops = 3, TotalDistanceKm = 1698.7 }]);

        var result = await _sut.GetShortestByDistance("HKT", "CXR");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paths = Assert.IsAssignableFrom<IReadOnlyList<FlightPathDto>>(ok.Value);
        Assert.Equal(1698.7, paths[0].TotalDistanceKm);
    }

    [Fact]
    public async Task GetHubAirports_ReturnsOkWithRankedHubs()
    {
        _service.Setup(s => s.GetHubAirportsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HubAirport { Code = "IST", Name = "Istanbul Airport", TotalConnections = 392 }]);

        var result = await _sut.GetHubAirports(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var hubs = Assert.IsAssignableFrom<IReadOnlyList<HubAirportDto>>(ok.Value);
        Assert.Equal("IST", hubs[0].Code);
    }
}
