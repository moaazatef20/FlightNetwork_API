using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.DataAccess.Mapping;
using FlightNetwork.DataAccess.Sessions;
using FlightNetwork.Models.Entities;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Repositories;

internal sealed class AirlineRepository(INeo4jSessionFactory sessionFactory)
    : Neo4jRepositoryBase(sessionFactory), IAirlineRepository
{
    private const string Projection = "a.code AS code, a.name AS name";

    private const string GetPageQuery =
        $"MATCH (a:Airline) RETURN {Projection} ORDER BY code SKIP $skip LIMIT $limit";

    private const string GetCountQuery =
        "MATCH (a:Airline) RETURN count(a) AS total";

    private const string GetByCodeQuery =
        $"MATCH (a:Airline {{code: $code}}) RETURN {Projection}";

    private const string ExistsQuery =
        "MATCH (a:Airline {code: $code}) RETURN true AS found LIMIT 1";

    private const string CreateQuery =
        "CREATE (a:Airline { code: $code, name: $name })";

    private const string UpdateQuery =
        """
        MATCH (a:Airline {code: $code})
        SET a.name = $name
        RETURN count(a) AS affected
        """;

    private const string DeleteQuery =
        """
        MATCH (a:Airline {code: $code})
        DETACH DELETE a
        RETURN count(a) AS affected
        """;

    public async Task<IReadOnlyList<Airline>> GetPageAsync(int skip, int limit, CancellationToken ct = default)
    {
        ValidatePaging(skip, limit);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetPageQuery, new { skip, limit });
            return await cursor.ToListAsync(record => record.ToAirline(), limit, ct);
        });
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default) =>
        await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetCountQuery);
            var record = await cursor.SingleAsync(ct);
            return (int)record["total"].As<long>();
        });

    public async Task<Airline?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await ReadAsync(async runner =>
        {
            var cursor = await runner.RunAsync(GetByCodeQuery, new { code });
            return await cursor.FetchAsync() ? cursor.Current.ToAirline() : null;
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

    public async Task<bool> CreateAsync(Airline airline, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airline);
        ArgumentException.ThrowIfNullOrWhiteSpace(airline.Code);

        try
        {
            return await WriteAsync(async runner =>
            {
                var cursor = await runner.RunAsync(CreateQuery, ToParameters(airline));
                await cursor.ConsumeAsync();
                return true;
            });
        }
        catch (ClientException exception) when (IsUniquenessViolation(exception))
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Airline airline, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(airline);
        ArgumentException.ThrowIfNullOrWhiteSpace(airline.Code);

        return await WriteAsync(async runner =>
        {
            var cursor = await runner.RunAsync(UpdateQuery, ToParameters(airline));
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

    private static object ToParameters(Airline airline) => new
    {
        code = airline.Code,
        name = airline.Name
    };
}
