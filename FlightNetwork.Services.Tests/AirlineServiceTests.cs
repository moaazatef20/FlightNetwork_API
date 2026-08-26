using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services;
using Moq;

namespace FlightNetwork.Services.Tests;

public class AirlineServiceTests
{
    private readonly Mock<IAirlineRepository> _repository = new();
    private readonly AirlineService _sut;

    public AirlineServiceTests()
    {
        _sut = new AirlineService(_repository.Object);
    }

    [Fact]
    public async Task GetPageAsync_ConvertsPageToSkipLimit_AndReturnsRepositoryTotalCount()
    {
        var airlines = new List<Airline> { new() { Code = "MS", Name = "EgyptAir" } };
        _repository.Setup(r => r.GetPageAsync(20, 10, It.IsAny<CancellationToken>())).ReturnsAsync(airlines);
        _repository.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(334);

        var result = await _sut.GetPageAsync(page: 3, pageSize: 10);

        Assert.Same(airlines, result.Items);
        Assert.Equal(334, result.TotalCount);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPageAsync_OutOfRangePageOrPageSize_ThrowsArgumentOutOfRangeException(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetPageAsync(page, pageSize));
    }

    [Fact]
    public async Task GetByCodeAsync_LowercaseCode_IsNormalizedToUppercaseBeforeQuerying()
    {
        await _sut.GetByCodeAsync("ms");

        _repository.Verify(r => r.GetByCodeAsync("MS", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCodeOnTheEntityBeforeCallingRepository()
    {
        var airline = new Airline { Code = "ms", Name = "EgyptAir" };

        await _sut.CreateAsync(airline);

        Assert.Equal("MS", airline.Code);
        _repository.Verify(r => r.CreateAsync(
            It.Is<Airline>(a => a.Code == "MS"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullAirline_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NormalizesCodeBeforeCallingRepository()
    {
        var airline = new Airline { Code = "ms", Name = "EgyptAir" };

        await _sut.UpdateAsync(airline);

        _repository.Verify(r => r.UpdateAsync(
            It.Is<Airline>(a => a.Code == "MS"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NormalizesCodeBeforeCallingRepository()
    {
        await _sut.DeleteAsync("ms");

        _repository.Verify(r => r.DeleteAsync("MS", It.IsAny<CancellationToken>()), Times.Once);
    }
}
