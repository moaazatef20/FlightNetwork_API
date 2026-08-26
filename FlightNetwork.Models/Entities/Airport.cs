namespace FlightNetwork.Models.Entities;

public class Airport
{
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string City { get; set; } = default!;

    public string Country { get; set; } = default!;

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
