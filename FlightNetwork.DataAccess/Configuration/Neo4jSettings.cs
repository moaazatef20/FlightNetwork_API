namespace FlightNetwork.DataAccess.Configuration;

public class Neo4jSettings
{
    public const string SectionName = "Neo4j";

    public string Uri { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string Database { get; set; } = "neo4j";
}
