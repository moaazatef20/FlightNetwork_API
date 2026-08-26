namespace FlightNetwork.Models.Entities;

public class FlightLeg
{
    public string OriginCode { get; set; } = default!;

    public string DestinationCode { get; set; } = default!;

    /// <summary>Every airline operating this hop.</summary>
    public IReadOnlyList<string> AirlineCodes { get; set; } = [];

    /// <summary>Great-circle distance between the two airports, in kilometres.</summary>
    public double DistanceKm { get; set; }
}
