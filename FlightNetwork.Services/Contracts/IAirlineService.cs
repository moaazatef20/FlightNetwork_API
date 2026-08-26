using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;

namespace FlightNetwork.Services.Contracts;

public interface IAirlineService
{
    Task<PagedResult<Airline>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);

    Task<Airline?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns false when an airline with the same code already exists.</summary>
    Task<bool> CreateAsync(Airline airline, CancellationToken ct = default);

    /// <summary>Returns false when no airline matches the code.</summary>
    Task<bool> UpdateAsync(Airline airline, CancellationToken ct = default);

    /// <summary>Returns false when no airline matches the code.</summary>
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
