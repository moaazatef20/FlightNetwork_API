using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services;
using Moq;

namespace FlightNetwork.Services.Tests;

public class RouteServiceTests
{
    private readonly Mock<IRouteRepository> _repository = new();
    private readonly RouteService _sut;

    public RouteServiceTests()
    {
        _sut = new RouteService(_repository.Object);
    }

    [Fact]
    public async Task FindShortestPathAsync_NormalizesOriginAndDestination_AndPassesMaxHopsThrough()
    {
        await _sut.FindShortestPathAsync("cai", "dlz", maxHops: 3);

        _repository.Verify(r => r.FindShortestPathAsync("CAI", "DLZ", 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAlternativePathsAsync_NormalizesCodes_AndPassesMaxHopsAndLimitThrough()
    {
        await _sut.FindAlternativePathsAsync("cai", "dlz", maxHops: 2, limit: 5);

        _repository.Verify(
            r => r.FindAlternativePathsAsync("CAI", "DLZ", 2, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindShortestByDistanceAsync_NormalizesCodes_AndPassesResultsCountAndMaxHopsThrough()
    {
        await _sut.FindShortestByDistanceAsync("hkt", "cxr", resultsCount: 3, maxHops: 3);

        _repository.Verify(
            r => r.FindShortestByDistanceAsync("HKT", "CXR", 3, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHubAirportsAsync_PassesLimitThrough()
    {
        var hubs = new List<HubAirport> { new() { Code = "IST", Name = "Istanbul Airport", TotalConnections = 392 } };
        _repository.Setup(r => r.GetHubAirportsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(hubs);

        var result = await _sut.GetHubAirportsAsync(limit: 3);

        Assert.Same(hubs, result);
    }
}
