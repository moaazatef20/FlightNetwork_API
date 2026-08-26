using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.DataAccess.Mapping;
using FlightNetwork.DataAccess.Sessions;
using FlightNetwork.Models.Entities;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Repositories;

internal sealed class AirportRepository(INeo4jSessionFactory sessionFactory)
    : Neo4jRepositoryBase(sessionFactory), IAirportRepository
{
    private const string Projection =
        "a.code AS code, a.name AS name, a.city AS city, a.country AS country, " +
        "a.latitude AS latitude, a.longitude AS longitude";

    private const string GetPageQuery =
        $"MATCH (a:Airport) RETURN {Projection} ORDER BY code SKIP $skip LIMIT $limit";

    private const string GetCountQuery =
        "MATCH (a:Airport) RETURN count(a) AS total";

    private const string GetByCodeQuery =
        $"MATCH (a:Airport {{code: $code}}) RETURN {Projection}";

    private const string ExistsQuery =
        "MATCH (a:Airport {code: $code}) RETURN true AS found LIMIT 1";

    private const string CreateQuery =
        """
        CREATE (a:Airport {
            code: $code,
            name: $name,
            city: $city,
            country: $country,
            latitude: $latitude,
            longitude: $longitude
        })
        """;

    private const string UpdateQuery =
        """
        MATCH (a:Airport {code: $code})
        SET a.name = $name,
            a.city = $city,
            a.country = $country,
            a.latitude = $latitude,
            a.longitude = $longitude
        RETURN count(a) AS affected
        """;

    private const string DeleteQuery =
        """
        MATCH (a:Airport {code: $code})
        DETACH DELETE a
        RETURN count(a) AS affected
        """;

    public async Task<IReadOnlyList<Airport>> GetPageAsync(int skip, int limit, CancellationToken ct = default)
    {
        ValidatePaging(skip, limit);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetPageQuery, new { skip, limit });
            return await cursor.ToListAsync(record => record.ToAirport(), limit, ct);
        });
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default) =>
        await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetCountQuery);
            var record = await cursor.SingleAsync(ct);
            return (int)record["total"].As<long>();
        });

    public async Task<Airport?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetByCodeQuery, new { code });
            return await cursor.FetchAsync() ? cursor.Current.ToAirport() : null;
        });
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(ExistsQuery, new { code });
            return await cursor.FetchAsync();
        });
    }

    public async Task<bool> CreateAsync(Airport airport, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airport);
        ArgumentException.ThrowIfNullOrWhiteSpace(airport.Code);

        try
        {
            return await WriteAsync(async runner =>
            {
                var cursor = await runner.RunAsync(CreateQuery, ToParameters(airport));
                await cursor.ConsumeAsync();
                return true;
            });
        }
        catch (ClientException exception) when (IsUniquenessViolation(exception))
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Airport airport, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airport);
        ArgumentException.ThrowIfNullOrWhiteSpace(airport.Code);

        return await WriteAsync(async runner =>
        {
            var cursor = await runner.RunAsync(UpdateQuery, ToParameters(airport));
            var record = await cursor.SingleAsync(ct);
            return record["affected"].As<long>() > 0;
        });
    }

    public async Task<bool> DeleteAsync(string code, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await WriteAsync(async runner =>
        {
            var cursor = await runner.RunAsync(DeleteQuery, new { code });
            var record = await cursor.SingleAsync(ct);
            return record["affected"].As<long>() > 0;
        });
    }

    private static object ToParameters(Airport airport) => new
    {
        code = airport.Code,
        name = airport.Name,
        city = airport.City,
        country = airport.Country,
        latitude = airport.Latitude,
        longitude = airport.Longitude
    };
}
