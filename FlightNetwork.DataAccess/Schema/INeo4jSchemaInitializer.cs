namespace FlightNetwork.DataAccess.Schema;

public interface INeo4jSchemaInitializer
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
}
