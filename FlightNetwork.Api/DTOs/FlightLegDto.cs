namespace FlightNetwork.Api.DTOs;

public sealed record FlightLegDto(
    string OriginCode,
    string DestinationCode,
    IReadOnlyList<string> AirlineCodes,
    double DistanceKm);
