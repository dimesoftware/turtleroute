using System;

namespace TurtleRoute.OpenCage
{
    public class OpenCageOptions
    {
        public string ApiKey { get; set; }

        public Uri BaseAddress { get; set; } = new("https://api.opencagedata.com/geocode/v1/");

        /// <summary>
        /// IETF language tag for the returned address components, e.g. "nl" or "fr". Defaults to "native" (e.g. "Antwerpen" rather than "Antwerp"), which the city check relies on.
        /// </summary>
        public string Language { get; set; } = "native";

        /// <summary>
        /// How long to wait for a response before giving up, so the pipeline can fall back to the next layer instead of waiting for the default HttpClient timeout of 100 seconds. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
