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

            List<GeoPosition> stops = coordinates.ToList().Select(x => new GeoPosition(x.Longitude, x.Latitude)).ToList();
            RouteDirectionQuery query = new(stops, opts);

            Response<RouteDirections> result = await client.GetDirectionsAsync(query);

            if (!(result.Value?.Routes ?? []).Any())
                return new Route();

            return new Route()
            {
                Distance = result.Value.Routes[0].Summary.LengthInMeters ?? 0,
                Duration = result.Value.Routes[0].Summary.TravelTimeInSeconds ?? 0,
                Waypoints = result.Value.Routes[0].Legs.SelectMany(x => x.Points).Select(x => new GeoCoordinate(x.Latitude, x.Longitude))
            };
        }

        public Task<Route> GetRouteAsync(params GeoCoordinate[] coordinates)
            => GetRouteAsync(null, coordinates);
    }
}