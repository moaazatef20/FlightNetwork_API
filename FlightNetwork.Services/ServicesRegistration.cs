using FlightNetwork.DataAccess;
using FlightNetwork.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlightNetwork.Services;

public static class ServicesRegistration
{
    /// <summary>
    /// Composition root for the service layer. The API calls only this, so it never has to
    /// reach past the layer directly below it to wire up data access.
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccess(configuration);

        services.AddScoped<IAirportService, AirportService>();
        services.AddScoped<IAirlineService, AirlineService>();
        services.AddScoped<IRouteService, RouteService>();

        return services;
    }
}
