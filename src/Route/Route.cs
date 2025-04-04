using System.Collections.Generic;

namespace TurtleRoute
{
    public class Route
    {
        public int Duration { get; set; }

        public int Distance { get; set; }

        public List<Leg> Legs { get; set; } = [];

        public string Summary { get; set; }

        public List<RouteRevision> Revisions { get; set; }
    }
}