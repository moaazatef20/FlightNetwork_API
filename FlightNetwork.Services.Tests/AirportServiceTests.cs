using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services;
using Moq;

namespace FlightNetwork.Services.Tests;

public class AirportServiceTests
{
    private readonly Mock<IAirportRepository> _repository = new();
    private readonly AirportService _sut;

    public AirportServiceTests()
    {
        _sut = new AirportService(_repository.Object);
    }

    [Fact]
    public async Task GetPageAsync_ConvertsPageToSkipLimit_AndReturnsRepositoryTotalCount()
    {
        var airports = new List<Airport> { new() { Code = "CAI", Name = "Cairo", City = "Cairo", Country = "Egypt" } };
        _repository.Setup(r => r.GetPageAsync(10, 10, It.IsAny<CancellationToken>())).ReturnsAsync(airports);
        _repository.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1403);

        var result = await _sut.GetPageAsync(page: 2, pageSize: 10);

        Assert.Same(airports, result.Items);
        Assert.Equal(1403, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPageAsync_OutOfRangePageOrPageSize_ThrowsArgumentOutOfRangeException(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetPageAsync(page, pageSize));
    }

    [Fact]
    public async Task GetByCodeAsync_LowercaseCode_IsNormalizedToUppercaseBeforeQuerying()
    {
        await _sut.GetByCodeAsync("cai");

        _repository.Verify(r => r.GetByCodeAsync("CAI", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCodeAsync_CodeWithWhitespace_IsTrimmed()
    {
        await _sut.GetByCodeAsync("  cai  ");

        _repository.Verify(r => r.GetByCodeAsync("CAI", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCodeOnTheEntityBeforeCallingRepository()
    {
        var airport = new Airport { Code = "cai", Name = "Cairo", City = "Cairo", Country = "Egypt" };

        await _sut.CreateAsync(airport);

        Assert.Equal("CAI", airport.Code);
        _repository.Verify(r => r.CreateAsync(
            It.Is<Airport>(a => a.Code == "CAI"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullAirport_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NormalizesCodeBeforeCallingRepository()
    {
        var airport = new Airport { Code = "cai", Name = "Cairo", City = "Cairo", Country = "Egypt" };

        await _sut.UpdateAsync(airport);

        _repository.Verify(r => r.UpdateAsync(
            It.Is<Airport>(a => a.Code == "CAI"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NormalizesCodeBeforeCallingRepository()
    {
        await _sut.DeleteAsync("cai");

        _repository.Verify(r => r.DeleteAsync("CAI", It.IsAny<CancellationToken>()), Times.Once);
    }
}
