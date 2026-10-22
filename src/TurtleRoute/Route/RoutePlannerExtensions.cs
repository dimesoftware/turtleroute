using System.Threading.Tasks;

namespace TurtleRoute
{
    public static class RoutePlannerExtensions
    {
        public static Task<Route> GetRouteAsync(this IRoutePlanner planner, params GeoCoordinate[] coordinates)
            => planner.GetRouteAsync(coordinates, null);

        public static Task<Route> GetRouteAsync(this IRoutePlanner planner, RoutePlanOptions options, params GeoCoordinate[] coordinates)
            => planner.GetRouteAsync(coordinates, options);
    }
}
