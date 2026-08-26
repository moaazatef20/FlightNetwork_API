namespace FlightNetwork.DataAccess.DataSeeding;

public class SeedingOptions
{
    public const string SectionName = "Seeding";

    public bool Enabled { get; set; } = true;

    public string FilesDirectory { get; set; } = "files";

    public int BatchSize { get; set; } = 1000;
}
