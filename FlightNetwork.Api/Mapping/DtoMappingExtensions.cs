using FlightNetwork.Api.DTOs;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;

namespace FlightNetwork.Api.Mapping;

internal static class DtoMappingExtensions
{
    public static AirportDto ToDto(this Airport airport) =>
        new(airport.Code, airport.Name, airport.City, airport.Country, airport.Latitude, airport.Longitude);

    public static Airport ToEntity(this AirportRequest request) =>
        new()
        {
            Code = request.Code,
            Name = request.Name,
            City = request.City,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

    public static AirlineDto ToDto(this Airline airline) => new(airline.Code, airline.Name);

    public static Airline ToEntity(this AirlineRequest request) => new() { Code = request.Code, Name = request.Name };

    public static HubAirportDto ToDto(this HubAirport hub) => new(hub.Code, hub.Name, hub.TotalConnections);

    public static FlightLegDto ToDto(this FlightLeg leg) =>
        new(leg.OriginCode, leg.DestinationCode, leg.AirlineCodes, leg.DistanceKm);

    public static FlightPathDto ToDto(this FlightPath path) =>
        new(path.Stops, [.. path.Legs.Select(ToDto)], path.Hops, path.TotalDistanceKm);

    public static PagedResponse<TDto> ToResponse<TEntity, TDto>(
        this PagedResult<TEntity> result, Func<TEntity, TDto> map) =>
        new([.. result.Items.Select(map)], result.TotalCount, result.Page, result.PageSize);
}
