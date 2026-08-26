using FlightNetwork.Api.DTOs;
using FlightNetwork.Api.Mapping;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;

namespace FlightNetwork.Api.Tests.Mapping;

public class DtoMappingExtensionsTests
{
    [Fact]
    public void AirportToDto_CopiesAllFields()
    {
        var airport = new Airport
        {
            Code = "CAI", Name = "Cairo International Airport", City = "Cairo", Country = "Egypt",
            Latitude = 30.12, Longitude = 31.4
        };

        var dto = airport.ToDto();

        Assert.Equal(airport.Code, dto.Code);
        Assert.Equal(airport.Name, dto.Name);
        Assert.Equal(airport.City, dto.City);
        Assert.Equal(airport.Country, dto.Country);
        Assert.Equal(airport.Latitude, dto.Latitude);
        Assert.Equal(airport.Longitude, dto.Longitude);
    }

    [Fact]
    public void AirportRequestToEntity_CopiesAllFields()
    {
        var request = new AirportRequest
        {
            Code = "CAI", Name = "Cairo International Airport", City = "Cairo", Country = "Egypt",
            Latitude = 30.12, Longitude = 31.4
        };

        var airport = request.ToEntity();

        Assert.Equal(request.Code, airport.Code);
        Assert.Equal(request.Name, airport.Name);
        Assert.Equal(request.City, airport.City);
        Assert.Equal(request.Country, airport.Country);
        Assert.Equal(request.Latitude, airport.Latitude);
        Assert.Equal(request.Longitude, airport.Longitude);
    }

    [Fact]
    public void AirlineToDto_CopiesAllFields()
    {
        var airline = new Airline { Code = "MS", Name = "EgyptAir" };

        var dto = airline.ToDto();

        Assert.Equal("MS", dto.Code);
        Assert.Equal("EgyptAir", dto.Name);
    }

    [Fact]
    public void HubAirportToDto_CopiesAllFields()
    {
        var hub = new HubAirport { Code = "IST", Name = "Istanbul Airport", TotalConnections = 392 };

        var dto = hub.ToDto();

        Assert.Equal("IST", dto.Code);
        Assert.Equal("Istanbul Airport", dto.Name);
        Assert.Equal(392, dto.TotalConnections);
    }

    [Fact]
    public void FlightPathToDto_ZipsLegsInOrder_AndCopiesTotals()
    {
        var path = new FlightPath
        {
            Stops = ["CAI", "SVO", "DLZ"],
            Hops = 2,
            TotalDistanceKm = 100,
            Legs =
            [
                new FlightLeg { OriginCode = "CAI", DestinationCode = "SVO", AirlineCodes = ["SU"], DistanceKm = 60 },
                new FlightLeg { OriginCode = "SVO", DestinationCode = "DLZ", AirlineCodes = ["OM"], DistanceKm = 40 }
            ]
        };

        var dto = path.ToDto();

        Assert.Equal(path.Stops, dto.Stops);
        Assert.Equal(2, dto.Hops);
        Assert.Equal(100, dto.TotalDistanceKm);
        Assert.Equal(2, dto.Legs.Count);
        Assert.Equal("CAI", dto.Legs[0].OriginCode);
        Assert.Equal("DLZ", dto.Legs[1].DestinationCode);
    }

    [Fact]
    public void PagedResultToResponse_MapsItems_AndCarriesPagingMetadata()
    {
        var result = new PagedResult<Airline>([new Airline { Code = "MS", Name = "EgyptAir" }], 334, 2, 10);

        var response = result.ToResponse(a => a.ToDto());

        Assert.Single(response.Items);
        Assert.Equal("MS", response.Items[0].Code);
        Assert.Equal(334, response.TotalCount);
        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.PageSize);
    }
}
