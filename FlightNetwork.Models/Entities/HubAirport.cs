namespace FlightNetwork.Models.Entities;

public class HubAirport
{
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    /// <summary>Routes touching this airport, inbound and outbound combined.</summary>
    public int TotalConnections { get; set; }
}
