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
    public class Matrix
    {
        public Matrix(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            Token = token;
        }

        private string Token { get; }

        public async Task<IEnumerable<Route>> GetTrip(params GeoCoordinate[] coordinates)
        {
            Route[,] matrix = await GetRouteMatrix(coordinates);

            int size = matrix.GetLength(0); // Number of locations
            List<Route> trips = [];

            for (int i = 1; i < size; i++) // Start from 1 to form trips from previous nodes
            {
                int duration = 0;
                int distance = 0;

                for (int previousTrip = 0; previousTrip < i; previousTrip++)
                {
                    duration += matrix[previousTrip, previousTrip + 1].Duration;
                    distance += matrix[previousTrip, previousTrip + 1].Distance;
                }

                trips.Add(new Route()
                {
                    Duration = duration,
                    Distance = distance,
                });
            }

            return trips;
        }

        public async Task<Route[,]> GetRouteMatrix(params GeoCoordinate[] coordinates)
        {
            AzureKeyCredential credential = new(Token);
            MapsRoutingClient client = new(credential);

            RouteMatrixQuery query = new()
            {
                Origins = [.. coordinates.Select(x => new GeoPosition(x.Longitude, x.Latitude))],
                Destinations = [.. coordinates.Select(x => new GeoPosition(x.Longitude, x.Latitude))]
            };

            RouteMatrixOptions options = new(query)
            {
                UseTrafficData = true,
                RouteType = RouteType.Economy
            };
            options.Avoid.Add(RouteAvoidType.Ferries);
            options.Avoid.Add(RouteAvoidType.UnpavedRoads);

            Response<RouteMatrixResult> result = await client.GetImmediateRouteMatrixAsync(options);

            int originCount = query.Origins.Count;
            int destinationCount = query.Destinations.Count;
            Route[,] routeMatrix = new Route[originCount, destinationCount];

            for (int i = 0; i < originCount; i++)
            {
                for (int j = 0; j < destinationCount; j++)
                {
                    var route = result.Value.Matrix[i][j];
                    routeMatrix[i, j] = new Route()
                    {
                        Distance = route.Summary.LengthInMeters ?? 0,
                        Duration = route.Summary.TravelTimeInSeconds ?? 0
                    };
                }
            }

            return routeMatrix;
        }
    }
}