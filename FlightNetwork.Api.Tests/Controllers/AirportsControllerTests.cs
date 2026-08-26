using FlightNetwork.Api.Controllers;
using FlightNetwork.Api.DTOs;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlightNetwork.Api.Tests.Controllers;

public class AirportsControllerTests
{
    private readonly Mock<IAirportService> _service = new();
    private readonly AirportsController _sut;

    public AirportsControllerTests()
    {
        _sut = new AirportsController(_service.Object);
    }

    [Fact]
    public async Task GetPage_ReturnsOk_WithMappedPagedResponse()
    {
        var airport = new Airport { Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt" };
        _service.Setup(s => s.GetPageAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Airport>([airport], 1403, 1, 20));

        var result = await _sut.GetPage(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PagedResponse<AirportDto>>(ok.Value);
        Assert.Single(response.Items);
        Assert.Equal("CAI", response.Items[0].Code);
        Assert.Equal(1403, response.TotalCount);
    }

    [Fact]
    public async Task GetByCode_ExistingAirport_ReturnsOkWithDto()
    {
        _service.Setup(s => s.GetByCodeAsync("CAI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Airport { Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt" });

        var result = await _sut.GetByCode("CAI");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("CAI", Assert.IsType<AirportDto>(ok.Value).Code);
    }

    [Fact]
    public async Task GetByCode_UnknownAirport_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByCodeAsync("JFK", It.IsAny<CancellationToken>())).ReturnsAsync((Airport?)null);

        var result = await _sut.GetByCode("JFK");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_NewAirport_ReturnsCreatedAtAction()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new AirportRequest
        {
            Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt", Latitude = 30, Longitude = 31
        };

        var result = await _sut.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AirportsController.GetByCode), created.ActionName);
        Assert.Equal("CAI", Assert.IsType<AirportDto>(created.Value).Code);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new AirportRequest
        {
            Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt", Latitude = 30, Longitude = 31
        };

        var result = await _sut.Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingAirport_ReturnsNoContent()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new AirportRequest
        {
            Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt", Latitude = 30, Longitude = 31
        };

        var result = await _sut.Update("CAI", request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_UnknownAirport_ReturnsNotFound()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Airport>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new AirportRequest
        {
            Code = "JFK", Name = "JFK", City = "New York", Country = "USA", Latitude = 40, Longitude = -73
        };

        var result = await _sut.Update("JFK", request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingAirport_ReturnsNoContent()
    {
        _service.Setup(s => s.DeleteAsync("CAI", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.Delete("CAI");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_UnknownAirport_ReturnsNotFound()
    {
        _service.Setup(s => s.DeleteAsync("JFK", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.Delete("JFK");

        Assert.IsType<NotFoundResult>(result);
    }
}
