using FlightNetwork.Models.Entities;

namespace FlightNetwork.DataAccess.Contracts;

public interface IAirportRepository
{
    /// <summary>Reads a page of airports ordered by code. Callers must page: there is no unbounded read.</summary>
    Task<IReadOnlyList<Airport>> GetPageAsync(int skip, int limit, CancellationToken ct = default);

    Task<int> GetCountAsync(CancellationToken ct = default);

    Task<Airport?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<bool> ExistsAsync(string code, CancellationToken ct = default);

    /// <summary>Creates the airport node. Returns false when an airport with the same code already exists.</summary>
    Task<bool> CreateAsync(Airport airport, CancellationToken ct = default);

    /// <summary>Updates the airport node. Returns false when no airport matches the code.</summary>
    Task<bool> UpdateAsync(Airport airport, CancellationToken ct = default);

    /// <summary>Deletes the airport node and its relationships. Returns false when no airport matches the code.</summary>
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
