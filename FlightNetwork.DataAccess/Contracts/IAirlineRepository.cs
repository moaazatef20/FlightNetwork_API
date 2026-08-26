using FlightNetwork.Models.Entities;

namespace FlightNetwork.DataAccess.Contracts;

public interface IAirlineRepository
{
    /// <summary>Reads a page of airlines ordered by code. Callers must page: there is no unbounded read.</summary>
    Task<IReadOnlyList<Airline>> GetPageAsync(int skip, int limit, CancellationToken ct = default);

    Task<int> GetCountAsync(CancellationToken ct = default);

    Task<Airline?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<bool> ExistsAsync(string code, CancellationToken ct = default);

    /// <summary>Creates the airline node. Returns false when an airline with the same code already exists.</summary>
    Task<bool> CreateAsync(Airline airline, CancellationToken ct = default);

    /// <summary>Updates the airline node. Returns false when no airline matches the code.</summary>
    Task<bool> UpdateAsync(Airline airline, CancellationToken ct = default);

    /// <summary>Deletes the airline node and its relationships. Returns false when no airline matches the code.</summary>
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
