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

        public async Task<IEnumerable<Trip>> GetTrip(params GeoCoordinate[] coordinates)
        {
            Route[,] matrix = await GetRouteMatrix(coordinates);

            List<Trip> trips = [];
            for (int i = 0; i < matrix.Length; i++)
            {
                if (i == 0)
                    continue;

                int duration = 0;
                int distance = 0;

                int previousTrip = 1;
                do
                {
                    duration += matrix[previousTrip - 1, previousTrip].Duration;
                    distance += matrix[previousTrip - 1, previousTrip].Distance;

                    previousTrip++;
                }
                while (previousTrip <= i);

                trips.Add(new Trip()
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

            RouteMatrixQuery query = new();
            query.Origins = coordinates.ToList().Select(x => new GeoPosition(x.Longitude, x.Latitude)).ToList(); ;
            query.Destinations = coordinates.ToList().Select(x => new GeoPosition(x.Longitude, x.Latitude)).ToList();

            RouteMatrixOptions options = new(query)
            {
                UseTrafficData = true,
                RouteType = RouteType.Economy
            };
            options.Avoid.Add(RouteAvoidType.Ferries);
            options.Avoid.Add(RouteAvoidType.UnpavedRoads);

            Trip trip = new();
            Response<RouteMatrixResult> result = await client.GetImmediateRouteMatrixAsync(options);

            List<Route> routes = [];
            foreach (IList<RouteMatrix> routeResult in result.Value.Matrix)
            {
                foreach (RouteMatrix route in routeResult)
                {
                    routes.Add(new Route()
                    {
                        Distance = route.Summary.LengthInMeters ?? 0,
                        Duration = route.Summary.TravelTimeInSeconds ?? 0
                    });
                }
            }

            return ToMatrix(routes.ToArray(), routes.Count());
        }

        private static T[,] ToMatrix<T>(T[] array, int n)
        {
            if (n <= 0)
                throw new ArgumentException("Array N dimension cannot be less or equals zero", nameof(n));
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Array cannot be null");
            if (array.Length == 0)
                throw new ArgumentException("Array cannot be empty", nameof(array));

            int m = array.Length % n == 0 ? array.Length / n : array.Length / n + 1;
            var newArr = new T[m, n];
            for (int i = 0; i < array.Length; i++)
            {
                int k = i / n;
                int l = i % n;
                newArr[k, l] = array[i];
            }

            return newArr;
        }
    }
}