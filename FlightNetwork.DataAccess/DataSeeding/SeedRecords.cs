namespace FlightNetwork.DataAccess.DataSeeding;

internal sealed record AirportSeedRecord(
    string Code,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude);

internal sealed record AirlineSeedRecord(
    string Code,
    string Name);

internal sealed record RouteSeedRecord(
    string OriginCode,
    string DestinationCode,
    string AirlineCode);
