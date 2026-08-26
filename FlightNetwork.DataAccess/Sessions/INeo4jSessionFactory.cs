using Neo4j.Driver;

namespace FlightNetwork.DataAccess.Sessions;
public interface INeo4jSessionFactory
{
    IAsyncSession CreateReadSession();

    IAsyncSession CreateWriteSession();
}
