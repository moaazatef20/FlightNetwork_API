using FlightNetwork.DataAccess.Configuration;
using FlightNetwork.DataAccess.Contracts;
using FlightNetwork.DataAccess.DataSeeding;
using FlightNetwork.DataAccess.Repositories;
using FlightNetwork.DataAccess.Schema;
using FlightNetwork.DataAccess.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace FlightNetwork.DataAccess;

public static class DataAccessServiceRegistration
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Neo4jSettings>(configuration.GetSection(Neo4jSettings.SectionName));

        // The driver owns the connection pool and is thread-safe: exactly one per application.
        services.AddSingleton<IDriver>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            return GraphDatabase.Driver(settings.Uri, AuthTokens.Basic(settings.Username, settings.Password));
        });

        services.AddSingleton<INeo4jSessionFactory, Neo4jSessionFactory>();
        services.AddSingleton<INeo4jSchemaInitializer, Neo4jSchemaInitializer>();

        services.Configure<SeedingOptions>(configuration.GetSection(SeedingOptions.SectionName));
        services.AddSingleton<SeedFileReader>();

        // Registered in dependency order: the route seeder links airports that the first
        // seeder must already have created. Neo4jSeedHostedService runs them in this order.
        services.AddSingleton<IDataSeeder, AirportDataSeeder>();
        services.AddSingleton<IDataSeeder, AirlineDataSeeder>();
        services.AddSingleton<IDataSeeder, RouteDataSeeder>();

        // Hosted services start in registration order: schema first, then seeding, because the
        // seeders' MERGEs rely on the uniqueness constraints being in place.
        services.AddHostedService<Neo4jSchemaHostedService>();
        services.AddHostedService<Neo4jSeedHostedService>();

        services.AddScoped<IAirportRepository, AirportRepository>();
        services.AddScoped<IAirlineRepository, AirlineRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();

        return services;
    }
}
