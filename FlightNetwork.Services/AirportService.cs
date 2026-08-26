using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;
using FlightNetwork.Services.Contracts;

namespace FlightNetwork.Services;

internal sealed class AirportService(IAirportRepository repository) : IAirportService
{
    public async Task<PagedResult<Airport>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (skip, limit) = Paging.ToSkipLimit(page, pageSize);

        // Both reads hit the graph independently; there is no single Cypher call that returns a
        // page and a total in one round trip without also materializing the whole match set.
        var items = await repository.GetPageAsync(skip, limit, ct);
        var totalCount = await repository.GetCountAsync(ct);

        return new PagedResult<Airport>(items, totalCount, page, pageSize);
    }

    public Task<Airport?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        repository.GetByCodeAsync(Code.Normalize(code), ct);

    public Task<bool> CreateAsync(Airport airport, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airport);

        airport.Code = Code.Normalize(airport.Code);
        return repository.CreateAsync(airport, ct);
    }

    public Task<bool> UpdateAsync(Airport airport, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airport);

        airport.Code = Code.Normalize(airport.Code);
        return repository.UpdateAsync(airport, ct);
    }

    public Task<bool> DeleteAsync(string code, CancellationToken ct = default) =>
        repository.DeleteAsync(Code.Normalize(code), ct);
}
