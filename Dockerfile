# syntax=docker/dockerfile:1

# ---- Build stage: full SDK, only exists in the build cache, never shipped ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from just the .csproj files, so editing source later doesn't
# invalidate Docker's restore-layer cache.
COPY FlightNetwork.Models/FlightNetwork.Models.csproj FlightNetwork.Models/
COPY FlightNetwork.DataAccess/FlightNetwork.DataAccess.csproj FlightNetwork.DataAccess/
COPY FlightNetwork.Services/FlightNetwork.Services.csproj FlightNetwork.Services/
COPY FlightNetwork.Api/FlightNetwork.Api.csproj FlightNetwork.Api/
RUN dotnet restore FlightNetwork.Api/FlightNetwork.Api.csproj

# Now bring in the rest of each project - FlightNetwork.DataAccess/files/*.json
# included, since RouteDataSeeder/AirportDataSeeder/AirlineDataSeeder read them
# from AppContext.BaseDirectory at startup. dotnet publish copies them into the
# output the same way a local `dotnet build` already does (the csproj's
# CopyToOutputDirectory item flows across the ProjectReference chain).
COPY FlightNetwork.Models/ FlightNetwork.Models/
COPY FlightNetwork.DataAccess/ FlightNetwork.DataAccess/
COPY FlightNetwork.Services/ FlightNetwork.Services/
COPY FlightNetwork.Api/ FlightNetwork.Api/

RUN dotnet publish FlightNetwork.Api/FlightNetwork.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage: ASP.NET runtime only, no SDK/compilers along for the ride ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render assigns the real port at container start via $PORT; 8080 is just the
# local-run fallback when that variable isn't set.
ENV PORT=8080
EXPOSE 8080

# Shell form so $PORT is read at container start, not baked in at build time.
# `exec` replaces the shell with the dotnet process so it receives Render's
# SIGTERM directly instead of the shell swallowing it on redeploy/scale-down.
ENTRYPOINT ["sh", "-c", "exec dotnet FlightNetwork.Api.dll --urls http://+:$PORT"]
