using System.Collections.Generic;

namespace TurtleRoute
{
    public class Route
    {
        public int Duration { get; set; }

        public int Distance { get; set; }

        public IEnumerable<GeoCoordinate> Waypoints { get; set; } = [];
    }
}