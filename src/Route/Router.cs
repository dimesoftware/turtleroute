using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Routing;
using Azure.Maps.Routing.Models;

namespace TurtleRoute
{
    public class Router
    {
        public Router(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            Token = token;
        }

        private string Token { get; }

        public async Task<Route> GetRouteAsync(RouteDirectionOptions options, params GeoCoordinate[] coordinates)
        {
            AzureKeyCredential credential = new(Token);
            MapsRoutingClient client = new(credential);

            RouteDirectionOptions opts = options ?? new RouteDirectionOptions()
            {
                RouteType = RouteType.Fastest,
                UseTrafficData = true,
            };

            List<GeoPosition> stops = [.. coordinates.ToList().Select(x => new GeoPosition(x.Longitude, x.Latitude))];
            RouteDirectionQuery query = new(stops, opts);

            Response<RouteDirections> result = await client.GetDirectionsAsync(query);

            Route journey = new();
            int totalDistance = 0;
            int totalDuration = 0;

            foreach (RouteData route in result.Value.Routes)
            {
                totalDistance += route.Summary.LengthInMeters ?? 0;
                totalDuration += route.Summary.TravelTimeInSeconds ?? 0;

                journey.Legs.AddRange(route.Legs.Select(leg => new Leg()
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

        public Task<Route> GetRouteAsync(params GeoCoordinate[] coordinates)
            => GetRouteAsync(null, coordinates);
    }
}