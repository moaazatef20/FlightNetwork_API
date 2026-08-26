# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Solution uses the `.slnx` format (not `.sln`); `dotnet` CLI works with it directly.

```bash
dotnet build FlightNetwork.slnx      # build the whole solution
dotnet run --project FlightNetwork.Api   # run the API
```

There are no test projects yet. When tests are added, prefer `dotnet test` scoped to the
specific test project, and `dotnet test --filter "FullyQualifiedName~<Name>"` to run a single test.

## Configuration

`appsettings.json` holds the Neo4j `Uri`, `Username`, and `Database`. The **password is not in
the repo** — it lives in User Secrets on the API project:

```bash
dotnet user-secrets list --project FlightNetwork.Api
dotnet user-secrets set "Neo4j:Password" "<password>" --project FlightNetwork.Api
```

User Secrets are only loaded when `ASPNETCORE_ENVIRONMENT=Development`. Running with
`--no-launch-profile` (or in any other environment) leaves the password empty and the driver
fails with `Neo4j.Driver.AuthenticationException` — which looks like bad credentials but is a
missing configuration source. Supply the password another way (env var `Neo4j__Password`) for
non-Development runs.

The graph schema (uniqueness constraints on `Airport.code` and `Airline.code`) is applied at
startup by a hosted service in the DataAccess layer, so nothing has to be run by hand.

## Graph model

```
(:Airport {code, name, city, country, latitude, longitude})
(:Airline {code, name})
(:Airport)-[:ROUTE {airlineCodes: [String]}]->(:Airport)
```

Routes are **relationships, not nodes** — there is no `Route` entity in Models. There is exactly
**one `:ROUTE` per ordered airport pair**, no parallel edges: every airline flying that pair is
listed in the `airlineCodes` array. `Airline` nodes are reference data and are not linked into
the route edges.

Read the operating airlines with `r.airlineCodes`, and filter with
`'MS' IN r.airlineCodes` — there is no singular `airlineCode` property.

## Seeding

`FlightNetwork.DataAccess/files/` holds `airports.json`, `airlines.json`, and `routes.json`.
They are copied to the output directory by the csproj and applied at startup by
`Neo4jSeedHostedService`, which runs the `IDataSeeder` implementations **in registration order**
(airports → airlines → routes; the route seeder `MATCH`es airports that must already exist).

**Data scope: Middle East + Europe + Asia only.** North/South America, Africa outside Egypt, and
Oceania are not in the data — a lookup for `JFK` or `GKA` returns nothing by design. Every
airport in the file has at least one route; there are no isolated nodes.

| | file rows | graph |
|---|---|---|
| `:Airport` | 1,403 | 1,403 |
| `:Airline` | 342 | **334** (8 duplicate codes collapse) |
| `:ROUTE` | 38,240 | **21,291** (grouped by airport pair) |

Each seeder skips itself when its label/relationship count is already non-zero, so restarts are
cheap. Writes go out as `UNWIND` batches (`Seeding:BatchSize`, default 1000) — one transaction
per batch, never one query per row. Set `Seeding:Enabled` to `false` to skip seeding entirely.

`airlines.json` contains duplicate codes, which is why the seeders use `MERGE` rather than
`CREATE`. `RouteDataSeeder` groups the file's one-row-per-airline shape by `(origin, destination)`
before writing, which is why 38,240 rows become 21,291 relationships.

### Re-seeding after a data or model change

The skip-if-present check means changed files are **not** picked up on a normal restart — the
graph has to be cleared first. Batched so no single transaction gets huge:

```cypher
MATCH ()-[r:ROUTE]->() WITH r LIMIT 10000 DELETE r RETURN count(r) AS deleted        -- repeat until 0
MATCH (a:Airport)      WITH a LIMIT 10000 DETACH DELETE a RETURN count(a) AS deleted -- repeat until 0
MATCH (a:Airline)      WITH a LIMIT 10000 DETACH DELETE a RETURN count(a) AS deleted -- repeat until 0
```

Then start the API and the seeders repopulate from the files.

### Pathfinding

`shortestPath` works on the current graph. Verified against the live CognoDB instance:

```cypher
MATCH p = shortestPath((:Airport {code:'CAI'})-[:ROUTE*1..4]->(:Airport {code:'DLZ'}))
RETURN length(p) AS hops, [n IN nodes(p) | n.code] AS path   -- 3 hops, e.g. CAI→PEK→ULN→DLZ
```

This used to close the connection (`ServiceUnavailableException`) on the previous global dataset
with parallel per-airline edges — 65,636 relationships. Merging the edges and narrowing the data
brought it to 21,291 and the expander now completes.

`RouteRepository` encapsulates all of this. Three rules were paid for with downed instances —
do not undo them when adding path queries:

1. **Directed only.** The undirected form (`-[:ROUTE*1..4]-`, no arrow) fails with
   `Neo.TransientError.General.MemoryPoolOutOfMemoryError: BFS budget exceeded (5000 ms)`.
   Direction is also correct semantically: a route A→B does not imply B→A exists.
2. **`shortestPath` / `allShortestPaths` only — never a bare variable-length `MATCH`.**
   `MATCH p = (a)-[:ROUTE*1..3]->(b) RETURN p LIMIT 10` enumerates every path rather than the
   shortest ones; it took the instance down for ~80 seconds even with the `LIMIT`. This is why
   `FindAlternativePathsAsync` uses `allShortestPaths` (every route tying for fewest hops,
   ~1s) instead of "all routes up to N hops".
3. **Bound every pattern, and cap the bound.** `RouteRepository.MaxSupportedHops` is 4, which
   reaches every airport in the current data; callers cannot ask for more.

Cypher does **not** accept a parameter inside a variable-length bound — `[:ROUTE*1..$maxHops]`
is a syntax error (`expected ], got PARAM`). The bound is interpolated into the query text after
being validated to a small integer range; everything else stays a real query parameter.

### Path result types

`FindShortestPathAsync` / `FindAlternativePathsAsync` return `FlightPath` from
`FlightNetwork.Models.Entities` — not a raw `IPath`, and not a DataAccess-local type. Services
return entities and the API maps entities to DTOs, so anything a service hands upward has to
live in Models.

```
FlightPath { Stops: [String], Legs: [FlightLeg], Hops: int }
FlightLeg  { OriginCode, DestinationCode, AirlineCodes: [String] }
HubAirport { Code, Name, TotalConnections }
```

Cypher returns the nodes and relationships as parallel lists; because the pattern is directed,
relationship *i* always connects stop *i* to stop *i+1*, which is what lets
`RecordMappingExtensions.ToFlightPath` zip the legs back together.

### Distance, not price

price/duration fields don't exist in the OpenFlights routes dataset; `distanceKm` (Haversine,
computed at seed time from real airport coordinates) is used as a real, verifiable proxy instead
of fabricated pricing data. Verified across all 38,240 route rows and 1,403 airport rows: the
only route fields are `origin_code`, `destination_code`, `airline_code`.

Never name any of this "cheapest" — it is geographic distance, not a fare.

`GeoDistance.HaversineKm` runs in the seeder rather than in Cypher because this server has **no
geospatial functions** (`point.distance` is an unknown function here). Every `:ROUTE` carries
`distanceKm`; verified 0 relationships missing it and 0 with a non-positive value. Spot checks:
`CAI→PEK` = 7,532.9 km, longest edge `BCN→SIN` = 10,899 km, shortest edge `PPW→WRY` = 2.8 km
(the real Papa Westray–Westray hop, the world's shortest scheduled flight).

### Fewest hops and shortest distance are different questions

They disagree constantly. Measured over 162,653 reachable pairs, **31.3%** have a shorter-distance
route that uses more hops than the minimum, missing **727 km** on average. `HKT→CXR`
(Phuket→Nha Trang, ~1,700 km apart) routes via Moscow in 2 hops for 15,165 km, while a 3-hop
route covers it in 1,699 km.

So `FindShortestByDistanceAsync` cannot be built on `shortestPath`/`allShortestPaths` — those
minimise hops. It uses **layered relaxation** instead: expand one hop, keep the best path per
airport, repeat. Each layer is bounded by the number of airports, which is what makes it safe
where a variable-length `MATCH` is not.

Constraints found by measuring against this instance — all three matter:

| | |
|---|---|
| Paths kept per airport per layer | **1**. At 3, the row count multiplies layer over layer and the query dies with `context deadline exceeded`. One is still exact: hop-limited shortest path is exact when the best path per (airport, hop count) is kept. Alternatives come from the final layer, which fans in from every airport with a route into the destination. |
| Max hops | **3**, versus 4 for the hop-based methods. A fourth layer exceeds the server's query deadline. |
| Cost | **9–14 seconds**, versus ~1s for the hop-based methods. An endpoint built on this needs caching. |

One query runs per hop count, because a single query cannot both force an exact hop count and
report the shorter paths it passed on the way; results are merged and re-ranked in the repository.

Verified against an independent offline Dijkstra over the same JSON files — `HKT→CXR` 1,698.7 km,
`DWC→ZYL` 3,804 km, `SGN→PNQ` 3,639 km, `CAI→DLZ` 8,054.6 km, all exact.

Dialect trap this relies on: map literals and `collect(x)[0]` work, but **list append must be
written `stops + [n.code]`** — this dialect evaluates `['A','B'] + 'C'` as the string `[A B]C`
rather than appending to the list.

## Architecture

This is an **N-tier** solution, not Onion/Clean Architecture. Do not introduce
Domain/Application/Infrastructure-style layering, a generic `BaseEntity<TKey>`, or a
Result/Repository/UnitOfWork abstraction modeled on Entity Framework — none of that applies here.

Four projects, each depending only on the layer directly below it (`ProjectReference` chain):

```
FlightNetwork.Api  →  FlightNetwork.Services  →  FlightNetwork.DataAccess  →  FlightNetwork.Models
```

- **FlightNetwork.Models** — Entities as plain POCOs only: properties, no logic, no base classes,
  no Entity Framework attributes (`[Key]`, `[Table]`, etc.). There is no EF Core in this solution.
- **FlightNetwork.DataAccess** — talks to Neo4j directly via `Neo4j.Driver`, using its native
  session/transaction pattern (`IAsyncSession`, `ExecuteReadAsync`/`ExecuteWriteAsync` with Cypher
  queries). No EF-style `UnitOfWork`/generic repository abstraction — repositories here work
  directly against sessions and Cypher.
- **FlightNetwork.Services** — business logic only. No DTOs live here.
- **FlightNetwork.Api** — Controllers, and this is the *only* layer that owns DTOs (`Api/DTOs`).
  DTOs must not be defined in Services or DataAccess.

### Database

The database is **Neo4j** (accessed here via **CognoDB**), a graph database — not SQL Server, and
not accessed through Entity Framework. Data is modeled as nodes/relationships and queried with
Cypher, not LINQ-to-SQL.

Target framework across all four projects: **net10.0**, nullable reference types enabled.

## Code Quality Standards

All code in this project — existing or new — must hold to:

- **Clean Code** principles.
- **SOLID** principles (Single Responsibility, Open/Closed, etc.).
- **Performance considered at design time, not bolted on afterward.** Concretely:
  - Avoid N+1 query patterns against Neo4j.
  - Use appropriate indexing for Cypher queries.
  - Paginate large result sets.
  - Don't fetch more data than the use case actually needs.
- Any new Cypher query or API endpoint should be reviewed for performance before it's considered done.
