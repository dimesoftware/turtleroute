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

        /// <summary>
        /// How long to wait for a response before giving up, so the pipeline can fall back to the next layer
        /// instead of waiting for the default HttpClient timeout of 100 seconds. Defaults to 30 seconds, as large
        /// matrices and waypoint sequences take longer than a single route.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
