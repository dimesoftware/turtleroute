using System.Collections.Generic;

namespace TurtleRoute
{
    public class Leg
    {
        public int Duration { get; set; }

        public int Distance { get; set; }

        public IEnumerable<GeoCoordinate> Waypoints { get; set; } = [];
    }
}