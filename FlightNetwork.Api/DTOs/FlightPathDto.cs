namespace FlightNetwork.Api.DTOs;

public sealed record FlightPathDto(
    IReadOnlyList<string> Stops,
    IReadOnlyList<FlightLegDto> Legs,
    int Hops,
    double TotalDistanceKm);
