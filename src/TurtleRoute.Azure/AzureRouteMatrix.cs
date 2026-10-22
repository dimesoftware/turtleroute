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
    public class AzureRouteMatrix : IRouteMatrix
    {
        private readonly MapsRoutingClient _client;

        public AzureRouteMatrix(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            _client = new MapsRoutingClient(new AzureKeyCredential(token));
        }

        public async Task<Route[,]> GetRouteMatrixAsync(IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(coordinates);

            List<GeoPosition> positions = [.. coordinates.Select(x => new GeoPosition(x.Longitude, x.Latitude))];
            RouteMatrixQuery query = new()
            {
                Origins = positions,
                Destinations = [.. positions]
            };

            RouteMatrixOptions options = new(query)
            {
                UseTrafficData = true,
                RouteType = AzureRouteType.Economy
            };
            options.Avoid.Add(RouteAvoidType.Ferries);
            options.Avoid.Add(RouteAvoidType.UnpavedRoads);

            Response<RouteMatrixResult> result = await _client.GetImmediateRouteMatrixAsync(options, cancellationToken);

            int originCount = query.Origins.Count;
            int destinationCount = query.Destinations.Count;
            Route[,] routeMatrix = new Route[originCount, destinationCount];

            for (int i = 0; i < originCount; i++)
            {
                for (int j = 0; j < destinationCount; j++)
                {
                    RouteMatrix route = result.Value.Matrix[i][j];
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
