using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Routing;
using Azure.Maps.Routing.Models;
using AzureRouteType = Azure.Maps.Routing.RouteType;

namespace TurtleRoute.Azure
{
    public class AzureRoutePlanner : IRoutePlanner
    {
        private readonly MapsRoutingClient _client;

        public AzureRoutePlanner(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            _client = new MapsRoutingClient(new AzureKeyCredential(token));
        }

        public async Task<Route> GetRouteAsync(IEnumerable<GeoCoordinate> coordinates, RoutePlanOptions options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            RoutePlanOptions route = options ?? new RoutePlanOptions();
            RouteDirectionOptions opts = new()
            {
                RouteType = ToAzure(route.RouteType),
                UseTrafficData = route.UseTrafficData,
                ComputeBestWaypointOrder = route.OptimizeWaypointOrder,
            };

            List<GeoPosition> stops = [.. coordinates.Select(x => new GeoPosition(x.Longitude, x.Latitude))];
            RouteDirectionQuery query = new(stops, opts);

            Response<RouteDirections> result = await _client.GetDirectionsAsync(query, cancellationToken);

            Route journey = new();
            int totalDistance = 0;
            int totalDuration = 0;

            foreach (RouteData data in result.Value.Routes)
            {
                totalDistance += data.Summary.LengthInMeters ?? 0;
                totalDuration += data.Summary.TravelTimeInSeconds ?? 0;

                journey.Legs.AddRange(data.Legs.Select(leg => new Leg()
                {
                    Distance = leg.Summary.LengthInMeters ?? 0,
                    Duration = leg.Summary.TravelTimeInSeconds ?? 0,
                    Waypoints = leg.Points.Select(x => new GeoCoordinate(x.Latitude, x.Longitude))
                }));
            }

            journey.Distance = totalDistance;
            journey.Duration = totalDuration;
            journey.Revisions = [.. result.Value.OptimizedWaypoints.Select(x => new RouteRevision(x.ProvidedIndex.GetValueOrDefault(), x.OptimizedIndex.GetValueOrDefault()))];

            return journey;
        }

        internal static AzureRouteType ToAzure(RouteType routeType) => routeType switch
        {
            RouteType.Shortest => AzureRouteType.Shortest,
            RouteType.Economy => AzureRouteType.Economy,
            _ => AzureRouteType.Fastest
        };
    }
}
