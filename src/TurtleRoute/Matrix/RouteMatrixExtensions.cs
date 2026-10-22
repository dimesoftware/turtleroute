using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute
{
    public static class RouteMatrixExtensions
    {
        public static Task<Route[,]> GetRouteMatrixAsync(this IRouteMatrix matrix, params GeoCoordinate[] coordinates)
            => matrix.GetRouteMatrixAsync(coordinates, default);

        public static Task<IEnumerable<Route>> GetTripAsync(this IRouteMatrix matrix, params GeoCoordinate[] coordinates)
            => matrix.GetTripAsync(coordinates, default);

        /// <summary>
        /// The cumulative duration and distance to each stop when visiting the coordinates in the given order.
        /// </summary>
        public static async Task<IEnumerable<Route>> GetTripAsync(this IRouteMatrix matrix, IEnumerable<GeoCoordinate> coordinates, CancellationToken cancellationToken)
        {
            Route[,] routes = await matrix.GetRouteMatrixAsync(coordinates, cancellationToken);

            int size = routes.GetLength(0); // Number of locations
            List<Route> trips = [];

            for (int i = 1; i < size; i++) // Start from 1 to form trips from previous nodes
            {
                int duration = 0;
                int distance = 0;

                for (int previousTrip = 0; previousTrip < i; previousTrip++)
                {
                    duration += routes[previousTrip, previousTrip + 1].Duration;
                    distance += routes[previousTrip, previousTrip + 1].Distance;
                }

                trips.Add(new Route()
                {
                    Duration = duration,
                    Distance = distance,
                });
            }

            return trips;
        }
    }
}
