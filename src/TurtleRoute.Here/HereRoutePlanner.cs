using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.Here
{
    /// <summary>
    /// Plans routes with the HERE Routing API v8. Waypoint optimization is a separate, billable call to the HERE Waypoints Sequence API v8.
    /// </summary>
    public class HereRoutePlanner : IRoutePlanner
    {
        private const string IntermediatePrefix = "w";

        private readonly HttpClient _httpClient;
        private readonly HereOptions _options;

        public HereRoutePlanner(HttpClient httpClient, HereOptions options)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new ArgumentNullException(nameof(options), "The HERE API key is required.");

            _httpClient = httpClient;
            _options = options;
        }

        public async Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            List<GeoCoordinate> stops = [.. coordinates];
            if (stops.Count < 2)
                throw new ArgumentException("A route needs at least an origin and a destination.", nameof(coordinates));

            RoutePlanOptions plan = options ?? new RoutePlanOptions();
            string transportMode = ToTransportMode(plan.TravelMode); // Throws before any billable call for an unsupported mode.
            List<RouteRevision> revisions = [];

            // With fewer than two intermediate stops there is nothing to reorder, so skip the extra transaction.
            if (plan.OptimizeWaypointOrder && stops.Count > 3)
            {
                List<int> order = await GetWaypointOrderAsync(stops, plan, cancellationToken);
                revisions = [.. order.Select((provided, optimized) => new RouteRevision(provided, optimized))];
                stops = [stops[0], .. order.Select(i => stops[i + 1]), stops[^1]];
            }

            List<KeyValuePair<string, string>> parameters =
            [
                new("transportMode", transportMode),
                new("origin", HereHttp.Format(stops[0])),
                new("destination", HereHttp.Format(stops[^1])),
                .. stops.Skip(1).Take(stops.Count - 2).Select(stop => new KeyValuePair<string, string>("via", HereHttp.Format(stop))),
                new("routingMode", ToRoutingMode(plan.RouteType)),
                new("return", "polyline,summary"),
                new("apiKey", _options.ApiKey)
            ];

            // Without a departure time HERE uses the current time and live traffic; "any" ignores traffic.
            if (!plan.UseTrafficData)
                parameters.Add(new("departureTime", "any"));
            else if (plan.GetFutureDeparture() is DateTimeOffset departure)
                parameters.Add(new("departureTime", FormatTime(departure)));

            using HttpResponseMessage response = await _httpClient.GetAsync(new Uri(_options.RoutingBaseAddress, $"routes?{HereHttp.Query(parameters)}"), cancellationToken);
            HereRoutesResponse body = await HereHttp.ReadAsync<HereRoutesResponse>(response, cancellationToken);

            HereRoute route = body?.Routes?.FirstOrDefault();
            if (route == null)
            {
                string reason = body?.Notices?.FirstOrDefault()?.Title ?? "no route was returned";
                throw new InvalidOperationException($"HERE could not calculate the route: {reason}");
            }

            Route journey = new() { Revisions = revisions };
            journey.Legs.AddRange(route.Sections.Select(section => new Leg()
            {
                Distance = section.Summary?.Length ?? 0,
                Duration = section.Summary?.Duration ?? 0,
                Waypoints = FlexiblePolyline.Decode(section.Polyline)
            }));

            journey.Distance = journey.Legs.Sum(leg => leg.Distance);
            journey.Duration = journey.Legs.Sum(leg => leg.Duration);

            return journey;
        }

        /// <summary>
        /// Returns the original indexes of the intermediate stops in the optimized order. The origin and destination stay in place.
        /// </summary>
        private async Task<List<int>> GetWaypointOrderAsync(List<GeoCoordinate> stops, RoutePlanOptions plan, CancellationToken cancellationToken)
        {
            List<KeyValuePair<string, string>> parameters =
            [
                new("start", $"start;{HereHttp.Format(stops[0])}"),
                .. stops.Skip(1).Take(stops.Count - 2).Select((stop, i) => new KeyValuePair<string, string>($"destination{i + 1}", $"{IntermediatePrefix}{i};{HereHttp.Format(stop)}")),
                new("end", $"end;{HereHttp.Format(stops[^1])}"),
                new("mode", $"fastest;{ToSequenceMode(plan.TravelMode)};traffic:{(plan.UseTrafficData ? "enabled" : "disabled")}"),
                new("improveFor", plan.RouteType == RouteType.Shortest ? "distance" : "time"),
                new("apiKey", _options.ApiKey)
            ];

            if (plan.UseTrafficData)
                parameters.Add(new("departure", FormatTime(plan.GetFutureDeparture() ?? DateTimeOffset.UtcNow)));

            using HttpResponseMessage response = await _httpClient.GetAsync(new Uri(_options.WaypointsSequenceBaseAddress, $"findsequence2?{HereHttp.Query(parameters)}"), cancellationToken);
            HereSequenceResponse body = await HereHttp.ReadAsync<HereSequenceResponse>(response, cancellationToken);

            List<HereSequenceWaypoint> waypoints = body?.Results?.FirstOrDefault()?.Waypoints;
            if (waypoints == null || waypoints.Count == 0)
                throw new InvalidOperationException("HERE could not optimize the waypoint order.");

            return [.. waypoints
                .Where(w => w.Id != null && w.Id.StartsWith(IntermediatePrefix, StringComparison.Ordinal))
                .OrderBy(w => w.Sequence)
                .Select(w => int.Parse(w.Id[IntermediatePrefix.Length..]))];
        }

        /// <summary>
        /// HERE has no van or motorcycle mode. Routing them as a car would ignore their restrictions, so they are refused and a
        /// <see cref="RoutePlannerPipeline"/> falls back to the next layer, e.g. Azure Maps, which supports both.
        /// </summary>
        internal static string ToTransportMode(TravelMode travelMode) => travelMode switch
        {
            TravelMode.Car => "car",
            TravelMode.Truck => "truck",
            TravelMode.Bicycle => "bicycle",
            TravelMode.Pedestrian => "pedestrian",
            _ => throw new NotSupportedException($"HERE cannot plan routes for travel mode {travelMode}.")
        };

        // The sequence only decides the order of the stops; the route itself is then planned with the actual transport mode.
        private static string ToSequenceMode(TravelMode travelMode) => travelMode switch
        {
            TravelMode.Truck => "truck",
            TravelMode.Pedestrian => "pedestrian",
            _ => "car"
        };

        private static string FormatTime(DateTimeOffset time) => time.ToString("yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

        // HERE has no economy mode; fast is the closest.
        internal static string ToRoutingMode(RouteType routeType) => routeType == RouteType.Shortest ? "short" : "fast";
    }
}
