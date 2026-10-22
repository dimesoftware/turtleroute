namespace TurtleRoute
{
    public class RoutePlanOptions
    {
        public RouteType RouteType { get; set; } = RouteType.Fastest;

        public bool UseTrafficData { get; set; } = true;

        /// <summary>
        /// Reorder the intermediate waypoints for the best route. The new order is returned in <see cref="Route.Revisions"/>.
        /// </summary>
        public bool OptimizeWaypointOrder { get; set; }
    }
}
