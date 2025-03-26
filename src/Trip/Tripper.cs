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
    public class Tripper
    {
        public Tripper(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            Token = token;
        }

        private string Token { get; }

        public Task<Trip> GetTrip(params GeoCoordinate[] coordinates)
            => GetTrip(null, coordinates);

        public async Task<Trip> GetTrip(RouteDirectionOptions options, params GeoCoordinate[] coordinates)
        {
            AzureKeyCredential credential = new(Token);
            MapsRoutingClient client = new(credential);

            RouteDirectionOptions opts = options ?? new RouteDirectionOptions()
            {
                RouteType = RouteType.Fastest,
                UseTrafficData = true,
            };

            List<GeoPosition> stops = coordinates.ToList().Select(x => new GeoPosition(x.Longitude, x.Latitude)).ToList();
            List<RouteDirectionQuery> queries = [];
            for (int i = 0; i < coordinates.Length - 1; i++)
                queries.Add(new RouteDirectionQuery([new(coordinates[i].Longitude, coordinates[i].Latitude), new(coordinates[i + 1].Longitude, coordinates[i + 1].Latitude)], opts));

            Trip trip = new();
            Response<RouteDirectionsBatchResult> result = await client.GetDirectionsImmediateBatchAsync(queries);

            int totalDistance = 0;
            int totalDuration = 0;

            foreach (RouteDirectionsBatchItemResponse batchResponse in result.Value.Results)
            {
                if (!string.IsNullOrEmpty(batchResponse.ResponseError?.Code))
                {
                    trip.Summary = batchResponse.ResponseError.Message;
                    return trip;
                }

                foreach (RouteData route in batchResponse.Routes)
                {
                    totalDistance += route.Summary.LengthInMeters ?? 0;
                    totalDuration += route.Summary.TravelTimeInSeconds ?? 0;

                    trip.Routes.Add(new Route()
                    {
                        Distance = route.Summary.LengthInMeters ?? 0,
                        Duration = route.Summary.TravelTimeInSeconds ?? 0,
                        Waypoints = route.Legs.SelectMany(x => x.Points).Select(x => new GeoCoordinate(x.Latitude, x.Longitude))
                    });
                }
            }

            trip.Distance = totalDistance;
            trip.Duration = totalDuration;
            return trip;
        }
    }
}