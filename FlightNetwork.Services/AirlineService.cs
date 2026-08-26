using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.Models.Entities;
using FlightNetwork.Services.Common;
using FlightNetwork.Services.Contracts;

namespace FlightNetwork.Services;

internal sealed class AirlineService(IAirlineRepository repository) : IAirlineService
{
    public async Task<PagedResult<Airline>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (skip, limit) = Paging.ToSkipLimit(page, pageSize);

        var items = await repository.GetPageAsync(skip, limit, ct);
        var totalCount = await repository.GetCountAsync(ct);

        return new PagedResult<Airline>(items, totalCount, page, pageSize);
    }

    public Task<Airline?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        repository.GetByCodeAsync(Code.Normalize(code), ct);

    public Task<bool> CreateAsync(Airline airline, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airline);

        airline.Code = Code.Normalize(airline.Code);
        return repository.CreateAsync(airline, ct);
    }

    public Task<bool> UpdateAsync(Airline airline, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airline);

        airline.Code = Code.Normalize(airline.Code);
        return repository.UpdateAsync(airline, ct);
    }

    public Task<bool> DeleteAsync(string code, CancellationToken ct = default) =>
        repository.DeleteAsync(Code.Normalize(code), ct);
}
