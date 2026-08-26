using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;

namespace FlightNetwork.Services.Contracts;

public interface IAirportService
{
    Task<PagedResult<Airport>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);

    Task<Airport?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns false when an airport with the same code already exists.</summary>
    Task<bool> CreateAsync(Airport airport, CancellationToken ct = default);

    /// <summary>Returns false when no airport matches the code.</summary>
    Task<bool> UpdateAsync(Airport airport, CancellationToken ct = default);

    /// <summary>Returns false when no airport matches the code.</summary>
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
