using System.ComponentModel.DataAnnotations;

namespace FlightNetwork.Api.DTOs;

public sealed class AirportRequest
{
    [Required, StringLength(10, MinimumLength = 2)]
    public string Code { get; init; } = default!;

    [Required, StringLength(200)]
    public string Name { get; init; } = default!;

    [Required, StringLength(200)]
    public string City { get; init; } = default!;

    [Required, StringLength(100)]
    public string Country { get; init; } = default!;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }
}
