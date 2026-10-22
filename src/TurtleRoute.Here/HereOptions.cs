using System;

namespace TurtleRoute.Here
{
    public class HereOptions
    {
        /// <summary>
        /// The HERE platform API key.
        /// </summary>
        public string ApiKey { get; set; }

        public Uri RoutingBaseAddress { get; set; } = new("https://router.hereapi.com/v8/");

        public Uri MatrixBaseAddress { get; set; } = new("https://matrix.router.hereapi.com/v8/");

        public Uri WaypointsSequenceBaseAddress { get; set; } = new("https://wps.hereapi.com/v8/");
    }
}
