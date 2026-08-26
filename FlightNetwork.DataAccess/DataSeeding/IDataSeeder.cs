namespace FlightNetwork.DataAccess.DataSeeding;

public interface IDataSeeder
{
    string Name { get; }

    Task<int> SeedAsync(CancellationToken ct = default);
}
