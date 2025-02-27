using System.Collections.Generic;

namespace TurtleRoute
{
    public class Trip
    {
        public int Duration { get; set; }

        public int Distance { get; set; }

        public List<Route> Routes { get; set; } = [];
    }
}