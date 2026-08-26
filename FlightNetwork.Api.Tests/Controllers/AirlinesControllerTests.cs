using FlightNetwork.Api.Controllers;
using FlightNetwork.Api.DTOs;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlightNetwork.Api.Tests.Controllers;

public class AirlinesControllerTests
{
    private readonly Mock<IAirlineService> _service = new();
    private readonly AirlinesController _sut;

    public AirlinesControllerTests()
    {
        _sut = new AirlinesController(_service.Object);
    }

    [Fact]
    public async Task GetPage_ReturnsOk_WithMappedPagedResponse()
    {
        var airline = new Airline { Code = "MS", Name = "EgyptAir" };
        _service.Setup(s => s.GetPageAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Airline>([airline], 334, 1, 20));

        var result = await _sut.GetPage(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PagedResponse<AirlineDto>>(ok.Value);
        Assert.Single(response.Items);
        Assert.Equal("MS", response.Items[0].Code);
    }

    [Fact]
    public async Task GetByCode_UnknownAirline_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByCodeAsync("ZZ", It.IsAny<CancellationToken>())).ReturnsAsync((Airline?)null);

        var result = await _sut.GetByCode("ZZ");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<Airline>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.Create(new AirlineRequest { Code = "MS", Name = "EgyptAir" });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_NewAirline_ReturnsCreatedAtAction()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<Airline>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.Create(new AirlineRequest { Code = "MS", Name = "EgyptAir" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AirlinesController.GetByCode), created.ActionName);
    }

    [Fact]
    public async Task Update_UnknownAirline_ReturnsNotFound()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Airline>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.Update("ZZ", new AirlineRequest { Code = "ZZ", Name = "Unknown" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingAirline_ReturnsNoContent()
    {
        _service.Setup(s => s.DeleteAsync("MS", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.Delete("MS");

        Assert.IsType<NoContentResult>(result);
    }
}
