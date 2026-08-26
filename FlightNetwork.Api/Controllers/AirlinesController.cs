using FlightNetwork.Api.DTOs;
using FlightNetwork.Api.Mapping;
using FlightNetwork.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FlightNetwork.Api.Controllers;

[ApiController]
[Route("api/airlines")]
public sealed class AirlinesController(IAirlineService airlineService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AirlineDto>>> GetPage(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await airlineService.GetPageAsync(page, pageSize, ct);
        return Ok(result.ToResponse(a => a.ToDto()));
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<AirlineDto>> GetByCode(string code, CancellationToken ct = default)
    {
        var airline = await airlineService.GetByCodeAsync(code, ct);
        return airline is null ? NotFound() : Ok(airline.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<AirlineDto>> Create(AirlineRequest request, CancellationToken ct = default)
    {
        var airline = request.ToEntity();
        var created = await airlineService.CreateAsync(airline, ct);

        if (!created)
        {
            return Conflict($"An airline with code '{request.Code}' already exists.");
        }

        return CreatedAtAction(nameof(GetByCode), new { code = airline.Code }, airline.ToDto());
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code, AirlineRequest request, CancellationToken ct = default)
    {
        var airline = request.ToEntity();
        airline.Code = code;

        var updated = await airlineService.UpdateAsync(airline, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct = default)
    {
        var deleted = await airlineService.DeleteAsync(code, ct);
        return deleted ? NoContent() : NotFound();
    }
}
