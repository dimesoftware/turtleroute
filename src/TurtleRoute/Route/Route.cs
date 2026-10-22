using System.Collections.Generic;

namespace TurtleRoute
{
    public class Route
    {
        public int Duration { get; set; }

        public int Distance { get; set; }

        public List<Leg> Legs { get; set; } = [];

        public string Summary { get; set; }

        /// <summary>
        /// With <see cref="RoutePlanOptions.OptimizeWaypointOrder"/>: one revision per intermediate stop (the stops between the origin and
        /// the destination, indexed from 0), in the order they are visited, which is also the order of <see cref="Legs"/>. The n-th revision
        /// is the n-th stop visited: <see cref="RouteRevision.OriginalIndex"/> is n, and <see cref="RouteRevision.NewIndex"/> is the index of
        /// that stop as it was provided. This follows Azure Maps' optimized waypoints, and every provider returns it the same way.
        /// Empty without optimization.
        /// </summary>
        /// <example>
        /// The intermediate stops in the order they are visited:
        /// <code>
        /// List&lt;T&gt; visited = [.. route.Revisions.Select(r => stops[r.NewIndex])];
        /// </code>
        /// </example>
        public List<RouteRevision> Revisions { get; set; }
    }
}