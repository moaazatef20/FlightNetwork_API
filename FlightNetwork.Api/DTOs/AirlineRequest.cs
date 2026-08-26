using System.ComponentModel.DataAnnotations;

namespace FlightNetwork.Api.DTOs;

public sealed class AirlineRequest
{
    [Required, StringLength(10, MinimumLength = 2)]
    public string Code { get; init; } = default!;

    [Required, StringLength(200)]
    public string Name { get; init; } = default!;
}
