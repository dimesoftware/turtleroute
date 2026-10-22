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
        /// With <see cref="RoutePlanOptions.OptimizeWaypointOrder"/>: one revision per intermediate stop (the stops between the origin and the destination, indexed from 0), in their original order.
        /// <see cref="RouteRevision.NewIndex"/> is the stop's position in the optimized route, which is also the order of <see cref="Legs"/>. 
        /// Empty without optimization.
        /// </summary>
        /// <example>
        /// The intermediate stops in the order they are visited:
        /// <code>
        /// List&lt;T&gt; visited = [.. route.Revisions.OrderBy(r => r.NewIndex).Select(r => stops[r.OriginalIndex])];
        /// </code>
        /// </example>
        public List<RouteRevision> Revisions { get; set; }
    }
}