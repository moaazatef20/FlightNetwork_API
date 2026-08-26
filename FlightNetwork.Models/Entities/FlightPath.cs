namespace FlightNetwork.Models.Entities;

public class FlightPath
{
    /// <summary>Airport codes in travel order, origin first and destination last.</summary>
    public IReadOnlyList<string> Stops { get; set; } = [];

    /// <summary>One entry per hop. Leg i connects Stops[i] to Stops[i + 1].</summary>
    public IReadOnlyList<FlightLeg> Legs { get; set; } = [];

    public int Hops { get; set; }

    /// <summary>
    /// Great-circle distance flown across every leg, in kilometres. Geographic distance, not a
    /// commercial price — the dataset carries no fares.
    /// </summary>
    public double TotalDistanceKm { get; set; }
}
