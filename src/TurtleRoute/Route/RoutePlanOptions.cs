using System;

namespace TurtleRoute
{
    public class RoutePlanOptions
    {
        public RouteType RouteType { get; set; } = RouteType.Fastest;

        public TravelMode TravelMode { get; set; } = TravelMode.Car;

        public bool UseTrafficData { get; set; } = true;

        /// <summary>
        /// When to leave the origin, so traffic can be predicted for that time. Null, or a time that is not in the future, departs now.
        /// Ignored when <see cref="UseTrafficData"/> is false.
        /// </summary>
        public DateTimeOffset? DepartAt { get; set; }

        /// <summary>
        /// Reorder the intermediate waypoints for the best route. The new order is returned in <see cref="Route.Revisions"/>.
        /// </summary>
        public bool OptimizeWaypointOrder { get; set; }

        /// <summary>
        /// <see cref="DepartAt"/> when it is a future departure with traffic, otherwise null for "now".
        /// </summary>
        public DateTimeOffset? GetFutureDeparture()
            => UseTrafficData && DepartAt > DateTimeOffset.UtcNow ? DepartAt : null;
    }
}
