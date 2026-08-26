namespace FlightNetwork.Api.DTOs;

public sealed record AirportDto(
    string Code,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude);
