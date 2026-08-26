using FlightNetwork.Api.DTOs;
using FlightNetwork.Api.Mapping;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FlightNetwork.Api.Controllers;

[ApiController]
[Route("api/airports")]
public sealed class AirportsController(IAirportService airportService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AirportDto>>> GetPage(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await airportService.GetPageAsync(page, pageSize, ct);
        return Ok(result.ToResponse(a => a.ToDto()));
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<AirportDto>> GetByCode(string code, CancellationToken ct = default)
    {
        var airport = await airportService.GetByCodeAsync(code, ct);
        return airport is null ? NotFound() : Ok(airport.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<AirportDto>> Create(AirportRequest request, CancellationToken ct = default)
    {
        var airport = request.ToEntity();
        var created = await airportService.CreateAsync(airport, ct);

        if (!created)
        {
            return Conflict($"An airport with code '{request.Code}' already exists.");
        }

        return CreatedAtAction(nameof(GetByCode), new { code = airport.Code }, airport.ToDto());
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code, AirportRequest request, CancellationToken ct = default)
    {
        var airport = request.ToEntity();
        airport.Code = code;

        var updated = await airportService.UpdateAsync(airport, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct = default)
    {
        var deleted = await airportService.DeleteAsync(code, ct);
        return deleted ? NoContent() : NotFound();
    }
}
