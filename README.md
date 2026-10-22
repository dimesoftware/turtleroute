<div align="center"><img src="assets/logo.png" alt="Logo"></div>

<div align="center">
<h1> Turtle Route </h1>
</div>

This is a project that supports the map functionalities in Dime.Scheduler. Most notably, the geocoding service is essential to the performance of the map as it eliminates the need for ad-hoc geocoding.

## Installation

Turtle Route is split into a core package with the contracts and the geocoding pipeline, and one package per vendor:

```cli
dotnet add package TurtleRoute            # IGeocoder, IRoutePlanner, IRouteMatrix, GeocodingPipeline
dotnet add package TurtleRoute.OpenCage   # OpenCage geocoding
dotnet add package TurtleRoute.Azure      # Azure Maps geocoding and routing
dotnet add package TurtleRoute.Here       # HERE routing
```

## Usage

Register the providers in the calling project. Geocoders are tried in the order they are registered: when a layer throws (outage, invalid key, quota exceeded), finds nothing, or returns a result below the minimum confidence, the next layer is tried. Routing providers work the same way (see [Routing](#routing)).

``` csharp
builder.Services.AddTurtleRoute()
    .AddOpenCageGeocoder(o => o.ApiKey = config["OpenCage:ApiKey"])   // primary
    .AddAzureMapsGeocoder(o => o.ApiKey = config["AzureMaps:ApiKey"]) // fallback
    .ConfigureGeocoding(o => o.MinimumConfidence = GeocodeConfidence.Medium)
    .AddAzureMapsRouting(o => o.ApiKey = config["AzureMaps:ApiKey"]);
```

Then inject `IGeocoder`, `IRoutePlanner` or `IRouteMatrix`:

``` csharp
GeocodeResult? result = await geocoder.GeocodeAsync(new Address("Katwilgweg", "2", "2050", "Antwerpen", "", "BE"));
// result.Coordinate, result.Confidence, result.Provider ("OpenCage" or "AzureMaps")
```

The pipeline returns `null` when no layer produced an acceptable result, and throws a `GeocodingException` only when every layer failed, so callers can tell "address not found" apart from "try again later".

Without DI, the providers and the pipeline can be created directly:

``` csharp
IGeocoder geocoder = new GeocodingPipeline([
    new OpenCageGeocoder(httpClient, new OpenCageOptions { ApiKey = "..." }),
    new AzureGeocoder("...")
]);
```

To add another vendor, implement `IGeocoder` and register it with `builder.AddGeocoder<MyGeocoder>()`.

### Shadow mode

To compare a new provider with the current one on real traffic, run it in shadow mode. Callers always get the configured pipeline's result; a sample of requests is repeated against the shadow geocoder in a background worker (so a host is required), and the shadow's results are never returned or stored.

``` csharp
builder.Services.AddTurtleRoute()
    .AddAzureMapsGeocoder(o => o.ApiKey = config["AzureMaps:ApiKey"])
    .AddShadowGeocoder(shadow => shadow.AddOpenCageGeocoder(o => o.ApiKey = config["OpenCage:ApiKey"]), o =>
    {
        o.SampleRate = 0.10;                          // share of requests repeated against the shadow
        o.SummaryInterval = TimeSpan.FromHours(1);    // one summary log line per interval
        o.DisagreementThresholdInMeters = 250;
        o.MaxDisagreementsPerInterval = 5;            // individually logged, with their address
        o.MaxRequestsPerSecond = 1;                   // stay within the shadow provider's plan
    });
```

The comparisons are aggregated in memory and logged as one line per interval, e.g.:

```
Geocoding shadow summary OpenCage vs AzureMaps over 60 min: 154 comparisons, accepted 87.0%, within 50 m 71.4%,
within 250 m 92.1%, within 1 km 97.4%, p50 25 m, p90 250 m, max 3200 m, not found 3.2%, errors 0.6%,
disagreements 3, dropped 0, 180 ms vs 140 ms, countries: BE 120 (89% accepted), DE 30 (80% accepted), other 4 (75% accepted)
```

A disagreement is a shadow result that would have been accepted but lies further than the threshold from the primary result; up to `MaxDisagreementsPerInterval` of them are logged as warnings with the address, the rest are only counted. Nothing is logged for an interval without traffic. To send the comparisons elsewhere, register your own `IGeocodingComparisonSink`.

### Routing

``` csharp
GeoCoordinate start = new(51.219501, 4.359231);
GeoCoordinate end = new(51.214625, 4.382112);

Route route = await routePlanner.GetRouteAsync(start, end);
```

Routing providers are layers too, tried in the order they are registered: when a route planner or matrix throws (outage, invalid key, quota exceeded, no route found), the next one is tried, and a `RoutingException` is thrown only when every layer failed. Invalid input is not retried. For example, HERE with Azure Maps as the fallback:

``` csharp
builder.Services.AddTurtleRoute()
    .AddHereRouting(o => o.ApiKey = config["Here:ApiKey"])
    .AddAzureMapsRouting(o => o.ApiKey = config["AzureMaps:ApiKey"]);
```

Routes use the HERE Routing API v8 and return the polyline of each leg in `Leg.Waypoints`. With `OptimizeWaypointOrder`, the order is first calculated by the HERE Waypoints Sequence API, which is a second billable transaction; it is skipped when there are fewer than two intermediate stops. The matrix uses the HERE Matrix Routing API v8 in a single region-mode request when all coordinates fit within 400 km, and in chunks of 15 origins otherwise.

## Contributing

![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)

Pull requests are welcome. Please check out the contribution and code of conduct guidelines.

## Local development

- You must have Visual Studio 2022 Community or higher.
- The dotnet cli is also highly recommended.

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](http://badges.mit-license.org)