using FlightNetwork.DataAccess.Sessions;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Repositories;

internal abstract class Neo4jRepositoryBase(INeo4jSessionFactory sessionFactory)
{
    private const string UniquenessViolationCode = "Neo.ClientError.Schema.ConstraintValidationFailed";

    protected async Task<T> ReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> work)
    {
        await using var session = sessionFactory.CreateReadSession();
        return await session.ExecuteReadAsync(work);
    }

    protected async Task<T> WriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> work)
    {
        await using var session = sessionFactory.CreateWriteSession();
        return await session.ExecuteWriteAsync(work);
    }
    protected static bool IsUniquenessViolation(ClientException exception) =>
        exception.Code == UniquenessViolationCode;

    protected static void ValidatePaging(int skip, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
    }
}
